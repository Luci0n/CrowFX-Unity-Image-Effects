#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using System.IO;
using CrowFX.Helpers;
using SectionKeys = CrowFX.Helpers.CrowFXSectionKeys;

namespace CrowFX.EditorTools
{
    [CustomEditor(typeof(CrowImageEffects))]
    public sealed class CrowImageEffectsEditor : Editor
    {
        // =============================================================================================
        // AUTO SECTION MODEL
        // =============================================================================================

        private readonly Dictionary<string, List<string>> _propsBySection = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AnimBool> _foldBySection = new(StringComparer.Ordinal);
        private readonly Dictionary<AnimBool, string> _prefKeyByFold = new();
        private readonly HashSet<string> _drawnThisSection = new(StringComparer.Ordinal);
        private readonly List<CrowFxSectionsModel.SectionDef> _sections = new();
        private readonly struct EnabledScope : IDisposable
        {
            private readonly bool _previousState;
            public EnabledScope(bool enabled) { _previousState = GUI.enabled; GUI.enabled = enabled; }
            public void Dispose() => GUI.enabled = _previousState;
        }

        private sealed class PreviewPropertyOverride
        {
            public string PropertyName;
            public SerializedPropertyType PropertyType;
            public bool BoolOriginalValue;
            public bool BoolPreviewValue;
            public float FloatOriginalValue;
            public float FloatPreviewValue;
            public int IntOriginalValue;
            public int IntPreviewValue;
        }

        private readonly struct SummaryPill
        {
            public readonly string Label;
            public readonly Color? Tint;
            public readonly float MinWidth;

            public SummaryPill(string label, Color? tint = null, float minWidth = 68f)
            {
                Label = label ?? "";
                Tint = tint;
                MinWidth = minWidth;
            }
        }

        private enum BleedPreviewHandleMode
        {
            None,
            ManualR,
            ManualG,
            ManualB,
            RadialCenter,
            RadialStrength
        }

        // Custom UI extras
        private AnimBool _foldResolutionPresets;

        // RGB Bleed sub-foldouts
        private AnimBool _foldBleedModeCombine;
        private AnimBool _foldBleedManual;
        private AnimBool _foldBleedRadial;
        private AnimBool _foldBleedEdge;
        private AnimBool _foldBleedSmear;
        private AnimBool _foldBleedPerChannel;
        private AnimBool _foldBleedSafety;
        private AnimBool _foldBleedWobble;
        private AnimBool _foldJitterAdvanced;
        private AnimBool _foldJitterHashNoise;

        private readonly List<AnimBool> _allFolds = new();

        // Search
        private const string Pref_Search = "CrowImageEffectsEditor.Search";
        private string _search = "";

        private static string _rootFromThisScript;

        private static string RootFromThisScript
        {
            get
            {
                if (!string.IsNullOrEmpty(_rootFromThisScript))
                    return _rootFromThisScript;

                var temp = CreateInstance<CrowImageEffectsEditor>();
                try
                {
                    var ms = MonoScript.FromScriptableObject(temp);
                    var scriptPath = AssetDatabase.GetAssetPath(ms);

                    if (string.IsNullOrEmpty(scriptPath))
                    {
                        _rootFromThisScript = "Assets";
                        return _rootFromThisScript;
                    }

                    scriptPath = scriptPath.Replace("\\", "/");

                    var editorIndex = scriptPath.LastIndexOf("/Editor/", StringComparison.Ordinal);
                    if (editorIndex >= 0)
                    {
                        _rootFromThisScript = scriptPath.Substring(0, editorIndex);
                        return _rootFromThisScript;
                    }

                    _rootFromThisScript = Path.GetDirectoryName(scriptPath)?.Replace("\\", "/") ?? "Assets";
                    return _rootFromThisScript;
                }
                finally
                {
                    if (temp != null)
                        DestroyImmediate(temp);
                }
            }
        }

        private static T LoadAssetAt<T>(string relativeToRoot) where T : UnityEngine.Object
        {
            var path = $"{RootFromThisScript}/{relativeToRoot}".Replace("\\", "/");
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        private Texture _collapseAllIcon;
        private Texture _expandAllIcon;

        private Texture GetCollapseAllIcon()
            => _collapseAllIcon ??= CrowFxEditorUI.IconCache.Get("d_Folder Icon");
        private Texture GetExpandAllIcon()
            => _expandAllIcon ??= CrowFxEditorUI.IconCache.Get("d_FolderOpened Icon");
        
        private Texture2D _diceIcon;
        private Texture2D GetDiceIcon()
            => _diceIcon != null ? _diceIcon : (_diceIcon = LoadAssetAt<Texture2D>("Editor/Icons/dice_icon.png"));

        private Texture2D _iconLogo;
        private Texture2D GetIconLogo()
            => _iconLogo != null ? _iconLogo : (_iconLogo = LoadAssetAt<Texture2D>("Editor/Icons/header.png"));

        private Font _customFont;
        private Font GetCustomFont()
            => _customFont != null ? _customFont : (_customFont = LoadAssetAt<Font>("Editor/Font/JetBrainsMonoNL-Thin.ttf"));

        // Favorites
        private HashSet<string> _favoriteSections = new(StringComparer.Ordinal);
        private const string Pref_Favorites = "CrowImageEffectsEditor.Favorites";

        // Preview / section clipboard state
        private static string _sectionClipboardKey;
        private static string _sectionClipboardJson;
        private readonly Dictionary<string, List<PreviewPropertyOverride>> _previewMutedSections = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<PreviewPropertyOverride>> _soloMutedSections = new(StringComparer.Ordinal);
        private List<PreviewPropertyOverride> _soloSectionPreviewOverrides;
        private string _soloSectionKey;
        private bool _previewBypassActive;
        private bool _previewBypassOriginalEnabled = true;
        private BleedPreviewHandleMode _bleedPreviewHandleMode;
        private float _bleedPreviewDragStartStrength;
        private float _bleedPreviewDragStartProjection;
        private Vector2 _bleedPreviewDragDirection;
        private bool _suspendAutoProfileSync;

        private static readonly string[] _previewActionSections =
        {
            SectionKeys.Sampling,
            SectionKeys.Pregrade,
            SectionKeys.Palette,
            SectionKeys.TextureMask,
            SectionKeys.DepthMask,
            SectionKeys.Jitter,
            SectionKeys.Bleed,
            SectionKeys.Ghost,
            SectionKeys.Edges,
            SectionKeys.Unsharp,
            SectionKeys.Dither
        };

        // =============================================================================================
        // LIFECYCLE
        // =============================================================================================
        private void OnEnable()
        {
            _search = EditorPrefs.GetString(Pref_Search, "");
            LoadFavorites();
            InitExtraFoldouts();
            RebuildAll();
        }

        private void OnDisable()
        {
            RestorePreviewStatesIfNeeded();
            RestorePreviewBypassIfNeeded();
            UnregisterAllFoldListeners();
        }

        private void RebuildAll()
        {
            BuildPropertyMapAndSections();
        }

        private void BuildPropertyMapAndSections()
        {
            var result = CrowFxSectionsModel.Build(
                serializedObject: serializedObject,
                favoriteSections: _favoriteSections,
                getOrCreateSectionFold: GetOrCreateSectionFold,
                resolveCustomDrawerOrNull: ResolveCustomDrawerOrNull
            );

            _propsBySection.Clear();
            foreach (var kv in result.PropsBySection)
                _propsBySection[kv.Key] = kv.Value;

            _sections.Clear();
            _sections.AddRange(result.Sections);
        }

        private void InitExtraFoldouts()
        {
            _foldResolutionPresets = NewFold("Sampling.ResolutionPresets", defaultExpanded: false);

            _foldBleedModeCombine = NewFold("Bleed.ModeCombine", defaultExpanded: true);
            _foldBleedManual      = NewFold("Bleed.Manual",      defaultExpanded: false);
            _foldBleedRadial      = NewFold("Bleed.Radial",      defaultExpanded: false);
            _foldBleedEdge        = NewFold("Bleed.Edge",        defaultExpanded: false);
            _foldBleedSmear       = NewFold("Bleed.Smear",       defaultExpanded: false);
            _foldBleedPerChannel  = NewFold("Bleed.PerChannel",  defaultExpanded: false);
            _foldBleedSafety      = NewFold("Bleed.Safety",      defaultExpanded: false);
            _foldBleedWobble      = NewFold("Bleed.Wobble",      defaultExpanded: false);
            _foldJitterAdvanced   = NewFold("Jitter.Advanced",   defaultExpanded: false);
            _foldJitterHashNoise = NewFold("Jitter.HashNoise", defaultExpanded: false);
        }

        // =============================================================================================
        // FOLDS (auto-created per section, persisted in EditorPrefs)
        // =============================================================================================
        private AnimBool NewFold(string id, bool defaultExpanded)
        {
            var key = PrefKey(id);
            bool start = EditorPrefs.GetBool(key, defaultExpanded);
            var fold = new AnimBool(start);

            // Track mapping for bulk operations
            _prefKeyByFold[fold] = key;

            fold.valueChanged.AddListener(() =>
            {
                EditorPrefs.SetBool(key, fold.target);
                Repaint();
            });

            _allFolds.Add(fold);
            return fold;
        }

        private AnimBool GetOrCreateSectionFold(string sectionKey, bool defaultExpanded)
        {
            if (_foldBySection.TryGetValue(sectionKey, out var fold))
                return fold;

            fold = NewFold("Section." + sectionKey, defaultExpanded);
            _foldBySection[sectionKey] = fold;
            return fold;
        }

        private void UnregisterAllFoldListeners()
        {
            foreach (var f in _allFolds)
                if (f != null) f.valueChanged.RemoveAllListeners();

            _allFolds.Clear();
            _prefKeyByFold.Clear();
        }

        private void SetAllFolds(bool expanded)
        {
            foreach (var f in _allFolds)
            {
                if (f == null) continue;
                f.target = expanded;

                if (_prefKeyByFold.TryGetValue(f, out var key))
                    EditorPrefs.SetBool(key, expanded);
            }

            GUI.FocusControl(null);
            Repaint();
        }
        private static string PrefKey(string id) => "CrowImageEffectsEditor." + id;

        // =============================================================================================
        // SERIALIZED PROPERTY ACCESS
        // =============================================================================================
        private SerializedProperty SP(string name)
        {
            var p = serializedObject.FindProperty(name);
            if (p == null) Debug.LogWarning($"Property '{name}' not found on CrowImageEffects");
            return p;
        }

        // =============================================================================================
        // ATTRIBUTE DISCOVERY (fields + sections)
        // =============================================================================================
        private void BuildPropertyMapFromAttributes()
        {
            _propsBySection.Clear();

            var t = typeof(CrowImageEffects);
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = t.GetFields(flags);

            var declIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < fields.Length; i++)
                declIndex[fields[i].Name] = i;

            var discovered = new List<(string section, int order, int decl, string name)>(fields.Length);

            foreach (var f in fields)
            {
                if (f.IsStatic) continue;
                if (f.IsNotSerialized) continue;
                if (Attribute.IsDefined(f, typeof(HideInInspector))) continue;

                bool serializable = f.IsPublic || Attribute.IsDefined(f, typeof(SerializeField));
                if (!serializable) continue;

                var attr = f.GetCustomAttribute<EffectSectionAttribute>(inherit: true);
                if (attr == null) continue;

                var prop = serializedObject.FindProperty(f.Name);
                if (prop == null) continue;

                discovered.Add((attr.Section ?? CrowFXSectionKeys.Misc, attr.Order, declIndex[f.Name], f.Name));
            }

            foreach (var g in discovered
                        .OrderBy(x => x.section, StringComparer.Ordinal)
                        .ThenBy(x => x.order)
                        .ThenBy(x => x.decl))
            {
                if (!_propsBySection.TryGetValue(g.section, out var list))
                {
                    list = new List<string>(32);
                    _propsBySection[g.section] = list;
                }
                list.Add(g.name);
            }
        }

        // =============================================================================================
        // INSPECTOR
        // =============================================================================================
        public override void OnInspectorGUI()
        {
            CrowFxEditorUI.Ensure(GetCustomFont());

            var fx = (CrowImageEffects)target;
            var previousProfile = fx != null ? fx.profile : null;
            bool previousAutoApplyProfile = fx != null && fx.autoApplyProfile;

            serializedObject.Update();

            DrawSummaryPanel(fx);

            foreach (var s in _sections.ToList())
            {
                if (!string.IsNullOrWhiteSpace(_search) && !SectionHasAnyMatch(s.Key))
                    continue;

                DrawSection(
                    sectionKey: s.Key,
                    title: s.Title,
                    icon: s.Icon,
                    fold: s.Fold,
                    hint: s.Hint,
                    drawContent: s.Draw ?? (() => DrawAutoSection(s.Key))
                );
            }

            bool changed = serializedObject.ApplyModifiedProperties();

            if (fx != null)
            {
                if (changed)
                    HandleInspectorProfileStateChange(fx, previousProfile, previousAutoApplyProfile);
                else
                    EnsureDepthModeIfNeeded(fx);
            }

            if (Event.current.type == EventType.Layout)
                _drawnThisSection.Clear();
        }

        private void DrawWorkflowBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("Reset", GUILayout.ExpandWidth(true)))
                {
                    if (EditorUtility.DisplayDialog("Reset Effects",
                        "Reset ALL values to factory defaults?\n\nThis cannot be undone.",
                        "Reset", "Cancel"))
                    {
                        RestorePreviewStatesIfNeeded();
                        ResetToDefaults();
                        RebuildAll();
                        GUI.FocusControl(null);
                    }
                }

                if (CrowFxEditorUI.MiniPill("Randomize", GUILayout.ExpandWidth(true)))
                {
                    if (EditorUtility.DisplayDialog("Randomize Effects",
                        "Randomize ALL values? This cannot be undone.\n\n(This will produce unpredictable effects, do NOT use if you suffer from epilepsy)",
                        "Randomize", "Cancel"))
                    {
                        RestorePreviewStatesIfNeeded();
                        RandomizeAllProperties();
                        GUI.FocusControl(null);
                    }
                }

                if (CrowFxEditorUI.MiniPill(_previewBypassActive ? "Resume" : "Bypass", GUILayout.ExpandWidth(true)))
                {
                    RestorePreviewStatesIfNeeded();
                    TogglePreviewBypass();
                    GUI.FocusControl(null);
                }
            }

            if (_previewBypassActive)
                CrowFxEditorUI.Hint("Preview bypass is active. Resume to re-enable the effect component.", CrowFxEditorUI.HintType.Warning);
        }

        private List<string> GetActiveStageLabels(CrowImageEffects fx)
        {
            var stages = new List<string>(12);
            if (fx == null) return stages;

            if (fx.pixelSize > 1 || fx.useVirtualGrid) stages.Add("Sampling");
            stages.Add("Posterize");
            if (fx.pregradeEnabled) stages.Add("Pregrade");
            if (fx.usePalette && fx.paletteTex != null) stages.Add("Palette");
            if (fx.useMask && fx.maskTex != null) stages.Add("Texture Mask");
            if (fx.useDepthMask) stages.Add("Depth Mask");
            if (fx.jitterEnabled && fx.jitterStrength > 0f) stages.Add("Jitter");
            if (fx.bleedBlend > 0f && fx.bleedIntensity > 0f) stages.Add("RGB Bleed");
            if (fx.ghostEnabled && fx.ghostBlend > 0f) stages.Add("Ghost");
            if (fx.edgeEnabled && fx.edgeBlend > 0f) stages.Add("Edges");
            if (fx.unsharpEnabled && fx.unsharpAmount > 0f) stages.Add("Unsharp");
            if (fx.ditherMode != CrowImageEffects.DitherMode.None && fx.ditherStrength > 0f) stages.Add("Dither");

            return stages;
        }

        private static bool NeedsDepthFix(CrowImageEffects fx)
        {
            if (fx == null || (!fx.useDepthMask && !fx.edgeEnabled))
                return false;

            var camera = fx.GetComponent<Camera>();
            return camera != null && (camera.depthTextureMode & DepthTextureMode.Depth) == 0;
        }

        private void DrawSummaryStatusStrip(CrowImageEffects targetFx)
        {
            var activeStages = GetActiveStageLabels(targetFx);
            string stageSummary = activeStages.Count == 1 ? "1 active stage" : $"{activeStages.Count} active stages";
            EditorGUILayout.LabelField(stageSummary, CrowFxEditorUI.Styles.SummaryText);

            if (targetFx != null && targetFx.profile != null)
                EditorGUILayout.LabelField($"Linked profile: {targetFx.profile.name}", CrowFxEditorUI.Styles.HintText);

            if (activeStages.Count > 0)
            {
                var stagePills = new List<SummaryPill>(activeStages.Count);
                for (int i = 0; i < activeStages.Count; i++)
                    stagePills.Add(new SummaryPill(activeStages[i]));

                DrawWrappedTagPills(stagePills);
            }

            var statusPills = new List<SummaryPill>(6);

            if (!string.IsNullOrEmpty(_soloSectionKey))
                statusPills.Add(new SummaryPill($"Solo: {_soloSectionKey}", new Color(0.3f, 0.24f, 0.14f, 0.65f), 92f));
            else if (_previewMutedSections.Count > 0)
                statusPills.Add(new SummaryPill($"{_previewMutedSections.Count} muted preview", new Color(0.22f, 0.22f, 0.22f, 0.65f), 128f));

            if (_previewBypassActive)
                statusPills.Add(new SummaryPill("Preview bypassed", new Color(0.28f, 0.2f, 0.16f, 0.65f), 120f));

            if (targetFx != null && targetFx.ghostEnabled && targetFx.ghostBlend > 0f)
            {
                float approxMb = EstimateGhostHistoryMegabytes(targetFx);
                statusPills.Add(new SummaryPill($"Ghost ~{approxMb:0.0} MB", new Color(0.18f, 0.22f, 0.28f, 0.65f), 116f));
            }

            if (targetFx != null && targetFx.ditherMode == CrowImageEffects.DitherMode.BlueNoise && targetFx.blueNoise == null)
                statusPills.Add(new SummaryPill("Blue noise missing", new Color(0.28f, 0.22f, 0.12f, 0.65f), 126f));

            if (targetFx != null && targetFx.jitterMode == CrowImageEffects.JitterMode.BlueNoiseTex && targetFx.jitterNoiseTex == null)
                statusPills.Add(new SummaryPill("Jitter noise missing", new Color(0.28f, 0.22f, 0.12f, 0.65f), 136f));

            if (NeedsDepthFix(targetFx))
                statusPills.Add(new SummaryPill("Depth fix needed", new Color(0.28f, 0.22f, 0.12f, 0.65f), 118f));

            DrawWrappedTagPills(statusPills);
        }

        private void DrawVersionStatus()
        {
            var snapshot = CrowFXVersionChecker.Current;
            string latestLabel = snapshot.State == CrowFXVersionChecker.VersionState.Checking
                ? "checking..."
                : string.IsNullOrEmpty(snapshot.LatestVersion) ? "-" : snapshot.LatestVersion;

            EditorGUILayout.LabelField($"Version: {snapshot.LocalVersion}   Latest release: {latestLabel}", CrowFxEditorUI.Styles.SummaryText);

            switch (snapshot.State)
            {
                case CrowFXVersionChecker.VersionState.Checking:
                    CrowFxEditorUI.Hint("Checking for CrowFX updates...");
                    Repaint();
                    break;

                case CrowFXVersionChecker.VersionState.Outdated when !snapshot.IsDismissed:
                    CrowFxEditorUI.Hint(
                        $"You are using CrowFX {snapshot.LocalVersion}. Latest release is {snapshot.LatestVersion}.",
                        CrowFxEditorUI.HintType.Warning);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (CrowFxEditorUI.MiniPill("Open Releases", GUILayout.ExpandWidth(true)))
                            CrowFXVersionChecker.OpenReleasesPage();

                        if (CrowFxEditorUI.MiniPill("Check Now", GUILayout.Width(92f)))
                            CrowFXVersionChecker.ForceRefresh();

                        if (CrowFxEditorUI.MiniPill("Dismiss", GUILayout.Width(86f)))
                            CrowFXVersionChecker.DismissCurrentLatest();
                    }
                    break;

                case CrowFXVersionChecker.VersionState.Error when string.IsNullOrEmpty(snapshot.LatestVersion):
                    CrowFxEditorUI.Hint("Could not check for CrowFX release updates right now.");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (CrowFxEditorUI.MiniPill("Check Now", GUILayout.Width(92f)))
                            CrowFXVersionChecker.ForceRefresh();

                        if (CrowFxEditorUI.MiniPill("Open Releases", GUILayout.Width(118f)))
                            CrowFXVersionChecker.OpenReleasesPage();
                    }
                    break;

                case CrowFXVersionChecker.VersionState.Ahead:
                    CrowFxEditorUI.Hint(
                        $"Installed version {snapshot.LocalVersion} is newer than the latest published release ({snapshot.LatestVersion}).");
                    break;
            }
        }

        private float GetSummaryPillWidth(string label, float minWidth, float availableWidth)
        {
            float textWidth = CrowFxEditorUI.Styles.PillButton.CalcSize(new GUIContent(label ?? "")).x + 20f;
            return Mathf.Min(availableWidth, Mathf.Max(minWidth, textWidth));
        }

        private void DrawWrappedTagPills(IReadOnlyList<SummaryPill> pills)
        {
            if (pills == null || pills.Count == 0)
                return;

            float availableWidth = Mathf.Max(140f, EditorGUIUtility.currentViewWidth - 56f);
            int index = 0;

            while (index < pills.Count)
            {
                float rowWidth = 0f;

                using (new EditorGUILayout.HorizontalScope())
                {
                    while (index < pills.Count)
                    {
                        var pill = pills[index];
                        float pillWidth = GetSummaryPillWidth(pill.Label, pill.MinWidth, availableWidth);

                        if (rowWidth > 0f && rowWidth + pillWidth > availableWidth)
                            break;

                        CrowFxEditorUI.TagPill(pill.Label, pill.Tint, GUILayout.Width(pillWidth));
                        rowWidth += pillWidth + 4f;
                        index++;
                    }

                    GUILayout.FlexibleSpace();
                }

                if (index < pills.Count)
                    GUILayout.Space(2f);
            }
        }

        // =============================================================================================
        // TOP SUMMARY (with embedded search)
        // =============================================================================================
        private void DrawSummaryPanel(CrowImageEffects targetFx)
        {
            using (CrowFxEditorUI.PanelScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    float w = EditorGUIUtility.currentViewWidth;

                    bool iconOnly = w < 360f;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var iconLogo = GetIconLogo();
                        if (iconLogo != null)
                        {
                            var iconRect = GUILayoutUtility.GetRect(24f, 24f, GUILayout.Width(120f), GUILayout.Height(24f));
                            GUI.DrawTexture(iconRect, iconLogo, ScaleMode.StretchToFill, true);
                        }

                        GUILayout.FlexibleSpace();

                        var collapseIcon = GetCollapseAllIcon();
                        var expandIcon   = GetExpandAllIcon();

                        if (iconOnly)
                        {
                            if (CrowFxEditorUI.IconPill(collapseIcon, "Collapse all sections", 18f))
                                SetAllFolds(false);

                            GUILayout.Space(6);

                            if (CrowFxEditorUI.IconPill(expandIcon, "Expand all sections", 18f))
                                SetAllFolds(true);
                        }
                        else
                        {
                            if (CrowFxEditorUI.MiniPill("Collapse All", GUILayout.Width(110f)))
                                SetAllFolds(false);

                            GUILayout.Space(6);

                            if (CrowFxEditorUI.MiniPill("Expand All", GUILayout.Width(110f)))
                                SetAllFolds(true);
                        }
                    }
                }

                string resolution = targetFx != null && targetFx.useVirtualGrid
                    ? $"{Mathf.Max(1, targetFx.virtualResolution.x)}x{Mathf.Max(1, targetFx.virtualResolution.y)}"
                    : "Screen";

                string pixelation = targetFx != null && targetFx.pixelSize > 1 ? $"x{targetFx.pixelSize}" : "Off";
                string ditherMode = targetFx != null ? targetFx.ditherMode.ToString() : "-";

                string ghostInfo = targetFx != null && targetFx.ghostEnabled
                    ? $"{targetFx.ghostFrames}f / +{targetFx.ghostCaptureInterval} / d{targetFx.ghostStartDelay}"
                    : "-";

                GUILayout.Space(4);

                var summary =
                    $"Grid: {resolution}   Pixel: {pixelation}   " +
                    $"Dither: {ditherMode}   Ghost: {ghostInfo}";

                EditorGUILayout.LabelField(summary, CrowFxEditorUI.Styles.SummaryText);
                GUILayout.Space(4);
                DrawVersionStatus();
                GUILayout.Space(6);
                DrawWorkflowBar();
                CrowFxEditorUI.Divider();

                GUILayout.Space(6);

                DrawSummaryStatusStrip(targetFx);

                GUILayout.Space(6);

                CrowFxEditorUI.SearchBar("Search", ref _search, Pref_Search);

                CrowFxEditorUI.Hint("Type to filter settings by name (e.g., \"ghost\", \"dither\", \"resolution\"). Click X to clear.");
            }
        }

        // =============================================================================================
        // SECTION CONTAINERS (header has per-section reset button & randomization)
        // =============================================================================================
        private void DrawSection(string sectionKey, string title, string icon, AnimBool fold, Action drawContent, string hint)
        {
            using (new EditorGUILayout.VerticalScope(CrowFxEditorUI.Styles.Panel))
            {
                var headerRect = GUILayoutUtility.GetRect(0f, 26f, GUILayout.ExpandWidth(true));

                if (Event.current.type == EventType.Repaint)
                {
                    GUI.DrawTexture(headerRect, CrowFxEditorUI.Styles.HeaderTexture, ScaleMode.StretchToFill);
                    CrowFxEditorUI.Theme.DrawBorder(headerRect);
                }

                Rect starRect   = new Rect(headerRect.x + 2f,    headerRect.y + 4f, 16f, 18f);
                Rect resetRect  = new Rect(headerRect.xMax - 96f, headerRect.y + 4f, 92f, 18f);
                Rect randomRect = new Rect(headerRect.xMax - 114f, headerRect.y + 4f, 16f, 18f);
                Rect rightButtons = new Rect(randomRect.x, randomRect.y, resetRect.xMax - randomRect.x, randomRect.height);

                Rect ignoreLeft  = new Rect(starRect.x,   starRect.y,   starRect.width,               starRect.height);
                Rect ignoreRight = new Rect(randomRect.x, randomRect.y, resetRect.xMax - randomRect.x, randomRect.height);
                Rect ignoreAll = new Rect(ignoreLeft.x, ignoreLeft.y, ignoreRight.xMax - ignoreLeft.x, ignoreLeft.height);

                HandleHeaderClick(headerRect, fold, ignoreRect1: starRect, ignoreRect2: rightButtons);

                DrawStarButton(starRect, sectionKey);
                DrawSectionHeader(headerRect, title, icon, hint, fold.target, randomRect);
                DrawSectionEnabledDot(headerRect, sectionKey);
                DrawDiceButton(randomRect, sectionKey);

                if (HandleHeaderResetButton(resetRect, sectionKey))
                    RebuildAll();

                using (var fade = new EditorGUILayout.FadeGroupScope(fold.faded))
                {
                    if (fade.visible)
                    {
                        GUILayout.Space(8);
                        if (DrawSectionToolbar(sectionKey))
                            GUILayout.Space(8);
                        drawContent?.Invoke();
                    }
                }
            }
        }

        private bool DrawSectionToolbar(string sectionKey)
        {
            var sectionProps = GetSectionPropertyNames(sectionKey);
            bool canPreview = SupportsPreviewActions(sectionKey);
            bool canCopyPaste = sectionProps != null && sectionProps.Count > 0;
            if (!canPreview && !canCopyPaste)
                return false;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (canPreview)
                {
                    bool soloed = IsSectionSoloed(sectionKey);
                    bool muted = IsSectionPreviewMuted(sectionKey);
                    bool soloLocked = !string.IsNullOrEmpty(_soloSectionKey) && !soloed;

                    using (new EnabledScope(!soloLocked || soloed))
                    {
                        if (CrowFxEditorUI.MiniPill(soloed ? "Unsolo" : "Solo", GUILayout.Width(68f)))
                            ToggleSectionPreviewSolo(sectionKey);
                    }

                    using (new EnabledScope(string.IsNullOrEmpty(_soloSectionKey)))
                    {
                        if (CrowFxEditorUI.MiniPill(muted ? "Unmute" : "Mute", GUILayout.Width(68f)))
                            ToggleSectionPreviewMute(sectionKey);
                    }
                }

                if (canCopyPaste)
                {
                    if (CrowFxEditorUI.MiniPill("Copy", GUILayout.Width(58f)))
                        CopySectionSettings(sectionKey);

                    using (new EnabledScope(CanPasteSection(sectionKey)))
                    {
                        if (CrowFxEditorUI.MiniPill("Paste", GUILayout.Width(58f)))
                            PasteSectionSettings(sectionKey);
                    }
                }

                GUILayout.FlexibleSpace();

                if (IsSectionSoloed(sectionKey))
                    CrowFxEditorUI.TagPill("Preview Solo", new Color(0.28f, 0.22f, 0.12f, 0.55f), GUILayout.Width(96f));
                else if (IsSectionPreviewMuted(sectionKey))
                    CrowFxEditorUI.TagPill("Preview Muted", new Color(0.18f, 0.18f, 0.18f, 0.65f), GUILayout.Width(112f));
                else if (IsSectionMutedBySolo(sectionKey))
                    CrowFxEditorUI.TagPill("Muted by Solo", new Color(0.18f, 0.18f, 0.18f, 0.65f), GUILayout.Width(104f));
            }

            return true;
        }

        private void DrawStarButton(Rect rect, string sectionKey)
        {
            bool isFav = _favoriteSections.Contains(sectionKey);

            bool isHovered = rect.Contains(Event.current.mousePosition);
            bool isPressed = isHovered && Event.current.type == EventType.MouseDown && Event.current.button == 0;
            Color bgColor  = isPressed ? CrowFxEditorUI.Theme.ButtonActive : isHovered ? CrowFxEditorUI.Theme.ButtonHover : CrowFxEditorUI.Theme.ButtonNormal;

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, bgColor);
                CrowFxEditorUI.Theme.DrawBorder(rect);
            }

            var starStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                padding   = new RectOffset(0, 0, 0, 0),
                fontSize  = 13,
                clipping  = TextClipping.Overflow,
                normal    = { textColor = Color.white }
            };

            var starContent = new GUIContent(isFav ? "\u2605" : "\u2606");
            Vector2 starSize = starStyle.CalcSize(starContent);
            var starRect = new Rect(
                Mathf.Round(rect.x + (rect.width - starSize.x) * 0.5f),
                Mathf.Round(rect.y + (rect.height - starSize.y) * 0.5f) - 1f,
                starSize.x,
                starSize.y);

            var prev = GUI.contentColor;
            GUI.contentColor = isFav ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(1f, 1f, 1f, 0.34f);
            GUI.Label(starRect, starContent, starStyle);
            GUI.contentColor = prev;

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                ToggleFavorite(sectionKey);
                Repaint();
                Event.current.Use();
            }
        }

        private void DrawDiceButton(Rect randomRect, string sectionKey)
        {
            var diceIcon = GetDiceIcon();
            if (diceIcon != null)
            {
                bool isHovered = randomRect.Contains(Event.current.mousePosition);
                bool isPressed = isHovered && Event.current.type == EventType.MouseDown && Event.current.button == 0;
                Color bgColor  = isPressed ? CrowFxEditorUI.Theme.ButtonActive : isHovered ? CrowFxEditorUI.Theme.ButtonHover : CrowFxEditorUI.Theme.ButtonNormal;

                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(randomRect, bgColor);
                    CrowFxEditorUI.Theme.DrawBorder(randomRect);
                    float pad = 2f;
                    var iconRect = new Rect(randomRect.x + pad, randomRect.y + pad,
                                            randomRect.width - pad, randomRect.height - pad);
                    GUI.DrawTexture(iconRect, diceIcon, ScaleMode.ScaleToFit, true);
                }

                if (GUI.Button(randomRect, GUIContent.none, GUIStyle.none))
                {
                    if (EditorUtility.DisplayDialog("Randomize Section",
                        $"Randomize \"{sectionKey}\" values?\n\nThis cannot be undone.",
                        "Randomize", "Cancel"))
                    {
                        RandomizeSectionProperties(sectionKey);
                    }
                    Event.current.Use();
                }

                if (randomRect.Contains(Event.current.mousePosition))
                    GUI.Label(randomRect, new GUIContent("", "Randomize"), GUIStyle.none);
            }
            else
            {
                if (CrowFxEditorUI.HeaderResetPill(randomRect, "?"))
                {
                    if (EditorUtility.DisplayDialog("Randomize Section",
                        $"Randomize \"{sectionKey}\" values?\n\nThis cannot be undone.",
                        "Randomize", "Cancel"))
                    {
                        RandomizeSectionProperties(sectionKey);
                    }
                }
            }
        }

        private void DrawSubSection(string title, string icon, AnimBool fold, Action drawContent, string hint)
        {
            var headerRect = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                var inset = headerRect;
                inset.xMin += 2f;
                inset.xMax -= 2f;

                GUI.DrawTexture(inset, CrowFxEditorUI.Styles.HeaderTexture, ScaleMode.StretchToFill);
                CrowFxEditorUI.Theme.DrawBorder(inset);
            }

            HandleHeaderClick(headerRect, fold, ignoreRect1: default, ignoreRect2: default);
            DrawSubSectionHeader(headerRect, title, icon, hint, fold.target);

            using (var fade = new EditorGUILayout.FadeGroupScope(fold.faded))
            {
                if (fade.visible)
                {
                    GUILayout.Space(6);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(6);
                        using (new EditorGUILayout.VerticalScope())
                            drawContent?.Invoke();
                    }
                    GUILayout.Space(2);
                }
            }
        }

        private static void HandleHeaderClick(Rect rect, AnimBool fold, Rect ignoreRect1, Rect ignoreRect2 = default)
        {
            var current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
            {
                if (ignoreRect1.width > 0f && ignoreRect1.Contains(current.mousePosition)) return;
                if (ignoreRect2.width > 0f && ignoreRect2.Contains(current.mousePosition)) return;

                fold.target = !fold.target;
                current.Use();
            }
        }

        private static bool ShouldHeaderUseIconOnly(float headerWidth, float rightButtonsWidth)
        {
            float usable = headerWidth - rightButtonsWidth;
            return usable < 180f;
        }

        private void DrawSectionHeader(Rect rect, string title, string iconName, string hint, bool isExpanded, Rect rightButtonsRect)
        {
            var icon = CrowFxEditorUI.IconCache.Get(iconName);

            var chevronRect = new Rect(rect.x + 24f, rect.y + 4f, 14f, 18f);
            var chevronStyle = new GUIStyle(CrowFxEditorUI.Styles.HeaderLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
            GUI.Label(chevronRect, isExpanded ? "\u25BE" : "\u25B8", chevronStyle);

            float rightButtonsWidth = (rect.xMax - rightButtonsRect.xMin) + 6f;
            bool iconOnly = ShouldHeaderUseIconOnly(rect.width, rightButtonsWidth);

            float xPos = rect.x + 40f;

            Rect iconRect = default;
            if (icon != null)
            {
                iconRect = new Rect(xPos, rect.y + 5f, 16f, 16f);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                xPos = iconRect.xMax + 6f;

                if (iconOnly)
                {
                    string tt = string.IsNullOrEmpty(hint) ? title : $"{title}\n{hint}";
                    GUI.Label(iconRect, new GUIContent("", tt));
                }
            }

            if (iconOnly)
            {
                if (icon == null)
                {
                    var tinyTitleRect = new Rect(xPos, rect.y + 4f, Mathf.Max(40f, rightButtonsRect.xMin - xPos - 6f), 18f);
                    GUI.Label(tinyTitleRect, title, CrowFxEditorUI.Styles.HeaderLabel);
                }
                return;
            }

            float maxTitleWidth = 120f;
            var titleRect = new Rect(xPos, rect.y + 4f, maxTitleWidth, 18f);
            GUI.Label(titleRect, title, CrowFxEditorUI.Styles.HeaderLabel);

            if (!string.IsNullOrEmpty(hint))
            {
                float hintLeft  = xPos + maxTitleWidth + 4f;
                float hintRight = rightButtonsRect.xMin - 4f;
                float hintWidth = hintRight - hintLeft;

                if (hintWidth > 20f)
                {
                    var hintRect = new Rect(hintLeft, rect.y + 6f, hintWidth, 16f);
                    GUI.Label(hintRect, $"<i>{hint}</i>", CrowFxEditorUI.Styles.HeaderHint);
                }
            }
        }
        
        private void DrawSubSectionHeader(Rect rect, string title, string iconName, string hint, bool isExpanded)
        {
            var icon = CrowFxEditorUI.IconCache.Get(iconName);

            var foldoutRect = new Rect(rect.x + 6f, rect.y + 4f, 14f, 14f);
            var foldoutStyle = new GUIStyle(CrowFxEditorUI.Styles.SubHeaderLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            GUI.Label(foldoutRect, isExpanded ? "\u25BE" : "\u25B8", foldoutStyle);

            bool iconOnly = rect.width < 180f;

            float xPos = rect.x + 24f;

            Rect iconRect = default;
            if (icon != null)
            {
                iconRect = new Rect(xPos, rect.y + 3f, 16f, 16f);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                xPos = iconRect.xMax + 6f;

                if (iconOnly)
                {
                    string tt = string.IsNullOrEmpty(hint) ? title : $"{title}\n{hint}";
                    GUI.Label(iconRect, new GUIContent("", tt));
                }
            }

            if (iconOnly)
                return;

            var titleRect = new Rect(xPos, rect.y + 2f, rect.width * 0.62f, 18f);
            GUI.Label(titleRect, title, CrowFxEditorUI.Styles.SubHeaderLabel);

            if (!string.IsNullOrEmpty(hint))
            {
                var hintRect = new Rect(rect.x + rect.width * 0.62f, rect.y + 4f, rect.width * 0.36f, 16f);
                GUI.Label(hintRect, $"<i>{hint}</i>", CrowFxEditorUI.Styles.HeaderHint);
            }
        }

        private void DrawHeader(string title, string hint)
        {
            var rect = GUILayoutUtility.GetRect(0f, 26f, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(rect, CrowFxEditorUI.Styles.HeaderTexture, ScaleMode.StretchToFill);
                CrowFxEditorUI.Theme.DrawBorder(rect);
            }

            var titleRect = new Rect(rect.x + 10f, rect.y + 4f, rect.width * 0.7f, 18f);
            GUI.Label(titleRect, title, CrowFxEditorUI.Styles.SectionTitle);

            if (!string.IsNullOrEmpty(hint))
            {
                var hintRect = new Rect(rect.x + rect.width * 0.7f, rect.y + 6f, rect.width * 0.28f, 16f);
                GUI.Label(hintRect, $"<i>{hint}</i>", CrowFxEditorUI.Styles.HeaderHint);
            }
        }

        private static Texture2D _dotCircleOn;
        private static Texture2D _dotCircleOff;

        private static Texture2D GetDotTexture(bool on)
        {
            ref var tex = ref on ? ref _dotCircleOn : ref _dotCircleOff;
            if (tex != null) return tex;

            const int size = 16;
            tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };

            Color fill   = on ? new Color(0.9f, 0.2f, 0.2f, 1f) : new Color(1f, 1f, 1f, 0.15f);
            Color clear  = new Color(0f, 0f, 0f, 0f);
            float center = (size - 1) * 0.5f;
            float radius = (size * 0.5f) - 1f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Smooth edge via alpha lerp in the last pixel
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                tex.SetPixel(x, y, dist <= radius + 0.5f
                    ? new Color(fill.r, fill.g, fill.b, fill.a * alpha)
                    : clear);
            }

            tex.Apply();
            return tex;
        }

        private static readonly Dictionary<string, string> _dotPropOverrides = new(StringComparer.Ordinal)
        {
            { SectionKeys.Edges,       "edgeEnabled"     },
            { SectionKeys.Unsharp,     "unsharpEnabled"  },
            { SectionKeys.Pregrade,    "pregradeEnabled" },
            { SectionKeys.TextureMask, "useMask"         },
            { SectionKeys.DepthMask,   "useDepthMask"    },
            { SectionKeys.Jitter,      "jitterEnabled"   },
            { SectionKeys.Bleed,       "bleedIntensity"  },
            { SectionKeys.Palette,     "usePalette"      },
            { SectionKeys.Dither,      "ditherMode"      },
            { SectionKeys.Ghost,       "ghostEnabled"    },
            { SectionKeys.Sampling,    null              },
            { SectionKeys.Posterize,   null              },
            { SectionKeys.Shaders,     null              },
            { SectionKeys.Master,      null              },
        };

        private void DrawSectionEnabledDot(Rect headerRect, string sectionKey)
        {
            bool isOn = IsSectionActive(sectionKey);

            if (_dotPropOverrides.TryGetValue(sectionKey, out var overrideName))
            {
                if (overrideName == null) return;
            }
            else
            {
                string lower = sectionKey.ToLower();
                string[] candidates = {
                    lower + "Enabled",
                    lower.TrimEnd('s') + "Enabled",
                    "use" + sectionKey,
                    "enable" + sectionKey
                };

                bool found = false;
                foreach (var candidate in candidates)
                {
                    var p = serializedObject.FindProperty(candidate);
                    if (p != null && p.propertyType == SerializedPropertyType.Boolean)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return;
            }

            var dotRect = new Rect(headerRect.xMax + 1f, headerRect.y + 9f, 8f, 8f);

            if (Event.current.type == EventType.Repaint)
            {
                if (isOn)
                {
                    var glowRect = new Rect(dotRect.x - 3f, dotRect.y - 3f, dotRect.width + 6f, dotRect.height + 6f);
                    GUI.DrawTexture(glowRect, GetDotTexture(true), ScaleMode.ScaleToFit, true, 0,
                        new Color(1f, 0.1f, 0.1f, 0.25f), 0, 0);
                }
                GUI.DrawTexture(dotRect, GetDotTexture(isOn), ScaleMode.ScaleToFit, true);
            }
        }
        private bool IsSectionActive(string sectionKey)
        {
            return sectionKey switch
            {
                SectionKeys.Sampling    => GetInt("pixelSize") > 1 || GetBool("useVirtualGrid"),
                SectionKeys.Posterize   => true,
                SectionKeys.Pregrade    => GetBool("pregradeEnabled"),
                SectionKeys.Palette     => GetBool("usePalette") && GetObject("paletteTex") != null,
                SectionKeys.TextureMask => GetBool("useMask") && GetObject("maskTex") != null,
                SectionKeys.DepthMask   => GetBool("useDepthMask"),
                SectionKeys.Jitter      => GetBool("jitterEnabled") && GetFloat("jitterStrength") > 0f,
                SectionKeys.Bleed       => GetFloat("bleedBlend") > 0f && GetFloat("bleedIntensity") > 0f,
                SectionKeys.Ghost       => GetBool("ghostEnabled") && GetFloat("ghostBlend") > 0f,
                SectionKeys.Edges       => GetBool("edgeEnabled") && GetFloat("edgeBlend") > 0f,
                SectionKeys.Unsharp     => GetBool("unsharpEnabled") && GetFloat("unsharpAmount") > 0f,
                SectionKeys.Dither      => GetEnum("ditherMode") != (int)CrowImageEffects.DitherMode.None && GetFloat("ditherStrength") > 0f,
                _                       => false
            };
        }

        // =============================================================================================
        // SEARCH FILTER HELPERS
        // =============================================================================================
        private bool PassesSearch(string haystack)
        {
            if (string.IsNullOrWhiteSpace(_search)) return true;
            if (string.IsNullOrEmpty(haystack)) return false;
            return haystack.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool PropMatchesSearch(SerializedProperty p)
        {
            if (p == null) return false;
            if (string.IsNullOrWhiteSpace(_search)) return true;

            if (PassesSearch(p.displayName)) return true;
            if (PassesSearch(p.name)) return true;
            if (PassesSearch(p.propertyPath)) return true;
            return false;
        }

        private bool SectionHasAnyMatch(string sectionKey)
        {
            if (string.IsNullOrWhiteSpace(_search)) return true;
            if (!_propsBySection.TryGetValue(sectionKey, out var list)) return false;

            for (int i = 0; i < list.Count; i++)
            {
                var p = serializedObject.FindProperty(list[i]);
                if (p != null && PropMatchesSearch(p)) return true;
            }

            return PassesSearch(sectionKey);
        }

        // =============================================================================================
        // AUTO DRAW CORE
        // =============================================================================================
        private void BeginSectionDrawn() => _drawnThisSection.Clear();

        private void MarkDrawn(string propName)
        {
            if (!string.IsNullOrEmpty(propName))
                _drawnThisSection.Add(propName);
        }

        // FIX: convenience to "claim" properties even when they are intentionally hidden by toggles.
        private void MarkDrawnMany(params string[] propNames)
        {
            if (propNames == null) return;
            for (int i = 0; i < propNames.Length; i++)
                MarkDrawn(propNames[i]);
        }

        private void DrawAutoSection(string sectionKey)
        {
            BeginSectionDrawn();
            DrawAutoRemaining(sectionKey);
        }

        private void DrawAutoRemaining(string sectionKey)
        {
            if (!_propsBySection.TryGetValue(sectionKey, out var list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                var name = list[i];
                if (_drawnThisSection.Contains(name)) continue;

                var p = serializedObject.FindProperty(name);
                if (p == null) continue;

                if (!PropMatchesSearch(p)) continue;

                EditorGUILayout.PropertyField(p, PropLabel(p), includeChildren: true);
            }
        }

        private string GetTooltipForProperty(SerializedProperty prop)
        {
            if (prop == null)
                return string.Empty;

            var cache = GetFieldCache();
            if (cache.TryGetValue(prop.name, out var field))
                return field.GetCustomAttribute<TooltipAttribute>(inherit: true)?.tooltip ?? string.Empty;

            return string.Empty;
        }

        private GUIContent PropLabel(SerializedProperty prop, string labelText = null, string tooltipOverride = null)
        {
            return new GUIContent(
                labelText ?? prop?.displayName ?? string.Empty,
                string.IsNullOrEmpty(tooltipOverride) ? GetTooltipForProperty(prop) : tooltipOverride);
        }

        private void DrawPropertyField(SerializedProperty prop, string labelText = null, bool includeChildren = false, string tooltipOverride = null)
        {
            if (prop == null || !PropMatchesSearch(prop))
                return;

            EditorGUILayout.PropertyField(prop, PropLabel(prop, labelText, tooltipOverride), includeChildren);
        }

        private void DrawFloatSliderProperty(SerializedProperty prop, string labelText, float leftValue, float rightValue, string tooltipOverride = null)
        {
            if (prop == null || !PropMatchesSearch(prop))
                return;

            var label = PropLabel(prop, labelText, tooltipOverride);

            if (prop.propertyType == SerializedPropertyType.Float)
                EditorGUILayout.Slider(prop, leftValue, rightValue, label);
            else if (prop.propertyType == SerializedPropertyType.Integer)
                EditorGUILayout.IntSlider(prop, Mathf.RoundToInt(leftValue), Mathf.RoundToInt(rightValue), label);
            else
                EditorGUILayout.PropertyField(prop, label);
        }

        private void DrawIntSliderProperty(SerializedProperty prop, string labelText, int leftValue, int rightValue, string tooltipOverride = null)
        {
            if (prop == null || !PropMatchesSearch(prop))
                return;

            var label = PropLabel(prop, labelText, tooltipOverride);

            if (prop.propertyType == SerializedPropertyType.Integer)
                EditorGUILayout.IntSlider(prop, leftValue, rightValue, label);
            else if (prop.propertyType == SerializedPropertyType.Float)
                EditorGUILayout.Slider(prop, leftValue, rightValue, label);
            else
                EditorGUILayout.PropertyField(prop, label);
        }

        // =============================================================================================
        // HEADER RESET (per-section) + COPY UTILS
        // =============================================================================================
        private bool HandleHeaderResetButton(Rect resetRect, string sectionKey)
        {
            if (!CrowFxEditorUI.HeaderResetPill(resetRect, "Reset"))
                return false;

            if (EditorUtility.DisplayDialog("Reset Section",
                $"Reset \"{sectionKey}\" values to defaults?\n\nThis cannot be undone.",
                "Reset", "Cancel"))
            {
                ResetSectionToDefaults(sectionKey);
            }

            return true;
        }

        private void ResetSectionToDefaults(string sectionKey)
        {
            if (!_propsBySection.TryGetValue(sectionKey, out var props) || props == null || props.Count == 0)
                return;

            RestorePreviewStatesIfNeeded();
            ResetPropertiesToDefaults(props, $"Reset {sectionKey}");
        }

        private void ResetPropertiesToDefaults(IReadOnlyList<string> propertyNames, string undoLabel)
        {
            var targetFx = (CrowImageEffects)target;
            if (targetFx == null || propertyNames == null || propertyNames.Count == 0) return;

            Undo.RecordObject(targetFx, undoLabel);

            var tmpGO = new GameObject("CrowImageEffects_Defaults__TEMP") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var tmp = tmpGO.AddComponent<CrowImageEffects>();

                var soDst = serializedObject;
                var soSrc = new SerializedObject(tmp);

                soSrc.Update();

                for (int i = 0; i < propertyNames.Count; i++)
                {
                    var name = propertyNames[i];
                    var dst = soDst.FindProperty(name);
                    var src = soSrc.FindProperty(name);
                    if (dst == null || src == null) continue;

                    CopyPropertyValue(dst, src);
                }

                soDst.ApplyModifiedProperties();
                FinalizeCommittedTargetChange(targetFx);
            }
            finally
            {
                DestroyImmediate(tmpGO);
            }
        }

        private static List<string> GetTopLevelSerializedPropertyNames(SerializedObject serializedObject)
        {
            var names = new List<string>();
            if (serializedObject == null) return names;

            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script" ||
                    iterator.propertyPath == "profile" ||
                    iterator.propertyPath == "autoApplyProfile")
                    continue;

                names.Add(iterator.propertyPath);
            }

            return names;
        }

        private static void CopyPropertyValue(SerializedProperty dst, SerializedProperty src)
        {
            if (dst == null || src == null) return;
            if (dst.propertyType != src.propertyType) return;

            if (dst.isArray && src.isArray && dst.propertyType != SerializedPropertyType.String)
            {
                dst.arraySize = src.arraySize;
                for (int i = 0; i < src.arraySize; i++)
                    CopyPropertyValue(dst.GetArrayElementAtIndex(i), src.GetArrayElementAtIndex(i));
                return;
            }

            switch (dst.propertyType)
            {
                case SerializedPropertyType.Integer: dst.intValue = src.intValue; break;
                case SerializedPropertyType.Boolean: dst.boolValue = src.boolValue; break;
                case SerializedPropertyType.Float: dst.floatValue = src.floatValue; break;
                case SerializedPropertyType.String: dst.stringValue = src.stringValue; break;
                case SerializedPropertyType.Color: dst.colorValue = src.colorValue; break;
                case SerializedPropertyType.ObjectReference: dst.objectReferenceValue = src.objectReferenceValue; break;
                case SerializedPropertyType.LayerMask: dst.intValue = src.intValue; break;
                case SerializedPropertyType.Enum: dst.enumValueIndex = src.enumValueIndex; break;
                case SerializedPropertyType.Vector2: dst.vector2Value = src.vector2Value; break;
                case SerializedPropertyType.Vector3: dst.vector3Value = src.vector3Value; break;
                case SerializedPropertyType.Vector4: dst.vector4Value = src.vector4Value; break;
                case SerializedPropertyType.Vector2Int: dst.vector2IntValue = src.vector2IntValue; break;
                case SerializedPropertyType.Vector3Int: dst.vector3IntValue = src.vector3IntValue; break;
                case SerializedPropertyType.Rect: dst.rectValue = src.rectValue; break;
                case SerializedPropertyType.RectInt: dst.rectIntValue = src.rectIntValue; break;
                case SerializedPropertyType.Bounds: dst.boundsValue = src.boundsValue; break;
                case SerializedPropertyType.BoundsInt: dst.boundsIntValue = src.boundsIntValue; break;
                case SerializedPropertyType.Quaternion: dst.quaternionValue = src.quaternionValue; break;
                case SerializedPropertyType.AnimationCurve: dst.animationCurveValue = src.animationCurveValue; break;
                case SerializedPropertyType.ExposedReference: dst.exposedReferenceValue = src.exposedReferenceValue; break;
                case SerializedPropertyType.ManagedReference: dst.managedReferenceValue = src.managedReferenceValue; break;

                case SerializedPropertyType.Generic:
                default:
                    var srcCopy = src.Copy();
                    var end = srcCopy.GetEndProperty();
                    bool enterChildren = true;

                    while (srcCopy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(srcCopy, end))
                    {
                        enterChildren = false;

                        if (!srcCopy.propertyPath.StartsWith(src.propertyPath, StringComparison.Ordinal))
                            continue;

                        var rel = srcCopy.propertyPath.Substring(src.propertyPath.Length);
                        if (rel.StartsWith(".")) rel = rel.Substring(1);

                        var dstChild = dst.serializedObject.FindProperty(dst.propertyPath + (string.IsNullOrEmpty(rel) ? "" : "." + rel));
                        if (dstChild == null) continue;
                        if (dstChild.propertyType != srcCopy.propertyType) continue;

                        CopyPropertyValue(dstChild, srcCopy);
                    }
                    break;
            }
        }

        private IReadOnlyList<string> GetSectionPropertyNames(string sectionKey)
        {
            return _propsBySection.TryGetValue(sectionKey, out var props) ? props : null;
        }

        private static bool IsPreviewActionSection(string sectionKey)
        {
            for (int i = 0; i < _previewActionSections.Length; i++)
                if (string.Equals(_previewActionSections[i], sectionKey, StringComparison.Ordinal))
                    return true;

            return false;
        }

        private bool SupportsPreviewActions(string sectionKey)
        {
            var props = GetSectionPropertyNames(sectionKey);
            return props != null && props.Count > 0 && IsPreviewActionSection(sectionKey);
        }

        private bool CanPasteSection(string sectionKey)
            => !string.IsNullOrEmpty(_sectionClipboardJson) &&
               string.Equals(_sectionClipboardKey, sectionKey, StringComparison.Ordinal);

        private bool IsSectionPreviewMuted(string sectionKey)
            => _previewMutedSections.ContainsKey(sectionKey);

        private bool IsSectionSoloed(string sectionKey)
            => string.Equals(_soloSectionKey, sectionKey, StringComparison.Ordinal);

        private bool IsSectionMutedBySolo(string sectionKey)
            => _soloMutedSections.ContainsKey(sectionKey);

        private void CopySectionSettings(string sectionKey)
        {
            RestorePreviewStatesIfNeeded();

            _sectionClipboardJson = CaptureSectionJson(sectionKey);
            _sectionClipboardKey = string.IsNullOrEmpty(_sectionClipboardJson) ? null : sectionKey;

            if (!string.IsNullOrEmpty(_sectionClipboardJson))
                GUIUtility.systemCopyBuffer = _sectionClipboardJson;
        }

        private void PasteSectionSettings(string sectionKey)
        {
            if (!CanPasteSection(sectionKey))
                return;

            RestorePreviewStatesIfNeeded();
            ApplySectionJson(sectionKey, _sectionClipboardJson, $"Paste {sectionKey}");
            GUI.FocusControl(null);
        }

        private string CaptureSectionJson(string sectionKey)
        {
            var props = GetSectionPropertyNames(sectionKey);
            if (props == null || props.Count == 0)
                return null;

            var tmpGO = new GameObject("CrowImageEffects_SectionCopy__TEMP") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var tmp = tmpGO.AddComponent<CrowImageEffects>();
                var soDst = new SerializedObject(tmp);
                soDst.Update();

                for (int i = 0; i < props.Count; i++)
                {
                    var src = serializedObject.FindProperty(props[i]);
                    var dst = soDst.FindProperty(props[i]);
                    if (src == null || dst == null) continue;
                    CopyPropertyValue(dst, src);
                }

                soDst.ApplyModifiedProperties();
                return EditorJsonUtility.ToJson(tmp, false);
            }
            finally
            {
                DestroyImmediate(tmpGO);
            }
        }

        private void ApplySectionJson(string sectionKey, string json, string undoLabel, bool recordUndo = true)
        {
            if (string.IsNullOrEmpty(json))
                return;

            var props = GetSectionPropertyNames(sectionKey);
            var targetFx = (CrowImageEffects)target;
            if (props == null || props.Count == 0 || targetFx == null)
                return;

            var tmpGO = new GameObject("CrowImageEffects_SectionPaste__TEMP") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var tmp = tmpGO.AddComponent<CrowImageEffects>();
                EditorJsonUtility.FromJsonOverwrite(json, tmp);

                var soSrc = new SerializedObject(tmp);
                soSrc.Update();

                if (recordUndo)
                    Undo.RecordObject(targetFx, undoLabel);

                for (int i = 0; i < props.Count; i++)
                {
                    var dst = serializedObject.FindProperty(props[i]);
                    var src = soSrc.FindProperty(props[i]);
                    if (dst == null || src == null) continue;
                    CopyPropertyValue(dst, src);
                }

                serializedObject.ApplyModifiedProperties();
                FinalizeCommittedTargetChange(targetFx);
            }
            finally
            {
                DestroyImmediate(tmpGO);
            }
        }

        private void HandleInspectorProfileStateChange(CrowImageEffects targetFx, CrowFXProfile previousProfile, bool previousAutoApplyProfile)
        {
            if (targetFx == null)
                return;

            CrowFXProfileSync.MarkEffectDirty(targetFx);
            EnsureDepthModeIfNeeded(targetFx);

            if (_suspendAutoProfileSync)
                return;

            bool profileChanged = targetFx.profile != previousProfile;
            bool autoApplyChanged = targetFx.autoApplyProfile != previousAutoApplyProfile;

            if (targetFx.profile == null || !targetFx.autoApplyProfile)
                return;

            if (profileChanged || autoApplyChanged)
            {
                CrowFXProfileSync.ApplyToEffect(targetFx.profile, targetFx);
                serializedObject.Update();
                return;
            }

            CrowFXProfileSync.PushFromEffect(targetFx, targetFx.profile);
        }

        private void FinalizeCommittedTargetChange(CrowImageEffects targetFx)
        {
            if (targetFx == null)
                return;

            CrowFXProfileSync.MarkEffectDirty(targetFx);
            EnsureDepthModeIfNeeded(targetFx);

            if (_suspendAutoProfileSync || targetFx.profile == null || !targetFx.autoApplyProfile)
                return;

            CrowFXProfileSync.PushFromEffect(targetFx, targetFx.profile);
        }

        private void WithProfileSyncSuspended(Action action)
        {
            bool previousState = _suspendAutoProfileSync;
            _suspendAutoProfileSync = true;

            try
            {
                action?.Invoke();
            }
            finally
            {
                _suspendAutoProfileSync = previousState;
            }
        }

        private void ToggleSectionPreviewMute(string sectionKey)
        {
            if (!SupportsPreviewActions(sectionKey) || !string.IsNullOrEmpty(_soloSectionKey))
                return;

            if (IsSectionPreviewMuted(sectionKey))
            {
                RestorePreviewOverrides(_previewMutedSections[sectionKey]);
                _previewMutedSections.Remove(sectionKey);
                GUI.FocusControl(null);
                return;
            }

            var overrides = CaptureNeutralPreviewOverrides(sectionKey);
            if (overrides.Count == 0)
                return;

            _previewMutedSections[sectionKey] = overrides;
            ApplyPreviewOverrides(overrides);
            GUI.FocusControl(null);
        }

        private void ToggleSectionPreviewSolo(string sectionKey)
        {
            if (!SupportsPreviewActions(sectionKey))
                return;

            if (IsSectionSoloed(sectionKey))
            {
                RestoreSoloPreviewIfNeeded();
                GUI.FocusControl(null);
                return;
            }

            RestoreSoloPreviewIfNeeded();

            if (IsSectionPreviewMuted(sectionKey))
            {
                RestorePreviewOverrides(_previewMutedSections[sectionKey]);
                _previewMutedSections.Remove(sectionKey);
            }

            _soloSectionKey = sectionKey;
            _soloSectionPreviewOverrides = CaptureActivePreviewOverrides(sectionKey);

            if (_soloSectionPreviewOverrides.Count > 0)
                ApplyPreviewOverrides(_soloSectionPreviewOverrides);

            for (int i = 0; i < _previewActionSections.Length; i++)
            {
                string otherSection = _previewActionSections[i];
                if (string.Equals(otherSection, sectionKey, StringComparison.Ordinal))
                    continue;
                if (!SupportsPreviewActions(otherSection) || IsSectionPreviewMuted(otherSection))
                    continue;

                var overrides = CaptureNeutralPreviewOverrides(otherSection);
                if (overrides.Count == 0)
                    continue;

                _soloMutedSections[otherSection] = overrides;
                ApplyPreviewOverrides(overrides);
            }

            GUI.FocusControl(null);
        }

        private void RestoreSoloPreviewIfNeeded()
        {
            if (_soloMutedSections.Count == 0 && _soloSectionPreviewOverrides == null && string.IsNullOrEmpty(_soloSectionKey))
                return;

            RestorePreviewOverrides(_soloSectionPreviewOverrides);

            foreach (var kv in _soloMutedSections.ToList())
                RestorePreviewOverrides(kv.Value);

            _soloMutedSections.Clear();
            _soloSectionPreviewOverrides = null;
            _soloSectionKey = null;
        }

        private void RestorePreviewStatesIfNeeded()
        {
            RestoreSoloPreviewIfNeeded();

            if (_previewMutedSections.Count == 0)
                return;

            foreach (var kv in _previewMutedSections.ToList())
                RestorePreviewOverrides(kv.Value);

            _previewMutedSections.Clear();
        }

        private void TogglePreviewBypass()
        {
            var targetFx = (CrowImageEffects)target;
            if (targetFx == null)
                return;

            if (!_previewBypassActive)
            {
                _previewBypassOriginalEnabled = targetFx.enabled;
                targetFx.enabled = false;
                _previewBypassActive = true;
            }
            else
            {
                targetFx.enabled = _previewBypassOriginalEnabled;
                _previewBypassActive = false;
            }

            EditorUtility.SetDirty(targetFx);
        }

        private void RestorePreviewBypassIfNeeded()
        {
            if (!_previewBypassActive)
                return;

            var targetFx = (CrowImageEffects)target;
            if (targetFx != null)
            {
                targetFx.enabled = _previewBypassOriginalEnabled;
                EditorUtility.SetDirty(targetFx);
            }

            _previewBypassActive = false;
        }

        private List<PreviewPropertyOverride> CaptureNeutralPreviewOverrides(string sectionKey)
        {
            var overrides = new List<PreviewPropertyOverride>(4);

            switch (sectionKey)
            {
                case SectionKeys.Sampling:
                    CollectIntPreviewOverride(overrides, "pixelSize", 1);
                    CollectBoolPreviewOverride(overrides, "useVirtualGrid", false);
                    break;
                case SectionKeys.Pregrade:
                    CollectBoolPreviewOverride(overrides, "pregradeEnabled", false);
                    break;
                case SectionKeys.Palette:
                    CollectBoolPreviewOverride(overrides, "usePalette", false);
                    break;
                case SectionKeys.TextureMask:
                    CollectBoolPreviewOverride(overrides, "useMask", false);
                    break;
                case SectionKeys.DepthMask:
                    CollectBoolPreviewOverride(overrides, "useDepthMask", false);
                    break;
                case SectionKeys.Jitter:
                    CollectBoolPreviewOverride(overrides, "jitterEnabled", false);
                    CollectFloatPreviewOverride(overrides, "jitterStrength", 0f);
                    break;
                case SectionKeys.Bleed:
                    CollectFloatPreviewOverride(overrides, "bleedBlend", 0f);
                    CollectFloatPreviewOverride(overrides, "bleedIntensity", 0f);
                    break;
                case SectionKeys.Ghost:
                    CollectBoolPreviewOverride(overrides, "ghostEnabled", false);
                    CollectFloatPreviewOverride(overrides, "ghostBlend", 0f);
                    break;
                case SectionKeys.Edges:
                    CollectBoolPreviewOverride(overrides, "edgeEnabled", false);
                    CollectFloatPreviewOverride(overrides, "edgeBlend", 0f);
                    break;
                case SectionKeys.Unsharp:
                    CollectBoolPreviewOverride(overrides, "unsharpEnabled", false);
                    CollectFloatPreviewOverride(overrides, "unsharpAmount", 0f);
                    break;
                case SectionKeys.Dither:
                    CollectEnumPreviewOverride(overrides, "ditherMode", (int)CrowImageEffects.DitherMode.None);
                    CollectFloatPreviewOverride(overrides, "ditherStrength", 0f);
                    break;
            }

            return overrides;
        }

        private List<PreviewPropertyOverride> CaptureActivePreviewOverrides(string sectionKey)
        {
            var overrides = new List<PreviewPropertyOverride>(4);

            switch (sectionKey)
            {
                case SectionKeys.Sampling:
                    if (GetInt("pixelSize") <= 1 && !GetBool("useVirtualGrid"))
                        CollectIntPreviewOverride(overrides, "pixelSize", 4);
                    break;
                case SectionKeys.Pregrade:
                    CollectBoolPreviewOverride(overrides, "pregradeEnabled", true);
                    break;
                case SectionKeys.Palette:
                    if (GetObject("paletteTex") != null)
                        CollectBoolPreviewOverride(overrides, "usePalette", true);
                    break;
                case SectionKeys.TextureMask:
                    if (GetObject("maskTex") != null)
                        CollectBoolPreviewOverride(overrides, "useMask", true);
                    break;
                case SectionKeys.DepthMask:
                    CollectBoolPreviewOverride(overrides, "useDepthMask", true);
                    break;
                case SectionKeys.Jitter:
                    CollectBoolPreviewOverride(overrides, "jitterEnabled", true);
                    if (GetFloat("jitterStrength") <= 0f)
                        CollectFloatPreviewOverride(overrides, "jitterStrength", 0.35f);
                    break;
                case SectionKeys.Bleed:
                    if (GetFloat("bleedBlend") <= 0f)
                        CollectFloatPreviewOverride(overrides, "bleedBlend", 0.35f);
                    if (GetFloat("bleedIntensity") <= 0f)
                        CollectFloatPreviewOverride(overrides, "bleedIntensity", 1f);
                    break;
                case SectionKeys.Ghost:
                    CollectBoolPreviewOverride(overrides, "ghostEnabled", true);
                    if (GetFloat("ghostBlend") <= 0f)
                        CollectFloatPreviewOverride(overrides, "ghostBlend", 0.35f);
                    break;
                case SectionKeys.Edges:
                    CollectBoolPreviewOverride(overrides, "edgeEnabled", true);
                    if (GetFloat("edgeBlend") <= 0f)
                        CollectFloatPreviewOverride(overrides, "edgeBlend", 1f);
                    break;
                case SectionKeys.Unsharp:
                    CollectBoolPreviewOverride(overrides, "unsharpEnabled", true);
                    if (GetFloat("unsharpAmount") <= 0f)
                        CollectFloatPreviewOverride(overrides, "unsharpAmount", 0.5f);
                    break;
                case SectionKeys.Dither:
                    if (GetEnum("ditherMode") == (int)CrowImageEffects.DitherMode.None)
                        CollectEnumPreviewOverride(overrides, "ditherMode", (int)CrowImageEffects.DitherMode.Ordered4x4);
                    if (GetFloat("ditherStrength") <= 0f)
                        CollectFloatPreviewOverride(overrides, "ditherStrength", 0.45f);
                    break;
            }

            return overrides;
        }

        private void ApplySerializedChanges()
        {
            var targetFx = (CrowImageEffects)target;
            bool changed = serializedObject.ApplyModifiedProperties();
            if (targetFx == null)
                return;

            if (changed)
                FinalizeCommittedTargetChange(targetFx);
            else
            {
                CrowFXProfileSync.MarkEffectDirty(targetFx);
                EnsureDepthModeIfNeeded(targetFx);
            }
        }

        private void ApplyPreviewOverrides(List<PreviewPropertyOverride> overrides)
        {
            if (overrides == null || overrides.Count == 0)
                return;

            for (int i = 0; i < overrides.Count; i++)
                SetPreviewOverrideValue(overrides[i], applyPreviewValue: true);

            WithProfileSyncSuspended(ApplySerializedChanges);
        }

        private void RestorePreviewOverrides(List<PreviewPropertyOverride> overrides)
        {
            if (overrides == null || overrides.Count == 0)
                return;

            bool restoredAny = false;
            for (int i = 0; i < overrides.Count; i++)
            {
                if (!CurrentValueMatchesPreview(overrides[i]))
                    continue;

                SetPreviewOverrideValue(overrides[i], applyPreviewValue: false);
                restoredAny = true;
            }

            if (restoredAny)
                WithProfileSyncSuspended(ApplySerializedChanges);
        }

        private void CollectBoolPreviewOverride(List<PreviewPropertyOverride> overrides, string propertyName, bool previewValue)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop == null || prop.propertyType != SerializedPropertyType.Boolean || prop.boolValue == previewValue)
                return;

            overrides.Add(new PreviewPropertyOverride
            {
                PropertyName = propertyName,
                PropertyType = SerializedPropertyType.Boolean,
                BoolOriginalValue = prop.boolValue,
                BoolPreviewValue = previewValue
            });
        }

        private void CollectFloatPreviewOverride(List<PreviewPropertyOverride> overrides, string propertyName, float previewValue)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop == null)
                return;

            if (prop.propertyType == SerializedPropertyType.Float)
            {
                if (Mathf.Approximately(prop.floatValue, previewValue))
                    return;

                overrides.Add(new PreviewPropertyOverride
                {
                    PropertyName = propertyName,
                    PropertyType = SerializedPropertyType.Float,
                    FloatOriginalValue = prop.floatValue,
                    FloatPreviewValue = previewValue
                });
            }
            else if (prop.propertyType == SerializedPropertyType.Integer)
            {
                int previewInt = Mathf.RoundToInt(previewValue);
                if (prop.intValue == previewInt)
                    return;

                overrides.Add(new PreviewPropertyOverride
                {
                    PropertyName = propertyName,
                    PropertyType = SerializedPropertyType.Integer,
                    IntOriginalValue = prop.intValue,
                    IntPreviewValue = previewInt
                });
            }
        }

        private void CollectIntPreviewOverride(List<PreviewPropertyOverride> overrides, string propertyName, int previewValue)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop == null || prop.propertyType != SerializedPropertyType.Integer || prop.intValue == previewValue)
                return;

            overrides.Add(new PreviewPropertyOverride
            {
                PropertyName = propertyName,
                PropertyType = SerializedPropertyType.Integer,
                IntOriginalValue = prop.intValue,
                IntPreviewValue = previewValue
            });
        }

        private void CollectEnumPreviewOverride(List<PreviewPropertyOverride> overrides, string propertyName, int previewValue)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop == null || prop.propertyType != SerializedPropertyType.Enum || prop.enumValueIndex == previewValue)
                return;

            overrides.Add(new PreviewPropertyOverride
            {
                PropertyName = propertyName,
                PropertyType = SerializedPropertyType.Enum,
                IntOriginalValue = prop.enumValueIndex,
                IntPreviewValue = previewValue
            });
        }

        private bool CurrentValueMatchesPreview(PreviewPropertyOverride previewOverride)
        {
            if (previewOverride == null)
                return false;

            var prop = serializedObject.FindProperty(previewOverride.PropertyName);
            if (prop == null || prop.propertyType != previewOverride.PropertyType)
                return false;

            return prop.propertyType switch
            {
                SerializedPropertyType.Boolean => prop.boolValue == previewOverride.BoolPreviewValue,
                SerializedPropertyType.Float => Mathf.Approximately(prop.floatValue, previewOverride.FloatPreviewValue),
                SerializedPropertyType.Integer => prop.intValue == previewOverride.IntPreviewValue,
                SerializedPropertyType.Enum => prop.enumValueIndex == previewOverride.IntPreviewValue,
                _ => false
            };
        }

        private void SetPreviewOverrideValue(PreviewPropertyOverride previewOverride, bool applyPreviewValue)
        {
            if (previewOverride == null)
                return;

            var prop = serializedObject.FindProperty(previewOverride.PropertyName);
            if (prop == null || prop.propertyType != previewOverride.PropertyType)
                return;

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    prop.boolValue = applyPreviewValue ? previewOverride.BoolPreviewValue : previewOverride.BoolOriginalValue;
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = applyPreviewValue ? previewOverride.FloatPreviewValue : previewOverride.FloatOriginalValue;
                    break;
                case SerializedPropertyType.Integer:
                    prop.intValue = applyPreviewValue ? previewOverride.IntPreviewValue : previewOverride.IntOriginalValue;
                    break;
                case SerializedPropertyType.Enum:
                    prop.enumValueIndex = applyPreviewValue ? previewOverride.IntPreviewValue : previewOverride.IntOriginalValue;
                    break;
            }
        }

        private void SetBool(string propertyName, bool value)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Boolean)
                prop.boolValue = value;
        }

        private void SetFloat(string propertyName, float value)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop == null) return;

            if (prop.propertyType == SerializedPropertyType.Float)
                prop.floatValue = value;
            else if (prop.propertyType == SerializedPropertyType.Integer)
                prop.intValue = Mathf.RoundToInt(value);
        }

        private void SetInt(string propertyName, int value)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Integer)
                prop.intValue = value;
        }

        private void SetVector2(string propertyName, Vector2 value)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Vector2)
                prop.vector2Value = value;
        }

        private void SetEnum(string propertyName, int value)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Enum)
                prop.enumValueIndex = value;
        }

        private bool GetBool(string propertyName)
        {
            var prop = serializedObject.FindProperty(propertyName);
            return prop != null && prop.propertyType == SerializedPropertyType.Boolean && prop.boolValue;
        }

        private float GetFloat(string propertyName)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop == null) return 0f;
            return prop.propertyType switch
            {
                SerializedPropertyType.Float => prop.floatValue,
                SerializedPropertyType.Integer => prop.intValue,
                _ => 0f
            };
        }

        private int GetInt(string propertyName)
        {
            var prop = serializedObject.FindProperty(propertyName);
            return prop != null && prop.propertyType == SerializedPropertyType.Integer ? prop.intValue : 0;
        }

        private int GetEnum(string propertyName)
        {
            var prop = serializedObject.FindProperty(propertyName);
            return prop != null && prop.propertyType == SerializedPropertyType.Enum ? prop.enumValueIndex : 0;
        }

        private UnityEngine.Object GetObject(string propertyName)
        {
            var prop = serializedObject.FindProperty(propertyName);
            return prop != null && prop.propertyType == SerializedPropertyType.ObjectReference ? prop.objectReferenceValue : null;
        }

        // =============================================================================================
        // CUSTOM SECTION DRAWERS
        // =============================================================================================

        private Action ResolveCustomDrawerOrNull(string sectionKey)
        {
            return sectionKey switch
            {
                SectionKeys.Master      => DrawMasterContent,
                SectionKeys.Pregrade    => DrawPregradeContent,
                SectionKeys.Sampling    => DrawSamplingContent,
                SectionKeys.Posterize   => DrawPosterizeContent,
                SectionKeys.Palette     => DrawPaletteContent,
                SectionKeys.TextureMask => DrawMaskingContent,
                SectionKeys.DepthMask   => DrawDepthMaskContent,
                SectionKeys.Jitter      => DrawJitterContent,
                SectionKeys.Bleed       => DrawBleedContent,
                SectionKeys.Ghost       => DrawGhostContent,
                SectionKeys.Edges       => DrawEdgeContent,
                SectionKeys.Unsharp     => DrawUnsharpContent,
                SectionKeys.Dither      => DrawDitherContent,
                SectionKeys.Shaders     => DrawShadersContent,
                _             => null
            };
        }

        private void DrawMasterContent()
        {
            BeginSectionDrawn();

            var masterBlend = SP("masterBlend");
            var profile = SP("profile");
            var autoApplyProfile = SP("autoApplyProfile");
            if (PropMatchesSearch(masterBlend))
                DrawPropertyField(masterBlend, "Master Blend");

            DrawProfileControls(profile, autoApplyProfile);

            // FIX: always claim it (whether drawn or not) so auto-draw can't duplicate it.
            MarkDrawnMany("masterBlend", "profile", "autoApplyProfile");

            if (string.IsNullOrWhiteSpace(_search) || PassesSearch("opacity global master"))
            {
                GUILayout.Space(6);
                CrowFxEditorUI.Hint("Global opacity for the entire effect stack. Does not affect internal parameters.");
            }

            DrawAutoRemaining(SectionKeys.Master);

            if (string.IsNullOrWhiteSpace(_search) || PassesSearch("workflow bar reset randomize bypass"))
            {
                GUILayout.Space(6);
                CrowFxEditorUI.Hint("Stack-wide reset, randomize, and bypass live in the workflow bar above so this section can stay focused on blend and profiles.");
            }
        }

        private void DrawProfileControls(SerializedProperty profileProp, SerializedProperty autoApplyProp)
        {
            bool showProfileUi =
                string.IsNullOrWhiteSpace(_search) ||
                AnyMatch(profileProp, autoApplyProp) ||
                PassesSearch("profile preset shared settings asset apply save create sync");

            if (!showProfileUi)
                return;

            GUILayout.Space(8);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (profileProp != null && PropMatchesSearch(profileProp))
                    DrawPropertyField(profileProp, "Profile");

                var profileAsset = profileProp?.objectReferenceValue as CrowFXProfile;

                if (profileAsset != null)
                {
                    if (autoApplyProp != null && PropMatchesSearch(autoApplyProp))
                        DrawPropertyField(autoApplyProp, "Auto Sync Profile");

                    GUILayout.Space(4);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool autoSyncEnabled = autoApplyProp != null && autoApplyProp.boolValue;

                        if (!autoSyncEnabled)
                        {
                            if (CrowFxEditorUI.MiniPill("Apply Profile", GUILayout.ExpandWidth(true)))
                                ApplyAssignedProfileToTarget();

                            if (CrowFxEditorUI.MiniPill("Save to Profile", GUILayout.ExpandWidth(true)))
                                SaveTargetToAssignedProfile();
                        }

                        if (CrowFxEditorUI.MiniPill("New Profile", GUILayout.ExpandWidth(true)))
                            CreateProfileFromCurrentSettings();
                    }

                    if (string.IsNullOrWhiteSpace(_search) || PassesSearch("shared preset asset"))
                    {
                        CrowFxEditorUI.Hint(
                            autoApplyProp != null && autoApplyProp.boolValue
                                ? "Live sync is on. Editing this camera updates the profile and every linked camera that also has Auto Sync Profile enabled."
                                : "Manual mode is on. Use Apply to pull from the shared profile or Save to push this camera back into it.");
                    }
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (CrowFxEditorUI.MiniPill("Create Profile", GUILayout.Width(140f)))
                            CreateProfileFromCurrentSettings();
                        GUILayout.FlexibleSpace();
                    }

                    if (string.IsNullOrWhiteSpace(_search) || PassesSearch("shared preset asset"))
                        CrowFxEditorUI.Hint("Assign or create a profile to reuse settings across cameras.");
                }
            }
        }

        private void ApplyAssignedProfileToTarget()
        {
            var targetFx = (CrowImageEffects)target;
            var profile = SP("profile")?.objectReferenceValue as CrowFXProfile;
            if (targetFx == null || profile == null) return;

            RestorePreviewStatesIfNeeded();
            CrowFXProfileSync.ApplyToEffect(profile, targetFx);
            serializedObject.Update();
            GUI.FocusControl(null);
        }

        private void SaveTargetToAssignedProfile()
        {
            var targetFx = (CrowImageEffects)target;
            var profile = SP("profile")?.objectReferenceValue as CrowFXProfile;
            if (targetFx == null || profile == null) return;

            RestorePreviewStatesIfNeeded();
            CrowFXProfileSync.PushFromEffect(targetFx, profile);
            AssetDatabase.SaveAssets();
            GUI.FocusControl(null);
        }

        private void CreateProfileFromCurrentSettings()
        {
            var targetFx = (CrowImageEffects)target;
            if (targetFx == null) return;

            RestorePreviewStatesIfNeeded();
            string path = EditorUtility.SaveFilePanelInProject(
                "Create CrowFX Profile",
                "CrowFXProfile",
                "asset",
                "Choose where to save the new CrowFX profile.");

            if (string.IsNullOrEmpty(path))
                return;

            var asset = ScriptableObject.CreateInstance<CrowFXProfile>();
            targetFx.SaveToProfile(asset);

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            var profileProp = SP("profile");
            if (profileProp != null)
            {
                profileProp.objectReferenceValue = asset;
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            EditorGUIUtility.PingObject(asset);
            GUI.FocusControl(null);
        }

        private Dictionary<string, FieldInfo> _fieldCache;

        private Dictionary<string, FieldInfo> GetFieldCache()
        {
            if (_fieldCache != null) return _fieldCache;
            _fieldCache = typeof(CrowImageEffects)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .ToDictionary(f => f.Name, StringComparer.Ordinal);
            return _fieldCache;
        }

        private void RandomizeSectionProperties(string sectionKey)
        {
            var targetFx = (CrowImageEffects)target;
            if (targetFx == null) return;

            if (!_propsBySection.TryGetValue(sectionKey, out var props) || props.Count == 0)
                return;

            RestorePreviewStatesIfNeeded();
            Undo.RecordObject(targetFx, $"Randomize {sectionKey}");
            RandomizeProps(props);
            serializedObject.ApplyModifiedProperties();
            FinalizeCommittedTargetChange(targetFx);
        }

        private void RandomizeAllProperties()
        {
            var targetFx = (CrowImageEffects)target;
            if (targetFx == null) return;

            RestorePreviewStatesIfNeeded();
            Undo.RecordObject(targetFx, "Randomize All");

            foreach (var props in _propsBySection.Values)
                RandomizeProps(props);

            serializedObject.ApplyModifiedProperties();
            FinalizeCommittedTargetChange(targetFx);
        }

        private void RandomizeProps(List<string> props)
        {
            var fields = GetFieldCache();

            foreach (var name in props)
            {
                var p = serializedObject.FindProperty(name);
                if (p == null) continue;

                RangeAttribute range = null;
                if (fields.TryGetValue(name, out var field))
                    range = field.GetCustomAttribute<RangeAttribute>();

                RandomizeProperty(p, range);
            }
        }

        private static void RandomizeProperty(SerializedProperty p, RangeAttribute range)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Float:
                    p.floatValue = range != null
                        ? UnityEngine.Random.Range(range.min, range.max)
                        : UnityEngine.Random.Range(0f, Mathf.Max(1f, Mathf.Abs(p.floatValue) * 2f));
                    break;

                case SerializedPropertyType.Integer:
                    p.intValue = range != null
                        ? UnityEngine.Random.Range((int)range.min, (int)range.max + 1)
                        : UnityEngine.Random.Range(0, Mathf.Max(4, Mathf.Abs(p.intValue) * 2));
                    break;

                case SerializedPropertyType.Boolean:
                    p.boolValue = UnityEngine.Random.value > 0.5f;
                    break;

                case SerializedPropertyType.Vector2:
                    p.vector2Value = range != null
                        ? new Vector2(UnityEngine.Random.Range(range.min, range.max),
                                    UnityEngine.Random.Range(range.min, range.max))
                        : UnityEngine.Random.insideUnitCircle;
                    break;

                case SerializedPropertyType.Color:
                    p.colorValue = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                    break;
            }
        }
        private void DrawPregradeContent()
        {
            BeginSectionDrawn();

            var pregradeEnabled = SP("pregradeEnabled");
            var exposure = SP("exposure");
            var contrast = SP("contrast");
            var gamma = SP("gamma");
            var saturation = SP("saturation");

            if (PropMatchesSearch(pregradeEnabled))
                DrawPropertyField(pregradeEnabled, "Enable Pregrade");

            bool enabled = pregradeEnabled != null && pregradeEnabled.boolValue;
            if (enabled)
            {
                DrawPropertyField(exposure, "Exposure");
                DrawPropertyField(contrast, "Contrast");
                DrawPropertyField(gamma, "Gamma");
                DrawPropertyField(saturation, "Saturation");
            }

            MarkDrawnMany("pregradeEnabled", "exposure", "contrast", "gamma", "saturation");

            if (!enabled)
            {
                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("pregrade quantization exposure contrast gamma saturation"))
                    CrowFxEditorUI.Hint("Enable Pregrade to adjust the image before quantization.");
            }
            else if (string.IsNullOrWhiteSpace(_search) || PassesSearch("pregrade quantization exposure contrast gamma saturation"))
            {
                GUILayout.Space(6);
                CrowFxEditorUI.Hint("Applied before quantization.");
            }

            DrawAutoRemaining(SectionKeys.Pregrade);
        }

        private void DrawSamplingContent()
        {
            BeginSectionDrawn();

            var pixelSize = SP("pixelSize");
            var useVirtualGrid = SP("useVirtualGrid");
            var virtualResolution = SP("virtualResolution");

            if (PropMatchesSearch(pixelSize))
                DrawPropertyField(pixelSize, "Pixel Block Size");

            GUILayout.Space(6);

            if (PropMatchesSearch(useVirtualGrid))
                DrawPropertyField(useVirtualGrid, "Lock to Virtual Grid");

            bool grid = useVirtualGrid != null && useVirtualGrid.boolValue;

            if (grid)
            {
                DrawPropertyField(virtualResolution, "Virtual Resolution");

                GUILayout.Space(6);

                bool showPresets = string.IsNullOrWhiteSpace(_search) || PassesSearch("resolution preset grid virtual 240 288 320 360 448 480 576 720 1080 160 200 224 256 300 384 400 512 600 768 854 960 1024 1366");
                if (showPresets)
                {
                    DrawSubSection(
                        title: "Resolution Presets",
                        icon: "d_GridLayoutGroup Icon",
                        fold: _foldResolutionPresets,
                        hint: "Quick set",
                        drawContent: DrawResolutionPresets
                    );

                    GUILayout.Space(6);
                    CrowFxEditorUI.Hint("Fixes sampling to a stable grid, preventing resolution-dependent flickering.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("screen backbuffer shimmer resize"))
                    CrowFxEditorUI.Hint("Off = sampling follows the backbuffer resolution (may cause shimmering on resize).");
            }

            MarkDrawnMany("pixelSize", "useVirtualGrid", "virtualResolution");

            DrawAutoRemaining(SectionKeys.Sampling);
        }

        private void DrawPosterizeContent()
        {
            BeginSectionDrawn();

            var usePerChannel = SP("usePerChannel");
            var levels = SP("levels");
            var levelsR = SP("levelsR");
            var levelsG = SP("levelsG");
            var levelsB = SP("levelsB");

            var animateLevels = SP("animateLevels");
            var minLevels = SP("minLevels");
            var maxLevels = SP("maxLevels");
            var speed = SP("speed");

            var luminanceOnly = SP("luminanceOnly");
            var invert = SP("invert");

            if (PropMatchesSearch(usePerChannel))
                DrawPropertyField(usePerChannel, "Per-Channel Levels");

            bool perCh = usePerChannel != null && usePerChannel.boolValue;

            if (perCh)
            {
                DrawIntSliderProperty(levelsR, "Red Levels", 2, 512);
                DrawIntSliderProperty(levelsG, "Green Levels", 2, 512);
                DrawIntSliderProperty(levelsB, "Blue Levels", 2, 512);

                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("per-channel quantization color shifting"))
                    CrowFxEditorUI.Hint("Separate quantization per channel can create color-shifting effects.");
            }
            else
            {
                DrawIntSliderProperty(levels, "Shared Levels", 2, 512);

                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("banding gradients quantization levels"))
                    CrowFxEditorUI.Hint("Lower values create more pronounced banding. Higher = smoother gradients.");
            }

            GUILayout.Space(8);

            DrawPropertyField(luminanceOnly, "Luminance Only");
            DrawPropertyField(invert, "Invert Output");

            GUILayout.Space(8);

            DrawPropertyField(animateLevels, "Animate Levels");

            bool anim = animateLevels != null && animateLevels.boolValue;
            if (anim)
            {
                float min = minLevels != null ? minLevels.intValue : 2;
                float max = maxLevels != null ? maxLevels.intValue : 2;

                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("range min max slider animate"))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.MinMaxSlider(
                        new GUIContent(
                            $"Range ({min:0}-{max:0})",
                            "Adjust the animated quantization range. Min Levels and Max Levels below expose the same values individually."),
                        ref min,
                        ref max,
                        2f,
                        512f);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (minLevels != null) { minLevels.intValue = Mathf.Clamp(Mathf.RoundToInt(min), 2, 512); }
                        if (maxLevels != null) { maxLevels.intValue = Mathf.Clamp(Mathf.RoundToInt(max), 2, 512); }
                    }

                    DrawIntSliderProperty(minLevels, "Min Levels", 2, 512);
                    DrawIntSliderProperty(maxLevels, "Max Levels", 2, 512);

                    DrawPropertyField(speed, "Cycle Speed");

                    if (string.IsNullOrWhiteSpace(_search) || PassesSearch("retro shimmer cycles"))
                        CrowFxEditorUI.Hint("Cycles quantization levels over time for a retro shimmer effect.");
                }
            }

            DrawPosterizeMiniPreview(
                perChannel: perCh,
                levelsValue: levels != null ? levels.intValue : 64,
                levelsRValue: levelsR != null ? levelsR.intValue : 64,
                levelsGValue: levelsG != null ? levelsG.intValue : 64,
                levelsBValue: levelsB != null ? levelsB.intValue : 64,
                animateLevelsEnabled: anim,
                minLevelsValue: minLevels != null ? minLevels.intValue : 2,
                maxLevelsValue: maxLevels != null ? maxLevels.intValue : 2,
                speedValue: speed != null ? speed.floatValue : 1f);

            if (anim && perCh && (string.IsNullOrWhiteSpace(_search) || PassesSearch("shared levels animation per-channel")))
                CrowFxEditorUI.Hint("Per-Channel Levels overrides Animated Levels in the live effect, so the preview stays on the R/G/B strips while both are enabled.");

            if (anim && !perCh)
                Repaint();

            // FIX: claim *all* of these always, so the "other branch" never leaks into auto-draw
            MarkDrawnMany(
                "usePerChannel",
                "levels", "levelsR", "levelsG", "levelsB",
                "luminanceOnly", "invert",
                "animateLevels", "minLevels", "maxLevels", "speed"
            );

            DrawAutoRemaining(SectionKeys.Posterize);
        }

        private void DrawPaletteContent()
        {
            BeginSectionDrawn();

            var thresholdCurve = SP("thresholdCurve");
            var usePalette = SP("usePalette");
            var paletteTex = SP("paletteTex");

            GUILayout.Space(6);

            if (PropMatchesSearch(usePalette))
                DrawPropertyField(usePalette, "Enable Palette Mapping");

            if (usePalette != null && usePalette.boolValue)
            {
                DrawPropertyField(thresholdCurve, "Threshold Curve");

                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("curve tonal remap palette lookup"))
                    CrowFxEditorUI.Hint("Remaps tonal range before palette lookup. Use to bias towards lights or darks.");

                DrawPropertyField(paletteTex, "Palette Texture");

                if (paletteTex != null && paletteTex.objectReferenceValue == null)
                {
                    if (string.IsNullOrWhiteSpace(_search) || PassesSearch("missing texture palette"))
                        DrawActionHint("Palette mapping is enabled but no palette texture is assigned.", "Disable Palette",
                            () =>
                            {
                                usePalette.boolValue = false;
                                ApplySerializedChanges();
                            },
                            CrowFxEditorUI.HintType.Warning);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(_search) || PassesSearch("maps final colors palette"))
                        CrowFxEditorUI.Hint("Maps final colors through the provided palette texture.");
                }
            }
            else if (string.IsNullOrWhiteSpace(_search) || PassesSearch("palette mapping threshold curve"))
            {
                CrowFxEditorUI.Hint("Enable Palette Mapping to reveal the palette texture and tone-remap controls.");
            }

            MarkDrawnMany("thresholdCurve", "usePalette", "paletteTex");
            DrawAutoRemaining(SectionKeys.Palette);
        }

        private void DrawMaskingContent()
        {
            BeginSectionDrawn();

            var useMask = SP("useMask");
            var maskTex = SP("maskTex");
            var maskThreshold = SP("maskThreshold");

            if (PropMatchesSearch(useMask))
                DrawPropertyField(useMask, "Enable Texture Mask");

            if (useMask != null && useMask.boolValue)
            {
                DrawPropertyField(maskTex, "Mask Texture");
                DrawPropertyField(maskThreshold, "Mask Threshold");

                if (maskTex != null && maskTex.objectReferenceValue == null)
                {
                    DrawActionHint("Texture mask is enabled but the mask texture is missing.", "Disable Mask",
                        () =>
                        {
                            useMask.boolValue = false;
                            ApplySerializedChanges();
                        },
                        CrowFxEditorUI.HintType.Warning);
                }
                else
                    CrowFxEditorUI.Hint("White = effect applied, Black = original image (threshold determines cutoff).");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("grayscale texture selectively apply ui safe"))
                    CrowFxEditorUI.Hint("Use a grayscale texture to selectively apply effects (great for UI-safe zones).");
            }

            MarkDrawnMany("useMask", "maskTex", "maskThreshold");
            DrawAutoRemaining(SectionKeys.TextureMask);
        }

        private void DrawDepthMaskContent()
        {
            BeginSectionDrawn();

            var useDepthMask = SP("useDepthMask");
            var depthThreshold = SP("depthThreshold");

            if (PropMatchesSearch(useDepthMask))
                DrawPropertyField(useDepthMask, "Enable Depth Mask");

            if (useDepthMask != null && useDepthMask.boolValue)
            {
                DrawPropertyField(depthThreshold, "Depth Threshold");

                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("attenuates distance depth texture"))
                    CrowFxEditorUI.Hint("Attenuates effects based on distance from camera. Automatically enables depth texture.");

                if (NeedsDepthFix((CrowImageEffects)target))
                {
                    DrawActionHint("Camera depth texture is still off for this camera.", "Enable Depth",
                        EnableDepthOnTarget,
                        CrowFxEditorUI.HintType.Warning);
                }
            }

            MarkDrawnMany("useDepthMask", "depthThreshold");
            DrawAutoRemaining(SectionKeys.DepthMask);
        }

        private void DrawJitterContent()
        {
            BeginSectionDrawn();

            var pEnabled  = SP("jitterEnabled");
            var pStrength = SP("jitterStrength");
            var pAmountPx = SP("jitterAmountPx");
            var pMode     = SP("jitterMode");
            var pSpeed    = SP("jitterSpeed");
            var pUseSeed  = SP("jitterUseSeed");
            var pSeed     = SP("jitterSeed");
            var pScanline = SP("jitterScanline");
            var pNoiseTex = SP("jitterNoiseTex");
            var pClampUV  = SP("jitterClampUV");
            var pWeights  = SP("jitterChannelWeights");
            var pDirR     = SP("jitterDirR");
            var pDirG     = SP("jitterDirG");
            var pDirB     = SP("jitterDirB");

            if (PropMatchesSearch(pEnabled))
                DrawPropertyField(pEnabled, "Enable Jitter");

            bool enabled = pEnabled != null && pEnabled.boolValue;

            if (enabled)
            {
                GUILayout.Space(6);
                DrawJitterStrengthAndAmount(pStrength, pAmountPx);
                GUILayout.Space(6);
                DrawJitterModeAndSpeed(pMode, pSpeed, pNoiseTex);
                GUILayout.Space(6);
                DrawJitterHashNoiseControls(pMode);
                GUILayout.Space(6);
                DrawJitterSeed(pUseSeed, pSeed);
                GUILayout.Space(6);
                DrawJitterScanline(pScanline);
                GUILayout.Space(6);

                DrawPropertyField(pClampUV, "Clamp UV");

                bool showAdvanced = string.IsNullOrWhiteSpace(_search)
                                    || PassesSearch("advanced weights dir direction channel")
                                    || AnyMatch(pWeights, pDirR, pDirG, pDirB);

                if (showAdvanced)
                {
                    GUILayout.Space(8);
                    DrawSubSection("Advanced", "d_ToolHandleGlobal", _foldJitterAdvanced,
                        () => DrawJitterAdvanced(pWeights, pDirR, pDirG, pDirB), "weights + dirs");
                }

                if (string.IsNullOrWhiteSpace(_search))
                {
                    GUILayout.Space(6);
                    CrowFxEditorUI.Hint(enabled
                        ? "Strength blends between base and jittered sampling. Amount (px) is the actual offset scale."
                        : "Enable to apply subtle per-channel sampling offsets.");
                }
            }
            else if (string.IsNullOrWhiteSpace(_search) || PassesSearch("channel jitter rgb offset"))
            {
                CrowFxEditorUI.Hint("Enable Jitter to reveal motion, seed, scanline, and per-channel controls.");
            }

            MarkDrawnMany(
                "jitterEnabled", "jitterStrength", "jitterAmountPx", "jitterMode", "jitterSpeed",
                "jitterUseSeed", "jitterSeed",
                "jitterScanline", "jitterScanlineDensity", "jitterScanlineAmp",
                "jitterChannelWeights", "jitterDirR", "jitterDirG", "jitterDirB",
                "jitterNoiseTex", "jitterClampUV",
                "jitterHashCellCount",
                "jitterHashTimeSmooth",
                "jitterHashRotateDeg",
                "jitterHashAniso",
                "jitterHashWarpAmpPx",
                "jitterHashWarpCells",
                "jitterHashWarpSpeed",
                "jitterHashPerChannel"
            );

            DrawAutoRemaining(SectionKeys.Jitter);
        }

        private void DrawJitterStrengthAndAmount(SerializedProperty pStrength, SerializedProperty pAmountPx)
        {
            DrawFloatSliderProperty(pStrength, "Jitter Strength", 0f, 1f);
            DrawFloatSliderProperty(pAmountPx, "Offset Scale (px)", 0f, 8f);
        }

        private void DrawJitterModeAndSpeed(SerializedProperty pMode, SerializedProperty pSpeed, SerializedProperty pNoiseTex)
        {
            DrawPropertyField(pMode, "Jitter Mode");

            bool showSpeed = pMode == null || pMode.enumValueIndex != 0;
            if (showSpeed && pSpeed != null && PropMatchesSearch(pSpeed))
            {
                DrawFloatSliderProperty(pSpeed, "Speed", 0f, 20f);
            }

            bool needsNoise = pMode != null && pMode.enumValueIndex == 3;
            if (!needsNoise) return;

            GUILayout.Space(6);
            DrawPropertyField(pNoiseTex, "Blue Noise Texture");

            bool showTextureHints = string.IsNullOrWhiteSpace(_search) || PassesSearch("blue noise texture 128x128 square");
            if (!showTextureHints)
                return;

            if (pNoiseTex != null && pNoiseTex.objectReferenceValue == null)
            {
                DrawActionHint("BlueNoiseTex mode needs a texture source.", "Use HashNoise",
                    () =>
                    {
                        if (pMode != null) pMode.enumValueIndex = (int)CrowImageEffects.JitterMode.HashNoise;
                        ApplySerializedChanges();
                    },
                    CrowFxEditorUI.HintType.Warning);
            }
            else
            {
                DrawBlueNoiseTextureDiagnostics(pNoiseTex, "BlueNoiseTex mode: assign a noise texture (128x128+ recommended).", CrowFxEditorUI.HintType.Warning);
            }
        }

        private void DrawJitterSeed(SerializedProperty pUseSeed, SerializedProperty pSeed)
        {
            DrawPropertyField(pUseSeed, "Use Stable Seed");

            if (pUseSeed == null || !pUseSeed.boolValue) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(16);
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawPropertyField(pSeed, "Seed Value");
                }
            }
        }

        private void DrawJitterHashNoiseControls(SerializedProperty pMode)
        {
            // HashNoise = enum index 2
            bool isHash = pMode != null
                        && pMode.propertyType == SerializedPropertyType.Enum
                        && pMode.enumValueIndex == 2;

            if (!isHash) return;

            var pCellCount  = SP("jitterHashCellCount");
            var pTimeSmooth = SP("jitterHashTimeSmooth");
            var pRotateDeg  = SP("jitterHashRotateDeg");
            var pAniso      = SP("jitterHashAniso");
            var pWarpAmpPx  = SP("jitterHashWarpAmpPx");
            var pWarpCells  = SP("jitterHashWarpCells");
            var pWarpSpeed  = SP("jitterHashWarpSpeed");
            var pPerChannel = SP("jitterHashPerChannel");

            bool searchHitsHash =
                !string.IsNullOrWhiteSpace(_search) &&
                (AnyMatch(pCellCount, pTimeSmooth, pRotateDeg, pAniso, pWarpAmpPx, pWarpCells, pWarpSpeed, pPerChannel)
                || PassesSearch("hashnoise hash noise domain warp cells aniso rotate flicker decorrelate"));

            if (searchHitsHash)
                _foldJitterHashNoise.target = true;

            GUILayout.Space(8);

            DrawSubSection(
                title: "HashNoise",
                icon: "d_PreMatCube",
                fold: _foldJitterHashNoise,
                hint: "procedural jitter",
                drawContent: () =>
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawPropertyField(pCellCount, "Cell Count");
                        DrawPropertyField(pTimeSmooth, "Time Smooth");
                        DrawPropertyField(pRotateDeg, "Rotate (deg)");
                        DrawPropertyField(pAniso, "Aniso");
                        DrawPropertyField(pWarpAmpPx, "Warp Amp (px)");
                        DrawPropertyField(pWarpCells, "Warp Cells");
                        DrawPropertyField(pWarpSpeed, "Warp Speed");
                        DrawPropertyField(pPerChannel, "Per Channel");

                        if (string.IsNullOrWhiteSpace(_search) || PassesSearch("hash noise cells warp domain rotate aniso flicker"))
                        {
                            CrowFxEditorUI.Hint("HashNoise = procedural jitter (no texture). Cell Count sets pattern scale; Time Smooth reduces flicker; Warp adds wobble; Rotate/Aniso bias direction; Per Channel decorrelates RGB.");
                        }
                    }
                }
            );
        }
        
        private void DrawJitterScanline(SerializedProperty pScanline)
        {
            DrawPropertyField(pScanline, "Scanline");

            if (pScanline == null || !pScanline.boolValue) return;

            var pScanDensity = SP("jitterScanlineDensity");
            var pScanAmp     = SP("jitterScanlineAmp");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(16);
                using (new EditorGUILayout.VerticalScope())
                {
                    if (pScanDensity != null && PropMatchesSearch(pScanDensity))
                    {
                        DrawFloatSliderProperty(pScanDensity, "Density", 64f, 2048f);
                    }

                    if (pScanAmp != null && PropMatchesSearch(pScanAmp))
                    {
                        DrawFloatSliderProperty(pScanAmp, "Amp", 0f, 2f);
                    }
                }
            }
        }

        private void DrawJitterAdvanced(SerializedProperty pWeights, SerializedProperty pDirR,
                                        SerializedProperty pDirG,   SerializedProperty pDirB)
        {
            DrawPropertyField(pWeights, "RGB Weights");
            DrawPropertyField(pDirR, "Red Direction");
            DrawPropertyField(pDirG, "Green Direction");
            DrawPropertyField(pDirB, "Blue Direction");
        }

        private void DrawBleedContent()
        {
            BeginSectionDrawn();

            var bleedBlend     = SP("bleedBlend");
            var bleedIntensity = SP("bleedIntensity");

            DrawPropertyField(bleedBlend, "Bleed Mix");
            DrawPropertyField(bleedIntensity, "Shift Distance");

            bool active = bleedBlend != null && bleedBlend.floatValue > 0f &&
                        bleedIntensity != null && bleedIntensity.floatValue > 0f;

            GUILayout.Space(6);

            var bleedMode     = SP("bleedMode");
            var bleedBlendMode = SP("bleedBlendMode");
            bool manualMode = bleedMode == null || bleedMode.enumValueIndex == (int)CrowImageEffects.BleedMode.Manual;
            bool radialMode = bleedMode != null && bleedMode.enumValueIndex == (int)CrowImageEffects.BleedMode.Radial;

            DrawBleedMiniPreview(
                manualMode: manualMode,
                shiftRValue: SP("shiftR") != null ? SP("shiftR").vector2Value : Vector2.zero,
                shiftGValue: SP("shiftG") != null ? SP("shiftG").vector2Value : Vector2.zero,
                shiftBValue: SP("shiftB") != null ? SP("shiftB").vector2Value : Vector2.zero,
                radialCenterValue: SP("bleedRadialCenter") != null ? SP("bleedRadialCenter").vector2Value : new Vector2(0.5f, 0.5f),
                radialStrengthValue: SP("bleedRadialStrength") != null ? SP("bleedRadialStrength").floatValue : 1f);

            if (!active && (string.IsNullOrWhiteSpace(_search) || PassesSearch("bleed mix shift distance")))
                CrowFxEditorUI.Hint("Raise Bleed Mix and Shift Distance to activate the effect.");

            if (string.IsNullOrWhiteSpace(_search) || AnyMatch(bleedMode, bleedBlendMode))
                DrawSubSection("Mode & Combine",              "d_Rigidbody Icon",         _foldBleedModeCombine, () => DrawBleedModeCombine(active),    "how to shift");

            var shiftR = SP("shiftR"); var shiftG = SP("shiftG"); var shiftB = SP("shiftB");
            if (manualMode && (string.IsNullOrWhiteSpace(_search) || AnyMatch(shiftR, shiftG, shiftB)))
                DrawSubSection("Manual Shifts",               "d_MoveTool",               _foldBleedManual,      () => DrawBleedManual(active),         "R/G/B vectors");

            var bleedRadialCenter = SP("bleedRadialCenter"); var bleedRadialStrength = SP("bleedRadialStrength");
            if (radialMode && (string.IsNullOrWhiteSpace(_search) || AnyMatch(bleedRadialCenter, bleedRadialStrength)))
                DrawSubSection("Radial Mode",                 "d_TransformTool On",       _foldBleedRadial,      () => DrawBleedRadial(active),         "center + strength");

            var bleedEdgeOnly = SP("bleedEdgeOnly"); var bleedEdgeThreshold = SP("bleedEdgeThreshold"); var bleedEdgePower = SP("bleedEdgePower");
            if (string.IsNullOrWhiteSpace(_search) || AnyMatch(bleedEdgeOnly, bleedEdgeThreshold, bleedEdgePower))
                DrawSubSection("Edge Gating",                 "d_SceneViewFx",            _foldBleedEdge,        () => DrawBleedEdge(active),           "only on edges");

            var bleedSamples = SP("bleedSamples"); var bleedSmear = SP("bleedSmear"); var bleedFalloff = SP("bleedFalloff");
            if (string.IsNullOrWhiteSpace(_search) || AnyMatch(bleedSamples, bleedSmear, bleedFalloff))
                DrawSubSection("Smear / Multi-tap",           "d_PreTextureMipMapHigh",   _foldBleedSmear,       () => DrawBleedSmear(active),          "samples + length");

            var bleedIntensityR = SP("bleedIntensityR"); var bleedIntensityG = SP("bleedIntensityG");
            var bleedIntensityB = SP("bleedIntensityB"); var bleedAnamorphic = SP("bleedAnamorphic");
            if (string.IsNullOrWhiteSpace(_search) || AnyMatch(bleedIntensityR, bleedIntensityG, bleedIntensityB, bleedAnamorphic))
                DrawSubSection("Per-channel Intensity & Shape","d_PreMatSphere",           _foldBleedPerChannel,  () => DrawBleedPerChannel(active),     "R/G/B gain");

            var bleedClampUV = SP("bleedClampUV"); var bleedPreserveLuma = SP("bleedPreserveLuma");
            if (string.IsNullOrWhiteSpace(_search) || AnyMatch(bleedClampUV, bleedPreserveLuma))
                DrawSubSection("Safety / Luma",               "d_console.warnicon",       _foldBleedSafety,      () => DrawBleedSafety(active),         "clamp + luma");

            var bleedWobbleAmp = SP("bleedWobbleAmp"); var bleedWobbleFreq = SP("bleedWobbleFreq"); var bleedWobbleScanline = SP("bleedWobbleScanline");
            if (string.IsNullOrWhiteSpace(_search) || AnyMatch(bleedWobbleAmp, bleedWobbleFreq, bleedWobbleScanline))
                DrawSubSection("Wobble",                      "d_FilterByLabel",          _foldBleedWobble,      () => DrawBleedWobble(active),         "VHS drift");

            MarkDrawnMany(
                "bleedBlend", "bleedIntensity", "bleedMode", "bleedBlendMode",
                "shiftR", "shiftG", "shiftB",
                "bleedEdgeOnly", "bleedEdgeThreshold", "bleedEdgePower",
                "bleedRadialCenter", "bleedRadialStrength",
                "bleedSamples", "bleedSmear", "bleedFalloff",
                "bleedIntensityR", "bleedIntensityG", "bleedIntensityB", "bleedAnamorphic",
                "bleedClampUV", "bleedPreserveLuma",
                "bleedWobbleAmp", "bleedWobbleFreq", "bleedWobbleScanline"
            );

            GUILayout.Space(6);
            DrawAutoRemaining(SectionKeys.Bleed);
        }

        private void DrawBleedModeCombine(bool active)
        {
            using (new EnabledScope(active))
            {
                var bleedMode     = SP("bleedMode");
                var bleedBlendMode = SP("bleedBlendMode");
                DrawPropertyField(bleedMode, "Bleed Mode");
                DrawPropertyField(bleedBlendMode, "Composite Mode");
            }
            if (string.IsNullOrWhiteSpace(_search) || PassesSearch("active inactive blend intensity"))
                CrowFxEditorUI.Hint(active ? "Active." : "Inactive until both Blend and Intensity are > 0.");
        }

        private void DrawBleedManual(bool active)
        {
            using (new EnabledScope(active))
            {
                var shiftR = SP("shiftR"); var shiftG = SP("shiftG"); var shiftB = SP("shiftB");
                DrawPropertyField(shiftR, "Red Shift");
                DrawPropertyField(shiftG, "Green Shift");
                DrawPropertyField(shiftB, "Blue Shift");
            }
            if (string.IsNullOrWhiteSpace(_search) || PassesSearch("uv offsets pixel-space"))
                CrowFxEditorUI.Hint("Per-channel offsets. Drag the colored preview arrow tips to edit shifts directly; the arrows show the visible screen shift rather than the raw sample direction.");
        }

        private void DrawBleedRadial(bool active)
        {
            using (new EnabledScope(active))
            {
                var bleedRadialCenter   = SP("bleedRadialCenter");
                var bleedRadialStrength = SP("bleedRadialStrength");
                DrawPropertyField(bleedRadialCenter, "Center");
                DrawPropertyField(bleedRadialStrength, "Radial Shift Strength");
            }
            if (string.IsNullOrWhiteSpace(_search) || PassesSearch("radial inward outward signed center"))
                CrowFxEditorUI.Hint("Signed control: positive values pull channels toward the center, negative values push them outward. Drag the center circle to reposition it, or drag the radial ring/arrow tips to change strength from the preview.");
        }

        private void DrawBleedEdge(bool active)
        {
            using (new EnabledScope(active))
            {
                var bleedEdgeOnly      = SP("bleedEdgeOnly");
                var bleedEdgeThreshold = SP("bleedEdgeThreshold");
                var bleedEdgePower     = SP("bleedEdgePower");
                DrawPropertyField(bleedEdgeOnly, "Edge Only");
                using (new EnabledScope(bleedEdgeOnly != null && bleedEdgeOnly.boolValue))
                {
                    DrawPropertyField(bleedEdgeThreshold, "Edge Threshold");
                    DrawPropertyField(bleedEdgePower, "Edge Power");
                }
            }
            if (string.IsNullOrWhiteSpace(_search) || PassesSearch("high-contrast edges cleaner separation"))
                CrowFxEditorUI.Hint("Restricts bleed to high-contrast edges for cleaner separation.");
        }

        private void DrawBleedSmear(bool active)
        {
            var bleedSamples = SP("bleedSamples");
            var bleedSmear   = SP("bleedSmear");
            var bleedFalloff = SP("bleedFalloff");

            using (new EnabledScope(active))
            {
                DrawPropertyField(bleedSamples, "Samples");
                DrawPropertyField(bleedSmear, "Smear Length");
                DrawPropertyField(bleedFalloff, "Falloff Curve");
            }

            if (string.IsNullOrWhiteSpace(_search) || PassesSearch("multi-sample trails cost texture reads"))
            {
                int samples = bleedSamples != null ? bleedSamples.intValue : 1;
                float smear = bleedSmear != null ? bleedSmear.floatValue : 0f;
                bool expensive = active && smear > 0f && samples >= 6;
                var hintType = expensive ? CrowFxEditorUI.HintType.Warning : CrowFxEditorUI.HintType.Info;
                string costLabel = active && smear > 0f
                    ? $"Approx cost: {samples * 3} RGB texture reads per pixel while smear is active."
                    : "Multi-sample trails only add cost when Smear is above 0.";

                CrowFxEditorUI.Hint(costLabel, hintType);
            }
        }

        private void DrawBleedPerChannel(bool active)
        {
            using (new EnabledScope(active))
            {
                var bleedIntensityR = SP("bleedIntensityR");
                var bleedIntensityG = SP("bleedIntensityG");
                var bleedIntensityB = SP("bleedIntensityB");
                var bleedAnamorphic = SP("bleedAnamorphic");
                DrawPropertyField(bleedIntensityR, "Red Strength");
                DrawPropertyField(bleedIntensityG, "Green Strength");
                DrawPropertyField(bleedIntensityB, "Blue Strength");
                DrawPropertyField(bleedAnamorphic, "Stretch");
            }
            if (string.IsNullOrWhiteSpace(_search) || PassesSearch("fine-tune channel separation stretch"))
                CrowFxEditorUI.Hint("Fine-tune channel separation + stretch horizontally/vertically.");
        }

        private void DrawBleedSafety(bool active)
        {
            using (new EnabledScope(active))
            {
                var bleedClampUV      = SP("bleedClampUV");
                var bleedPreserveLuma = SP("bleedPreserveLuma");
                DrawPropertyField(bleedClampUV, "Clamp Screen UV");
                DrawPropertyField(bleedPreserveLuma, "Preserve Brightness");
            }
            if (string.IsNullOrWhiteSpace(_search) || PassesSearch("sampling outside screen stabilizes brightness"))
                CrowFxEditorUI.Hint("Clamp avoids sampling outside screen. Preserve luma stabilizes brightness.");
        }

        private void DrawBleedWobble(bool active)
        {
            using (new EnabledScope(active))
            {
                var bleedWobbleAmp      = SP("bleedWobbleAmp");
                var bleedWobbleFreq     = SP("bleedWobbleFreq");
                var bleedWobbleScanline = SP("bleedWobbleScanline");
                DrawPropertyField(bleedWobbleAmp, "Wobble Amount");
                DrawPropertyField(bleedWobbleFreq, "Wobble Frequency");
                DrawPropertyField(bleedWobbleScanline, "Scanline Modulation");
            }
            if (string.IsNullOrWhiteSpace(_search) || PassesSearch("animated drift subtle"))
                CrowFxEditorUI.Hint("Animated drift (keep subtle).");
        }

        private bool AnyMatch(params SerializedProperty[] props)
        {
            for (int i = 0; i < props.Length; i++)
                if (props[i] != null && PropMatchesSearch(props[i])) return true;
            return false;
        }

        private static bool TryGetTextureSize(SerializedProperty textureProp, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (textureProp?.objectReferenceValue is not Texture texture)
                return false;

            width = Mathf.Max(1, texture.width);
            height = Mathf.Max(1, texture.height);
            return true;
        }

        private bool DrawBlueNoiseTextureDiagnostics(SerializedProperty textureProp, string missingMessage, CrowFxEditorUI.HintType missingType)
        {
            if (textureProp == null)
                return false;

            if (textureProp.objectReferenceValue == null)
            {
                CrowFxEditorUI.Hint(missingMessage, missingType);
                return true;
            }

            if (!TryGetTextureSize(textureProp, out int width, out int height))
                return false;

            if (width < 128 || height < 128)
            {
                CrowFxEditorUI.Hint($"Assigned texture is {width}x{height}. 128x128 or larger is recommended.", CrowFxEditorUI.HintType.Warning);
                return true;
            }

            if (width != height)
            {
                CrowFxEditorUI.Hint($"Assigned texture is {width}x{height}. Square blue-noise textures usually give the most even coverage.", CrowFxEditorUI.HintType.Warning);
                return true;
            }

            return false;
        }

        private static float EstimateGhostHistoryMegabytes(CrowImageEffects fx)
        {
            if (fx == null)
                return 0f;

            var camera = fx.GetComponent<Camera>();
            int width = camera != null ? Mathf.Max(1, camera.pixelWidth) : Mathf.Max(1, Screen.width);
            int height = camera != null ? Mathf.Max(1, camera.pixelHeight) : Mathf.Max(1, Screen.height);
            int historyBuffers = Mathf.Clamp(fx.ghostFrames, 1, 16) + 1;
            float totalBytes = width * height * historyBuffers * 4f;

            return totalBytes / (1024f * 1024f);
        }

        private void DrawActionHint(string message, string actionLabel, Action action, CrowFxEditorUI.HintType type = CrowFxEditorUI.HintType.Info, bool actionEnabled = true)
        {
            if (CrowFxEditorUI.HintWithAction(message, actionLabel, type, actionEnabled: actionEnabled))
            {
                action?.Invoke();
                GUI.FocusControl(null);
            }
        }

        private void EnableDepthOnTarget()
        {
            var targetFx = (CrowImageEffects)target;
            if (targetFx == null) return;

            var camera = targetFx.GetComponent<Camera>();
            if (camera == null) return;

            camera.depthTextureMode |= DepthTextureMode.Depth;
            EditorUtility.SetDirty(camera);
        }

        private void DrawMiniPreview(string title, float height, Action<Rect> drawBody)
        {
            GUILayout.Space(6);
            EditorGUILayout.LabelField(title, CrowFxEditorUI.Styles.SubHeaderLabel);

            var rect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            rect.xMin += 2f;
            rect.xMax -= 2f;

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.24f));
                CrowFxEditorUI.Theme.DrawBorder(rect);
            }

            var bodyRect = new Rect(rect.x + 6f, rect.y + 6f, Mathf.Max(10f, rect.width - 12f), Mathf.Max(10f, rect.height - 12f));
            GUI.BeginGroup(bodyRect);
            try
            {
                drawBody?.Invoke(new Rect(0f, 0f, bodyRect.width, bodyRect.height));
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private int EvaluateAnimatedLevelsPreview(int minLevelsValue, int maxLevelsValue, float speedValue)
        {
            float min = Mathf.Max(2f, minLevelsValue);
            float max = Mathf.Max(min, maxLevelsValue);
            float t = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * Mathf.Max(0f, speedValue));
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(min, max, t)), 2, 512);
        }

        private void DrawPosterizeMiniPreview(bool perChannel, int levelsValue, int levelsRValue, int levelsGValue, int levelsBValue, bool animateLevelsEnabled, int minLevelsValue, int maxLevelsValue, float speedValue)
        {
            bool previewAnimated = animateLevelsEnabled && !perChannel;
            bool previewPerChannel = perChannel;

            int animatedLevels = previewAnimated
                ? EvaluateAnimatedLevelsPreview(minLevelsValue, maxLevelsValue, speedValue)
                : Mathf.Max(2, levelsValue);

            string title = previewAnimated
                ? $"Animated Preview ({animatedLevels} levels)"
                : "Preview";

            DrawMiniPreview(title, previewPerChannel ? 72f : (previewAnimated ? 72f : 48f), rect =>
            {
                if (previewPerChannel)
                {
                    DrawQuantizedStrip(new Rect(rect.x, rect.y, rect.width, 14f), Mathf.Max(2, levelsRValue), new Color(0.9f, 0.25f, 0.25f, 0.95f));
                    DrawQuantizedStrip(new Rect(rect.x, rect.y + 18f, rect.width, 14f), Mathf.Max(2, levelsGValue), new Color(0.3f, 0.9f, 0.3f, 0.95f));
                    DrawQuantizedStrip(new Rect(rect.x, rect.y + 36f, rect.width, 14f), Mathf.Max(2, levelsBValue), new Color(0.3f, 0.5f, 0.95f, 0.95f));
                }
                else
                {
                    float stripY = previewAnimated ? rect.y + 6f : rect.center.y - 8f;
                    DrawQuantizedStrip(new Rect(rect.x, stripY, rect.width, 16f), animatedLevels, Color.white);

                    if (previewAnimated)
                    {
                        var rangeRect = new Rect(rect.x, rect.yMax - 18f, rect.width, 12f);
                        EditorGUI.DrawRect(new Rect(rangeRect.x, rangeRect.center.y - 1f, rangeRect.width, 2f), new Color(1f, 1f, 1f, 0.14f));

                        float min = Mathf.Max(2f, minLevelsValue);
                        float max = Mathf.Max(min, maxLevelsValue);
                        float markerT = Mathf.InverseLerp(min, max, animatedLevels);
                        float markerX = Mathf.Lerp(rangeRect.x, rangeRect.xMax, markerT);

                        EditorGUI.DrawRect(new Rect(markerX - 2f, rangeRect.y, 4f, rangeRect.height), new Color(1f, 0.75f, 0.35f, 0.9f));
                        GUI.Label(new Rect(rangeRect.x, rangeRect.y - 2f, 56f, rangeRect.height + 2f), $"{Mathf.RoundToInt(min)}", EditorStyles.miniLabel);
                        GUI.Label(new Rect(rangeRect.xMax - 56f, rangeRect.y - 2f, 56f, rangeRect.height + 2f), $"{Mathf.RoundToInt(max)}", EditorStyles.miniLabel);
                    }
                }
            });
        }

        private static void DrawQuantizedStrip(Rect rect, int levels, Color tint)
        {
            int columns = Mathf.Clamp(Mathf.RoundToInt(rect.width / 6f), 8, 36);
            float w = rect.width / columns;
            float steps = Mathf.Max(2, levels - 1);

            for (int i = 0; i < columns; i++)
            {
                float t = columns <= 1 ? 0f : i / (float)(columns - 1);
                float q = Mathf.Round(t * steps) / steps;
                Color c = new Color(tint.r * q, tint.g * q, tint.b * q, 1f);
                var colRect = new Rect(rect.x + i * w, rect.y, Mathf.Ceil(w), rect.height);
                EditorGUI.DrawRect(colRect, c);
            }
        }

        private static bool IsManualBleedPreviewHandle(BleedPreviewHandleMode mode)
            => mode == BleedPreviewHandleMode.ManualR ||
               mode == BleedPreviewHandleMode.ManualG ||
               mode == BleedPreviewHandleMode.ManualB;

        private void BeginBleedPreviewDrag(int controlId, BleedPreviewHandleMode mode, string undoLabel, float startStrength = 0f, float startProjection = 0f, Vector2 startDirection = default)
        {
            var targetFx = (CrowImageEffects)target;
            if (targetFx == null)
                return;

            Undo.RecordObject(targetFx, undoLabel);
            GUIUtility.hotControl = controlId;
            _bleedPreviewHandleMode = mode;
            _bleedPreviewDragStartStrength = startStrength;
            _bleedPreviewDragStartProjection = startProjection;
            _bleedPreviewDragDirection = startDirection.sqrMagnitude > 0.0001f ? startDirection.normalized : Vector2.right;
            GUI.FocusControl(null);
        }

        private void EndBleedPreviewDrag(int controlId = -1)
        {
            if (controlId >= 0 && GUIUtility.hotControl == controlId)
                GUIUtility.hotControl = 0;

            _bleedPreviewHandleMode = BleedPreviewHandleMode.None;
            _bleedPreviewDragStartStrength = 0f;
            _bleedPreviewDragStartProjection = 0f;
            _bleedPreviewDragDirection = Vector2.right;
        }

        private static Rect GetPreviewHandleRect(Vector2 position, float radius)
            => new Rect(position.x - radius, position.y - radius, radius * 2f, radius * 2f);

        private static Vector2 GetShiftArrowTip(Vector2 origin, Vector2 shift)
            => origin + shift * 10f;

        private static Rect GetCenteredSquareRect(Rect rect)
        {
            float size = Mathf.Min(rect.width, rect.height);
            return new Rect(
                rect.x + (rect.width - size) * 0.5f,
                rect.y + (rect.height - size) * 0.5f,
                size,
                size);
        }

        private static Vector2 PreviewUvToGuiPosition(Rect rect, Vector2 uv)
        {
            return new Vector2(
                rect.x + rect.width * Mathf.Clamp01(uv.x),
                rect.yMax - rect.height * Mathf.Clamp01(uv.y));
        }

        private static Vector2 PreviewUvFromGuiPosition(Rect rect, Vector2 guiPosition)
        {
            return new Vector2(
                Mathf.InverseLerp(rect.x, rect.xMax, guiPosition.x),
                Mathf.InverseLerp(rect.yMax, rect.y, guiPosition.y));
        }

        private static void DrawPreviewHandleDisc(Vector2 position, float radius, Color color, bool active)
        {
            Handles.color = new Color(0f, 0f, 0f, 0.45f);
            Handles.DrawSolidDisc(new Vector3(position.x, position.y, 0f), Vector3.forward, radius + 1.5f);
            Handles.color = color;
            Handles.DrawSolidDisc(new Vector3(position.x, position.y, 0f), Vector3.forward, radius);
            Handles.color = active ? Color.white : new Color(1f, 1f, 1f, 0.85f);
            Handles.DrawWireDisc(new Vector3(position.x, position.y, 0f), Vector3.forward, radius + 0.5f);
        }

        private void HandleManualBleedPreviewInput(int controlId, Rect bounds, Vector2 center, Vector2 tipR, Vector2 tipG, Vector2 tipB)
        {
            const float handleRadius = 7f;
            const float hitRadius = 10f;
            var evt = Event.current;

            EditorGUIUtility.AddCursorRect(GetPreviewHandleRect(tipR, handleRadius), MouseCursor.MoveArrow);
            EditorGUIUtility.AddCursorRect(GetPreviewHandleRect(tipG, handleRadius), MouseCursor.MoveArrow);
            EditorGUIUtility.AddCursorRect(GetPreviewHandleRect(tipB, handleRadius), MouseCursor.MoveArrow);

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                BleedPreviewHandleMode nextMode = BleedPreviewHandleMode.None;
                float bestDistance = hitRadius * hitRadius;

                void Consider(Vector2 tip, BleedPreviewHandleMode mode)
                {
                    float dist = (evt.mousePosition - tip).sqrMagnitude;
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        nextMode = mode;
                    }
                }

                Consider(tipR, BleedPreviewHandleMode.ManualR);
                Consider(tipG, BleedPreviewHandleMode.ManualG);
                Consider(tipB, BleedPreviewHandleMode.ManualB);

                if (nextMode != BleedPreviewHandleMode.None)
                {
                    BeginBleedPreviewDrag(controlId, nextMode, "Adjust RGB Bleed Shift");
                    evt.Use();
                    return;
                }
            }

            if (!IsManualBleedPreviewHandle(_bleedPreviewHandleMode) || GUIUtility.hotControl != controlId)
                return;

            if (evt.type == EventType.MouseDrag || evt.type == EventType.MouseUp)
            {
                Vector2 clampedMouse = new Vector2(
                    Mathf.Clamp(evt.mousePosition.x, bounds.xMin + handleRadius, bounds.xMax - handleRadius),
                    Mathf.Clamp(evt.mousePosition.y, bounds.yMin + handleRadius, bounds.yMax - handleRadius));
                Vector2 rawShift = PreviewShiftFromGui((clampedMouse - center) / 10f);

                switch (_bleedPreviewHandleMode)
                {
                    case BleedPreviewHandleMode.ManualR: SetVector2("shiftR", rawShift); break;
                    case BleedPreviewHandleMode.ManualG: SetVector2("shiftG", rawShift); break;
                    case BleedPreviewHandleMode.ManualB: SetVector2("shiftB", rawShift); break;
                }

                ApplySerializedChanges();
                Repaint();

                if (evt.type == EventType.MouseUp)
                    EndBleedPreviewDrag(controlId);

                evt.Use();
            }
        }

        private void HandleRadialBleedPreviewInput(int controlId, Rect rect, Vector2 radialCenter, float ringRadius, Vector2 tipR, Vector2 tipG, Vector2 tipB)
        {
            const float centerHandleRadius = 7f;
            const float tipHandleRadius = 6f;
            const float ringHitWidth = 8f;
            var evt = Event.current;
            bool mouseInRect = rect.Contains(evt.mousePosition);

            if (mouseInRect)
            {
                EditorGUIUtility.AddCursorRect(GetPreviewHandleRect(radialCenter, centerHandleRadius), MouseCursor.MoveArrow);
                EditorGUIUtility.AddCursorRect(GetPreviewHandleRect(tipR, tipHandleRadius), MouseCursor.MoveArrow);
                EditorGUIUtility.AddCursorRect(GetPreviewHandleRect(tipG, tipHandleRadius), MouseCursor.MoveArrow);
                EditorGUIUtility.AddCursorRect(GetPreviewHandleRect(tipB, tipHandleRadius), MouseCursor.MoveArrow);

                if (Mathf.Abs(Vector2.Distance(evt.mousePosition, radialCenter) - ringRadius) <= ringHitWidth)
                    EditorGUIUtility.AddCursorRect(GetPreviewHandleRect(evt.mousePosition, ringHitWidth), MouseCursor.MoveArrow);
            }

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (!mouseInRect)
                    return;

                float distToCenter = Vector2.Distance(evt.mousePosition, radialCenter);
                if (distToCenter <= centerHandleRadius + 2f)
                {
                    BeginBleedPreviewDrag(controlId, BleedPreviewHandleMode.RadialCenter, "Adjust RGB Bleed Center");
                    evt.Use();
                    return;
                }

                bool nearTip = (evt.mousePosition - tipR).sqrMagnitude <= (tipHandleRadius + 3f) * (tipHandleRadius + 3f) ||
                               (evt.mousePosition - tipG).sqrMagnitude <= (tipHandleRadius + 3f) * (tipHandleRadius + 3f) ||
                               (evt.mousePosition - tipB).sqrMagnitude <= (tipHandleRadius + 3f) * (tipHandleRadius + 3f);
                bool nearRing = Mathf.Abs(distToCenter - ringRadius) <= ringHitWidth;

                if (nearTip || nearRing)
                {
                    Vector2 dir = evt.mousePosition - radialCenter;
                    if (dir.sqrMagnitude < 0.0001f)
                        dir = Vector2.right;

                    BeginBleedPreviewDrag(
                        controlId,
                        BleedPreviewHandleMode.RadialStrength,
                        "Adjust RGB Bleed Radial Strength",
                        startStrength: GetFloat("bleedRadialStrength"),
                        startProjection: Vector2.Dot(evt.mousePosition - radialCenter, dir.normalized),
                        startDirection: dir.normalized);
                    evt.Use();
                    return;
                }
            }

            if (_bleedPreviewHandleMode == BleedPreviewHandleMode.RadialCenter &&
                GUIUtility.hotControl == controlId &&
                (evt.type == EventType.MouseDrag || evt.type == EventType.MouseUp))
            {
                var uv = PreviewUvFromGuiPosition(rect, evt.mousePosition);
                SetVector2("bleedRadialCenter", uv);
                ApplySerializedChanges();
                Repaint();

                if (evt.type == EventType.MouseUp)
                    EndBleedPreviewDrag(controlId);

                evt.Use();
                return;
            }

            if (_bleedPreviewHandleMode == BleedPreviewHandleMode.RadialStrength &&
                GUIUtility.hotControl == controlId &&
                (evt.type == EventType.MouseDrag || evt.type == EventType.MouseUp))
            {
                float sensitivity = Mathf.Max(6f, ringRadius * 0.5f);
                float projection = Vector2.Dot(evt.mousePosition - radialCenter, _bleedPreviewDragDirection);
                float delta = projection - _bleedPreviewDragStartProjection;
                float nextStrength = Mathf.Clamp(_bleedPreviewDragStartStrength - (delta / sensitivity), -5f, 5f);
                if (Mathf.Abs(nextStrength) < 0.02f)
                    nextStrength = 0f;

                SetFloat("bleedRadialStrength", nextStrength);
                ApplySerializedChanges();
                Repaint();

                if (evt.type == EventType.MouseUp)
                    EndBleedPreviewDrag(controlId);

                evt.Use();
            }
        }

        private void DrawBleedMiniPreview(bool manualMode, Vector2 shiftRValue, Vector2 shiftGValue, Vector2 shiftBValue, Vector2 radialCenterValue, float radialStrengthValue)
        {
            if (manualMode && !IsManualBleedPreviewHandle(_bleedPreviewHandleMode) && _bleedPreviewHandleMode != BleedPreviewHandleMode.None)
                EndBleedPreviewDrag();
            else if (!manualMode && IsManualBleedPreviewHandle(_bleedPreviewHandleMode))
                EndBleedPreviewDrag();

            float previewHeight = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.3f, 98f, 122f);
            DrawMiniPreview("Preview", previewHeight, rect =>
            {
                var previewRect = GetCenteredSquareRect(rect);
                var evt = Event.current;
                int controlId = GUIUtility.GetControlID("CrowFX.BleedPreview".GetHashCode(), FocusType.Passive, previewRect);
                bool ownsHotControl = GUIUtility.hotControl == controlId;

                if (_bleedPreviewHandleMode != BleedPreviewHandleMode.None &&
                    ((evt.type == EventType.MouseDown && evt.button == 0) ||
                     evt.type == EventType.MouseLeaveWindow))
                {
                    EndBleedPreviewDrag(controlId);
                }

                if (_bleedPreviewHandleMode != BleedPreviewHandleMode.None &&
                    !ownsHotControl &&
                    evt.rawType == EventType.MouseUp)
                {
                    EndBleedPreviewDrag(controlId);
                }

                var center = previewRect.center;
                var manualTipR = GetShiftArrowTip(center, PreviewShiftToGui(shiftRValue));
                var manualTipG = GetShiftArrowTip(center, PreviewShiftToGui(shiftGValue));
                var manualTipB = GetShiftArrowTip(center, PreviewShiftToGui(shiftBValue));
                Vector2 radialCenter = Vector2.zero;
                float ringRadius = 0f;
                Vector2 radialTipR = Vector2.zero;
                Vector2 radialTipG = Vector2.zero;
                Vector2 radialTipB = Vector2.zero;

                Handles.BeginGUI();
                try
                {
                    Handles.color = new Color(1f, 1f, 1f, 0.04f);
                    Handles.DrawSolidRectangleWithOutline(
                        new[]
                        {
                            new Vector3(previewRect.xMin, previewRect.yMin, 0f),
                            new Vector3(previewRect.xMax, previewRect.yMin, 0f),
                            new Vector3(previewRect.xMax, previewRect.yMax, 0f),
                            new Vector3(previewRect.xMin, previewRect.yMax, 0f)
                        },
                        new Color(1f, 1f, 1f, 0.02f),
                        new Color(1f, 1f, 1f, 0.08f));

                    Handles.color = new Color(1f, 1f, 1f, 0.12f);
                    Handles.DrawLine(new Vector3(previewRect.x, center.y), new Vector3(previewRect.xMax, center.y));
                    Handles.DrawLine(new Vector3(center.x, previewRect.y), new Vector3(center.x, previewRect.yMax));

                    if (manualMode)
                    {
                        DrawShiftArrow(center, PreviewShiftToGui(shiftRValue), new Color(0.95f, 0.3f, 0.3f, 0.95f));
                        DrawShiftArrow(center, PreviewShiftToGui(shiftGValue), new Color(0.35f, 0.9f, 0.35f, 0.95f));
                        DrawShiftArrow(center, PreviewShiftToGui(shiftBValue), new Color(0.35f, 0.55f, 0.95f, 0.95f));
                        DrawPreviewHandleDisc(manualTipR, 4.5f, new Color(0.95f, 0.3f, 0.3f, 1f), ownsHotControl && _bleedPreviewHandleMode == BleedPreviewHandleMode.ManualR);
                        DrawPreviewHandleDisc(manualTipG, 4.5f, new Color(0.35f, 0.9f, 0.35f, 1f), ownsHotControl && _bleedPreviewHandleMode == BleedPreviewHandleMode.ManualG);
                        DrawPreviewHandleDisc(manualTipB, 4.5f, new Color(0.35f, 0.55f, 0.95f, 1f), ownsHotControl && _bleedPreviewHandleMode == BleedPreviewHandleMode.ManualB);
                    }
                    else
                    {
                        Vector2 rc = PreviewUvToGuiPosition(previewRect, radialCenterValue);
                        float strengthT = Mathf.Clamp01(Mathf.Abs(radialStrengthValue) / 5f);
                        float directionSign = radialStrengthValue < 0f ? 1f : -1f;
                        float radius = Mathf.Lerp(10f, Mathf.Min(previewRect.width, previewRect.height) * 0.42f, strengthT);
                        float arrowLength = Mathf.Lerp(0f, 1.4f, strengthT);
                        Vector2 dirR = new Vector2(1f, 0f).normalized;
                        Vector2 dirG = new Vector2(-0.35f, -0.85f).normalized;
                        Vector2 dirB = new Vector2(-0.75f, 0.45f).normalized;

                        Handles.color = new Color(1f, 1f, 1f, 0.22f);
                        Handles.DrawWireDisc(new Vector3(rc.x, rc.y, 0f), Vector3.forward, radius);
                        DrawShiftArrow(rc + dirR * radius, dirR * arrowLength * directionSign, new Color(0.95f, 0.3f, 0.3f, 0.9f));
                        DrawShiftArrow(rc + dirG * radius, dirG * arrowLength * directionSign, new Color(0.35f, 0.9f, 0.35f, 0.9f));
                        DrawShiftArrow(rc + dirB * radius, dirB * arrowLength * directionSign, new Color(0.35f, 0.55f, 0.95f, 0.9f));

                        radialCenter = rc;
                        ringRadius = radius;
                        radialTipR = GetShiftArrowTip(rc + dirR * radius, dirR * arrowLength * directionSign);
                        radialTipG = GetShiftArrowTip(rc + dirG * radius, dirG * arrowLength * directionSign);
                        radialTipB = GetShiftArrowTip(rc + dirB * radius, dirB * arrowLength * directionSign);

                        DrawPreviewHandleDisc(radialCenter, 5f, new Color(1f, 1f, 1f, 0.95f), ownsHotControl && _bleedPreviewHandleMode == BleedPreviewHandleMode.RadialCenter);
                        DrawPreviewHandleDisc(radialTipR, 4f, new Color(0.95f, 0.3f, 0.3f, 1f), ownsHotControl && _bleedPreviewHandleMode == BleedPreviewHandleMode.RadialStrength);
                        DrawPreviewHandleDisc(radialTipG, 4f, new Color(0.35f, 0.9f, 0.35f, 1f), ownsHotControl && _bleedPreviewHandleMode == BleedPreviewHandleMode.RadialStrength);
                        DrawPreviewHandleDisc(radialTipB, 4f, new Color(0.35f, 0.55f, 0.95f, 1f), ownsHotControl && _bleedPreviewHandleMode == BleedPreviewHandleMode.RadialStrength);
                    }
                }
                finally
                {
                    Handles.EndGUI();
                }

                if (manualMode)
                    HandleManualBleedPreviewInput(controlId, previewRect, center, manualTipR, manualTipG, manualTipB);
                else
                    HandleRadialBleedPreviewInput(controlId, previewRect, radialCenter, ringRadius, radialTipR, radialTipG, radialTipB);
            });
        }

        private static Vector2 PreviewShiftToGui(Vector2 shift)
        {
            // The shader offsets the sampled source UV. The visible screen displacement is the inverse on X,
            // while GUI-space Y already points downward like the visible result.
            return new Vector2(-shift.x, shift.y);
        }

        private static Vector2 PreviewShiftFromGui(Vector2 guiShift)
            => new Vector2(-guiShift.x, guiShift.y);

        private static void DrawVectorArrow(Vector2 origin, Vector2 end, Color color, float thickness = 2.4f, float headLength = 8f, float headWidth = 4f)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(thickness, new Vector3(origin.x, origin.y, 0f), new Vector3(end.x, end.y, 0f));

            Vector2 dir = (end - origin).normalized;
            if (dir.sqrMagnitude < 0.001f)
                return;

            Vector2 right = new Vector2(-dir.y, dir.x);
            Handles.DrawAAConvexPolygon(
                new Vector3(end.x, end.y, 0f),
                new Vector3(end.x - dir.x * headLength + right.x * headWidth, end.y - dir.y * headLength + right.y * headWidth, 0f),
                new Vector3(end.x - dir.x * headLength - right.x * headWidth, end.y - dir.y * headLength - right.y * headWidth, 0f));
        }

        private static void DrawShiftArrow(Vector2 origin, Vector2 shift, Color color)
        {
            DrawVectorArrow(origin, origin + shift * 10f, color);
        }

        private static Color GetGhostPreviewTint(CrowImageEffects.GhostCombineMode combineMode)
        {
            return combineMode switch
            {
                CrowImageEffects.GhostCombineMode.Mix => new Color(0.72f, 0.82f, 1f, 1f),
                CrowImageEffects.GhostCombineMode.Add => new Color(1f, 0.62f, 0.42f, 1f),
                CrowImageEffects.GhostCombineMode.Screen => new Color(0.68f, 0.92f, 1f, 1f),
                CrowImageEffects.GhostCombineMode.Max => new Color(0.92f, 0.94f, 1f, 1f),
                _ => new Color(0.72f, 0.82f, 1f, 1f)
            };
        }

        private static Rect CenteredRect(Vector2 center, Vector2 size)
        {
            return new Rect(
                center.x - size.x * 0.5f,
                center.y - size.y * 0.5f,
                size.x,
                size.y);
        }

        private static void DrawPreviewRectFrame(Rect rect, Color tint, float fillAlpha, float borderAlpha)
        {
            if (fillAlpha > 0f)
                EditorGUI.DrawRect(rect, new Color(tint.r, tint.g, tint.b, fillAlpha));

            var border = new Color(
                Mathf.Lerp(tint.r, 1f, 0.45f),
                Mathf.Lerp(tint.g, 1f, 0.45f),
                Mathf.Lerp(tint.b, 1f, 0.45f),
                borderAlpha);

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), border);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);
        }

        private void DrawGhostMiniPreview(
            int frames,
            float weightCurve,
            Vector2 offsetPx,
            int captureInterval,
            int startDelay,
            float ghostBlend,
            CrowImageEffects.GhostCombineMode combineMode)
        {
            DrawMiniPreview("Trail Preview", 92f, rect =>
            {
                int count = Mathf.Clamp(frames, 1, 16);
                int delaySteps = Mathf.Max(0, startDelay);
                int stride = Mathf.Max(1, captureInterval + 1);
                int firstVisibleStep = delaySteps + 1;
                int farthestStep = firstVisibleStep + Mathf.Max(0, count - 1) * stride;
                float blend = Mathf.Clamp01(ghostBlend);
                float curve = Mathf.Max(0.25f, weightCurve);
                Vector2 visibleOffset = PreviewShiftToGui(offsetPx);
                float offsetMagnitude = visibleOffset.magnitude;
                Color tint = GetGhostPreviewTint(combineMode);

                var bodyRect = new Rect(2f, 2f, Mathf.Max(10f, rect.width - 4f), Mathf.Max(10f, rect.height - 4f));
                var frameSize = new Vector2(
                    Mathf.Clamp(bodyRect.width * 0.18f, 24f, 34f),
                    Mathf.Clamp(bodyRect.height * 0.22f, 16f, 22f));

                EditorGUI.DrawRect(bodyRect, new Color(1f, 1f, 1f, 0.015f));
                EditorGUI.DrawRect(new Rect(bodyRect.x, bodyRect.center.y, bodyRect.width, 1f), new Color(1f, 1f, 1f, 0.06f));
                EditorGUI.DrawRect(new Rect(bodyRect.center.x, bodyRect.y, 1f, bodyRect.height), new Color(1f, 1f, 1f, 0.045f));

                Vector2 trailStep = Vector2.zero;
                Vector2 currentCenter = bodyRect.center;

                if (offsetMagnitude > 0.01f)
                {
                    float fitScaleX = Mathf.Abs(visibleOffset.x) > 0.01f
                        ? (bodyRect.width - frameSize.x - 18f) / (Mathf.Abs(visibleOffset.x) * farthestStep)
                        : float.MaxValue;
                    float fitScaleY = Mathf.Abs(visibleOffset.y) > 0.01f
                        ? (bodyRect.height - frameSize.y - 18f) / (Mathf.Abs(visibleOffset.y) * farthestStep)
                        : float.MaxValue;
                    float fitScale = Mathf.Min(fitScaleX, fitScaleY);
                    float desiredStep = Mathf.Clamp(Mathf.Min(bodyRect.width, bodyRect.height) * 0.12f, 8f, 16f);
                    float displayScale = desiredStep / offsetMagnitude;
                    float scale = Mathf.Clamp(Mathf.Min(fitScale, displayScale), 0.75f, 14f);

                    trailStep = visibleOffset * scale;
                    Vector2 trailExtent = trailStep * farthestStep;
                    currentCenter = bodyRect.center - trailExtent * 0.35f;
                    currentCenter.x = Mathf.Clamp(currentCenter.x, bodyRect.xMin + frameSize.x * 0.5f + 6f, bodyRect.xMax - frameSize.x * 0.5f - 6f);
                    currentCenter.y = Mathf.Clamp(currentCenter.y, bodyRect.yMin + frameSize.y * 0.5f + 6f, bodyRect.yMax - frameSize.y * 0.5f - 6f);

                    Handles.BeginGUI();
                    try
                    {
                        Vector2 arrowEnd = currentCenter + trailStep * Mathf.Max(1f, firstVisibleStep);
                        DrawVectorArrow(currentCenter, arrowEnd, new Color(tint.r, tint.g, tint.b, 0.75f), 2f, 7f, 3.5f);
                    }
                    finally
                    {
                        Handles.EndGUI();
                    }

                    for (int delay = 1; delay <= delaySteps; delay++)
                    {
                        Vector2 marker = currentCenter + trailStep * delay;
                        var markerRect = new Rect(marker.x - 1.5f, marker.y - 1.5f, 3f, 3f);
                        EditorGUI.DrawRect(markerRect, new Color(1f, 1f, 1f, 0.18f));
                    }
                }
                else
                {
                    for (int i = count - 1; i >= 0; i--)
                    {
                        float age01 = count <= 1 ? 1f : 1f - (i / (float)(count - 1));
                        float weight = Mathf.Pow(age01, curve);
                        float expand = 2f + (count - 1 - i) * 2f;
                        Rect stackedRect = CenteredRect(currentCenter, frameSize + Vector2.one * expand);
                        DrawPreviewRectFrame(stackedRect, tint, 0f, Mathf.Lerp(0.12f, 0.45f * Mathf.Lerp(0.35f, 1f, blend), weight));
                    }
                }

                for (int i = count - 1; i >= 0; i--)
                {
                    float age01 = count <= 1 ? 1f : 1f - (i / (float)(count - 1));
                    float weight = Mathf.Pow(age01, curve);
                    float alpha = Mathf.Lerp(0.08f, 0.58f * Mathf.Lerp(0.35f, 1f, blend), weight);
                    float borderAlpha = Mathf.Lerp(0.14f, 0.72f, weight);
                    float sizeScale = Mathf.Lerp(0.92f, 1f, weight);
                    Vector2 ghostCenter = currentCenter + trailStep * (firstVisibleStep + i * stride);
                    Rect ghostRect = CenteredRect(ghostCenter, frameSize * sizeScale);
                    DrawPreviewRectFrame(ghostRect, tint, alpha, borderAlpha);
                }

                Rect currentRect = CenteredRect(currentCenter, frameSize);
                DrawPreviewRectFrame(currentRect, Color.white, 0.16f + blend * 0.08f, 0.9f);
                Rect coreRect = new Rect(currentRect.x + 3f, currentRect.y + 3f, Mathf.Max(4f, currentRect.width - 6f), Mathf.Max(4f, currentRect.height - 6f));
                EditorGUI.DrawRect(coreRect, new Color(1f, 1f, 1f, 0.72f));
            });
        }

        private void DrawGhostContent()
        {
            BeginSectionDrawn();

            var ghostEnabled = SP("ghostEnabled");
            var ghostBlend = SP("ghostBlend");
            var ghostCombineMode = SP("ghostCombineMode");
            var ghostOffsetPx = SP("ghostOffsetPx");
            var ghostFrames = SP("ghostFrames");
            var ghostCaptureInterval = SP("ghostCaptureInterval");
            var ghostStartDelay = SP("ghostStartDelay");
            var ghostWeightCurve = SP("ghostWeightCurve");

            if (PropMatchesSearch(ghostEnabled))
                DrawPropertyField(ghostEnabled, "Enable Ghosting");

            bool enabled = ghostEnabled != null && ghostEnabled.boolValue;
            if (enabled)
            {
                DrawPropertyField(ghostBlend, "Trail Blend");
                DrawPropertyField(ghostCombineMode, "Blend Mode");
                DrawPropertyField(ghostOffsetPx, "Trail Offset (px)");
                GUILayout.Space(6);
                DrawPropertyField(ghostFrames, "Stored Frames");
                DrawPropertyField(ghostCaptureInterval, "Capture Interval");
                DrawPropertyField(ghostStartDelay, "Start Delay");
                DrawPropertyField(ghostWeightCurve, "Weight Curve");

                DrawGhostMiniPreview(
                    frames: ghostFrames != null ? ghostFrames.intValue : 4,
                    weightCurve: ghostWeightCurve != null ? ghostWeightCurve.floatValue : 1.5f,
                    offsetPx: ghostOffsetPx != null ? ghostOffsetPx.vector2Value : Vector2.zero,
                    captureInterval: ghostCaptureInterval != null ? ghostCaptureInterval.intValue : 0,
                    startDelay: ghostStartDelay != null ? ghostStartDelay.intValue : 0,
                    ghostBlend: ghostBlend != null ? ghostBlend.floatValue : 0.35f,
                    combineMode: ghostCombineMode != null
                        ? (CrowImageEffects.GhostCombineMode)ghostCombineMode.enumValueIndex
                        : CrowImageEffects.GhostCombineMode.Screen);
            }

            MarkDrawnMany(
                "ghostEnabled",
                "ghostBlend", "ghostCombineMode", "ghostOffsetPx",
                "ghostFrames", "ghostCaptureInterval", "ghostStartDelay", "ghostWeightCurve"
            );

            if (!enabled)
            {
                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("ghost frames weighted composite curve"))
                    CrowFxEditorUI.Hint("Enable Ghosting to reveal trail blending, history, and capture timing controls.");
            }
            else if (string.IsNullOrWhiteSpace(_search) || PassesSearch("ghost frames weighted composite curve"))
            {
                CrowFxEditorUI.Hint("Blends a weighted composite of previous frames. Higher weight curve = favors newer frames.");
            }

            var targetFx = (CrowImageEffects)target;
            if (enabled && targetFx != null &&
                (string.IsNullOrWhiteSpace(_search) || PassesSearch("history memory render texture allocation")))
            {
                int historyBuffers = Mathf.Clamp(targetFx.ghostFrames, 1, 16) + 1;
                float approxMb = EstimateGhostHistoryMegabytes(targetFx);
                var hintType = approxMb >= 32f ? CrowFxEditorUI.HintType.Warning : CrowFxEditorUI.HintType.Info;
                CrowFxEditorUI.Hint(
                    $"Approx history allocation at current camera size: {approxMb:0.0} MB across {historyBuffers} RTs (32-bit color estimate).",
                    hintType);
            }

            DrawAutoRemaining(SectionKeys.Ghost);
        }

        private void DrawEdgeContent()
        {
            BeginSectionDrawn();

            var edgeEnabled = SP("edgeEnabled");
            var edgeStrength = SP("edgeStrength");
            var edgeThreshold = SP("edgeThreshold");
            var edgeBlend = SP("edgeBlend");
            var edgeColor = SP("edgeColor");

            if (PropMatchesSearch(edgeEnabled))
                DrawPropertyField(edgeEnabled, "Enable Edges");

            bool enabled = edgeEnabled != null && edgeEnabled.boolValue;
            if (enabled)
            {
                DrawPropertyField(edgeStrength, "Outline Strength");
                DrawPropertyField(edgeThreshold, "Depth Threshold");
                DrawPropertyField(edgeBlend, "Outline Blend");
                DrawPropertyField(edgeColor, "Outline Color");
            }

            MarkDrawnMany("edgeEnabled", "edgeStrength", "edgeThreshold", "edgeBlend", "edgeColor");

            if (!enabled)
            {
                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("depth outline requires camera depth"))
                    CrowFxEditorUI.Hint("Enable Edges to reveal the outline controls.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("depth outline requires camera depth"))
                    CrowFxEditorUI.Hint("Depth-based outline effect. Requires camera depth.");

                if (NeedsDepthFix((CrowImageEffects)target))
                {
                    DrawActionHint("Camera depth texture is still off for outlines.", "Enable Depth",
                        EnableDepthOnTarget,
                        CrowFxEditorUI.HintType.Warning);
                }
            }

            DrawAutoRemaining(SectionKeys.Edges);
        }

        private void DrawUnsharpContent()
        {
            BeginSectionDrawn();

            var unsharpEnabled = SP("unsharpEnabled");
            var unsharpAmount = SP("unsharpAmount");
            var unsharpRadius = SP("unsharpRadius");
            var unsharpThreshold = SP("unsharpThreshold");
            var unsharpLumaOnly = SP("unsharpLumaOnly");
            var unsharpChroma = SP("unsharpChroma");

            if (PropMatchesSearch(unsharpEnabled))
                DrawPropertyField(unsharpEnabled, "Enable Unsharp Mask");

            bool enabled = unsharpEnabled != null && unsharpEnabled.boolValue;
            if (enabled)
            {
                DrawPropertyField(unsharpAmount, "Sharpen Amount");
                DrawPropertyField(unsharpRadius, "Blur Radius");
                DrawPropertyField(unsharpThreshold, "Noise Threshold");

                GUILayout.Space(6);

                DrawPropertyField(unsharpLumaOnly, "Luma Only");
                if (unsharpLumaOnly != null && unsharpLumaOnly.boolValue)
                {
                    if (unsharpChroma != null && PropMatchesSearch(unsharpChroma))
                        EditorGUILayout.Slider(unsharpChroma, 0f, 1f, PropLabel(unsharpChroma, "Chroma Amount"));
                }
            }

            MarkDrawnMany("unsharpEnabled", "unsharpAmount", "unsharpRadius", "unsharpThreshold", "unsharpLumaOnly", "unsharpChroma");

            if (!enabled)
            {
                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("subtracts blurred threshold noise"))
                    CrowFxEditorUI.Hint("Enable Unsharp Mask to reveal the sharpening controls.");
            }
            else if (string.IsNullOrWhiteSpace(_search) || PassesSearch("subtracts blurred threshold noise"))
            {
                CrowFxEditorUI.Hint("Subtracts a blurred version. Threshold helps avoid amplifying noise.");
            }

            DrawAutoRemaining(SectionKeys.Unsharp);
        }

        private void DrawDitherContent()
        {
            BeginSectionDrawn();

            var ditherMode = SP("ditherMode");
            var ditherStrength = SP("ditherStrength");
            var ditherAngle = SP("ditherAngle");
            var blueNoise = SP("blueNoise");

            if (PropMatchesSearch(ditherMode))
                DrawPropertyField(ditherMode, "Pattern");

            bool hasDither = ditherMode != null && ditherMode.enumValueIndex != (int)CrowImageEffects.DitherMode.None;
            int modeIndex = ditherMode != null ? ditherMode.enumValueIndex : (int)CrowImageEffects.DitherMode.None;

            if (hasDither)
            {
                DrawPropertyField(ditherStrength, "Dither Strength");

                bool needsAngle = modeIndex == (int)CrowImageEffects.DitherMode.Linear;
                if (needsAngle)
                    DrawPropertyField(ditherAngle, "Line Angle");

                bool needsBlueNoise = modeIndex == (int)CrowImageEffects.DitherMode.BlueNoise;
                if (needsBlueNoise)
                {
                    DrawPropertyField(blueNoise, "Blue Noise Texture");

                    bool showedDiagnostic = false;
                    if (string.IsNullOrWhiteSpace(_search) || PassesSearch("blue noise texture 128x128 square"))
                    {
                        if (blueNoise != null && blueNoise.objectReferenceValue == null)
                        {
                            showedDiagnostic = true;
                            DrawActionHint("Blue noise mode needs a texture source.", "Use Ordered 4x4",
                                () =>
                                {
                                    if (ditherMode != null)
                                        ditherMode.enumValueIndex = (int)CrowImageEffects.DitherMode.Ordered4x4;
                                    ApplySerializedChanges();
                                },
                                CrowFxEditorUI.HintType.Error);
                        }
                        else
                        {
                            showedDiagnostic = DrawBlueNoiseTextureDiagnostics(blueNoise, "Blue noise requires a texture (typically 128x128 or larger).", CrowFxEditorUI.HintType.Error);
                        }
                    }

                    if (!showedDiagnostic)
                        CrowFxEditorUI.Hint("Blue noise provides more organic grain than Bayer patterns.");
                }
                else if (modeIndex == (int)CrowImageEffects.DitherMode.Linear)
                {
                    if (string.IsNullOrWhiteSpace(_search) || PassesSearch("line angle directional bands"))
                        CrowFxEditorUI.Hint("Linear uses directional threshold bands. Rotate Line Angle to steer the pattern.");
                }
                else if (modeIndex == (int)CrowImageEffects.DitherMode.Halftone)
                {
                    if (string.IsNullOrWhiteSpace(_search) || PassesSearch("halftone dot matrix print"))
                        CrowFxEditorUI.Hint("Halftone creates a repeating dot matrix look with softer clustered breakup.");
                }
                else if (modeIndex == (int)CrowImageEffects.DitherMode.Diamond)
                {
                    if (string.IsNullOrWhiteSpace(_search) || PassesSearch("diamond rhombus pattern"))
                        CrowFxEditorUI.Hint("Diamond uses a clustered rhombus threshold for a sharper geometric texture.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(_search) || PassesSearch("noise before quantization banding"))
                        CrowFxEditorUI.Hint("Adds structured noise before quantization to reduce banding.");
                }
            }

            if (!hasDither)
            {
                if (string.IsNullOrWhiteSpace(_search) || PassesSearch("off quantization banding"))
                    CrowFxEditorUI.Hint("Pattern is Off, so only posterization remains. Enable a dither pattern to reduce visible banding.");
            }

            MarkDrawnMany("ditherMode", "ditherStrength", "ditherAngle", "blueNoise");

            DrawAutoRemaining(SectionKeys.Dither);
        }

        private void DrawShadersContent()
        {
            BeginSectionDrawn();

            if (string.IsNullOrWhiteSpace(_search) || PassesSearch("shader path auto-find renamed"))
                CrowFxEditorUI.Hint("Leave empty to auto-find shaders by name. Only assign if you've renamed shader paths.");

            GUILayout.Space(6);

            // No manual properties here; just auto draw.
            DrawAutoRemaining(SectionKeys.Shaders);
        }

        // =============================================================================================
        // RESOLUTION PRESETS
        // =============================================================================================
        private void DrawResolutionPresets()
        {
            var virtualResolution = SP("virtualResolution");
            var useVirtualGrid = SP("useVirtualGrid");

            void SetRes(int w, int h)
            {
                if (virtualResolution == null) return;
                virtualResolution.vector2IntValue = new Vector2Int(Mathf.Max(1, w), Mathf.Max(1, h));
                GUI.FocusControl(null);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("120p", GUILayout.ExpandWidth(true))) SetRes(160, 120);
                if (CrowFxEditorUI.MiniPill("144p", GUILayout.ExpandWidth(true))) SetRes(256, 144);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("224p", GUILayout.ExpandWidth(true))) SetRes(256, 224);
                if (CrowFxEditorUI.MiniPill("240p", GUILayout.ExpandWidth(true))) SetRes(320, 240);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("240p (Wide)", GUILayout.ExpandWidth(true))) SetRes(426, 240);
                if (CrowFxEditorUI.MiniPill("200p (PC)", GUILayout.ExpandWidth(true))) SetRes(320, 200);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("288p", GUILayout.ExpandWidth(true))) SetRes(384, 288);
                if (CrowFxEditorUI.MiniPill("288p (Wide)", GUILayout.ExpandWidth(true))) SetRes(512, 288);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("300p", GUILayout.ExpandWidth(true))) SetRes(400, 300);
                if (CrowFxEditorUI.MiniPill("360p", GUILayout.ExpandWidth(true))) SetRes(640, 360);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("360p (Wide)", GUILayout.ExpandWidth(true))) SetRes(854, 360);
                if (CrowFxEditorUI.MiniPill("384p", GUILayout.ExpandWidth(true))) SetRes(512, 384);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("448p", GUILayout.ExpandWidth(true))) SetRes(512, 448);
                if (CrowFxEditorUI.MiniPill("448p (Hi)", GUILayout.ExpandWidth(true))) SetRes(640, 448);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("480p", GUILayout.ExpandWidth(true))) SetRes(640, 480);
                if (CrowFxEditorUI.MiniPill("480p (Wide)", GUILayout.ExpandWidth(true))) SetRes(720, 480);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("480p (16:9)", GUILayout.ExpandWidth(true))) SetRes(854, 480);
                if (CrowFxEditorUI.MiniPill("480p (WS DVD)", GUILayout.ExpandWidth(true))) SetRes(720, 405);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("600p", GUILayout.ExpandWidth(true))) SetRes(800, 600);
                if (CrowFxEditorUI.MiniPill("540p", GUILayout.ExpandWidth(true))) SetRes(960, 540);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("576p", GUILayout.ExpandWidth(true))) SetRes(720, 576);
                if (CrowFxEditorUI.MiniPill("576p (Wide)", GUILayout.ExpandWidth(true))) SetRes(1024, 576);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("720p", GUILayout.ExpandWidth(true))) SetRes(1280, 720);
                if (CrowFxEditorUI.MiniPill("768p", GUILayout.ExpandWidth(true))) SetRes(1024, 768);
            }
            GUILayout.Space(3);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("768p (WXGA)", GUILayout.ExpandWidth(true))) SetRes(1366, 768);
                if (CrowFxEditorUI.MiniPill("1080p", GUILayout.ExpandWidth(true))) SetRes(1920, 1080);
            }

            GUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (CrowFxEditorUI.MiniPill("Reset (640x448)", GUILayout.ExpandWidth(true))) SetRes(640, 448);
                if (CrowFxEditorUI.MiniPill("Screen", GUILayout.ExpandWidth(true)) && useVirtualGrid != null) useVirtualGrid.boolValue = false;
            }
        }

        // =============================================================================================
        // RESET ALL
        // =============================================================================================
        private void ResetToDefaults()
        {
            RestorePreviewStatesIfNeeded();
            ResetPropertiesToDefaults(GetTopLevelSerializedPropertyNames(serializedObject), "Reset All Effects");
        }

        // =============================================================================================
        // DEPTH MODE
        // =============================================================================================
        private void EnsureDepthModeIfNeeded(CrowImageEffects targetFx)
        {
            if (targetFx == null) return;
            if (!targetFx.useDepthMask && !targetFx.edgeEnabled) return;

            var camera = targetFx.GetComponent<Camera>();
            if (camera != null && (camera.depthTextureMode & DepthTextureMode.Depth) == 0)
            {
                camera.depthTextureMode |= DepthTextureMode.Depth;
                EditorUtility.SetDirty(camera);
            }
        }

        // MISC
        private void LoadFavorites()
        {
            _favoriteSections.Clear();
            var raw = EditorPrefs.GetString(Pref_Favorites, "");
            if (!string.IsNullOrEmpty(raw))
                foreach (var k in raw.Split(','))
                    if (!string.IsNullOrEmpty(k)) _favoriteSections.Add(k);
        }

        private void SaveFavorites()
        {
            EditorPrefs.SetString(Pref_Favorites, string.Join(",", _favoriteSections));
        }

        private void ToggleFavorite(string sectionKey)
        {
            if (!_favoriteSections.Add(sectionKey))
                _favoriteSections.Remove(sectionKey);
            SaveFavorites();
            RebuildAll(); // re-sort sections
        }
    }
}
#endif
