#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CrowFX.EditorTools
{
    /// <summary>
    /// Schematic preview bodies for the few sections that cannot be shown by rendering their
    /// stage over a test chart. Everything that is a pure image operation uses
    /// CrowImageEffectsEditor.DrawLivePreview instead, which runs the real shader and therefore
    /// reacts to every control automatically.
    ///
    /// What remains here needs inputs a standalone blit cannot supply:
    ///   Edge Outline and Depth Mask read scene depth and normals.
    ///   Texture Mask blends two images through a user texture.
    ///   Palette swatches describe the source data rather than the result.
    ///
    /// Each drawer receives the content rect DrawMiniPreview has already framed.
    /// </summary>
    internal static class CrowFxPreviewDrawers
    {
        private static readonly Color Accent = new Color(1f, 0.75f, 0.35f, 0.95f);

        private static GUIStyle _caption;

        private static GUIStyle Caption =>
            _caption ??= new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 1f, 1f, 0.55f) },
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Clip
            };

        private static void DrawCaption(Rect rect, string text)
        {
            GUI.Label(new Rect(rect.x, rect.yMax - 13f, rect.width, 13f), text, Caption);
        }

        // =========================================================================================
        // SAMPLING READOUT
        // =========================================================================================

        /// <summary>Cell aspect, source texels per cell and tap count for the current settings.
        /// The rendered preview shows the resulting image but cannot convey these numbers, and
        /// they are the part that is impossible to work out from the sliders alone.</summary>
        public static string DescribeSampling(
            int pixelSize,
            bool useVirtualGrid,
            Vector2Int virtualResolution,
            float pixelAspect,
            CrowImageEffects.SamplingFilter filter,
            int sourceWidth,
            int sourceHeight)
        {
            float srcW = Mathf.Max(1, sourceWidth);
            float srcH = Mathf.Max(1, sourceHeight);
            float gridX = (useVirtualGrid ? Mathf.Max(1, virtualResolution.x) : srcW) / Mathf.Max(pixelAspect, 0.001f);
            float gridY = useVirtualGrid ? Mathf.Max(1, virtualResolution.y) : srcH;
            float block = Mathf.Max(1, pixelSize);

            float cellsX = Mathf.Max(1f, gridX / block);
            float cellsY = Mathf.Max(1f, gridY / block);
            float texelsX = srcW / cellsX;
            float texelsY = srcH / cellsY;
            int taps = SamplingTapsPerAxis(texelsX, texelsY, filter);

            string tapText = filter switch
            {
                CrowImageEffects.SamplingFilter.Box => $"{taps}x{taps} taps",
                CrowImageEffects.SamplingFilter.Bilinear => "1 filtered tap",
                _ => "1 point tap"
            };

            return $"at {srcW:0}x{srcH:0}: {cellsX:0}x{cellsY:0} cells · {texelsX:0.#}x{texelsY:0.#} texels/cell · {tapText}";
        }

        /// <summary>Mirrors the shader's footprint rule so the readout cannot drift from it:
        /// one tap per two covered texels per axis, capped at 8.</summary>
        private static int SamplingTapsPerAxis(float texelsX, float texelsY, CrowImageEffects.SamplingFilter filter)
        {
            if (filter != CrowImageEffects.SamplingFilter.Box) return 1;
            float worst = Mathf.Max(texelsX, texelsY);
            return Mathf.Clamp(Mathf.CeilToInt(worst * 0.5f), 1, 8);
        }

        /// <summary>True when the current settings alias badly enough that Box is worth suggesting.
        /// Below roughly two source texels per cell there is nothing meaningful to integrate.</summary>
        public static bool SamplingWouldBenefitFromBox(
            int pixelSize, bool useVirtualGrid, Vector2Int virtualResolution, float pixelAspect, int sourceWidth, int sourceHeight)
        {
            float srcW = Mathf.Max(1, sourceWidth);
            float srcH = Mathf.Max(1, sourceHeight);
            float gridX = (useVirtualGrid ? Mathf.Max(1, virtualResolution.x) : srcW) / Mathf.Max(pixelAspect, 0.001f);
            float gridY = useVirtualGrid ? Mathf.Max(1, virtualResolution.y) : srcH;
            float block = Mathf.Max(1, pixelSize);
            float texelsX = srcW / Mathf.Max(1f, gridX / block);
            float texelsY = srcH / Mathf.Max(1f, gridY / block);
            return Mathf.Max(texelsX, texelsY) >= 2f;
        }

        // =========================================================================================
        // PALETTE SWATCHES
        // =========================================================================================

        /// <summary>Shows the palette as CrowFX will read it: a tonal strip for Ramp, or the swatch
        /// set actually scanned for Nearest, capped at the configured colour count. A mis-sized or
        /// mis-imported palette can still produce a plausible-looking render, so this describes the
        /// source data rather than the result.</summary>
        public static void Palette(
            Rect rect,
            Texture2D palette,
            CrowImageEffects.PaletteMode mode,
            int colorCount,
            AnimationCurve thresholdCurve,
            bool invert)
        {
            var strip = new Rect(rect.x, rect.y, rect.width, Mathf.Max(10f, rect.height - 16f));

            if (palette == null)
            {
                EditorGUI.DrawRect(strip, new Color(1f, 1f, 1f, 0.04f));
                DrawCaption(rect, "no palette texture assigned");
                return;
            }

            if (mode == CrowImageEffects.PaletteMode.Ramp)
            {
                // Ramp reads along the longest axis, so drawing the texture itself is faithful.
                GUI.DrawTexture(strip, palette, ScaleMode.StretchToFill, false);

                if (thresholdCurve != null)
                {
                    Handles.BeginGUI();
                    try
                    {
                        Handles.color = new Color(0f, 0f, 0f, 0.55f);
                        Vector3 prev = Vector3.zero;
                        for (int i = 0; i <= 40; i++)
                        {
                            float t = i / 40f;
                            float v = Mathf.Clamp01(thresholdCurve.Evaluate(t));
                            var p = new Vector3(
                                Mathf.Lerp(strip.x, strip.xMax, t),
                                Mathf.Lerp(strip.yMax - 1f, strip.y + 1f, v), 0f);
                            if (i > 0) Handles.DrawLine(prev, p);
                            prev = p;
                        }
                    }
                    finally { Handles.EndGUI(); }
                }

                DrawCaption(rect, $"ramp · {palette.width}x{palette.height} · reads longest axis");
                return;
            }

            int total = Mathf.Min(palette.width * palette.height, Mathf.Max(colorCount, 2));

            if (!palette.isReadable)
            {
                GUI.DrawTexture(strip, palette, ScaleMode.ScaleToFit, false);
                DrawCaption(rect, $"nearest · scans {total} of {palette.width * palette.height} texels · enable Read/Write to preview swatches");
                return;
            }

            int swatchCols = Mathf.Min(total, Mathf.Max(1, Mathf.FloorToInt(strip.width / 12f)));
            int swatchRows = Mathf.Max(1, Mathf.CeilToInt(total / (float)swatchCols));
            float sw = strip.width / swatchCols;
            float sh = Mathf.Min(strip.height / swatchRows, 22f);

            for (int i = 0; i < total; i++)
            {
                int x = i % Mathf.Max(1, palette.width);
                int y = i / Mathf.Max(1, palette.width);
                if (y >= palette.height) break;

                Color c = palette.GetPixel(x, y);
                if (invert) c = new Color(1f - c.r, 1f - c.g, 1f - c.b, 1f);

                int col = i % swatchCols;
                int row = i / swatchCols;
                EditorGUI.DrawRect(
                    new Rect(strip.x + col * sw, strip.y + row * sh, Mathf.Ceil(sw) - 1f, sh - 1f),
                    new Color(c.r, c.g, c.b, 1f));
            }

            DrawCaption(rect, $"nearest · scanning {total} of {palette.width * palette.height} texels");
        }

        // =========================================================================================
        // EDGE OUTLINE
        // =========================================================================================

        /// <summary>Draws the outline colour and thickness against a mock silhouette, and reports
        /// whether view-normal detection is actually available on the running pipeline. Real edge
        /// detection needs scene depth, which a standalone blit has no access to.</summary>
        public static void Edges(
            Rect rect, Color edgeColor, float thickness, float blend, float strength, bool useNormals, bool normalsAvailable)
        {
            var body = new Rect(rect.x, rect.y, rect.width, Mathf.Max(10f, rect.height - 16f));
            EditorGUI.DrawRect(body, new Color(0.30f, 0.34f, 0.40f, 1f));

            // Two overlapping shapes at different depths: one silhouette edge against the
            // background, one interior edge between the shapes.
            var far = new Rect(body.x + body.width * 0.16f, body.y + body.height * 0.22f,
                               body.width * 0.34f, body.height * 0.62f);
            var near = new Rect(body.x + body.width * 0.42f, body.y + body.height * 0.34f,
                                body.width * 0.36f, body.height * 0.52f);

            EditorGUI.DrawRect(far, new Color(0.46f, 0.50f, 0.56f, 1f));
            EditorGUI.DrawRect(near, new Color(0.62f, 0.66f, 0.72f, 1f));

            float w = Mathf.Clamp(thickness, 0.5f, 4f);
            var line = new Color(edgeColor.r, edgeColor.g, edgeColor.b,
                                 Mathf.Clamp01(blend) * Mathf.Clamp01(strength / 2f));

            DrawRectOutline(far, w, line);
            DrawRectOutline(near, w, line);

            DrawCaption(rect, useNormals && !normalsAvailable
                ? $"depth only · normals unavailable on this pipeline · {w:0.#}px"
                : $"{(useNormals ? "depth + normals" : "depth only")} · {w:0.#}px outline");
        }

        private static void DrawRectOutline(Rect r, float w, Color c)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - w, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, w, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - w, r.y, w, r.height), c);
        }

        // =========================================================================================
        // TEXTURE MASK
        // =========================================================================================

        /// <summary>Maps mask input value to effect opacity, showing where the threshold sits and
        /// how wide the feather is. The band underneath previews the resulting blend.</summary>
        public static void TextureMask(
            Rect rect, Texture2D maskTex, float threshold, float softness, float opacity, bool invert,
            CrowImageEffects.MaskChannel channel)
        {
            float bandH = 16f;
            var plot = new Rect(rect.x, rect.y, rect.width, Mathf.Max(10f, rect.height - bandH - 6f - 13f));
            var band = new Rect(rect.x, plot.yMax + 6f, rect.width, bandH);

            EditorGUI.DrawRect(plot, new Color(1f, 1f, 1f, 0.03f));

            int columns = Mathf.Clamp(Mathf.RoundToInt(rect.width / 4f), 16, 128);
            float colW = rect.width / columns;

            Handles.BeginGUI();
            try
            {
                Handles.color = Accent;
                Vector3 prev = Vector3.zero;
                for (int i = 0; i < columns; i++)
                {
                    float v = columns <= 1 ? 0f : i / (float)(columns - 1);
                    float a = MaskAlpha(v, threshold, softness, opacity, invert);

                    var p = new Vector3(Mathf.Lerp(plot.x, plot.xMax, v),
                                        Mathf.Lerp(plot.yMax - 2f, plot.y + 2f, a), 0f);
                    if (i > 0) Handles.DrawLine(prev, p);
                    prev = p;

                    // Band: black = source restored, white = full effect.
                    EditorGUI.DrawRect(new Rect(rect.x + i * colW, band.y, Mathf.Ceil(colW), band.height),
                        new Color(a, a, a, 1f));
                }
            }
            finally
            {
                Handles.EndGUI();
            }

            float tx = Mathf.Lerp(plot.x, plot.xMax, Mathf.Clamp01(threshold));
            EditorGUI.DrawRect(new Rect(tx - 1f, plot.y, 2f, plot.height), new Color(1f, 1f, 1f, 0.30f));

            string src = maskTex != null ? $"{channel}" : "no mask texture";
            DrawCaption(rect, $"{src} · threshold {threshold:0.##} · feather {softness:0.##}{(invert ? " · inverted" : "")}");
        }

        private static float MaskAlpha(float value, float threshold, float softness, float opacity, bool invert)
        {
            float feather = Mathf.Max(softness, 0.0001f);
            float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(threshold - feather, threshold + feather, value));
            if (invert) a = 1f - a;
            return Mathf.Clamp01(a * Mathf.Clamp01(opacity));
        }

        // =========================================================================================
        // DEPTH MASK
        // =========================================================================================

        /// <summary>Plots effect opacity against scene depth so the near edge, far edge and feather
        /// widths are visible relative to each other on one axis.</summary>
        public static void DepthMask(
            Rect rect, float near, float far, float softness, float opacity, bool invert)
        {
            var plot = new Rect(rect.x, rect.y, rect.width, Mathf.Max(10f, rect.height - 16f));
            EditorGUI.DrawRect(plot, new Color(1f, 1f, 1f, 0.03f));

            float safeFar = Mathf.Max(near, far);
            // Show a little context beyond the far edge so the trailing feather is visible.
            float axisMax = Mathf.Max(safeFar + Mathf.Max(softness, 0.001f) * 2f, near + 1f) * 1.15f;

            const int steps = 96;
            Handles.BeginGUI();
            try
            {
                Handles.color = Accent;
                Vector3 prev = Vector3.zero;
                for (int i = 0; i <= steps; i++)
                {
                    float depth = i / (float)steps * axisMax;
                    float a = DepthAlpha(depth, near, safeFar, softness, opacity, invert);
                    var p = new Vector3(Mathf.Lerp(plot.x, plot.xMax, i / (float)steps),
                                        Mathf.Lerp(plot.yMax - 2f, plot.y + 2f, a), 0f);
                    if (i > 0) Handles.DrawLine(prev, p);
                    prev = p;
                }
            }
            finally
            {
                Handles.EndGUI();
            }

            DrawMarker(plot, near / axisMax, "near");
            if (safeFar / axisMax <= 1f) DrawMarker(plot, safeFar / axisMax, "far");

            DrawCaption(rect, $"near {near:0.##} · far {safeFar:0.##} · feather {softness:0.##}{(invert ? " · inverted" : "")}");
        }

        private static void DrawMarker(Rect plot, float t01, string label)
        {
            float x = Mathf.Lerp(plot.x, plot.xMax, Mathf.Clamp01(t01));
            EditorGUI.DrawRect(new Rect(x - 1f, plot.y, 2f, plot.height), new Color(1f, 1f, 1f, 0.25f));
            GUI.Label(new Rect(x + 3f, plot.y, 40f, 12f), label, Caption);
        }

        private static float DepthAlpha(float depth, float near, float far, float softness, float opacity, bool invert)
        {
            float feather = Mathf.Max(softness, 0.00001f);
            float nearMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(near - feather, near + feather, depth));
            float farMask = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(far - feather, far + feather, depth));
            float a = nearMask * farMask;
            if (invert) a = 1f - a;
            return Mathf.Clamp01(a * Mathf.Clamp01(opacity));
        }
    }
}
#endif
