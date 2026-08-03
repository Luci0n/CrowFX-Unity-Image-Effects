#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CrowFX.EditorTools
{
    internal static class CrowFxEditorUI
    {
        internal enum HintType { Info, Warning, Error }

        // =============================================================================================
        // THEME
        // =============================================================================================
        internal static class Theme
        {
            public static readonly Color PanelBackground   = new Color(0.13f, 0.13f, 0.13f, 1f);
            public static readonly Color HeaderBackground  = new Color(0.16f, 0.16f, 0.16f, 1f);
            public static readonly Color BorderColor       = new Color(0f, 0f, 0f, 0.35f);
            public static readonly Color DividerColor      = new Color(1f, 1f, 1f, 0.06f);
            public static readonly Color TextPrimary       = new Color(1f, 1f, 1f, 0.86f);
            public static readonly Color TextSecondary     = new Color(1f, 1f, 1f, 0.70f);
            public static readonly Color HintBackground    = new Color(0f, 0f, 0f, 0.30f);
            public static readonly Color WarningBackground = new Color(1f, 1f, 1f, 0.065f);
            public static readonly Color ErrorBackground   = new Color(1f, 1f, 1f, 0.085f);

            public static readonly Color ButtonNormal      = new Color(1f, 1f, 1f, 0.055f);
            public static readonly Color ButtonHover       = new Color(1f, 1f, 1f, 0.085f);
            public static readonly Color ButtonActive      = new Color(1f, 1f, 1f, 0.12f);

            public static void DrawBorder(Rect rect)
            {
                if (Event.current.type != EventType.Repaint) return;
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), BorderColor);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), BorderColor);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), BorderColor);
                EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), BorderColor);
            }

            public static void DrawDivider(float padding = 2f)
            {
                var rect = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
                rect.xMin += padding;
                rect.xMax -= padding;

                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(rect, DividerColor);
            }
        }

        // =============================================================================================
        // ICON CACHE
        // =============================================================================================
        internal static class IconCache
        {
            private static readonly Dictionary<string, Texture> Cache = new(StringComparer.Ordinal);

            public static Texture Get(string name)
            {
                if (string.IsNullOrEmpty(name)) return null;
                if (Cache.TryGetValue(name, out var cached)) return cached;

                var content = EditorGUIUtility.IconContent(name.StartsWith("d_", StringComparison.Ordinal) ? name : "d_" + name);
                var texture = content?.image;

                if (texture == null)
                {
                    content = EditorGUIUtility.IconContent(name);
                    texture = content?.image;
                }

                Cache[name] = texture;
                return texture;
            }
        }

        // =============================================================================================
        // STYLES
        // =============================================================================================
        internal static class Styles
        {
            private static bool _initialized;
            private static Font _appliedFont;

            public static Texture2D PanelTexture;
            public static Texture2D HeaderTexture;

            public static GUIStyle Panel;
            public static GUIStyle HeaderLabel;
            public static GUIStyle HeaderHint;
            public static GUIStyle RowDetail;
            public static GUIStyle SectionTitle;
            public static GUIStyle SummaryText;
            public static GUIStyle HintText;
            public static GUIStyle PillButton;
            public static GUIStyle ResetButton;
            public static GUIStyle SubHeaderLabel;

            public static GUIStyle SearchField;
            public static GUIStyle SearchCancel;
            public static GUIStyle PopupLabel;
            public static GUIStyle PopupValue;
            public static GUIStyle PopupArrow;

            public static void Ensure()
            {
                if (_initialized) return;

                PanelTexture  = CreateColorTexture(Theme.PanelBackground);
                HeaderTexture = CreateColorTexture(Theme.HeaderBackground);

                Panel = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 8, 10),
                    margin  = new RectOffset(0, 0, 6, 6),
                    normal  = { background = PanelTexture }
                };

                HeaderLabel = new GUIStyle(EditorStyles.label)
                {
                    fontSize  = 12,
                    alignment = TextAnchor.MiddleLeft,
                    normal    = { textColor = Color.white }
                };

                SubHeaderLabel = new GUIStyle(EditorStyles.label)
                {
                    fontSize  = 11,
                    alignment = TextAnchor.MiddleLeft,
                    normal    = { textColor = Color.white }
                };

                HeaderHint = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    richText  = true,
                    normal    = { textColor = Theme.TextSecondary }
                };

                RowDetail = new GUIStyle(HeaderHint)
                {
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true,
                    clipping = TextClipping.Clip
                };

                SectionTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize  = 13,
                    alignment = TextAnchor.MiddleLeft,
                    normal    = { textColor = Color.white }
                };

                SummaryText = new GUIStyle(EditorStyles.miniLabel)
                {
                    richText = true,
                    normal   = { textColor = Theme.TextPrimary }
                };

                HintText = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    richText = true,
                    normal   = { textColor = Theme.TextPrimary }
                };

                PillButton = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding   = new RectOffset(10, 10, 0, 0),
                    fontSize  = 11,
                    normal    = { textColor = Theme.TextPrimary }
                };

                ResetButton = new GUIStyle(PillButton)
                {
                    fontSize  = 12,
                    alignment = TextAnchor.MiddleCenter,
                    normal    = { textColor = Color.white }
                };

                SearchField  = GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.textField;
                SearchCancel = GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? GUI.skin.button;

                PopupLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    fontSize = 11
                };
                PopupLabel.normal.textColor = Theme.TextSecondary;

                PopupValue = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    fontSize = 10,
                    padding = new RectOffset(0, 0, 1, 0)
                };
                PopupValue.normal.textColor = Theme.TextSecondary;

                PopupArrow = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip,
                    fontSize = 11
                };
                PopupArrow.normal.textColor = new Color(0.48f, 0.72f, 0.76f, 0.92f);

                _initialized = true;
            }

            public static void ApplyFont(Font font)
            {
                if (font == null || font == _appliedFont) return;
                _appliedFont = font;

                HeaderLabel.font    = font;
                SubHeaderLabel.font = font;
                SectionTitle.font   = font;
                SummaryText.font    = font;
                HintText.font       = font;
                HeaderHint.font     = font;
                RowDetail.font      = font;
                PillButton.font     = font;
                ResetButton.font    = font;
                PopupLabel.font     = font;
                PopupValue.font     = font;
                PopupArrow.font     = font;
            }

            private static Texture2D CreateColorTexture(Color color)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                tex.SetPixel(0, 0, color);
                tex.Apply();
                return tex;
            }
        }

        // =============================================================================================
        // PUBLIC ENTRY
        // =============================================================================================
        internal static void Ensure(Font font = null)
        {
            Styles.Ensure();
            if (font != null) Styles.ApplyFont(font);
        }

        internal static float CompactControlHeight(GUIStyle style, float minimum = 22f)
        {
            if (style == null) return minimum;
            float measured = style.CalcHeight(new GUIContent("Ag"), 256f);
            return Mathf.Max(minimum, Mathf.Ceil(measured + 6f));
        }

        internal static float ContentWidth(GUIStyle style, string text, float minimum = 0f)
        {
            if (style == null) return minimum;
            return Mathf.Max(minimum, Mathf.Ceil(style.CalcSize(new GUIContent(text ?? "")).x + 8f));
        }

        internal static void WrappedLabel(string message, GUIStyle style)
        {
            var content = new GUIContent(message ?? "");
            float width = Mathf.Max(40f, EditorGUIUtility.currentViewWidth - 36f);
            float height = Mathf.Max(CompactControlHeight(style), style.CalcHeight(content, width) + 4f);
            EditorGUILayout.LabelField(content, style, GUILayout.Height(height), GUILayout.ExpandWidth(true));
        }

        internal static IDisposable PanelScope()
            => new EditorGUILayout.VerticalScope(Styles.Panel);

        internal static void Divider(float padding = 2f)
            => Theme.DrawDivider(padding);

        internal static void DrawHeaderBackground(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(rect, Styles.HeaderTexture, ScaleMode.StretchToFill);
            Theme.DrawBorder(rect);
        }

        // =============================================================================================
        // SEARCH BAR (writes EditorPrefs, returns true if changed)
        // =============================================================================================
        internal static bool SearchBar(string label, ref string value, string prefsKey)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.SetNextControlName("CrowFX_Search");
                var next = EditorGUILayout.TextField(new GUIContent(label), value ?? "", Styles.SearchField);

                bool changed = !string.Equals(next, value, StringComparison.Ordinal);
                if (changed)
                {
                    value = next ?? "";
                    if (!string.IsNullOrEmpty(prefsKey))
                        EditorPrefs.SetString(prefsKey, value);
                }

                var clearRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f));
                if (GUI.Button(clearRect, GUIContent.none, Styles.SearchCancel))
                {
                    value = "";
                    if (!string.IsNullOrEmpty(prefsKey))
                        EditorPrefs.SetString(prefsKey, value);

                    GUI.FocusControl(null);
                    changed = true;
                }

                return changed;
            }
        }

        // =============================================================================================
        // HINT BOX
        // =============================================================================================
        internal static void Hint(string message, HintType type = HintType.Info)
        {
            var content = new GUIContent(message ?? "");

            // currentViewWidth includes the inspector chrome and any containing panel. Keep the
            // estimate conservative so narrow/nested inspectors reserve every wrapped line.
            float labelWidth = Mathf.Max(40f, EditorGUIUtility.currentViewWidth - 64f);
            float height = Mathf.Max(CompactControlHeight(Styles.HintText), Styles.HintText.CalcHeight(content, labelWidth) + 10f);

            var rect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            rect.xMin += 2f;
            rect.xMax -= 2f;

            Color bg = type switch
            {
                HintType.Warning => Theme.WarningBackground,
                HintType.Error   => Theme.ErrorBackground,
                _                => Theme.HintBackground
            };

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, bg);
                Theme.DrawBorder(rect);
            }

            var labelRect = new Rect(rect.x + 6f, rect.y + 3f, rect.width - 12f, rect.height - 6f);
            var prev = GUI.contentColor;
            GUI.contentColor = Theme.TextPrimary;
            GUI.Label(labelRect, content, Styles.HintText);
            GUI.contentColor = prev;
        }

        internal static bool HintWithAction(string message, string actionLabel, HintType type = HintType.Info, float actionWidth = 108f, bool actionEnabled = true)
        {
            var content = new GUIContent(message ?? "");

            float desiredActionWidth = ContentWidth(Styles.PillButton, actionLabel, actionWidth);
            float maxActionWidth = Mathf.Max(72f, EditorGUIUtility.currentViewWidth - 112f);
            float resolvedActionWidth = Mathf.Min(desiredActionWidth, maxActionWidth);
            float labelWidth = Mathf.Max(40f, EditorGUIUtility.currentViewWidth - resolvedActionWidth - 76f);
            float buttonHeight = CompactControlHeight(Styles.PillButton);
            float height = Mathf.Max(buttonHeight + 8f, Styles.HintText.CalcHeight(content, labelWidth) + 12f);

            var rect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            rect.xMin += 2f;
            rect.xMax -= 2f;

            Color bg = type switch
            {
                HintType.Warning => Theme.WarningBackground,
                HintType.Error   => Theme.ErrorBackground,
                _                => Theme.HintBackground
            };

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, bg);
                Theme.DrawBorder(rect);
            }

            var buttonRect = new Rect(rect.xMax - resolvedActionWidth - 6f, rect.y + (rect.height - buttonHeight) * 0.5f, resolvedActionWidth, buttonHeight);
            var labelRect = new Rect(rect.x + 6f, rect.y + 4f, Mathf.Max(40f, buttonRect.x - rect.x - 12f), rect.height - 8f);

            var prev = GUI.contentColor;
            GUI.contentColor = Theme.TextPrimary;
            GUI.Label(labelRect, content, Styles.HintText);
            GUI.contentColor = prev;

            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && actionEnabled;
            bool clicked = DrawPill(buttonRect, actionLabel ?? "", Styles.PillButton);
            GUI.enabled = oldEnabled;

            return clicked;
        }

        // =============================================================================================
        // PILLS
        // =============================================================================================
        internal static bool MiniPill(string label, params GUILayoutOption[] options)
            => PillButton(label, CompactControlHeight(Styles.PillButton), Styles.PillButton, options);

        internal static bool SelectionPill(string label, bool selected, string tooltip = null, params GUILayoutOption[] options)
        {
            var rect = GUILayoutUtility.GetRect(0f, CompactControlHeight(Styles.PillButton), options);
            return DrawPill(rect, label ?? "", Styles.PillButton, clickable: true, tooltip: tooltip,
                tint: selected ? Theme.ButtonActive : (Color?)null);
        }

        // Popup selections are queued so the owning inspector applies serialized changes during
        // its normal IMGUI and Undo lifecycle on the next repaint.
        private static readonly Dictionary<string, int> PendingPopupSelections = new(StringComparer.Ordinal);
        private static ThemedPopupWindow _activePopupWindow;
        private static string _activePopupKey;
        private static string _suppressPopupOpenKey;
        private static double _suppressPopupOpenUntil;

        internal static int ThemedPopup(string key, int current, string[] options, params GUILayoutOption[] layoutOptions)
        {
            var rect = GUILayoutUtility.GetRect(0f, CompactControlHeight(Styles.PopupValue), layoutOptions);
            return ThemedPopup(rect, key, current, options);
        }

        internal static int ThemedPopup(string key, GUIContent label, int current, string[] options)
        {
            float controlHeight = Mathf.Max(CompactControlHeight(Styles.PopupLabel), CompactControlHeight(Styles.PopupValue));
            var rect = EditorGUILayout.GetControlRect(false, controlHeight);
            var indentedRect = EditorGUI.IndentedRect(rect);
            float labelWidth = Mathf.Clamp(EditorGUIUtility.labelWidth, 72f, Mathf.Max(72f, indentedRect.width - 124f));
            var labelRect = new Rect(indentedRect.x, indentedRect.y, Mathf.Max(40f, labelWidth - 4f), controlHeight);
            var fieldRect = new Rect(labelRect.xMax + 4f, indentedRect.y, Mathf.Max(40f, indentedRect.xMax - labelRect.xMax - 4f), controlHeight);
            EditorGUI.LabelField(labelRect, label ?? GUIContent.none, Styles.PopupLabel);
            return ThemedPopup(fieldRect, key, current, options);
        }

        internal static int ThemedPopup(Rect rect, string key, int current, string[] options)
        {
            if (options == null || options.Length == 0) return current;
            key ??= string.Empty;
            current = Mathf.Clamp(current, 0, options.Length - 1);

            if (PendingPopupSelections.TryGetValue(key, out int pending))
            {
                PendingPopupSelections.Remove(key);
                current = Mathf.Clamp(pending, 0, options.Length - 1);
                GUI.changed = true;
            }

            bool isOpen = _activePopupWindow != null && string.Equals(_activePopupKey, key, StringComparison.Ordinal);
            if (DrawThemedPopupField(rect, current, options, isOpen))
            {
                if (isOpen)
                    CloseActivePopupAnimated();
                else if (!ConsumePopupReopenSuppression(key))
                    ShowPopupWindow(key, rect, current, options);
            }

            return current;
        }

        internal static void CloseActivePopup()
        {
            if (_activePopupWindow != null)
                _activePopupWindow.Close();
            ClearActivePopupState();
        }

        private static bool DrawThemedPopupField(Rect rect, int current, string[] options, bool isOpen)
        {
            Event evt = Event.current;
            bool hovered = rect.Contains(evt.mousePosition) && GUI.enabled;
            bool active = isOpen || (GUIUtility.hotControl != 0 && hovered);

            if (evt.type == EventType.Repaint)
            {
                Color fill = active ? Theme.ButtonActive : hovered ? Theme.ButtonHover : Theme.ButtonNormal;
                Color edge = active
                    ? new Color(0.38f, 0.82f, 0.86f, 0.88f)
                    : hovered ? new Color(0.38f, 0.82f, 0.86f, 0.56f) : Theme.BorderColor;

                EditorGUI.DrawRect(rect, fill);
                Theme.DrawBorder(rect);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), edge);

                var textRect = new Rect(rect.x + 8f, rect.y, Mathf.Max(0f, rect.width - 30f), rect.height);
                var arrowRect = new Rect(rect.xMax - 20f, rect.y, 16f, rect.height);
                GUI.Label(textRect, options[current], Styles.PopupValue);
                GUI.Label(arrowRect, isOpen ? "▴" : "▾", Styles.PopupArrow);
            }

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private static void ShowPopupWindow(string key, Rect anchorRect, int current, string[] options)
        {
            CloseActivePopup();
            var window = new ThemedPopupWindow();
            window.Initialize(key, options, current, Mathf.Max(120f, anchorRect.width));
            _activePopupWindow = window;
            _activePopupKey = key;
            PopupWindow.Show(anchorRect, window);
        }

        private static void QueuePopupSelection(string key, int selected)
        {
            PendingPopupSelections[key] = selected;
            GUI.changed = true;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static void SuppressPopupReopen(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _suppressPopupOpenKey = key;
            _suppressPopupOpenUntil = EditorApplication.timeSinceStartup + 0.30;
        }

        private static bool ConsumePopupReopenSuppression(string key)
        {
            if (string.IsNullOrEmpty(_suppressPopupOpenKey)) return false;
            if (EditorApplication.timeSinceStartup > _suppressPopupOpenUntil)
            {
                _suppressPopupOpenKey = null;
                _suppressPopupOpenUntil = 0.0;
                return false;
            }

            if (!string.Equals(_suppressPopupOpenKey, key, StringComparison.Ordinal)) return false;
            _suppressPopupOpenKey = null;
            _suppressPopupOpenUntil = 0.0;
            return true;
        }

        private static void CloseActivePopupAnimated()
        {
            if (_activePopupWindow == null)
            {
                ClearActivePopupState();
                return;
            }

            SuppressPopupReopen(_activePopupKey);
            _activePopupWindow.BeginClose();
        }

        private static void ClearActivePopupState()
        {
            _activePopupWindow = null;
            _activePopupKey = null;
        }

        private sealed class ThemedPopupWindow : PopupWindowContent
        {
            private const float Padding = 2f;
            private const double AnimationDuration = 0.12;
            private static float RowHeight => CompactControlHeight(Styles.PopupValue);

            private string _key;
            private string[] _options = Array.Empty<string>();
            private int _current;
            private int _hovered = -1;
            private float _requestedWidth;
            private Vector2 _scroll;
            private double _openTime;
            private double _closeTime;
            private bool _closing;
            private bool _closed;

            internal void Initialize(string key, string[] options, int current, float requestedWidth)
            {
                _key = key;
                _options = options ?? Array.Empty<string>();
                _current = Mathf.Clamp(current, 0, Mathf.Max(0, _options.Length - 1));
                _requestedWidth = requestedWidth;
                _openTime = EditorApplication.timeSinceStartup;
            }

            public override Vector2 GetWindowSize()
            {
                float height = Mathf.Min(352f, Mathf.Max(26f, _options.Length * RowHeight + Padding * 2f));
                return new Vector2(_requestedWidth, height);
            }

            public override void OnOpen()
            {
                _openTime = EditorApplication.timeSinceStartup;
                if (editorWindow != null) editorWindow.wantsMouseMove = true;
            }

            internal void BeginClose()
            {
                if (_closing) return;
                _closing = true;
                _closeTime = EditorApplication.timeSinceStartup;
                editorWindow?.Repaint();
            }

            internal void Close()
            {
                if (editorWindow != null) editorWindow.Close();
                else NotifyClosed();
            }

            public override void OnClose()
            {
                SuppressPopupReopen(_key);
                NotifyClosed();
            }

            private void NotifyClosed()
            {
                if (_closed) return;
                _closed = true;
                if (_activePopupWindow == this) ClearActivePopupState();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }

            public override void OnGUI(Rect windowRect)
            {
                Event evt = Event.current;
                double now = EditorApplication.timeSinceStartup;
                float rawT = _closing
                    ? 1f - (float)((now - _closeTime) / AnimationDuration)
                    : (float)((now - _openTime) / AnimationDuration);
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(rawT));

                if (_closing && rawT <= 0f)
                {
                    Close();
                    return;
                }

                float contentHeight = _options.Length * RowHeight + Padding * 2f;
                bool needsScroll = contentHeight > windowRect.height;
                Rect viewport = new Rect(0f, 0f, windowRect.width, windowRect.height);
                Rect content = new Rect(0f, 0f, needsScroll ? windowRect.width - 13f : windowRect.width, contentHeight);

                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(viewport, new Color(0.085f, 0.09f, 0.095f, t));
                    Theme.DrawBorder(viewport);
                }

                if (needsScroll) _scroll = GUI.BeginScrollView(viewport, _scroll, content, false, true);
                float slide = Mathf.Lerp(-6f, 0f, t);
                Rect panel = new Rect(0f, slide, content.width, content.height);

                int hovered = -1;
                for (int i = 0; i < _options.Length; i++)
                {
                    Rect row = GetRowRect(panel, i);
                    if (row.Contains(evt.mousePosition)) hovered = i;
                }
                if (hovered != _hovered)
                {
                    _hovered = hovered;
                    editorWindow?.Repaint();
                }

                if (evt.type == EventType.MouseDown && evt.button == 0 && _hovered >= 0)
                {
                    QueuePopupSelection(_key, _hovered);
                    evt.Use();
                    BeginClose();
                }
                else if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
                {
                    evt.Use();
                    BeginClose();
                }

                if (evt.type == EventType.Repaint)
                    DrawRows(panel, t);

                if (_hovered >= 0)
                    EditorGUIUtility.AddCursorRect(GetRowRect(panel, _hovered), MouseCursor.Link);
                if (needsScroll) GUI.EndScrollView();
                if (!_closing && rawT < 1f || _closing) editorWindow?.Repaint();
            }

            private static Rect GetRowRect(Rect panel, int index)
                => new(panel.x + Padding, panel.y + Padding + index * RowHeight, Mathf.Max(0f, panel.width - Padding * 2f), RowHeight);

            private void DrawRows(Rect panel, float t)
            {
                for (int i = 0; i < _options.Length; i++)
                {
                    Rect row = GetRowRect(panel, i);
                    bool selected = i == _current;
                    bool hovered = i == _hovered;
                    Color fill = selected
                        ? new Color(0.18f, 0.45f, 0.50f, 0.60f * t)
                        : hovered ? new Color(1f, 1f, 1f, 0.08f * t) : new Color(1f, 1f, 1f, 0.018f * t);
                    EditorGUI.DrawRect(row, fill);
                    if (selected) EditorGUI.DrawRect(new Rect(row.x, row.y, 2f, row.height), new Color(0.38f, 0.82f, 0.86f, t));

                    var style = new GUIStyle(Styles.PopupValue)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(selected ? 10 : 8, 4, 0, 0)
                    };
                    style.normal.textColor = new Color(1f, 1f, 1f, (selected || hovered ? 0.94f : 0.70f) * t);
                    GUI.Label(row, _options[i], style);
                }
            }
        }

        internal static bool ResetPill(string label, params GUILayoutOption[] options)
            => PillButton(label, CompactControlHeight(Styles.ResetButton), Styles.ResetButton, options);

        internal static bool PillButton(string label, float height, GUIStyle style, params GUILayoutOption[] options)
        {
            var rect = GUILayoutUtility.GetRect(0f, height, options);
            return DrawPill(rect, label, style);
        }

        internal static void TagPill(string label, Color? tint = null, params GUILayoutOption[] options)
        {
            var rect = GUILayoutUtility.GetRect(0f, CompactControlHeight(Styles.PillButton), options);
            DrawPill(rect, label ?? "", Styles.PillButton, clickable: false, tint: tint);
        }

        internal static bool HeaderPill(Rect rect, string label, string tooltip = null, bool active = false)
        {
            return DrawPill(rect, label ?? "", Styles.PillButton, clickable: true, tooltip: tooltip, tint: active ? Theme.ButtonActive : (Color?)null);
        }

        private static bool DrawPill(Rect rect, string label, GUIStyle style, bool clickable = true, string tooltip = null, Color? tint = null)
        {
            bool isHovered = rect.Contains(Event.current.mousePosition);
            bool isHot     = GUIUtility.hotControl != 0 && isHovered;
            bool isPressed = isHovered && Event.current.type == EventType.MouseDown && Event.current.button == 0;

            Color baseTint = tint ?? Theme.ButtonNormal;
            Color bg = !GUI.enabled ? new Color(baseTint.r, baseTint.g, baseTint.b, 0.03f)
                     : (isPressed || isHot) ? Theme.ButtonActive
                     : isHovered ? Theme.ButtonHover
                     : baseTint;

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, bg);
                Theme.DrawBorder(rect);
            }

            bool clicked = clickable && GUI.Button(rect, GUIContent.none, GUIStyle.none);

            var prev = GUI.contentColor;
            GUI.contentColor = GUI.enabled ? Color.white : new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(rect, label ?? "", style);
            GUI.contentColor = prev;

            if (!string.IsNullOrEmpty(tooltip) && isHovered)
                GUI.Label(rect, new GUIContent("", tooltip), GUIStyle.none);

            return clicked;
        }

        internal static bool IconPill(Texture icon, string tooltip, float size = 18f)
        {
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            return IconPill(rect, icon, tooltip);
        }

        internal static bool IconPill(Rect rect, Texture icon, string tooltip)
        {
            bool isHovered = rect.Contains(Event.current.mousePosition);
            bool isHot     = GUIUtility.hotControl != 0 && isHovered;
            bool isPressed = isHovered && Event.current.type == EventType.MouseDown && Event.current.button == 0;

            Color bg = !GUI.enabled ? new Color(1f, 1f, 1f, 0.03f)
                     : (isPressed || isHot) ? Theme.ButtonActive
                     : isHovered ? Theme.ButtonHover
                     : Theme.ButtonNormal;

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, bg);
                Theme.DrawBorder(rect);

                if (icon != null)
                {
                    float pad = 2f;
                    var iconRect = new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2f, rect.height - pad * 2f);
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                }
            }

            if (!string.IsNullOrEmpty(tooltip) && isHovered)
                GUI.Label(rect, new GUIContent("", tooltip), GUIStyle.none);

            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            if (clicked) Event.current.Use();
            return clicked;
        }

        internal static bool HeaderResetPill(Rect rect, string label)
        {
            bool isHovered = rect.Contains(Event.current.mousePosition);
            bool isHot     = GUIUtility.hotControl != 0 && isHovered;
            bool isPressed = isHovered && Event.current.type == EventType.MouseDown && Event.current.button == 0;

            Color bg = (isPressed || isHot) ? Theme.ButtonActive
                     : isHovered ? Theme.ButtonHover
                     : Theme.ButtonNormal;

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, bg);
                Theme.DrawBorder(rect);
            }

            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);

            var prev = GUI.contentColor;
            GUI.contentColor = Color.white;
            GUI.Label(rect, new GUIContent(label ?? ""), Styles.ResetButton);
            GUI.contentColor = prev;

            if (clicked) Event.current.Use();
            return clicked;
        }
    }
}
#endif
