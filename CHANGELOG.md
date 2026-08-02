# Changelog

All notable changes to CrowFX are documented in this file.

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
