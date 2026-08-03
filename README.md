<p align="center">
  <img width="718" height="131" alt="CrowFX" src="https://github.com/user-attachments/assets/e4fbf6dc-9f26-40bd-9dae-4f3ff64741fc" />
</p>

<p align="center">
  <img alt="Unity 2022.3+" src="https://img.shields.io/badge/Unity-2022.3%2B-000000?logo=unity" />
  <img alt="CrowFX 2.0.0" src="https://img.shields.io/badge/CrowFX-2.0.0-6f4ca6" />
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/License-MIT-blue.svg" /></a>
</p>

<p align="center">
  <strong>Free, open-source CRT, VHS, film, glitch, dithering, and retro post-processing for Unity.</strong><br />
  Built for the Built-in Render Pipeline, URP, and HDRP.
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
- Searchable custom inspector with previews, favorites, copy/paste, solo, mute, profiles, and Undo support
- Active-stage execution: disabled effects do not add render passes
- HDR-capable intermediate buffers with source-alpha preservation
- Built-in Render Pipeline support plus optional URP 14+ and HDRP 14+ integrations
- Installation validation, build-time shader checks, calibration charts, and editor tests

## Compatibility

| Pipeline | Support |
|---|---|
| Built-in Render Pipeline | `OnRenderImage` camera component |
| Universal Render Pipeline 14+ | `CrowFXRendererFeature` |
| High Definition Render Pipeline 14+ | `CrowFXCustomPostProcess` |

Unity 2022.3 LTS or newer is recommended.

## Installation

### Unity Package Manager

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

For URP, add `CrowFXRendererFeature` to the active renderer. For HDRP, register `CrowFXCustomPostProcess` in the HDRP custom post-process list and add it to a Volume.

<p align="center">
  <img width="610" alt="CrowFX 2.0 Unity inspector showing workflow controls and effect sections" src="Documentation/Images/crowfx-inspector-top.png" /><br />
  <img width="610" alt="CrowFX 2.0 Unity inspector showing analog, display, and digital-video effects" src="Documentation/Images/crowfx-inspector-bottom.png" />
</p>

## Effect families

| Effect | Capabilities |
|---|---|
| Sampling & Grid | Pixel scaling, virtual resolution, aspect-aware sampling |
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

Looks can be searched by name, purpose, or active-stage recipe. Preview is non-destructive; stopping a preview restores the complete previous stack. Favorites, category selection, search, amount, and the selected look persist between inspector sessions.

## Profiles and custom presets

- `CrowFXProfile` stores the complete component configuration and can be live-synced across cameras.
- `CrowFXPresetAsset` adds metadata such as description, usage tags, thumbnail, dependencies, strength, GPU tier, and schema version.
- Use the component context menu **Create Data-Driven Preset** to capture a reusable preset/profile pair.

## Render order

```text
Sampling → Grade → Lens/Sensor → Film → Channel/Temporal → Quantization/Palette/Edges
→ Digital Video → VHS → Composite → CRT/LCD → Stack Masks → Presentation
```

VHS and composite stages operate in a gamma-encoded signal domain. Creative stages retain HDR headroom where supported. Temporal history is captured before transport and display simulation so tape and display artifacts do not recursively accumulate.

## Performance and validation

- Low, Balanced, and Reference quality tiers cap expensive history, smear, and palette operations.
- Ghost and datamosh buffers have independent resolution controls.
- Scanlines and phosphor masks fade safely when the output cannot resolve them.
- `Tools > CrowFX > Validate Installation` checks all required runtime shaders.
- Build validation reports missing or stripped shaders before creating a player.
- `Tools > CrowFX > Generate Calibration Chart` creates a reference image for evaluating tone, resolution, and color behavior.

Projects with aggressive shader stripping should keep the CrowFX shader folder or add the reported hidden shaders to **Always Included Shaders**.

## More examples

**Per-channel posterize + edge outline + noise dither**

![Per-channel posterize, edge outline, and noise dither](https://github.com/user-attachments/assets/597c467b-2dcf-46ab-9e45-bdf9f59ac928)

## License

[MIT](LICENSE)
