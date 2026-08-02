#if CROWFX_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CrowFX.Integrations.URP
{
    public sealed class CrowFXRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
        private CrowFXPass _pass;

        public override void Create() => _pass = new CrowFXPass(injectionPoint);

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            var effect = renderingData.cameraData.camera.GetComponent<CrowImageEffects>();
            if (effect == null || !effect.isActiveAndEnabled || effect.masterBlend <= 0f) return;
            _pass.Setup(renderer.cameraColorTargetHandle, effect);
            renderer.EnqueuePass(_pass);
        }

        private sealed class CrowFXPass : ScriptableRenderPass
        {
            private RTHandle _cameraColor, _input, _output;
            private CrowImageEffects _effect;

            public CrowFXPass(RenderPassEvent evt) => renderPassEvent = evt;

            public void Setup(RTHandle cameraColor, CrowImageEffects effect)
            {
                _cameraColor = cameraColor;
                _effect = effect;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(ref _input, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CrowFXURPInput");
                RenderingUtils.ReAllocateIfNeeded(ref _output, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CrowFXURPOutput");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_effect == null || _input == null || _output == null || _input.rt == null || _output.rt == null) return;
                var cmd = CommandBufferPool.Get("CrowFX URP");
                Blitter.BlitCameraTexture(cmd, _cameraColor, _input);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                _effect.RenderStack(_input.rt, _output.rt);

                Blitter.BlitCameraTexture(cmd, _output, _cameraColor);
                context.ExecuteCommandBuffer(cmd);
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
