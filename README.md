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
  <strong>Free, open-source CRT, VHS, film, glitch, dithering, and retro post-processing for Unity.</strong>
</p>

<p align="center">
  <a href="#installation"><strong>Install</strong></a> ·
  <a href="#quick-start"><strong>Quick start</strong></a> ·
  <a href="Documentation/README.md"><strong>Documentation</strong></a> ·
  <a href="https://github.com/Luci0n/CrowFX-Unity-Image-Effects/releases"><strong>Releases</strong></a>
</p>

<br />

| Ghosting, jitter, and dither | Posterize, RGB bleed, and virtual resolution |
|---|---|
| ![Unity ghosting, RGB jitter, and dithering post-processing](https://github.com/user-attachments/assets/604eeb15-4901-4867-8834-d25287cdd2c3) | ![Unity posterization, RGB bleed, sharpening, and virtual-resolution effects](https://github.com/user-attachments/assets/9504b73c-be0f-4189-9c00-7c710078ede5) |

CrowFX turns a Unity camera into a complete real-time image-effects stack. Build convincing CRT
displays, VHS and composite video, film damage, PSX-style color and dithering, analog-horror footage,
digital glitches, LCD artifacts, and polished cinematic looks — from one searchable inspector.

<br />

<p align="center">
  <img alt="Per-channel posterize, edge outline, and noise dither" src="https://github.com/user-attachments/assets/597c467b-2dcf-46ab-9e45-bdf9f59ac928" />
</p>

## Highlights

- **88 curated looks** across 11 categories, searchable, with non-destructive preview
- **High-fidelity simulation** of CRT, VHS, composite video, film, lens, sensor, LCD, and codec artifacts
- **Live per-section previews** that run the real stage shader, so every control moves the image
- **Only what you use** — disabled effects add no render passes
- **Built to share** — profiles and preset assets that travel with your project

## Installation

### OpenUPM

```text
openupm add com.luci0n.crowfx
```

### Git URL

In Unity, open `Window > Package Manager`, select `+` → `Add package from git URL`, and enter:

```text
https://github.com/Luci0n/CrowFX-Unity-Image-Effects.git?path=CrowFX
```

<p align="center">
  <img alt="Installing CrowFX through the Unity Package Manager" src="https://github.com/user-attachments/assets/8b059973-532d-47cd-8ec3-a3a35e8e3b58" />
</p>

### Release package

Download a `.unitypackage` from [Releases](https://github.com/Luci0n/CrowFX-Unity-Image-Effects/releases)
and import it through `Assets > Import Package > Custom Package`.

## Quick start

1. Add `CrowImageEffects` to a camera.
2. Open **Look Library** in the inspector and preview a look, or build a stack from individual sections.
3. Select **Apply** to commit a preview.
4. Save reusable configurations as `CrowFXProfile` or `CrowFXPresetAsset` assets.

<p align="center">
  <img width="610" alt="CrowFX inspector showing workflow controls and effect sections" src="Documentation/Images/crowfx-inspector-top.png" /><br />
  <img width="610" alt="CrowFX inspector showing analog, display, and digital-video effects" src="Documentation/Images/crowfx-inspector-bottom.png" />
</p>

## Effects

Sampling & Grid · Pre-Grade · Lens & Sensor · Film · Posterize & Palette · Dithering ·
RGB Bleed & Jitter · Ghosting · Motion & Datamosh · Edges & Sharpening · Digital Video ·
VHS Tape · Composite Signal · CRT Display · LCD Display · Masks

See [Effects](Documentation/Effects.md) for what each family does and the order they run in.

## Compatibility

Built for the **Built-in Render Pipeline** on Unity 2022.3 LTS or newer. URP and HDRP adapters are
**experimental** — see [Pipelines](Documentation/Pipelines.md) before relying on them.

## Documentation

- [Inspector](Documentation/Inspector.md) — sections, Solo/Mute/Bypass, live previews, multi-camera editing
- [Effects](Documentation/Effects.md) — effect families, render order, signal domain, sampling
- [Looks and presets](Documentation/Looks-and-Presets.md) — Look Library, bookmarks, profiles, preset assets
- [Pipelines](Documentation/Pipelines.md) — Built-in, URP, HDRP, XR and stereo
- [Performance and builds](Documentation/Performance-and-Builds.md) — quality tiers, validation, shader stripping

## License

[MIT](LICENSE)
