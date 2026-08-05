#if CROWFX_HDRP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace CrowFX.Integrations.HDRP
{
    /// <summary>
    /// EXPERIMENTAL. CrowFX renders through immediate-mode <see cref="Graphics.Blit"/> calls while
    /// HDRP defers the supplied <see cref="CommandBuffer"/>, so this component's work is not ordered
    /// against the surrounding HDRP passes. It is usable for stills and simple setups but is not
    /// production-ready, and stages that need normals or motion vectors degrade to depth-only
    /// behavior because HDRP does not publish those buffers under names CrowFX reads.
    /// Superseded once RenderStack is rebuilt on command buffers and RTHandles.
    /// </summary>
    [System.Serializable, VolumeComponentMenu("Post-processing/Custom/CrowFX (Experimental)")]
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

            // Flush HDRP's pending work before the immediate-mode stack runs, so the
            // source contains everything the earlier passes recorded.
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // The volume intensity is passed per invocation. Writing it into the
            // component's serialized masterBlend would dirty the scene, interact with
            // Undo, and race when several cameras share one CrowImageEffects instance.
            effect.RenderStack(source.rt, destination.rt, intensity.value);
        }
    }
}
#endif
