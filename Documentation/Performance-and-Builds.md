# Performance, validation, and builds

## Quality tiers

Low, Balanced, and Reference tiers cap the expensive work: history depth, smear sample counts, and
palette operations. Ghost and datamosh buffers have independent resolution controls, so temporal
effects can run at a fraction of the output resolution.

Only enabled stages cost anything — a disabled effect schedules no render pass at all. The inspector
reports the active pass count and an estimate of history memory at the current camera size.

Scanlines and phosphor masks fade out safely when the output resolution cannot resolve them, rather
than aliasing into moiré.

## Editor tools

| Tool | What it does |
|---|---|
| `Tools > CrowFX > Validate Installation` | Checks that every runtime shader resolves and is protected against stripping |
| `Tools > CrowFX > Generate Calibration Chart` | Creates a reference image for evaluating tone, resolution, and color behavior |

## Shader stripping

Every CrowFX shader is `Hidden/` and resolved by name at runtime. Nothing in the scene graph
references it, so Unity's shader stripping can remove it from a player build. When that happens the
affected stage renders as a passthrough rather than erroring.

Build validation fails on missing shaders and warns when shaders are unregistered and therefore
strippable. To prevent it, add the reported shaders to **Project Settings → Graphics → Always
Included Shaders**, or keep the CrowFX shader folder out of your stripping rules.

## Reporting problems

Useful details when filing an issue: Unity version, render pipeline and version, color space
(Linear or Gamma), whether the camera is rendering to a RenderTexture, and the look or individual
stage that reproduces it. A calibration chart screenshot helps for tone and color questions.
