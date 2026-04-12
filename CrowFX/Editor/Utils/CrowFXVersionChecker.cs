#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

namespace CrowFX.EditorTools
{
    [InitializeOnLoad]
    internal static class CrowFXVersionChecker
    {
        internal enum VersionState
        {
            Unknown,
            Checking,
            UpToDate,
            Outdated,
            Ahead,
            Stale,
            Error
        }

        internal struct Snapshot
        {
            public string LocalVersion;
            public string LatestVersion;
            public VersionState State;
            public string ErrorMessage;
            public bool IsDismissed;
            public bool LatestVersionIsCached;
        }

        [Serializable]
        private sealed class PackageJsonData
        {
            public string version;
        }

        [Serializable]
        private sealed class ReleaseData
        {
            public string tag_name;
            public string html_url;
        }

        private const string LatestReleaseApiUrl = "https://api.github.com/repos/Luci0n/CrowFX-Unity-Image-Effects/releases/latest";
        private const string MainPackageJsonUrl = "https://raw.githubusercontent.com/Luci0n/CrowFX-Unity-Image-Effects/main/CrowFX/package.json";
        internal const string ReleasesUrl = "https://github.com/Luci0n/CrowFX-Unity-Image-Effects/releases";

        private const string PrefPrefix = "CrowFX.VersionChecker.";
        private const string PrefCacheSchema = PrefPrefix + "CacheSchema";
        private const string PrefLatestVersion = PrefPrefix + "LatestVersion";
        private const string PrefLatestReleaseUrl = PrefPrefix + "LatestReleaseUrl";
        private const string PrefLastCheckedTicks = PrefPrefix + "LastCheckedTicks";
        private const string PrefDismissedVersion = PrefPrefix + "DismissedVersion";
        private const int CurrentCacheSchemaVersion = 2;
        private const double CacheHours = 12d;

        private static string _localVersion;
        private static string _latestVersion;
        private static string _errorMessage;
        private static string _packageJsonAssetPath;
        private static string _latestReleaseUrl;
        private static long _localVersionFileTicks;
        private static DateTime _lastCheckedUtc;
        private static bool _initialized;
        private static bool _checking;
        private static bool _latestVersionIsCached;
        private static bool _lastRefreshFailed;
        private static string _fallbackErrorContext;
        private static UnityWebRequest _request;
        private static VersionState _state;
        private static RemoteCheckSource _remoteCheckSource;

        private enum RemoteCheckSource
        {
            ReleaseApi,
            PackageJsonFallback
        }

        static CrowFXVersionChecker()
        {
            LoadCache();
            EditorApplication.delayCall += () => EnsureInitialized();
        }

        internal static Snapshot Current
        {
            get
            {
                EnsureInitialized();
                return new Snapshot
                {
                    LocalVersion = string.IsNullOrEmpty(_localVersion) ? "unknown" : _localVersion,
                    LatestVersion = _latestVersion,
                    State = _state,
                    ErrorMessage = _errorMessage,
                    IsDismissed = IsLatestVersionDismissed(),
                    LatestVersionIsCached = _latestVersionIsCached
                };
            }
        }

        internal static void ForceRefresh()
        {
            EnsureInitialized(forceRefresh: true);
        }

        internal static void OpenReleasesPage()
        {
            Application.OpenURL(string.IsNullOrEmpty(_latestReleaseUrl) ? ReleasesUrl : _latestReleaseUrl);
        }

        internal static void DismissCurrentLatest()
        {
            if (string.IsNullOrEmpty(_latestVersion))
                return;

            EditorPrefs.SetString(PrefDismissedVersion, _latestVersion);
            InternalEditorUtility.RepaintAllViews();
        }

        private static void EnsureInitialized(bool forceRefresh = false)
        {
            RefreshLocalVersion(forceRefresh || !_initialized);
            _initialized = true;
            EvaluateState();

            if (forceRefresh || ShouldRefresh())
                BeginRemoteCheck();
        }

        private static bool ShouldRefresh()
        {
            if (_checking)
                return false;

            if (string.IsNullOrEmpty(_latestVersion))
                return true;

            if (_lastCheckedUtc == default)
                return true;

            return (DateTime.UtcNow - _lastCheckedUtc).TotalHours >= CacheHours;
        }

        private static void BeginRemoteCheck()
        {
            if (_checking)
                return;

            _checking = true;
            _lastRefreshFailed = false;
            _fallbackErrorContext = string.Empty;
            _state = VersionState.Checking;
            _errorMessage = string.Empty;

            BeginRequest(LatestReleaseApiUrl, RemoteCheckSource.ReleaseApi, "application/vnd.github+json");
            InternalEditorUtility.RepaintAllViews();
        }

        private static void BeginRequest(string url, RemoteCheckSource source, string acceptHeader)
        {
            _request?.Dispose();
            _request = UnityWebRequest.Get(url);
            _request.timeout = 10;
            _request.SetRequestHeader("Accept", acceptHeader);
            _request.SetRequestHeader("User-Agent", "CrowFX-VersionChecker");
            _remoteCheckSource = source;

            var operation = _request.SendWebRequest();
            operation.completed += _ => CompleteRemoteCheck();
        }

        private static void CompleteRemoteCheck()
        {
            bool waitingForFallback = false;
            try
            {
                if (_request == null || _request.result != UnityWebRequest.Result.Success)
                {
                    string requestError = _request != null ? _request.error : "Unknown version check error.";
                    if (TryBeginPackageJsonFallback(requestError))
                    {
                        waitingForFallback = true;
                        return;
                    }

                    _lastCheckedUtc = DateTime.UtcNow;
                    _lastRefreshFailed = true;
                    _errorMessage = BuildFallbackErrorMessage(requestError);
                    SaveCache();
                    return;
                }

                string body = _request.downloadHandler.text ?? string.Empty;
                if (_remoteCheckSource == RemoteCheckSource.ReleaseApi)
                {
                    var payload = JsonUtility.FromJson<ReleaseData>(body);
                    string remoteVersion = NormalizeVersion(payload != null ? payload.tag_name : string.Empty);

                    if (string.IsNullOrEmpty(remoteVersion))
                    {
                        if (TryBeginPackageJsonFallback("Latest release response did not include a valid version tag."))
                        {
                            waitingForFallback = true;
                            return;
                        }

                        _lastCheckedUtc = DateTime.UtcNow;
                        _lastRefreshFailed = true;
                        _errorMessage = "Latest release response did not include a valid version tag.";
                        SaveCache();
                        return;
                    }

                    _lastCheckedUtc = DateTime.UtcNow;
                    _latestVersion = remoteVersion;
                    _latestReleaseUrl = payload != null && !string.IsNullOrWhiteSpace(payload.html_url)
                        ? payload.html_url
                        : ReleasesUrl;
                    _latestVersionIsCached = false;
                    _lastRefreshFailed = false;
                    _errorMessage = string.Empty;
                    SaveCache();
                    return;
                }

                var packagePayload = JsonUtility.FromJson<PackageJsonData>(body);
                string fallbackVersion = NormalizeVersion(packagePayload != null ? packagePayload.version : string.Empty);
                if (string.IsNullOrEmpty(fallbackVersion))
                {
                    _lastCheckedUtc = DateTime.UtcNow;
                    _lastRefreshFailed = true;
                    _errorMessage = BuildFallbackErrorMessage("Fallback package metadata did not include a valid version.");
                    SaveCache();
                    return;
                }

                _lastCheckedUtc = DateTime.UtcNow;
                _latestVersion = fallbackVersion;
                _latestReleaseUrl = ReleasesUrl;
                _latestVersionIsCached = false;
                _lastRefreshFailed = false;
                _errorMessage = string.Empty;
                SaveCache();
            }
            finally
            {
                if (!waitingForFallback)
                {
                    _checking = false;
                    _request?.Dispose();
                    _request = null;
                    EvaluateState();
                    InternalEditorUtility.RepaintAllViews();
                }
            }
        }

        private static bool TryBeginPackageJsonFallback(string requestError)
        {
            if (_remoteCheckSource != RemoteCheckSource.ReleaseApi)
                return false;

            _fallbackErrorContext = requestError;
            BeginRequest(MainPackageJsonUrl, RemoteCheckSource.PackageJsonFallback, "application/json");
            return true;
        }

        private static string BuildFallbackErrorMessage(string fallbackError)
        {
            if (string.IsNullOrWhiteSpace(_fallbackErrorContext))
                return fallbackError;

            return $"{_fallbackErrorContext} Fallback package metadata request failed: {fallbackError}";
        }

        private static void LoadCache()
        {
            if (EditorPrefs.GetInt(PrefCacheSchema, 0) != CurrentCacheSchemaVersion)
            {
                ClearCache();
                EditorPrefs.SetInt(PrefCacheSchema, CurrentCacheSchemaVersion);
                return;
            }

            _latestVersion = NormalizeVersion(EditorPrefs.GetString(PrefLatestVersion, string.Empty));
            _latestReleaseUrl = EditorPrefs.GetString(PrefLatestReleaseUrl, ReleasesUrl);
            _latestVersionIsCached = !string.IsNullOrEmpty(_latestVersion);
            _lastRefreshFailed = false;

            string rawTicks = EditorPrefs.GetString(PrefLastCheckedTicks, string.Empty);
            if (long.TryParse(rawTicks, out long ticks) && ticks > 0)
                _lastCheckedUtc = new DateTime(ticks, DateTimeKind.Utc);
        }

        private static void SaveCache()
        {
            EditorPrefs.SetInt(PrefCacheSchema, CurrentCacheSchemaVersion);
            EditorPrefs.SetString(PrefLatestVersion, _latestVersion ?? string.Empty);
            EditorPrefs.SetString(PrefLatestReleaseUrl, _latestReleaseUrl ?? ReleasesUrl);
            EditorPrefs.SetString(PrefLastCheckedTicks, _lastCheckedUtc.Ticks.ToString());
        }

        private static void ClearCache()
        {
            _latestVersion = string.Empty;
            _latestReleaseUrl = ReleasesUrl;
            _lastCheckedUtc = default;
            _latestVersionIsCached = false;
            _lastRefreshFailed = false;
            EditorPrefs.DeleteKey(PrefLatestVersion);
            EditorPrefs.DeleteKey(PrefLatestReleaseUrl);
            EditorPrefs.DeleteKey(PrefLastCheckedTicks);
        }

        private static void EvaluateState()
        {
            if (_checking)
            {
                _state = VersionState.Checking;
                return;
            }

            if (string.IsNullOrEmpty(_localVersion))
            {
                _state = VersionState.Error;
                if (string.IsNullOrEmpty(_errorMessage))
                    _errorMessage = "Installed CrowFX version could not be read from package.json.";
                return;
            }

            if (_lastRefreshFailed && !string.IsNullOrEmpty(_latestVersion))
            {
                _state = VersionState.Stale;
                return;
            }

            if (!string.IsNullOrEmpty(_latestVersion))
            {
                int compare = CompareVersions(_localVersion, _latestVersion);
                _state = compare switch
                {
                    < 0 => VersionState.Outdated,
                    > 0 => VersionState.Ahead,
                    _ => VersionState.UpToDate
                };
                return;
            }

            _state = string.IsNullOrEmpty(_errorMessage) ? VersionState.Unknown : VersionState.Error;
        }

        private static string LoadLocalVersion()
        {
            try
            {
                string fullPath = GetPackageJsonFullPath();
                if (!File.Exists(fullPath))
                    return string.Empty;

                string json = File.ReadAllText(fullPath);
                var payload = JsonUtility.FromJson<PackageJsonData>(json);
                return NormalizeVersion(payload != null ? payload.version : string.Empty);
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
                return string.Empty;
            }
        }

        private static void RefreshLocalVersion(bool force)
        {
            string fullPath = GetPackageJsonFullPath();
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                _localVersionFileTicks = 0;
                _localVersion = string.Empty;
                return;
            }

            long currentTicks = File.GetLastWriteTimeUtc(fullPath).Ticks;
            if (!force && !string.IsNullOrEmpty(_localVersion) && currentTicks == _localVersionFileTicks)
                return;

            _localVersionFileTicks = currentTicks;
            _localVersion = LoadLocalVersion();
        }

        private static string GetPackageJsonFullPath()
        {
            string assetPath = ResolvePackageJsonAssetPath();
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;

            return Path.GetFullPath(assetPath);
        }

        private static string ResolvePackageJsonAssetPath()
        {
            if (!string.IsNullOrEmpty(_packageJsonAssetPath))
                return _packageJsonAssetPath;

            string[] guids = AssetDatabase.FindAssets("CrowImageEffectsEditor t:MonoScript");
            for (int i = 0; i < guids.Length; i++)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guids[i]).Replace("\\", "/");
                int editorIndex = scriptPath.LastIndexOf("/Editor/", StringComparison.Ordinal);
                if (editorIndex < 0)
                    continue;

                string candidate = scriptPath.Substring(0, editorIndex) + "/package.json";
                string fullPath = Path.GetFullPath(candidate);
                if (!File.Exists(fullPath))
                    continue;

                _packageJsonAssetPath = candidate;
                return _packageJsonAssetPath;
            }

            return string.Empty;
        }

        private static bool IsLatestVersionDismissed()
        {
            if (string.IsNullOrEmpty(_latestVersion))
                return false;

            return string.Equals(
                NormalizeVersion(EditorPrefs.GetString(PrefDismissedVersion, string.Empty)),
                _latestVersion,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return string.Empty;

            string trimmed = version.Trim();
            if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(1);

            return trimmed;
        }

        private static int CompareVersions(string left, string right)
        {
            int[] leftParts = ParseVersionParts(left);
            int[] rightParts = ParseVersionParts(right);
            int count = Mathf.Max(leftParts.Length, rightParts.Length);

            for (int i = 0; i < count; i++)
            {
                int leftValue = i < leftParts.Length ? leftParts[i] : 0;
                int rightValue = i < rightParts.Length ? rightParts[i] : 0;

                if (leftValue != rightValue)
                    return leftValue.CompareTo(rightValue);
            }

            return 0;
        }

        private static int[] ParseVersionParts(string version)
        {
            string normalized = NormalizeVersion(version);
            if (string.IsNullOrEmpty(normalized))
                return Array.Empty<int>();

            string[] split = normalized.Split('.');
            int[] parts = new int[split.Length];

            for (int i = 0; i < split.Length; i++)
            {
                string token = split[i];
                int dash = token.IndexOf('-', StringComparison.Ordinal);
                if (dash >= 0)
                    token = token.Substring(0, dash);

                int digitCount = 0;
                while (digitCount < token.Length && char.IsDigit(token[digitCount]))
                    digitCount++;

                token = digitCount > 0 ? token.Substring(0, digitCount) : "0";
                parts[i] = int.TryParse(token, out int value) ? value : 0;
            }

            return parts;
        }
    }
}
#endif
