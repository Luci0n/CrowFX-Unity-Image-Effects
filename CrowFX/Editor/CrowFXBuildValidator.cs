using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

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
            var missing = FindMissingShaders();
            if (missing.Count > 0)
                throw new BuildFailedException("CrowFX cannot build because these shaders are missing: " + string.Join(", ", missing));

            if (Object.FindObjectOfType<CrowImageEffects>() != null)
                Debug.Log("CrowFX build validation passed: all required shaders resolve. Keep the CrowFX shader folder in the build or add its shaders to Always Included Shaders when using aggressive stripping.");
        }

        [MenuItem("Tools/CrowFX/Validate Installation")]
        private static void ValidateFromMenu()
        {
            var missing = FindMissingShaders();
            if (missing.Count == 0)
                EditorUtility.DisplayDialog("CrowFX Validation", "All runtime shaders resolve successfully.", "OK");
            else
                EditorUtility.DisplayDialog("CrowFX Validation", "Missing shaders:\n\n" + string.Join("\n", missing), "OK");
        }

        private static List<string> FindMissingShaders()
        {
            var missing = new List<string>();
            for (int i = 0; i < RequiredShaders.Length; i++)
                if (Shader.Find(RequiredShaders[i]) == null) missing.Add(RequiredShaders[i]);
            return missing;
        }
    }
}
