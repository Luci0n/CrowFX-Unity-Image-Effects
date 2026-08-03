using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrowFX.EditorTools
{
    internal static class CrowFXPresetAssetTools
    {
        [MenuItem("CONTEXT/CrowImageEffects/Create Data-Driven Preset")]
        private static void CreatePreset(MenuCommand command)
        {
            var effect = command.context as CrowImageEffects;
            if (effect == null) return;

            CreateFromCurrent(effect);
        }

        internal static CrowFXPresetAsset CreateFromCurrent(CrowImageEffects effect, string defaultFolder = null)
        {
            if (effect == null) return null;

            if (!string.IsNullOrEmpty(defaultFolder)) EnsureFolder(defaultFolder);
            string path = string.IsNullOrEmpty(defaultFolder)
                ? EditorUtility.SaveFilePanelInProject("Create CrowFX Preset", "CrowFXPreset", "asset", "Choose a location for the preset asset.")
                : EditorUtility.SaveFilePanelInProject("Create CrowFX Preset", "CrowFXPreset", "asset", "Choose a location for the preset asset.", defaultFolder);
            if (string.IsNullOrEmpty(path)) return null;
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var preset = ScriptableObject.CreateInstance<CrowFXPresetAsset>();
            preset.displayName = Path.GetFileNameWithoutExtension(path);
            preset.authoredStrength = 1f;
            AssetDatabase.CreateAsset(preset, path);

            var profile = ScriptableObject.CreateInstance<CrowFXProfile>();
            profile.name = "Settings";
            effect.SaveToProfile(profile);
            AssetDatabase.AddObjectToAsset(profile, preset);
            preset.profile = profile;
            preset.requiredTextures = CollectReferencedTextures(effect);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            Selection.activeObject = preset;
            EditorGUIUtility.PingObject(preset);
            ShowPortabilityWarning(path, preset.requiredTextures);
            return preset;
        }

        internal static bool UpdateFromCurrent(CrowImageEffects effect, CrowFXPresetAsset preset)
        {
            if (effect == null || preset == null) return false;

            string presetPath = AssetDatabase.GetAssetPath(preset);
            if (string.IsNullOrEmpty(presetPath)) return false;

            Undo.RecordObject(preset, "Update CrowFX Preset");
            CrowFXProfile profile = preset.profile;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CrowFXProfile>();
                profile.name = "Settings";
                AssetDatabase.AddObjectToAsset(profile, preset);
                preset.profile = profile;
            }
            else
            {
                Undo.RecordObject(profile, "Update CrowFX Preset Settings");
            }

            effect.SaveToProfile(profile);
            preset.requiredTextures = CollectReferencedTextures(effect);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(presetPath);
            EditorGUIUtility.PingObject(preset);
            ShowPortabilityWarning(presetPath, preset.requiredTextures);
            return true;
        }

        internal static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            string leaf = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;
            EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(parent, leaf);
        }

        private static Texture2D[] CollectReferencedTextures(CrowImageEffects effect)
        {
            var textures = new List<Texture2D>(4);
            AddUnique(textures, effect.paletteTex);
            AddUnique(textures, effect.maskTex);
            AddUnique(textures, effect.jitterNoiseTex);
            AddUnique(textures, effect.blueNoise);
            return textures.ToArray();
        }

        private static void AddUnique(List<Texture2D> textures, Texture2D texture)
        {
            if (texture != null && !textures.Contains(texture)) textures.Add(texture);
        }

        private static void ShowPortabilityWarning(string presetPath, Texture2D[] textures)
        {
            const string packageRoot = "Assets/CrowFX-Unity-Image-Effects/";
            if (!presetPath.StartsWith(packageRoot, System.StringComparison.OrdinalIgnoreCase)) return;

            var external = new List<string>();
            for (int i = 0; textures != null && i < textures.Length; i++)
            {
                string texturePath = AssetDatabase.GetAssetPath(textures[i]);
                if (!string.IsNullOrEmpty(texturePath) &&
                    !texturePath.StartsWith(packageRoot, System.StringComparison.OrdinalIgnoreCase))
                    external.Add(texturePath);
            }

            if (external.Count == 0) return;
            EditorUtility.DisplayDialog("Preset has external texture dependencies",
                "The preset was saved, but these referenced textures are outside the CrowFX repository folder and must also be uploaded or moved into it:\n\n" +
                string.Join("\n", external), "OK");
        }
    }
}
