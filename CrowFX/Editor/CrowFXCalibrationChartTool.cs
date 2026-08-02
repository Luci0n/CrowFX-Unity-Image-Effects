using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrowFX.EditorTools
{
    internal static class CrowFXCalibrationChartTool
    {
        [MenuItem("Tools/CrowFX/Generate Calibration Chart")]
        private static void Generate()
        {
            string assetPath = EditorUtility.SaveFilePanelInProject("Generate CrowFX Calibration Chart", "CrowFX_Calibration_1024", "png", "Choose where to save the chart.");
            if (string.IsNullOrEmpty(assetPath)) return;

            const int size = 1024;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var pixels = new Color[size * size];
            Color[] bars = { Color.white, Color.yellow, Color.cyan, Color.green, Color.magenta, Color.red, Color.blue, Color.black };

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (size - 1f);
                float v = y / (size - 1f);
                Color color;

                if (v < 0.20f)
                {
                    float ramp = u;
                    color = new Color(ramp, ramp, ramp, 1f);
                }
                else if (v < 0.40f)
                {
                    color = bars[Mathf.Clamp(Mathf.FloorToInt(u * bars.Length), 0, bars.Length - 1)];
                }
                else if (v < 0.60f)
                {
                    int cell = ((x / 4) + (y / 4)) & 1;
                    color = cell == 0 ? new Color(0.08f, 0.08f, 0.08f, 1f) : new Color(0.92f, 0.92f, 0.92f, 1f);
                }
                else if (v < 0.80f)
                {
                    GetZoneCoordinates(out float zx, out float zy, u, (v - 0.60f) * 5f);
                    float radius2 = zx * zx + zy * zy;
                    float zone = 0.5f + 0.5f * Mathf.Sin(radius2 * 180f);
                    color = new Color(zone, zone, zone, 1f);
                }
                else
                {
                    float frequency = Mathf.Lerp(4f, 128f, u);
                    float signal = 0.5f + 0.5f * Mathf.Sin(u * frequency * Mathf.PI * 2f);
                    int channel = Mathf.FloorToInt((v - 0.80f) * 15f) % 3;
                    color = channel == 0 ? new Color(signal, 0.15f, 0.15f, 1f) :
                            channel == 1 ? new Color(0.15f, signal, 0.15f, 1f) : new Color(0.15f, 0.15f, signal, 1f);
                }

                pixels[y * size + x] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolutePath = Path.Combine(projectRoot, assetPath);
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.sRGBTexture = false;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static void GetZoneCoordinates(out float x, out float y, float u, float v)
        {
            x = u * 2f - 1f;
            y = v * 2f - 1f;
        }
    }
}
