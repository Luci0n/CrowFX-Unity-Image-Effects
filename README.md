<p align="center">
  <img width="718" height="131" alt="CrowFX" src="https://github.com/user-attachments/assets/e4fbf6dc-9f26-40bd-9dae-4f3ff64741fc" />
</p>

<p align="center">
  <img alt="Unity 2022.3+" src="https://img.shields.io/badge/Unity-2022.3%2B-000000?logo=unity" />
  <a href="https://github.com/Luci0n/CrowFX-Unity-Image-Effects/releases/latest"><img alt="Latest CrowFX release" src="https://img.shields.io/github/v/release/Luci0n/CrowFX-Unity-Image-Effects?label=CrowFX&amp;color=6f4ca6" /></a>
  <a href="https://openupm.com/packages/com.luci0n.crowfx/"><img alt="OpenUPM package version" src="https://img.shields.io/npm/v/com.luci0n.crowfx?label=OpenUPM&amp;registry_uri=https%3A%2F%2Fpackage.openupm.com" /></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/License-MIT-blue.svg" /></a>
</p>

<p align="center">
  <strong>Free, open-source CRT, VHS, film, glitch, dithering, and retro post-processing for Unity.</strong><br />
  Built for the Built-in Render Pipeline. URP and HDRP adapters are experimental.
</p>

CrowFX turns a Unity camera into a complete real-time image-effects stack. Create convincing CRT displays, VHS and composite video, film damage, PSX-style color and dithering, analog-horror footage, digital glitches, LCD artifacts, and polished cinematic looks from one searchable inspector.

<p align="center">
  <a href="#installation"><strong>Install</strong></a> ·
  <a href="#quick-start"><strong>Quick start</strong></a> ·
  <a href="#showcase"><strong>Showcase</strong></a> ·
  <a href="#effect-families"><strong>Effects</strong></a> ·
  <a href="https://github.com/Luci0n/CrowFX-Unity-Image-Effects/releases"><strong>Releases</strong></a>
</p>

## Showcase

| Ghosting, jitter, and dither | Posterize, RGB bleed, and virtual resolution |
|---|---|
| ![Unity ghosting, RGB jitter, and dithering post-processing](https://github.com/user-attachments/assets/604eeb15-4901-4867-8834-d25287cdd2c3) | ![Unity posterization, RGB bleed, sharpening, and virtual-resolution effects](https://github.com/user-attachments/assets/9504b73c-be0f-4189-9c00-7c710078ede5) |

> Add `CrowImageEffects` to a camera, choose a look, and adjust or combine individual stages. CrowFX includes 88 curated looks and dedicated preset banks for each effect family.

## Highlights

- High-fidelity CRT, VHS, composite-video, film, lens, sensor, LCD, and codec simulation
- 88 curated full-stack looks across 11 production and experimental categories
- Dedicated preset banks for individual effect families
- Searchable custom inspector with favorites, copy/paste, solo, mute, profiles, and Undo support
- Live per-section previews that run the real stage shader over a test chart, so every control moves the image and time-based stages animate
- A `?` button in each section header reveals that section's explanatory notes, hidden by default; warnings and errors always stay visible
- Active-stage execution: disabled effects do not add render passes
- HDR-capable intermediate buffers with source-alpha preservation
- Built-in Render Pipeline support, plus experimental URP and HDRP adapters
- Installation validation, build-time shader checks, calibration charts, and editor tests

## Compatibility

| Pipeline | Status | Entry point |
|---|---|---|
| Built-in Render Pipeline | **Supported** | `OnRenderImage` camera component |
| Universal Render Pipeline 14–16 | **Experimental** | `CrowFXRendererFeature` |
| High Definition Render Pipeline 14+ | **Experimental** | `CrowFXCustomPostProcess` |

Unity 2022.3 LTS or newer is recommended.

### XR

Multi-pass stereo works without any special handling: Unity renders each eye separately and hands
the stage an ordinary texture. Single-pass instanced renders both eyes into one texture array, and
every stage now samples the correct slice through Unity's screen-space texture macros, which
compile back to plain 2D sampling when stereo is off.

Single-pass instanced support is **untested on hardware** — it is implemented against Unity's
documented stereo macros but has not been run on a headset. Report anything that renders one eye
into both, or renders only to one eye.

### What "experimental" means here

CrowFX renders its stack with immediate-mode `Graphics.Blit` calls. That is exactly how the
Built-in Render Pipeline expects post-processing to work, but URP and HDRP record their work into
command buffers that execute later, so the adapters have to flush around the stack instead of
recording into it. They are usable for stills and simple setups, and they are not production-ready.

Known limitations on URP and HDRP:

- **No RenderGraph path.** URP 17 (Unity 6) routes rendering through RenderGraph, where
  `ScriptableRenderPass.Execute` and `cameraColorTargetHandle` are unavailable. The renderer feature
  requires URP 14–16 with Compatibility Mode.
- **Pass ordering is not guaranteed.** The stack is flushed around, not merged into, the host
  pipeline's command buffer.
- **Edge Outline normals and Motion & Datamosh vectors are degraded.** CrowFX reads URP's prepass
  buffers where present; HDRP does not publish equivalents under names CrowFX can read. Both stages
  fall back to depth-only detection and log a warning rather than producing incorrect output.
  Depth Mask is unaffected, since `_CameraDepthTexture` is common to all three pipelines.

Everything else — sampling, grading, quantization, palette, dithering, bleed, ghosting, sharpening,
tape, composite, CRT and LCD — does not read scene buffers and behaves identically on all three.

These adapters will be rebuilt on command buffers and `RTHandle` operations, with a dedicated
RenderGraph path for URP 17+.

## Installation

### OpenUPM

Install the versioned package from the [OpenUPM registry](https://openupm.com/packages/com.luci0n.crowfx/):

```text
openupm add com.luci0n.crowfx
```

### Git URL

In Unity, open `Window > Package Manager`, select `+` → `Add package from git URL`, and enter:

```text
https://github.com/Luci0n/CrowFX-Unity-Image-Effects.git?path=CrowFX
```

![Installing CrowFX through the Unity Package Manager](https://github.com/user-attachments/assets/8b059973-532d-47cd-8ec3-a3a35e8e3b58)

### Release package

Download a `.unitypackage` from [Releases](https://github.com/Luci0n/CrowFX-Unity-Image-Effects/releases) and import it through `Assets > Import Package > Custom Package`.

## Quick start

1. Add `CrowImageEffects` to a camera.
2. Open **Look Library** in the component inspector and preview a look, or build a stack from individual sections.
3. Select **Apply** to commit a preview. Applying a full-stack look requires confirmation because it replaces all effect settings.
4. Save reusable configurations as `CrowFXProfile` or `CrowFXPresetAsset` assets.

On the experimental URP path, add `CrowFXRendererFeature` to the active renderer. On the experimental
HDRP path, register `CrowFXCustomPostProcess` in the HDRP custom post-process list and add it to a
Volume. Read [what "experimental" means](#what-experimental-means-here) first.

<p align="center">
  <img width="610" alt="CrowFX 2.0 Unity inspector showing workflow controls and effect sections" src="Documentation/Images/crowfx-inspector-top.png" /><br />
  <img width="610" alt="CrowFX 2.0 Unity inspector showing analog, display, and digital-video effects" src="Documentation/Images/crowfx-inspector-bottom.png" />
</p>

## Effect families

| Effect | Capabilities |
|---|---|
| Sampling & Grid | Pixel scaling, virtual resolution, aspect-aware sampling, area-averaged downsampling |
| Pre-Grade | Exposure, endpoint-preserving contrast, gamma, saturation, color filter |
| Lens & Sensor | Radial distortion, lateral aberration, vignette, bloom, rolling shutter, sensor defects |
| Film | Grain, halation, gate weave, dust, scratches, flicker |
| Posterize & Palette | Uniform/per-channel quantization, luminance-only processing, ramp and Oklab palette matching |
| Dithering | Ordered matrices, temporal noise, blue noise, linear, diamond, and print-screen patterns |
| RGB Bleed & Jitter | Manual/radial separation, edge gating, chroma smear, deterministic channel movement |
| Ghosting | Difference-gated trails, response smear, millisecond capture, exponential decay |
| Motion & Datamosh | Motion-vector displacement, persistent history, frame holds, codec-vector corruption |
| Edges & Sharpening | Relative-depth/view-normal outlines, unsharp mask, contrast-adaptive detail |
| Digital Video | Macroblocks, quantization, chroma subsampling, ringing, mosquito noise, bitrate pumping |
| VHS Tape | NTSC/PAL processing, SP/LP/EP bandwidth, copy generations, AGC, head switching, RF dropouts |
| Composite Signal | Chroma bandwidth, phase error, dot crawl, rainbowing, crosstalk, comb filtering |
| CRT Display | Gaussian beams, scanlines, phosphor masks, focus, convergence, halation, tube geometry, hum |
| LCD Display | Pixel lattice, RGB subpixels, inversion, viewing-angle shift, response smear, backlight bleed |
| Masks | Channel-selectable texture masks and feathered near/far depth masks |

## Look Library

The Look Library contains eight looks in each category:

- Clean & Production
- Pixel & Handheld
- CRT Displays
- VHS & Tape
- Print & Illustration
- Digital & Glitch
- Dream & Music Video
- Surveillance & Broadcast
- Horror & Distress
- Color & Experimental
- Research & Analysis

Pick one category, or choose **All Looks** to browse every category at once under collapsible headers.
Looks can be searched by name, purpose, or active-stage recipe, and each look's recipe is derived from
its own settings rather than authored by hand. Preview is non-destructive; stopping a preview restores
the complete previous stack.

**Bookmark** adds a look to a personal shortlist. The `Bookmarks` filter then shows every bookmarked
look across all categories and custom asset looks. Bookmarks are stored per machine and write nothing
to disk — `Save Current as New Look` and `Update Asset from Current Controls` create and modify preset
assets, and `Delete Look` removes a custom one. Bookmarks, category selection, search, amount,
collapsed groups, and the selected look persist between inspector sessions.

## Profiles and custom presets

- `CrowFXProfile` stores the complete component configuration and can be live-synced across cameras.
- `CrowFXPresetAsset` adds metadata such as description, usage tags, dependencies, strength, GPU tier, and schema version.
- To author a shareable preset, configure the `CrowImageEffects` controls, open **Presets > Look Library > Asset Looks**, and choose **Save Current**.
- New project presets embed their complete profile in one `.asset`. Commit that `.asset`, its `.meta`, and any referenced texture assets to the repository.
- Select any authored or custom look, apply it, polish the current controls, then choose **Update Asset from Current Controls** in its detail card. The embedded profile is recaptured while metadata and the asset GUID are preserved.
- Saving inside `Assets/CrowFX-Unity-Image-Effects` warns when a texture dependency lives outside the repository folder.
- Asset looks are displayed and searched inside the Look Library. Assets carrying a built-in source ID back the matching authored row, preserving the existing category, preview, favorite, and Apply workflow without creating duplicate entries.
- The 88 authored looks are stored under `CrowFX/Presets`; their stable source IDs connect them to the existing Look Library categories and rows.

## Render order

```text
Sampling → Grade → Lens/Sensor → Film → Channel/Temporal → Quantization/Palette/Edges
→ Digital Video → VHS → Composite → CRT/LCD → Stack Masks → Presentation
```

VHS, composite, CRT and LCD stages operate in a gamma-encoded signal domain, because scanlines,
phosphor masks and subpixel structure scale encoded drive values rather than radiometric intensity.
Creative stages retain HDR headroom where supported. Temporal history is captured before transport
and display simulation so tape and display artifacts do not recursively accumulate.

Set **Sampling & Grid > Sampling Filter** to `Box` when pixelating heavily. Point sampling takes one
texel per destination cell, so thin geometry and specular highlights fall between sample points and
crawl as the camera moves. `Box` averages every source texel a cell covers and removes that shimmer
at its source.

## Performance and validation

- Low, Balanced, and Reference quality tiers cap expensive history, smear, and palette operations.
- Ghost and datamosh buffers have independent resolution controls.
- Scanlines and phosphor masks fade safely when the output cannot resolve them.
- `Tools > CrowFX > Validate Installation` checks that every runtime shader resolves and is protected against stripping.
- Build validation fails on missing shaders and warns when shaders are unregistered and therefore strippable.
- `Tools > CrowFX > Generate Calibration Chart` creates a reference image for evaluating tone, resolution, and color behavior.

Every CrowFX shader is `Hidden/` and resolved by name at runtime, so nothing in the scene graph keeps
it alive and shader stripping can remove it from a player. When that happens the affected stage
renders as a passthrough rather than erroring. Registering the shaders once prevents this.

## More examples

**Per-channel posterize + edge outline + noise dither**

![Per-channel posterize, edge outline, and noise dither](https://github.com/user-attachments/assets/597c467b-2dcf-46ab-9e45-bdf9f59ac928)

## License

[MIT](LICENSE)
