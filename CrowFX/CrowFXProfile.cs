using System;
using UnityEngine;

namespace CrowFX
{
    [Serializable]
    public sealed class CrowFXMasterSettings
    {
        [Tooltip("Global opacity for the entire CrowFX stack.")]
        [Range(0f, 1f)] public float masterBlend = 1f;
    }

    [Serializable]
    public sealed class CrowFXSamplingSettings
    {
        [Tooltip("Size of each pixel block in screen pixels.")]
        [Range(1, 1024)] public int pixelSize = 1;
        [Tooltip("Locks sampling and dithering to a fixed virtual grid without replacing Pixel Block Size.")]
        public bool useVirtualGrid = false;
        [Tooltip("Virtual resolution used when Lock to Virtual Grid is enabled.")]
        public Vector2Int virtualResolution = new Vector2Int(720, 480);
    }

    [Serializable]
    public sealed class CrowFXPregradeSettings
    {
        [Tooltip("Enable exposure, contrast, gamma, and saturation adjustments before posterization.")]
        public bool pregradeEnabled = false;
        [Tooltip("Brightness adjustment applied before posterization.")]
        [Range(-5f, 5f)] public float exposure = 0f;
        [Tooltip("Contrast multiplier applied before posterization.")]
        [Range(0f, 2f)] public float contrast = 1f;
        [Tooltip("Gamma correction applied before posterization.")]
        [Range(0.1f, 3f)] public float gamma = 1f;
        [Tooltip("Color saturation applied before posterization.")]
        [Range(0f, 2f)] public float saturation = 1f;
    }

    [Serializable]
    public sealed class CrowFXPosterizeSettings
    {
        [Tooltip("Shared number of quantization levels for all channels.")]
        [Range(2, 512)] public int levels = 64;
        [Tooltip("Use independent quantization levels for red, green, and blue.")]
        public bool usePerChannel = false;
        [Tooltip("Quantization levels for the red channel.")]
        [Range(2, 512)] public int levelsR = 64;
        [Tooltip("Quantization levels for the green channel.")]
        [Range(2, 512)] public int levelsG = 64;
        [Tooltip("Quantization levels for the blue channel.")]
        [Range(2, 512)] public int levelsB = 64;
        [Tooltip("Animate the shared quantization level count over time.")]
        public bool animateLevels = false;
        [Tooltip("Lower bound used when Animated Levels is enabled.")]
        [Range(2, 512)] public int minLevels = 64;
        [Tooltip("Upper bound used when Animated Levels is enabled.")]
        [Range(2, 512)] public int maxLevels = 64;
        [Tooltip("Animation speed for cycling quantization levels.")]
        public float speed = 1f;
        [Tooltip("Posterize luminance while preserving overall color relationships.")]
        public bool luminanceOnly = false;
        [Tooltip("Invert the posterized output colors.")]
        public bool invert = false;
    }

    [Serializable]
    public sealed class CrowFXPaletteSettings
    {
        [Tooltip("Map final colors through a palette texture.")]
        public bool usePalette = false;
        [Tooltip("Palette lookup texture used when palette mapping is enabled.")]
        public Texture2D paletteTex;
        [Tooltip("Remap tonal values before palette lookup.")]
        public AnimationCurve thresholdCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }

    [Serializable]
    public sealed class CrowFXTextureMaskSettings
    {
        [Tooltip("Enable a texture mask to blend between processed and original image.")]
        public bool useMask = false;
        [Tooltip("Grayscale mask texture. White keeps the effect; black restores the source.")]
        public Texture2D maskTex;
        [Tooltip("Threshold used to cut between masked and unmasked areas.")]
        [Range(0f, 1f)] public float maskThreshold = 0.5f;
    }

    [Serializable]
    public sealed class CrowFXDepthMaskSettings
    {
        [Tooltip("Attenuate the effect based on scene depth.")]
        public bool useDepthMask = false;
        [Tooltip("Depth distance where the mask starts attenuating the effect.")]
        [Range(0f, 10f)] public float depthThreshold = 1f;
    }

    [Serializable]
    public sealed class CrowFXJitterSettings
    {
        [Tooltip("Enable per-channel sampling jitter.")]
        public bool jitterEnabled = false;
        [Tooltip("Blend amount between the original image and jittered sampling.")]
        [Range(0f, 1f)] public float jitterStrength = 0f;
        [Tooltip("Pattern used to generate jitter offsets.")]
        public CrowImageEffects.JitterMode jitterMode = CrowImageEffects.JitterMode.TimeSine;
        [Tooltip("Scales offset in pixels.")]
        [Range(0f, 8f)] public float jitterAmountPx = 1f;
        [Tooltip("Speed for animated jitter modes.")]
        [Range(0f, 30f)] public float jitterSpeed = 8f;
        [Tooltip("Use a stable seed so the pattern stays deterministic.")]
        public bool jitterUseSeed = false;
        [Tooltip("Stable seed used when Use Stable Seed is enabled.")]
        [Range(0, 9999)] public int jitterSeed = 1337;
        [Tooltip("Vary jitter per scanline for a VHS-style effect.")]
        public bool jitterScanline = false;
        [Tooltip("Scanline density measured in lines per screen height.")]
        [Range(32f, 2048f)] public float jitterScanlineDensity = 480f;
        [Tooltip("How much scanline modulation affects the jitter offset.")]
        [Range(0f, 2f)] public float jitterScanlineAmp = 0.35f;
        [Tooltip("Per-channel intensity multipliers (R, G, B).")]
        public Vector3 jitterChannelWeights = new Vector3(1f, 1f, 1f);
        [Tooltip("Per-channel direction in pixel space for the red channel.")]
        public Vector2 jitterDirR = new Vector2(1f, 0f);
        [Tooltip("Per-channel direction in pixel space for the green channel.")]
        public Vector2 jitterDirG = new Vector2(0f, 1f);
        [Tooltip("Per-channel direction in pixel space for the blue channel.")]
        public Vector2 jitterDirB = new Vector2(-1f, -1f);
        [Tooltip("Noise texture used by the BlueNoiseTex jitter mode.")]
        public Texture2D jitterNoiseTex;
        [Tooltip("Clamp UVs after offset to avoid sampling outside the source image.")]
        public bool jitterClampUV = true;
        [Tooltip("HashNoise only: number of noise cells per axis.")]
        [Range(4, 1024)] public int jitterHashCellCount = 256;
        [Tooltip("HashNoise only: blend between stepped and smoothed time.")]
        [Range(0f, 1f)] public float jitterHashTimeSmooth = 0f;
        [Tooltip("HashNoise only: rotate the hash grid to reduce axis-aligned patterns.")]
        [Range(-180f, 180f)] public float jitterHashRotateDeg = 0f;
        [Tooltip("HashNoise only: anisotropic scaling of the hash domain.")]
        public Vector2 jitterHashAniso = Vector2.one;
        [Tooltip("HashNoise only: domain warp amplitude in pixels.")]
        [Range(0f, 8f)] public float jitterHashWarpAmpPx = 0f;
        [Tooltip("HashNoise only: domain warp cell count.")]
        [Range(4, 1024)] public int jitterHashWarpCells = 64;
        [Tooltip("HashNoise only: domain warp animation speed.")]
        [Range(0f, 30f)] public float jitterHashWarpSpeed = 6f;
        [Tooltip("HashNoise only: give each channel its own hash vector.")]
        public bool jitterHashPerChannel = false;
    }

    [Serializable]
    public sealed class CrowFXBleedSettings
    {
        [Tooltip("Blend amount of the RGB bleed composite.")]
        [Range(0f, 1f)] public float bleedBlend = 0f;
        [Tooltip("Base distance used for channel separation.")]
        [Range(0f, 10f)] public float bleedIntensity = 0f;
        [Tooltip("Choose between manual per-channel shifts or radial shifting.")]
        public CrowImageEffects.BleedMode bleedMode = CrowImageEffects.BleedMode.Manual;
        [Tooltip("How the separated channels are combined back into the image.")]
        public CrowImageEffects.BleedBlendMode bleedBlendMode = CrowImageEffects.BleedBlendMode.Mix;
        [Tooltip("Manual screen-space shift for the red channel.")]
        public Vector2 shiftR = new Vector2(-0.5f, 0.5f);
        [Tooltip("Manual screen-space shift for the green channel.")]
        public Vector2 shiftG = new Vector2(0.5f, -0.5f);
        [Tooltip("Manual screen-space shift for the blue channel.")]
        public Vector2 shiftB = Vector2.zero;
        [Tooltip("Restrict bleed to higher-contrast edges.")]
        public bool bleedEdgeOnly = false;
        [Tooltip("Threshold for detecting edges when Edge Only is enabled.")]
        [Range(0f, 1f)] public float bleedEdgeThreshold = 0.05f;
        [Tooltip("Sharpness and contrast of the edge mask.")]
        [Range(0.25f, 8f)] public float bleedEdgePower = 2f;
        [Tooltip("Center point used by radial bleed mode.")]
        public Vector2 bleedRadialCenter = new Vector2(0.5f, 0.5f);
        [Tooltip("Signed radial shift strength. Positive pulls inward, negative pushes outward.")]
        [Range(-5f, 5f)] public float bleedRadialStrength = 1f;
        [Tooltip("Number of taps used when smear is active.")]
        [Range(1, 8)] public int bleedSamples = 1;
        [Tooltip("Additional trail length for multi-sample smear.")]
        [Range(0f, 5f)] public float bleedSmear = 0f;
        [Tooltip("How quickly smear samples fade over distance.")]
        [Range(0.25f, 6f)] public float bleedFalloff = 2f;
        [Tooltip("Per-channel multiplier for red shift strength.")]
        [Range(0f, 2f)] public float bleedIntensityR = 1f;
        [Tooltip("Per-channel multiplier for green shift strength.")]
        [Range(0f, 2f)] public float bleedIntensityG = 1f;
        [Tooltip("Per-channel multiplier for blue shift strength.")]
        [Range(0f, 2f)] public float bleedIntensityB = 1f;
        [Tooltip("Horizontal and vertical stretch applied to the bleed shape.")]
        public Vector2 bleedAnamorphic = Vector2.one;
        [Tooltip("Clamp screen UVs to avoid sampling outside the source image.")]
        public bool bleedClampUV = false;
        [Tooltip("Preserve approximate brightness after channel separation.")]
        public bool bleedPreserveLuma = false;
        [Tooltip("Animated wobble amount added to bleed offsets.")]
        [Range(0f, 2f)] public float bleedWobbleAmp = 0f;
        [Tooltip("Frequency of the bleed wobble animation.")]
        [Range(0f, 20f)] public float bleedWobbleFreq = 4f;
        [Tooltip("Modulate wobble per scanline for a VHS-style drift.")]
        public bool bleedWobbleScanline = false;
    }

    [Serializable]
    public sealed class CrowFXGhostSettings
    {
        [Tooltip("Enable motion-trail ghosting.")]
        public bool ghostEnabled = false;
        [Tooltip("Blend amount of the accumulated history.")]
        [Range(0f, 1f)] public float ghostBlend = 0.35f;
        [Tooltip("Per-frame offset applied between stored history frames.")]
        public Vector2 ghostOffsetPx = Vector2.zero;
        [Tooltip("Number of previous frames to store in history.")]
        [Range(1, 16)] public int ghostFrames = 4;
        [Tooltip("Frames to skip between history captures.")]
        [Range(0, 8)] public int ghostCaptureInterval = 0;
        [Tooltip("Delay before the first ghost frame appears.")]
        [Range(0, 8)] public int ghostStartDelay = 0;
        [Tooltip("Bias toward newer or older frames in the composite.")]
        [Range(0.25f, 4f)] public float ghostWeightCurve = 1.5f;
        [Tooltip("How the history composite blends with the current frame.")]
        public CrowImageEffects.GhostCombineMode ghostCombineMode = CrowImageEffects.GhostCombineMode.Screen;
    }

    [Serializable]
    public sealed class CrowFXEdgeSettings
    {
        [Tooltip("Enable depth-based outlines.")]
        public bool edgeEnabled = false;
        [Tooltip("Strength of the outline detection.")]
        [Range(0f, 8f)] public float edgeStrength = 1f;
        [Tooltip("Depth difference required to create an edge.")]
        [Range(0f, 1f)] public float edgeThreshold = 0.02f;
        [Tooltip("Blend amount of the outline pass.")]
        [Range(0f, 1f)] public float edgeBlend = 1f;
        [Tooltip("Tint used for the outline.")]
        public Color edgeColor = Color.black;
    }

    [Serializable]
    public sealed class CrowFXUnsharpSettings
    {
        [Tooltip("Enable the unsharp mask sharpening pass.")]
        public bool unsharpEnabled = false;
        [Tooltip("Strength of the sharpening effect.")]
        [Range(0f, 3f)] public float unsharpAmount = 0.5f;
        [Tooltip("Radius of the blur used to build the sharpen mask.")]
        [Range(0.25f, 4f)] public float unsharpRadius = 1f;
        [Tooltip("Ignore smaller differences to reduce sharpening of noise.")]
        [Range(0f, 0.25f)] public float unsharpThreshold = 0f;
        [Tooltip("Sharpen luminance only and keep color sharpening separate.")]
        public bool unsharpLumaOnly = false;
        [Tooltip("Additional sharpening applied to chroma when Luma Only is enabled.")]
        [Range(0f, 1f)] public float unsharpChroma = 0f;
    }

    [Serializable]
    public sealed class CrowFXDitherSettings
    {
        [Tooltip("Pattern used for dithering before final quantization.")]
        public CrowImageEffects.DitherMode ditherMode = CrowImageEffects.DitherMode.None;
        [Tooltip("How strongly the dither pattern affects the image.")]
        [Range(0f, 1f)] public float ditherStrength = 0f;
        [Tooltip("Rotation in degrees for the Linear dither pattern.")]
        [Range(0f, 180f)] public float ditherAngle = 45f;
        [Tooltip("Blue-noise texture used by Blue Noise mode.")]
        public Texture2D blueNoise;
    }

    [CreateAssetMenu(fileName = "CrowFXProfile", menuName = "CrowFX/CrowFX Profile")]
    public sealed class CrowFXProfile : ScriptableObject
    {
        [Tooltip("Global master settings shared by linked CrowFX components.")]
        public CrowFXMasterSettings master = new CrowFXMasterSettings();
        [Tooltip("Sampling and virtual-grid settings shared by linked CrowFX components.")]
        public CrowFXSamplingSettings sampling = new CrowFXSamplingSettings();
        [Tooltip("Pregrade settings shared by linked CrowFX components.")]
        public CrowFXPregradeSettings pregrade = new CrowFXPregradeSettings();
        [Tooltip("Posterize settings shared by linked CrowFX components.")]
        public CrowFXPosterizeSettings posterize = new CrowFXPosterizeSettings();
        [Tooltip("Palette-mapping settings shared by linked CrowFX components.")]
        public CrowFXPaletteSettings palette = new CrowFXPaletteSettings();
        [Tooltip("Texture-mask settings shared by linked CrowFX components.")]
        public CrowFXTextureMaskSettings textureMask = new CrowFXTextureMaskSettings();
        [Tooltip("Depth-mask settings shared by linked CrowFX components.")]
        public CrowFXDepthMaskSettings depthMask = new CrowFXDepthMaskSettings();
        [Tooltip("Jitter settings shared by linked CrowFX components.")]
        public CrowFXJitterSettings jitter = new CrowFXJitterSettings();
        [Tooltip("RGB bleed settings shared by linked CrowFX components.")]
        public CrowFXBleedSettings bleed = new CrowFXBleedSettings();
        [Tooltip("Ghosting settings shared by linked CrowFX components.")]
        public CrowFXGhostSettings ghost = new CrowFXGhostSettings();
        [Tooltip("Edge outline settings shared by linked CrowFX components.")]
        public CrowFXEdgeSettings edges = new CrowFXEdgeSettings();
        [Tooltip("Unsharp mask settings shared by linked CrowFX components.")]
        public CrowFXUnsharpSettings unsharp = new CrowFXUnsharpSettings();
        [Tooltip("Dithering settings shared by linked CrowFX components.")]
        public CrowFXDitherSettings dither = new CrowFXDitherSettings();

        public void ApplyTo(CrowImageEffects fx)
        {
            if (fx == null) return;

            fx.masterBlend = master.masterBlend;
            fx.pixelSize = sampling.pixelSize;
            fx.useVirtualGrid = sampling.useVirtualGrid;
            fx.virtualResolution = sampling.virtualResolution;
            fx.pregradeEnabled = pregrade.pregradeEnabled;
            fx.exposure = pregrade.exposure;
            fx.contrast = pregrade.contrast;
            fx.gamma = pregrade.gamma;
            fx.saturation = pregrade.saturation;
            fx.levels = posterize.levels;
            fx.usePerChannel = posterize.usePerChannel;
            fx.levelsR = posterize.levelsR;
            fx.levelsG = posterize.levelsG;
            fx.levelsB = posterize.levelsB;
            fx.animateLevels = posterize.animateLevels;
            fx.minLevels = posterize.minLevels;
            fx.maxLevels = posterize.maxLevels;
            fx.speed = posterize.speed;
            fx.luminanceOnly = posterize.luminanceOnly;
            fx.invert = posterize.invert;
            fx.usePalette = palette.usePalette;
            fx.paletteTex = palette.paletteTex;
            fx.thresholdCurve = CloneCurve(palette.thresholdCurve);
            fx.useMask = textureMask.useMask;
            fx.maskTex = textureMask.maskTex;
            fx.maskThreshold = textureMask.maskThreshold;
            fx.useDepthMask = depthMask.useDepthMask;
            fx.depthThreshold = depthMask.depthThreshold;
            fx.jitterEnabled = jitter.jitterEnabled;
            fx.jitterStrength = jitter.jitterStrength;
            fx.jitterMode = jitter.jitterMode;
            fx.jitterAmountPx = jitter.jitterAmountPx;
            fx.jitterSpeed = jitter.jitterSpeed;
            fx.jitterUseSeed = jitter.jitterUseSeed;
            fx.jitterSeed = jitter.jitterSeed;
            fx.jitterScanline = jitter.jitterScanline;
            fx.jitterScanlineDensity = jitter.jitterScanlineDensity;
            fx.jitterScanlineAmp = jitter.jitterScanlineAmp;
            fx.jitterChannelWeights = jitter.jitterChannelWeights;
            fx.jitterDirR = jitter.jitterDirR;
            fx.jitterDirG = jitter.jitterDirG;
            fx.jitterDirB = jitter.jitterDirB;
            fx.jitterNoiseTex = jitter.jitterNoiseTex;
            fx.jitterClampUV = jitter.jitterClampUV;
            fx.jitterHashCellCount = jitter.jitterHashCellCount;
            fx.jitterHashTimeSmooth = jitter.jitterHashTimeSmooth;
            fx.jitterHashRotateDeg = jitter.jitterHashRotateDeg;
            fx.jitterHashAniso = jitter.jitterHashAniso;
            fx.jitterHashWarpAmpPx = jitter.jitterHashWarpAmpPx;
            fx.jitterHashWarpCells = jitter.jitterHashWarpCells;
            fx.jitterHashWarpSpeed = jitter.jitterHashWarpSpeed;
            fx.jitterHashPerChannel = jitter.jitterHashPerChannel;
            fx.bleedBlend = bleed.bleedBlend;
            fx.bleedIntensity = bleed.bleedIntensity;
            fx.bleedMode = bleed.bleedMode;
            fx.bleedBlendMode = bleed.bleedBlendMode;
            fx.shiftR = bleed.shiftR;
            fx.shiftG = bleed.shiftG;
            fx.shiftB = bleed.shiftB;
            fx.bleedEdgeOnly = bleed.bleedEdgeOnly;
            fx.bleedEdgeThreshold = bleed.bleedEdgeThreshold;
            fx.bleedEdgePower = bleed.bleedEdgePower;
            fx.bleedRadialCenter = bleed.bleedRadialCenter;
            fx.bleedRadialStrength = bleed.bleedRadialStrength;
            fx.bleedSamples = bleed.bleedSamples;
            fx.bleedSmear = bleed.bleedSmear;
            fx.bleedFalloff = bleed.bleedFalloff;
            fx.bleedIntensityR = bleed.bleedIntensityR;
            fx.bleedIntensityG = bleed.bleedIntensityG;
            fx.bleedIntensityB = bleed.bleedIntensityB;
            fx.bleedAnamorphic = bleed.bleedAnamorphic;
            fx.bleedClampUV = bleed.bleedClampUV;
            fx.bleedPreserveLuma = bleed.bleedPreserveLuma;
            fx.bleedWobbleAmp = bleed.bleedWobbleAmp;
            fx.bleedWobbleFreq = bleed.bleedWobbleFreq;
            fx.bleedWobbleScanline = bleed.bleedWobbleScanline;
            fx.ghostEnabled = ghost.ghostEnabled;
            fx.ghostBlend = ghost.ghostBlend;
            fx.ghostOffsetPx = ghost.ghostOffsetPx;
            fx.ghostFrames = ghost.ghostFrames;
            fx.ghostCaptureInterval = ghost.ghostCaptureInterval;
            fx.ghostStartDelay = ghost.ghostStartDelay;
            fx.ghostWeightCurve = ghost.ghostWeightCurve;
            fx.ghostCombineMode = ghost.ghostCombineMode;
            fx.unsharpEnabled = unsharp.unsharpEnabled;
            fx.unsharpAmount = unsharp.unsharpAmount;
            fx.unsharpRadius = unsharp.unsharpRadius;
            fx.unsharpThreshold = unsharp.unsharpThreshold;
            fx.unsharpLumaOnly = unsharp.unsharpLumaOnly;
            fx.unsharpChroma = unsharp.unsharpChroma;
            fx.edgeEnabled = edges.edgeEnabled;
            fx.edgeStrength = edges.edgeStrength;
            fx.edgeThreshold = edges.edgeThreshold;
            fx.edgeBlend = edges.edgeBlend;
            fx.edgeColor = edges.edgeColor;
            fx.ditherMode = dither.ditherMode;
            fx.ditherStrength = dither.ditherStrength;
            fx.ditherAngle = dither.ditherAngle;
            fx.blueNoise = dither.blueNoise;
        }

        public void CaptureFrom(CrowImageEffects fx)
        {
            if (fx == null) return;

            master.masterBlend = fx.masterBlend;
            sampling.pixelSize = fx.pixelSize;
            sampling.useVirtualGrid = fx.useVirtualGrid;
            sampling.virtualResolution = fx.virtualResolution;
            pregrade.pregradeEnabled = fx.pregradeEnabled;
            pregrade.exposure = fx.exposure;
            pregrade.contrast = fx.contrast;
            pregrade.gamma = fx.gamma;
            pregrade.saturation = fx.saturation;
            posterize.levels = fx.levels;
            posterize.usePerChannel = fx.usePerChannel;
            posterize.levelsR = fx.levelsR;
            posterize.levelsG = fx.levelsG;
            posterize.levelsB = fx.levelsB;
            posterize.animateLevels = fx.animateLevels;
            posterize.minLevels = fx.minLevels;
            posterize.maxLevels = fx.maxLevels;
            posterize.speed = fx.speed;
            posterize.luminanceOnly = fx.luminanceOnly;
            posterize.invert = fx.invert;
            palette.usePalette = fx.usePalette;
            palette.paletteTex = fx.paletteTex;
            palette.thresholdCurve = CloneCurve(fx.thresholdCurve);
            textureMask.useMask = fx.useMask;
            textureMask.maskTex = fx.maskTex;
            textureMask.maskThreshold = fx.maskThreshold;
            depthMask.useDepthMask = fx.useDepthMask;
            depthMask.depthThreshold = fx.depthThreshold;
            jitter.jitterEnabled = fx.jitterEnabled;
            jitter.jitterStrength = fx.jitterStrength;
            jitter.jitterMode = fx.jitterMode;
            jitter.jitterAmountPx = fx.jitterAmountPx;
            jitter.jitterSpeed = fx.jitterSpeed;
            jitter.jitterUseSeed = fx.jitterUseSeed;
            jitter.jitterSeed = fx.jitterSeed;
            jitter.jitterScanline = fx.jitterScanline;
            jitter.jitterScanlineDensity = fx.jitterScanlineDensity;
            jitter.jitterScanlineAmp = fx.jitterScanlineAmp;
            jitter.jitterChannelWeights = fx.jitterChannelWeights;
            jitter.jitterDirR = fx.jitterDirR;
            jitter.jitterDirG = fx.jitterDirG;
            jitter.jitterDirB = fx.jitterDirB;
            jitter.jitterNoiseTex = fx.jitterNoiseTex;
            jitter.jitterClampUV = fx.jitterClampUV;
            jitter.jitterHashCellCount = fx.jitterHashCellCount;
            jitter.jitterHashTimeSmooth = fx.jitterHashTimeSmooth;
            jitter.jitterHashRotateDeg = fx.jitterHashRotateDeg;
            jitter.jitterHashAniso = fx.jitterHashAniso;
            jitter.jitterHashWarpAmpPx = fx.jitterHashWarpAmpPx;
            jitter.jitterHashWarpCells = fx.jitterHashWarpCells;
            jitter.jitterHashWarpSpeed = fx.jitterHashWarpSpeed;
            jitter.jitterHashPerChannel = fx.jitterHashPerChannel;
            bleed.bleedBlend = fx.bleedBlend;
            bleed.bleedIntensity = fx.bleedIntensity;
            bleed.bleedMode = fx.bleedMode;
            bleed.bleedBlendMode = fx.bleedBlendMode;
            bleed.shiftR = fx.shiftR;
            bleed.shiftG = fx.shiftG;
            bleed.shiftB = fx.shiftB;
            bleed.bleedEdgeOnly = fx.bleedEdgeOnly;
            bleed.bleedEdgeThreshold = fx.bleedEdgeThreshold;
            bleed.bleedEdgePower = fx.bleedEdgePower;
            bleed.bleedRadialCenter = fx.bleedRadialCenter;
            bleed.bleedRadialStrength = fx.bleedRadialStrength;
            bleed.bleedSamples = fx.bleedSamples;
            bleed.bleedSmear = fx.bleedSmear;
            bleed.bleedFalloff = fx.bleedFalloff;
            bleed.bleedIntensityR = fx.bleedIntensityR;
            bleed.bleedIntensityG = fx.bleedIntensityG;
            bleed.bleedIntensityB = fx.bleedIntensityB;
            bleed.bleedAnamorphic = fx.bleedAnamorphic;
            bleed.bleedClampUV = fx.bleedClampUV;
            bleed.bleedPreserveLuma = fx.bleedPreserveLuma;
            bleed.bleedWobbleAmp = fx.bleedWobbleAmp;
            bleed.bleedWobbleFreq = fx.bleedWobbleFreq;
            bleed.bleedWobbleScanline = fx.bleedWobbleScanline;
            ghost.ghostEnabled = fx.ghostEnabled;
            ghost.ghostBlend = fx.ghostBlend;
            ghost.ghostOffsetPx = fx.ghostOffsetPx;
            ghost.ghostFrames = fx.ghostFrames;
            ghost.ghostCaptureInterval = fx.ghostCaptureInterval;
            ghost.ghostStartDelay = fx.ghostStartDelay;
            ghost.ghostWeightCurve = fx.ghostWeightCurve;
            ghost.ghostCombineMode = fx.ghostCombineMode;
            unsharp.unsharpEnabled = fx.unsharpEnabled;
            unsharp.unsharpAmount = fx.unsharpAmount;
            unsharp.unsharpRadius = fx.unsharpRadius;
            unsharp.unsharpThreshold = fx.unsharpThreshold;
            unsharp.unsharpLumaOnly = fx.unsharpLumaOnly;
            unsharp.unsharpChroma = fx.unsharpChroma;
            edges.edgeEnabled = fx.edgeEnabled;
            edges.edgeStrength = fx.edgeStrength;
            edges.edgeThreshold = fx.edgeThreshold;
            edges.edgeBlend = fx.edgeBlend;
            edges.edgeColor = fx.edgeColor;
            dither.ditherMode = fx.ditherMode;
            dither.ditherStrength = fx.ditherStrength;
            dither.ditherAngle = fx.ditherAngle;
            dither.blueNoise = fx.blueNoise;
        }

        private static AnimationCurve CloneCurve(AnimationCurve curve)
        {
            if (curve == null)
                return AnimationCurve.Linear(0f, 0f, 1f, 1f);

            var clone = new AnimationCurve(curve.keys)
            {
                preWrapMode = curve.preWrapMode,
                postWrapMode = curve.postWrapMode
            };
            return clone;
        }
    }
}
