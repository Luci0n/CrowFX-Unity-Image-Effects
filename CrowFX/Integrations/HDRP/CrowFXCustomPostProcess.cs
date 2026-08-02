#if CROWFX_HDRP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace CrowFX.Integrations.HDRP
{
    [System.Serializable, VolumeComponentMenu("Post-processing/Custom/CrowFX")]
    public sealed class CrowFXCustomPostProcess : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 1f);
        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        public bool IsActive() => intensity.value > 0f;

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            var effect = camera.camera.GetComponent<CrowImageEffects>();
            if (effect == null || !effect.isActiveAndEnabled || source?.rt == null || destination?.rt == null)
            {
                HDUtils.BlitCameraTexture(cmd, source, destination);
                return;
            }

            float previousBlend = effect.masterBlend;
            try
            {
                effect.masterBlend *= intensity.value;
                effect.RenderStack(source.rt, destination.rt);
            }
            finally
            {
                effect.masterBlend = previousBlend;
            }
        }
    }
}
#endif
