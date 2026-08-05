# Effect families and render order

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

Disabled effects add no render passes, so a stack costs only what it actually uses.

## Render order

```text
Sampling → Grade → Lens/Sensor → Film → Channel/Temporal → Quantization/Palette/Edges
→ Digital Video → VHS → Composite → CRT/LCD → Stack Masks → Presentation
```

Temporal history is captured before transport and display simulation, so tape and display artifacts
do not recursively accumulate.

## The signal domain

VHS, composite, CRT and LCD stages operate in a gamma-encoded signal domain, because scanlines,
phosphor masks and subpixel structure scale encoded drive values rather than radiometric intensity.
In a Linear color-space project this makes their response noticeably stronger than treating the
frame as linear light would.

Creative stages retain HDR headroom where supported, and source alpha is preserved through the stack.

## Sampling: point versus box

Set **Sampling & Grid > Sampling Filter** to `Box` when pixelating heavily.

Point sampling takes one texel per destination cell, so thin geometry and specular highlights fall
between sample points and crawl as the camera moves. `Box` averages every source texel a cell covers
and removes that shimmer at its source, at the cost of extra samples per pixel.

## Depth-dependent stages

Edge Outline and Depth Mask read the camera depth texture, and Motion & Datamosh reads motion
vectors. CrowFX enables the required `depthTextureMode` automatically and the inspector offers a
fix-it button when a camera is missing it.

Edge Outline snaps its sampling to the pixel lattice, so outlines follow the blocky silhouette after
the sampling stage has quantized the image rather than tracing the underlying geometry.
