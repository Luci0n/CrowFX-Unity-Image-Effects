# Changelog

All notable changes to CrowFX are documented in this file.

## [Unreleased]

## [2.2.0] - 2026-08-05

### Added

- Pipeline setup detection in the inspector. On URP it reports a missing renderer feature, an injection point that cannot reach the screen, and an intermediate texture mode CrowFX cannot read; on HDRP it reports a custom post process that was never registered or sits in the wrong injection list. Each carries a button that reveals the asset to fix
- Scene view rendering as an opt-in toggle on the URP renderer feature

### Fixed

- The URP and HDRP integration assemblies failed to compile, so neither adapter existed: `RTHandle`, `Blitter`, and the volume parameter types live in Core RP, which neither assembly definition referenced. On URP this left no entry in Add Renderer Feature at all
- The URP pass read the camera colour target outside a render pass and then cached it, so it wrote into an attachment URP had already swapped away from, and never flushed the queued work its immediate-mode stack depended on. Both produced an unchanged frame with an empty console
- The URP renderer feature defaulted to injecting after post-processing, where its output could be discarded
- A stereo pipeline hands the stack a texture array, which every stage then bound to its 2D sampler properties, logging "Dimensions must match" for `_OriginalTex`, `_HistoryTex`, and `_PrevTex` once per frame. The source is now flattened to 2D once at the top of the stack, covering intermediates, ghost and motion history, masks, and presentation. Stereo callers get a single eye

### Changed

- Moved the technical reference into `Documentation/`, split across inspector, effects, looks and presets, pipelines, and performance pages, leaving the README as a visual overview with installation and a quick start

## [2.1.0] - 2026-08-04

### Added

- Live inspector previews that run each stage's real shader over a test chart, so every control moves the image and time-based stages animate
- Multi-object editing across selected cameras, covering field edits, reset, randomize, paste, and look and profile application
- Single-pass instanced stereo support so every stage samples the correct eye slice instead of showing the left eye in both, implemented against Unity's stereo macros but untested on a headset
- Box sampling filter that averages the source texels each destination cell covers, removing the shimmer point-sampled pixelation produces
- All Looks view, look bookmarking, and preset removal in the Look Library
- Uncovered Edges, Dust Opacity, Bright/Dark Balance, and Display Signal Domain controls

### Changed

- URP and HDRP are now labelled experimental rather than supported; both adapters will be rebuilt on command buffers and RTHandle operations
- CRT and LCD simulation runs in the gamma-encoded signal domain, making their response noticeably stronger in Linear color-space projects
- Rebuilt film dust as sparse hard-edged particles at varied size, rotation, and elongation instead of a soft fixed-size dot lattice
- Stage shaders derive resolution from the render target rather than `_ScreenParams`, so pattern pitch survives render scale and off-screen cameras
- Compacted the Look Library to single-line rows, derived each recipe from its own settings, renamed look saving to bookmarking, and widened bookmarks to span every category
- Reworked inspector presentation with collapsible previews, per-section help buttons that are hidden by default, amber and red hints for warnings and errors, a tinted panel for global actions, and capitals in place of faked bold
- Build validation reports strippable shaders and points at Project Settings instead of offering a tool to register them

### Fixed

- Solo left seven sections rendering while everything else was muted, collapsed the sections it did mute, and kept the controls hidden on a section that was switched off when soloed
- Switched-off sections still drew any control their body reads without drawing, such as Edge Outline's thickness and normal-detection settings
- Edge Outline detected edges at full resolution after the sampling stage had already quantized the image to cells, so outlines sat off the blocky edges they belonged to
- Edge Outline, Depth Mask, and Motion & Datamosh read depth, normal, and motion buffers without correcting for the flipped blit projection, and sampled buffers that URP and HDRP never bind
- Lens distortion left the undistorted frame visible in a band around the warped image, and film dust translated diagonally instead of being redrawn each frame
- The summary pill rows wrapped against a view width that differs between the layout and repaint passes, aborting the repaint with a control-count mismatch
- Starting a look preview inserted its status line above the Preview button, pushing Stop Preview out from under the cursor that had just pressed it
- An authored look and a custom asset look could appear selected at the same time, and Lens & Sensor, Digital Video, and Motion & Datamosh showed no header status dot
- The HDRP volume component wrote its intensity into the serialized masterBlend field during rendering
- An AnimationCurve with no keyframes was treated as an authored tone remap and crushed the frame to black

## [2.0.1] - 2026-08-03

### Added

- Repository-ready preset assets for the complete Look Library, with direct editing and updating from current controls
- Per-frame temporal controls for procedural-noise and blue-noise dithering, plus adjustable dither pattern size
- Area-modulated halftone dots with Luminance, subtractive CMYK Print, and vivid RGB Rosette color modes

### Changed

- Reorganized and compacted the Look Library so built-in and custom preset assets share the same browsing and editing workflow
- Reworked film grain and general noise generation to evolve per frame instead of scrolling a static texture
- Strengthened composite-signal processing and rebuilt VHS noise, tracking, dropout, head-switching, and chroma behavior

### Fixed

- Restored active look, section, solo, mute, and bypass previews before entering Play Mode so previews cannot become runtime settings
- Prevented cropped labels, hints, tabs, and buttons throughout the custom inspector
- Fixed D3D11 compilation errors in the VHS tape shader
- Reduced unwanted rainbow coloration in halftone output by making the former RGB treatment opt-in

## [2.0.0] - 2026-08-02

### Added

- Calibrated CRT display simulation with consumer TV, arcade, PVM/BVM, and PC monitor presets
- VHS tape and composite-signal stages with standards, recording speeds, copy generations, RF behavior, AGC, and head switching
- Lens and sensor, film, motion/datamosh, digital-video, and LCD effect families
- URP 14+ Renderer Feature and HDRP 14+ Custom Post Process integrations
- 88-look searchable Look Library with preview, confirmation, favorites, persistent browsing state, and recipe summaries
- Data-driven `CrowFXPresetAsset` workflow with metadata, thumbnails, dependencies, strength, and GPU tier
- Low, Balanced, and Reference quality tiers with pass, sample, and history-memory estimates
- Installation/build validation, calibration-chart tooling, and editor tests

### Changed

- Rebuilt the custom inspector with themed controls, ordered sections, active-stage reporting, and compact preset browsing
- Updated grading, palette matching, dithering, edge detection, sharpening, masks, and HDR handling
- Reworked temporal history to use explicit capture cadence and stable injection points
- Rebalanced full-stack looks for clearer separation and more restrained contrast and ghosting
- Updated package metadata and documentation for the 2.0 public release

### Fixed

- Prevented lens distortion from producing doubled geometry and stretched edge samples
- Prevented RGB separation from turning fine luminance grain into strong colored speckles in affected looks
- Made datamosh readable in low-motion and static previews while preserving motion-vector behavior
- Difference-gated overlay ghosting so unchanged areas no longer brighten or appear double-exposed
- Preserved intentional color in terminal, print, and experimental looks

## [1.1.1]

- Previous public release
