using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrowFX.EditorTools
{
    internal sealed class CrowFXBuildValidator : IPreprocessBuildWithReport
    {
        private static readonly string[] RequiredShaders =
        {
            "Hidden/CrowFX/Stages/SamplingGrid", "Hidden/CrowFX/Stages/Pregrade",
            "Hidden/CrowFX/Stages/ChannelJitter", "Hidden/CrowFX/Stages/Ghosting",
            "Hidden/CrowFX/Stages/RGBBleeding", "Hidden/CrowFX/Stages/UnsharpMask",
            "Hidden/CrowFX/Stages/Dithering", "Hidden/CrowFX/Stages/PaletteMapping",
            "Hidden/CrowFX/Stages/EdgeOutline", "Hidden/CrowFX/Stages/TextureMask",
            "Hidden/CrowFX/Stages/DepthMask", "Hidden/CrowFX/Stages/VHSTape",
            "Hidden/CrowFX/Stages/CRTDisplay", "Hidden/CrowFX/Stages/ProfessionalEffects",
            "Hidden/CrowFX/Stages/MasterPresent", "Hidden/CrowFX/Helpers/GhostComposite"
        };

        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            // Shader.Find resolves from the editor's asset database, so a missing file is
            // the only thing it can detect. That is a real error and worth failing on.
            var missing = FindMissingShaders();
            if (missing.Count > 0)
                throw new BuildFailedException(
                    "CrowFX cannot build because these shaders are missing from the project: " +
                    string.Join(", ", missing));

            // Every CrowFX shader is Hidden/ and referenced only through Shader.Find at
            // runtime, so nothing in the scene graph keeps them alive. Unless they are in
            // Always Included Shaders they can be stripped from the player and every stage
            // silently falls back to a passthrough Blit. This is the failure this validator
            // exists to prevent, and it is not something Shader.Find can observe.
            if (!ProjectUsesCrowFX()) return;

            var unregistered = FindUnregisteredShaders();
            if (unregistered.Count == 0) return;

            Debug.LogWarning(
                $"CrowFX: {unregistered.Count} of {RequiredShaders.Length} runtime shaders are not in " +
                "Graphics Settings > Always Included Shaders. Because CrowFX resolves them by name at " +
                "runtime, shader stripping can remove them from the player and every affected stage will " +
                "render as a passthrough. Add them under Project Settings > Graphics > Always Included Shaders.\n" +
                string.Join("\n", unregistered));
        }

        [MenuItem("Tools/CrowFX/Validate Installation")]
        private static void ValidateFromMenu()
        {
            var missing = FindMissingShaders();
            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog("CrowFX Validation",
                    "Missing shaders:\n\n" + string.Join("\n", missing), "OK");
                return;
            }

            var unregistered = FindUnregisteredShaders();
            if (unregistered.Count == 0)
            {
                EditorUtility.DisplayDialog("CrowFX Validation",
                    "All runtime shaders resolve and are registered in Always Included Shaders.\n\n" +
                    "Builds are protected against shader stripping.", "OK");
                return;
            }

            EditorUtility.DisplayDialog("CrowFX Validation",
                $"All {RequiredShaders.Length} runtime shaders resolve in the editor, but " +
                $"{unregistered.Count} are not in Always Included Shaders.\n\n" +
                "CrowFX loads them by name at runtime, so shader stripping can remove them from a " +
                "player build and the affected stages will render as a passthrough.\n\n" +
                "Add them under Project Settings > Graphics > Always Included Shaders:\n\n" +
                string.Join("\n", unregistered), "OK");
        }

        private static List<string> FindMissingShaders()
        {
            var missing = new List<string>();
            for (int i = 0; i < RequiredShaders.Length; i++)
                if (Shader.Find(RequiredShaders[i]) == null) missing.Add(RequiredShaders[i]);
            return missing;
        }

        private static List<string> FindUnregisteredShaders()
        {
            var registered = new HashSet<Shader>(GetAlwaysIncludedShaders());
            var unregistered = new List<string>();
            foreach (string name in RequiredShaders)
            {
                var shader = Shader.Find(name);
                if (shader != null && !registered.Contains(shader)) unregistered.Add(name);
            }
            return unregistered;
        }

        private static IEnumerable<Shader> GetAlwaysIncludedShaders()
        {
            var serialized = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var alwaysIncluded = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (alwaysIncluded == null || !alwaysIncluded.isArray) yield break;

            for (int i = 0; i < alwaysIncluded.arraySize; i++)
            {
                if (alwaysIncluded.GetArrayElementAtIndex(i).objectReferenceValue is Shader shader)
                    yield return shader;
            }
        }

        /// <summary>True when any scene included in the build carries a CrowImageEffects component.
        /// The previous check only looked at the scene that happened to be open, so it reported
        /// nothing for the common case of building from a different scene.</summary>
        private static bool ProjectUsesCrowFX()
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled || string.IsNullOrEmpty(scene.path)) continue;

                foreach (string dependency in AssetDatabase.GetDependencies(scene.path, true))
                {
                    if (dependency.EndsWith("CrowImageEffects.cs", System.StringComparison.Ordinal))
                        return true;
                }
            }

            // No build scene list yet (or none reference CrowFX): fall back to the open scene.
            return Object.FindObjectOfType<CrowImageEffects>() != null;
        }
    }
}
