using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrowFX.EditorTools
{
    /// <summary>
    /// Detects the URP and HDRP setup mistakes that otherwise fail in complete silence: a renderer
    /// feature that was never added, a custom post process that was never registered, an injection
    /// point that cannot reach the screen, and an intermediate texture mode that leaves nothing
    /// blittable. Every one of those renders the frame exactly as the pipeline would have, with
    /// nothing in the console to explain it.
    ///
    /// Everything here goes through SerializedObject, asset searches, and type names rather than
    /// pipeline types, so this assembly needs no reference to URP or HDRP and these checks still
    /// compile in a Built-in project where neither package exists.
    /// </summary>
    internal static class CrowFXPipelineSetup
    {
        internal readonly struct Problem
        {
            public readonly string Message;
            public readonly Object PingTarget;
            public readonly string ActionLabel;

            /// <summary>True when nothing renders at all, rather than only might not.</summary>
            public readonly bool IsError;

            public Problem(string message, Object pingTarget, string actionLabel, bool isError)
            {
                Message = message;
                PingTarget = pingTarget;
                ActionLabel = actionLabel;
                IsError = isError;
            }

            public bool Exists => !string.IsNullOrEmpty(Message);
        }

        // RenderPassEvent.AfterRenderingPostProcessing. Injecting at or after this point writes
        // into a target URP may already have finished presenting from.
        private const int AfterRenderingPostProcessing = 600;

        private const string UrpFeatureType = "CrowFXRendererFeature";
        private const string HdrpVolumeType = "CrowFXCustomPostProcess";

        internal static Problem Check()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null) return default;

            return pipeline.GetType().Name switch
            {
                "UniversalRenderPipelineAsset" => CheckUrp(pipeline),
                "HDRenderPipelineAsset" => CheckHdrp(),
                _ => default
            };
        }

        // =====================================================================================
        // URP
        // =====================================================================================
        private static Problem CheckUrp(RenderPipelineAsset pipeline)
        {
            var rendererDataList = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
            if (rendererDataList == null || !rendererDataList.isArray) return default;

            Object firstRenderer = null;
            Object rendererWithFeature = null;
            Object feature = null;

            for (int i = 0; i < rendererDataList.arraySize; i++)
            {
                var rendererData = rendererDataList.GetArrayElementAtIndex(i).objectReferenceValue;
                if (rendererData == null) continue;

                if (firstRenderer == null) firstRenderer = rendererData;

                feature = FindArrayElementByTypeName(rendererData, "m_RendererFeatures", UrpFeatureType);
                if (feature == null) continue;

                rendererWithFeature = rendererData;
                break;
            }

            if (rendererWithFeature == null)
            {
                return new Problem(
                    "URP is active but CrowFX Renderer Feature is not on any renderer, so this " +
                    "stack will not render. Select the renderer asset and choose Add Renderer " +
                    "Feature, then Crow FX Renderer Feature.",
                    firstRenderer, "Show Renderer", isError: true);
            }

            var injectionPoint = new SerializedObject(feature).FindProperty("injectionPoint");
            if (injectionPoint != null && injectionPoint.intValue >= AfterRenderingPostProcessing)
            {
                return new Problem(
                    "CrowFX Renderer Feature injects after post-processing, where URP may have " +
                    "finished with the target it writes to. If the image does not change, set " +
                    "Injection Point to Before Rendering Post Processing.",
                    rendererWithFeature, "Show Renderer", isError: false);
            }

            // UniversalRendererData only. 0 is Auto, 1 is Always. Auto lets URP render straight
            // to the backbuffer, which cannot be blitted from.
            var intermediateTexture = new SerializedObject(rendererWithFeature)
                .FindProperty("m_IntermediateTextureMode");

            if (intermediateTexture != null && intermediateTexture.intValue == 0)
            {
                return new Problem(
                    "The renderer's Intermediate Texture mode is Auto. URP may render straight to " +
                    "the backbuffer, which CrowFX cannot read. Set it to Always if the image does " +
                    "not change.",
                    rendererWithFeature, "Show Renderer", isError: false);
            }

            return default;
        }

        // =====================================================================================
        // HDRP
        // =====================================================================================
        private static Problem CheckHdrp()
        {
            // Located by asset search rather than through HDRP's API, which would need a
            // reference to the package.
            var guids = AssetDatabase.FindAssets("t:HDRenderPipelineGlobalSettings");
            if (guids == null || guids.Length == 0) return default;

            var settings = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (settings == null) return default;

            // HDRP keeps one string list per injection point, each named "...CustomPostProcesses"
            // and holding type names. Walking every matching list rather than naming them keeps
            // this working if HDRP renames or adds one.
            string containingList = null;
            bool foundAnyList = false;

            var iterator = new SerializedObject(settings).GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (!iterator.isArray || !iterator.name.EndsWith("CustomPostProcesses")) continue;
                foundAnyList = true;

                for (int i = 0; i < iterator.arraySize; i++)
                {
                    var entry = iterator.GetArrayElementAtIndex(i);
                    if (entry.propertyType != SerializedPropertyType.String) continue;
                    if (string.IsNullOrEmpty(entry.stringValue)) continue;
                    if (!entry.stringValue.Contains(HdrpVolumeType)) continue;

                    containingList = iterator.name;
                    break;
                }

                if (containingList != null) break;
            }

            if (!foundAnyList) return default;

            if (containingList == null)
            {
                return new Problem(
                    "HDRP is active but CrowFX is not registered as a custom post process, so it " +
                    "will never run. Add CrowFXCustomPostProcess to the After Post Process list " +
                    "in HDRP Global Settings, under Custom Post Process Orders.",
                    settings, "Show Settings", isError: true);
            }

            // The volume component declares AfterPostProcess, so any other list never invokes it.
            if (!containingList.StartsWith("afterPostProcess"))
            {
                return new Problem(
                    $"CrowFX is registered under {ObjectNames.NicifyVariableName(containingList)}, " +
                    "but it injects at After Post Process and is only invoked from that list. " +
                    "Move it in HDRP Global Settings, under Custom Post Process Orders.",
                    settings, "Show Settings", isError: true);
            }

            return default;
        }

        // =====================================================================================
        private static Object FindArrayElementByTypeName(Object owner, string arrayPath, string typeName)
        {
            var array = new SerializedObject(owner).FindProperty(arrayPath);
            if (array == null || !array.isArray) return null;

            for (int i = 0; i < array.arraySize; i++)
            {
                var element = array.GetArrayElementAtIndex(i).objectReferenceValue;
                if (element != null && element.GetType().Name == typeName)
                    return element;
            }

            return null;
        }

        /// <summary>Selects and highlights an asset so the fix is one click away.</summary>
        internal static void Reveal(Object asset)
        {
            if (asset == null) return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
