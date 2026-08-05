#if CROWFX_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CrowFX.Integrations.URP
{
    /// <summary>
    /// EXPERIMENTAL. CrowFX renders through immediate-mode <see cref="Graphics.Blit"/> calls, so this
    /// pass flushes the command buffer around the stack rather than recording into it. It requires
    /// URP 14-16 with Compatibility Mode; URP 17+ routes rendering through RenderGraph, where
    /// <see cref="ScriptableRenderPass.Execute"/> and cameraColorTargetHandle are unavailable.
    /// Edge Outline normals and Motion &amp; Datamosh vectors read URP's prepass buffers and require
    /// the renderer to produce them; without a DepthNormals or motion-vector prepass both stages
    /// degrade to depth-only behavior. Superseded once RenderStack is rebuilt on command buffers
    /// and RTHandles with a dedicated RenderGraph path.
    /// </summary>
    public sealed class CrowFXRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

        [SerializeField]
        [Tooltip("Also apply the stack in the Scene view. Off by default because the mid-pass " +
                 "submit this adapter relies on costs more with several views open.")]
        private bool renderInSceneView;

        private CrowFXPass _pass;

        public override void Create() => _pass = new CrowFXPass(injectionPoint);

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraType = renderingData.cameraData.cameraType;
            bool allowed = cameraType == CameraType.Game ||
                           (renderInSceneView && cameraType == CameraType.SceneView);
            if (!allowed) return;
            var effect = renderingData.cameraData.camera.GetComponent<CrowImageEffects>();
            if (effect == null || !effect.isActiveAndEnabled || effect.masterBlend <= 0f) return;

            _pass.Setup(effect);
            renderer.EnqueuePass(_pass);
        }

        private sealed class CrowFXPass : ScriptableRenderPass
        {
            private RTHandle _input, _output;
            private CrowImageEffects _effect;

            public CrowFXPass(RenderPassEvent evt) => renderPassEvent = evt;

            public void Setup(CrowImageEffects effect) => _effect = effect;

            private static bool _warnedAboutStereoArray;

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;

                // With single-pass instanced XR the camera target is a texture array. CrowFX runs
                // on immediate-mode Graphics.Blit, which never establishes the stereo eye state the
                // pipeline would, so its shaders compile as plain 2D samplers. Inheriting the array
                // dimension therefore binds array textures to 2D properties and Unity logs
                // "Dimensions must match" once per property per frame, forever.
                //
                // Forcing plain 2D intermediates keeps the stack rendering. Single-pass instanced
                // is not supported on this adapter; use Multi Pass, which hands each eye an
                // ordinary texture.
                if (desc.dimension == TextureDimension.Tex2DArray || desc.volumeDepth > 1)
                {
                    if (!_warnedAboutStereoArray)
                    {
                        _warnedAboutStereoArray = true;
                        Debug.LogWarning(
                            "CrowFX: the URP camera target is a texture array, which means " +
                            "single-pass instanced stereo. This adapter cannot render stereo and " +
                            "will process a single eye. Switch the XR plug-in to Multi Pass, or " +
                            "use the Built-in Render Pipeline for stereo work.");
                    }

                    desc.dimension = TextureDimension.Tex2D;
                    desc.volumeDepth = 1;
                    desc.vrUsage = VRTextureUsage.None;
                }

                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(ref _input, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CrowFXURPInput");
                RenderingUtils.ReAllocateIfNeeded(ref _output, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CrowFXURPOutput");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // Read here rather than cached from AddRenderPasses or SetupRenderPasses. URP
                // forbids touching it in AddRenderPasses at all, and a handle captured before the
                // frame runs goes stale: URP ping-pongs between _CameraColorAttachmentA and B
                // during post-processing, so a cached handle can name the attachment that is no
                // longer current. Writing into that produces no error and no visible change.
                // Execute is inside the pass scope, so reading it here is both legal and current.
                var cameraColor = renderingData.cameraData.renderer?.cameraColorTargetHandle;

                if (_effect == null || cameraColor == null || cameraColor.rt == null ||
                    _input == null || _output == null || _input.rt == null || _output.rt == null) return;

                // ExecuteCommandBuffer only queues work into the render context; nothing has run
                // on the GPU yet. RenderStack is immediate-mode Graphics.Blit, which runs now.
                // Without the Submit calls below the stack read _input before the blit that fills
                // it had executed, and wrote _output back before the scene had been drawn -- so
                // the frame came out exactly as URP rendered it, with no error to show for it.
                //
                // Submitting mid-pass is the cost of driving an immediate-mode stack from a
                // deferred pipeline, and is the reason this adapter is experimental.
                var cmd = CommandBufferPool.Get("CrowFX URP");

                Blitter.BlitCameraTexture(cmd, cameraColor, _input);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                context.Submit();

                _effect.RenderStack(_input.rt, _output.rt);

                Blitter.BlitCameraTexture(cmd, _output, cameraColor);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                context.Submit();

                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                _input?.Release();
                _output?.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            _pass = null;
        }
    }
}
#endif
