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
            public static GUIStyle SectionTitle;
            public static GUIStyle SummaryText;
            public static GUIStyle HintText;
            public static GUIStyle PillButton;
            public static GUIStyle ResetButton;
            public static GUIStyle SubHeaderLabel;

            public static GUIStyle SearchField;
            public static GUIStyle SearchCancel;

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
                PillButton.font     = font;
                ResetButton.font    = font;
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

            float labelWidth = EditorGUIUtility.currentViewWidth - 48f;
            float height = Mathf.Max(18f, Styles.HintText.CalcHeight(content, labelWidth) + 6f);

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

        // =============================================================================================
        // PILLS
        // =============================================================================================
        internal static bool MiniPill(string label, params GUILayoutOption[] options)
            => PillButton(label, 18f, Styles.PillButton, options);

        internal static bool ResetPill(string label, params GUILayoutOption[] options)
            => PillButton(label, 18f, Styles.ResetButton, options);

        internal static bool PillButton(string label, float height, GUIStyle style, params GUILayoutOption[] options)
        {
            var rect = GUILayoutUtility.GetRect(0f, height, options);

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
            }

            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);

            var prev = GUI.contentColor;
            GUI.contentColor = GUI.enabled ? Color.white : new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(rect, label ?? "", style);
            GUI.contentColor = prev;

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