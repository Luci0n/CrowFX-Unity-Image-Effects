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

            string path = EditorUtility.SaveFilePanelInProject("Create CrowFX Preset", "CrowFXPreset", "asset", "Choose a location for the preset asset.");
            if (string.IsNullOrEmpty(path)) return;
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            string profilePath = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.ChangeExtension(path, null) + "_Profile.asset");
            var profile = ScriptableObject.CreateInstance<CrowFXProfile>();
            effect.SaveToProfile(profile);
            AssetDatabase.CreateAsset(profile, profilePath);

            var preset = ScriptableObject.CreateInstance<CrowFXPresetAsset>();
            preset.displayName = System.IO.Path.GetFileNameWithoutExtension(path);
            preset.profile = profile;
            preset.authoredStrength = 1f;
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = preset;
            EditorGUIUtility.PingObject(preset);
        }
    }
}
