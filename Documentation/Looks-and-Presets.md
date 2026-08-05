# Look Library, profiles, and presets

## The Look Library

The Look Library contains eight looks in each of eleven categories:

| | |
|---|---|
| Clean & Production | Digital & Glitch |
| Pixel & Handheld | Dream & Music Video |
| CRT Displays | Surveillance & Broadcast |
| VHS & Tape | Horror & Distress |
| Print & Illustration | Color & Experimental |
| Research & Analysis | |

Pick one category, or choose **All Looks** to browse every category at once under collapsible
headers. Looks can be searched by name, purpose, or active-stage recipe, and each recipe is derived
from the look's own settings rather than authored by hand, so it cannot drift from what the look
actually does.

Preview is non-destructive: stopping a preview restores the complete previous stack. Applying a
full-stack look asks for confirmation, because it replaces every effect setting.

## Bookmarks

**Bookmark** adds a look to a personal shortlist. The `Bookmarks` filter then shows every bookmarked
look across all categories, including custom asset looks.

Bookmarks are stored per machine and write nothing to disk. Category selection, search, amount,
collapsed groups, and the selected look all persist between inspector sessions.

## Profiles

`CrowFXProfile` stores the complete component configuration as an asset and can be live-synced
across cameras. With **Auto Sync Profile** enabled, editing one camera updates the profile and every
other linked camera that also has Auto Sync enabled. With it disabled, **Apply Profile** pulls from
the asset and **Save to Profile** pushes into it.

## Preset assets

`CrowFXPresetAsset` adds metadata on top of a profile: description, usage tags, dependencies,
strength, GPU tier, and schema version.

To author one:

1. Configure the `CrowImageEffects` controls.
2. Open **Look Library → Asset Looks** and choose **Save Current as New Look**.
3. Commit the resulting `.asset`, its `.meta`, and any referenced texture assets.

To revise one, select the look, apply it, adjust the controls, then choose **Update Asset from
Current Controls** in its detail card. The embedded profile is recaptured while metadata and the
asset GUID are preserved, so existing references keep working.

Saving inside `Assets/CrowFX-Unity-Image-Effects` warns when a texture dependency lives outside the
repository folder, since that dependency would not travel with the preset.

## How authored and custom looks relate

The 88 authored looks are stored as preset assets under `CrowFX/Presets`. Each carries a stable
source ID that connects it to its Look Library category and row, so an asset backing a built-in look
updates that row rather than creating a duplicate entry.

Custom asset looks appear and are searched alongside authored ones, and can be removed with
**Delete Look**.
