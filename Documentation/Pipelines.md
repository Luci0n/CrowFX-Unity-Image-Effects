# Render pipelines and XR

## Compatibility

| Pipeline | Status | Entry point |
|---|---|---|
| Built-in Render Pipeline | **Supported** | `OnRenderImage` camera component |
| Universal Render Pipeline 14–16 | **Experimental** | `CrowFXRendererFeature` |
| High Definition Render Pipeline 14+ | **Experimental** | `CrowFXCustomPostProcess` |

Unity 2022.3 LTS or newer is recommended.

## Setup

### Built-in

Add `CrowImageEffects` to a camera. Nothing else is required.

### URP

The URP integration assembly compiles itself in automatically — `CrowFX.URP.asmdef` carries a
version define that switches on `CROWFX_URP` as soon as `com.unity.render-pipelines.universal`
14.0.0 or newer is in the project. There is no scripting define to add by hand.

1. **Find the renderer asset.** `Project Settings > Graphics` points at your URP Asset; the URP
   Asset's `Renderer List` points at a `Universal Renderer Data` asset. Select that renderer asset.
2. **Add the feature.** In its Inspector, choose `Add Renderer Feature > Crow FX Renderer Feature`.
3. **Add the component.** Put `CrowImageEffects` on the camera as usual. The feature looks the
   component up on the camera being rendered, so both pieces are required.

Optional: the feature exposes an `Injection Point`, defaulting to
`AfterRenderingPostProcessing`. Move it earlier if you want CrowFX to run before URP's own
post-processing.

**If nothing appears, check these first:**

- **The Scene view never shows it.** The pass runs only for `CameraType.Game`, so the Game view is
  the only place the stack renders. This is deliberate rather than a bug.
- **`Master Blend` must be above 0.** The feature skips the camera entirely at zero.
- **The component must be enabled**, along with its GameObject.
- **On URP 17 (Unity 6), enable Compatibility Mode.** The version define switches on for URP 17 too,
  so the assembly compiles, but RenderGraph never calls the `Execute` path this feature relies on.
  Without Compatibility Mode the feature is present and silently does nothing.
- **Edge Outline and Motion & Datamosh need a prepass.** They read URP's depth-normals and
  motion-vector buffers; without a renderer producing them, both degrade to depth-only and log a
  warning.

### HDRP

The HDRP assembly compiles itself in the same way as URP's, via a version define that switches on
`CROWFX_HDRP` once `com.unity.render-pipelines.high-definition` 14.0.0 or newer is present.

Unlike URP, HDRP will not run a custom post process that has not been *registered*, so there are
four steps rather than three:

1. **Register it.** `Project Settings > Graphics > HDRP Global Settings > Custom Post Process
   Orders`. CrowFX injects at `After Post Process`, so add `CrowFXCustomPostProcess` to the
   **After Post Process** list specifically — adding it to a different list leaves it inert.
2. **Add a Volume.** A Global Volume is simplest. On its profile choose
   `Add Override > Post-processing > Custom > CrowFX (Experimental)`.
3. **Enable the override.** Tick the checkbox beside `Intensity` and raise it above 0. An unticked
   or zero intensity makes `IsActive()` false and HDRP skips the effect entirely.
4. **Add the component.** Put `CrowImageEffects` on the camera. The override reads its settings
   from there; the Volume only supplies the intensity.

**If nothing appears, check these first:**

- **Registration in the wrong list**, or missing. This is the most common cause and produces no
  error of any kind.
- **The override is unticked, or Intensity is 0.**
- **No `CrowImageEffects` on the camera.** The override falls back to a plain passthrough blit, so
  the frame renders normally with nothing to indicate the effect was skipped.
- **Custom Post Process is disabled in Frame Settings** for that camera or in the HDRP asset.

The volume `Intensity` scales the whole stack per invocation and is deliberately not written back
into the component's `Master Blend`, so several cameras can share one `CrowImageEffects` at
different strengths without fighting over the serialized value.

Note that HDRP does not publish normals or motion vectors under names CrowFX reads, so Edge Outline
and Motion & Datamosh degrade to depth-only here regardless of setup.

## What "experimental" means here

CrowFX renders its stack with immediate-mode `Graphics.Blit` calls. That is exactly how the Built-in
Render Pipeline expects post-processing to work, but URP and HDRP record their work into command
buffers that execute later, so the adapters have to flush around the stack instead of recording into
it. They are usable for stills and simple setups, and they are not production-ready.

Known limitations on URP and HDRP:

- **No RenderGraph path.** URP 17 (Unity 6) routes rendering through RenderGraph, where
  `ScriptableRenderPass.Execute` and `cameraColorTargetHandle` are unavailable. The renderer feature
  requires URP 14–16 with Compatibility Mode.
- **Pass ordering is not guaranteed.** The stack is flushed around, not merged into, the host
  pipeline's command buffer.
- **Edge Outline normals and Motion & Datamosh vectors are degraded.** CrowFX reads URP's prepass
  buffers where present; HDRP does not publish equivalents under names CrowFX can read. Both stages
  fall back to depth-only detection and log a warning rather than producing incorrect output. Depth
  Mask is unaffected, since `_CameraDepthTexture` is common to all three pipelines.

Everything else — sampling, grading, quantization, palette, dithering, bleed, ghosting, sharpening,
tape, composite, CRT and LCD — does not read scene buffers and behaves identically on all three.

These adapters will be rebuilt on command buffers and `RTHandle` operations, with a dedicated
RenderGraph path for URP 17+.

## XR and stereo rendering

Multi-pass stereo works without any special handling: Unity renders each eye separately and hands the
stage an ordinary texture.

Single-pass instanced renders both eyes into one texture array, and every stage samples the correct
slice through Unity's screen-space texture macros, which compile back to plain 2D sampling when
stereo is off.

> **Single-pass instanced support is untested on hardware.** It is implemented against Unity's
> documented stereo macros but has not been run on a headset. Please report anything that renders one
> eye into both, or renders to only one eye.

### Stereo on URP and HDRP

Neither adapter supports single-pass instanced stereo. Both drive the stack through immediate-mode
`Graphics.Blit`, which never establishes the eye state that would compile the texture-array
variants of the shaders, so the stack cannot process an array target.

When a stereo pipeline hands CrowFX an array, the source is flattened to a plain 2D copy of the
first slice and the stack runs on that — one eye, rather than a per-frame flood of "Dimensions must
match" errors. The URP adapter logs a warning once when it detects this.

### Where stereo stands

| Setup | Result |
|---|---|
| Built-in, multi-pass | Full stack on both eyes |
| Built-in, single-pass instanced | Implemented against Unity's stereo macros, **untested on hardware** |
| URP or HDRP, any stereo mode | Flattened to one eye |

## Using CrowFX in VR

Getting stereo working is only half the question. Most of this library is display simulation, and
applying it to the whole headset view has a problem no amount of stereo correctness solves.

**Screen-space patterns fuse at the wrong depth.** Scanlines, phosphor masks, grain, dust and dither
are computed in screen UV, so they come out identical in both eyes. Identical imagery has zero
disparity, which the visual system fuses at the screen plane rather than at scene depth. In a
headset that reads as dirt on your eyeballs, floating in front of the world and following your head.
Nothing occludes it either, because a post-process overlay has no depth of its own.

**Some stages also work against comfort.** Ghosting and response smear fight motion-to-photon
latency, and lens distortion and rolling shutter are established nausea triggers. Low-frequency,
view-independent stages — grading, posterize, palette, colour work — are the comfortable subset.

### The recommended approach: an in-world display

Put `CrowImageEffects` on a camera that renders to a RenderTexture, and show that texture on a
surface in the scene: a CRT in a room, an arcade cabinet, a security monitor, a handheld console.

This sidesteps the whole problem. The off-screen camera is mono, so no stereo path is involved and
any pipeline works. The scanlines live on a surface at a real distance your eyes converge on
correctly, and the display occludes and is occluded like any other object — which is exactly how a
real screen behaves.

It is also the case CrowFX simulates most convincingly. Stage shaders derive their resolution from
the render target rather than the screen, so pattern pitch stays correct on off-screen cameras and
under render scale.

## Multiple cameras

Select several cameras carrying `CrowImageEffects` and the inspector edits all of them at once. Field
edits, Reset, Randomize, section Paste, and applying a look or profile reach every selected component,
and a dash marks values they disagree on.

The preview tools — Solo, Mute, Bypass, live section previews, and look previews — stay
single-selection. They work by overwriting values and restoring the originals afterwards, and a mixed
selection has no single original to restore. Saving settings *into* an asset is single-selection for
the same reason: it captures one camera's configuration.
