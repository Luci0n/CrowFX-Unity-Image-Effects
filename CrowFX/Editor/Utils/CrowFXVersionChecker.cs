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
            Error
        }

        internal struct Snapshot
        {
            public string LocalVersion;
            public string LatestVersion;
            public VersionState State;
            public string ErrorMessage;
            public bool IsDismissed;
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
        private static DateTime _lastCheckedUtc;
        private static bool _initialized;
        private static bool _checking;
        private static UnityWebRequest _request;
        private static VersionState _state;

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
                    IsDismissed = IsLatestVersionDismissed()
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
            if (!_initialized)
            {
                _localVersion = LoadLocalVersion();
                _initialized = true;
                EvaluateState();
            }

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
            _state = VersionState.Checking;
            _errorMessage = string.Empty;

            _request?.Dispose();
            _request = UnityWebRequest.Get(LatestReleaseApiUrl);
            _request.timeout = 10;
            _request.SetRequestHeader("Accept", "application/vnd.github+json");
            _request.SetRequestHeader("User-Agent", "CrowFX-VersionChecker");

            var operation = _request.SendWebRequest();
            operation.completed += _ => CompleteRemoteCheck();

            InternalEditorUtility.RepaintAllViews();
        }

        private static void CompleteRemoteCheck()
        {
            try
            {
                _lastCheckedUtc = DateTime.UtcNow;

                if (_request == null || _request.result != UnityWebRequest.Result.Success)
                {
                    _errorMessage = _request != null ? _request.error : "Unknown version check error.";
                    SaveCache();
                    return;
                }

                var payload = JsonUtility.FromJson<ReleaseData>(_request.downloadHandler.text ?? string.Empty);
                string remoteVersion = NormalizeVersion(payload != null ? payload.tag_name : string.Empty);

                if (string.IsNullOrEmpty(remoteVersion))
                {
                    _errorMessage = "Latest release response did not include a valid version tag.";
                    SaveCache();
                    return;
                }

                _latestVersion = remoteVersion;
                _latestReleaseUrl = payload != null && !string.IsNullOrWhiteSpace(payload.html_url)
                    ? payload.html_url
                    : ReleasesUrl;
                _errorMessage = string.Empty;
                SaveCache();
            }
            finally
            {
                _checking = false;
                _request?.Dispose();
                _request = null;
                EvaluateState();
                InternalEditorUtility.RepaintAllViews();
            }
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
                string assetPath = ResolvePackageJsonAssetPath();
                if (string.IsNullOrEmpty(assetPath))
                    return string.Empty;

                string fullPath = Path.GetFullPath(assetPath);
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
