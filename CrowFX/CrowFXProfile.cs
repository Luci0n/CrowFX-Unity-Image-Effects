using System;
using UnityEngine;

namespace CrowFX
{
    [Serializable]
    public sealed class CrowFXMasterSettings
    {
        [Tooltip("Global opacity for the entire CrowFX stack.")]
        [Range(0f, 1f)] public float masterBlend = 1f;
        public CrowImageEffects.QualityTier qualityTier = CrowImageEffects.QualityTier.Balanced;
        public CrowImageEffects.MaskPlacement maskPlacement = CrowImageEffects.MaskPlacement.EntireStack;
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
        public Vector2 samplingPhase = Vector2.zero;
        [Range(0.25f, 4f)] public float pixelAspect = 1f;
        public CrowImageEffects.SamplingFilter samplingFilter = CrowImageEffects.SamplingFilter.Point;
    }

    [Serializable]
    public sealed class CrowFXPregradeSettings
    {
        [Tooltip("Enable exposure, contrast, gamma, and saturation adjustments before posterization.")]
        public bool pregradeEnabled = false;
        [Tooltip("Brightness adjustment applied before posterization.")]
        [Range(-5f, 5f)] public float exposure = 0f;
        [Tooltip("Endpoint-preserving luminance contrast curve. Values above 1 add contrast without hard black or white clipping.")]
        [Range(0f, 2f)] public float contrast = 1f;
        [Tooltip("Gamma correction applied before posterization.")]
        [Range(0.1f, 3f)] public float gamma = 1f;
        [Tooltip("Color saturation applied before posterization.")]
        [Range(0f, 2f)] public float saturation = 1f;
        [Tooltip("Color filter applied after saturation while approximately preserving luminance.")]
        public Color pregradeTint = Color.white;
        [Tooltip("Strength of the pre-grade color filter.")]
        [Range(0f, 1f)] public float pregradeTintStrength = 0f;
        public Color pregradeLift = new Color(0f, 0f, 0f, 0f);
        public Color pregradeGain = Color.white;
        public Color pregradeOffset = new Color(0f, 0f, 0f, 0f);
        [Range(-1f, 1f)] public float pregradeTemperature = 0f;
        [Range(0f, 2f)] public float pregradeHighlightRolloff = 0.5f;
    }

    [Serializable]
    public sealed class CrowFXPosterizeSettings
    {
        [Tooltip("Shared number of quantization levels for all channels.")]
        [Range(2, 512)] public int levels = 512;
        [Tooltip("Use independent quantization levels for red, green, and blue.")]
        public bool usePerChannel = false;
        [Tooltip("Quantization levels for the red channel.")]
        [Range(2, 512)] public int levelsR = 512;
        [Tooltip("Quantization levels for the green channel.")]
        [Range(2, 512)] public int levelsG = 512;
        [Tooltip("Quantization levels for the blue channel.")]
        [Range(2, 512)] public int levelsB = 512;
        [Tooltip("Animate the shared quantization level count over time.")]
        public bool animateLevels = false;
        [Tooltip("Lower bound used when Animated Levels is enabled.")]
        [Range(2, 512)] public int minLevels = 512;
        [Tooltip("Upper bound used when Animated Levels is enabled.")]
        [Range(2, 512)] public int maxLevels = 512;
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
        [Tooltip("Ramp follows tonal value along the palette strip. Nearest matches each pixel to the closest palette swatch.")]
        public CrowImageEffects.PaletteMode paletteMode = CrowImageEffects.PaletteMode.Nearest;
        [Tooltip("Palette lookup texture used when palette mapping is enabled.")]
        public Texture2D paletteTex;
        [Tooltip("Remap tonal values before palette lookup or nearest-color matching.")]
        public AnimationCurve thresholdCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [Range(2, 64)] public int paletteColorCount = 16;
        public bool palettePerceptual = true;
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
        [Range(0f, 1f)] public float maskSoftness = 0.1f;
        [Range(0f, 1f)] public float maskOpacity = 1f;
        public bool maskInvert = false;
        public CrowImageEffects.MaskChannel maskChannel = CrowImageEffects.MaskChannel.Luminance;
        public Vector2 maskTiling = Vector2.one;
        public Vector2 maskOffset = Vector2.zero;
    }

    [Serializable]
    public sealed class CrowFXDepthMaskSettings
    {
        [Tooltip("Attenuate the effect based on scene depth.")]
        public bool useDepthMask = false;
        [Tooltip("Depth distance where the mask starts attenuating the effect.")]
        [Range(0f, 10f)] public float depthThreshold = 1f;
        [Range(0f, 1000f)] public float depthFar = 1000f;
        [Range(0f, 50f)] public float depthSoftness = 0.25f;
        [Range(0f, 1f)] public float depthOpacity = 1f;
        public bool depthInvert = false;
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
        [Range(0f, 1f)] public float ghostBlend = 0.20f;
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
        [Range(0.25f, 1f)] public float ghostResolutionScale = 0.5f;
        [Range(8f, 250f)] public float ghostFrameIntervalMs = 33.333f;
        [Range(16f, 2000f)] public float ghostDecayMs = 180f;
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
        [Range(0.5f, 4f)] public float edgeThickness = 1f;
        public bool edgeUseNormals = true;
        [Range(0f, 1f)] public float edgeNormalThreshold = 0.18f;
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
        public CrowImageEffects.SharpenMode sharpenMode = CrowImageEffects.SharpenMode.ContrastAdaptive;
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
        public bool temporalDither = false;
        [Range(1f, 120f)] public float temporalDitherRate = 30f;
        [Range(2f, 24f)] public float halftoneScale = 6f;
        [Range(-0.5f, 0.5f)] public float halftoneDotGain = 0f;
    }

    [Serializable]
    public sealed class CrowFXCrtSettings
    {
        public bool crtEnabled = false;
        [Range(0f, 0.35f)] public float crtCurvature = 0.08f;
        [Range(0.8f, 1.2f)] public float crtOverscan = 1.02f;
        [Range(0f, 1f)] public float crtScanlineStrength = 0.75f;
        [Range(0, 1200)] public int crtScanlineCount = 240;
        [Range(0.2f, 1.5f)] public float crtBeamWidth = 0.5f;
        public CrowImageEffects.CrtMaskMode crtMaskMode = CrowImageEffects.CrtMaskMode.ApertureGrille;
        [Range(0f, 1f)] public float crtMaskStrength = 0.35f;
        [Range(1f, 6f)] public float crtMaskScale = 1f;
        [Range(0f, 1.5f)] public float crtBloom = 0.2f;
        [Range(0.5f, 4f)] public float crtBloomRadius = 1.5f;
        [Range(0f, 1f)] public float crtVignette = 0.35f;
        [Range(0.01f, 0.5f)] public float crtVignetteSoftness = 0.18f;
        [Range(0f, 0.2f)] public float crtNoise = 0.015f;
        [Range(0f, 0.2f)] public float crtFlicker = 0.015f;
        [Range(0.5f, 3f)] public float crtBrightness = 1.2f;
        [Range(0f, 1f)] public float crtTubeEdge = 1f;
        [Range(0f, 2f)] public float crtBloomThreshold = 0.55f;
        [Range(0f, 3f)] public float crtConvergencePx = 0f;
        [Range(0f, 1f)] public float crtFocus = 0.35f;
        [Range(0f, 0.2f)] public float crtBlackLevel = 0f;
        [Range(0f, 0.2f)] public float crtHumBar = 0f;
        [Range(24f, 120f)] public float crtFlickerHz = 60f;
    }

    [Serializable]
    public sealed class CrowFXVhsSettings
    {
        public bool vhsEnabled = false;
        [Range(0f, 1f)] public float vhsIntensity = 0.8f;
        [Range(0f, 4f)] public float vhsTapeSpeed = 1f;
        [Range(0f, 12f)] public float vhsHorizontalJitter = 1.5f;
        [Range(0f, 12f)] public float vhsLineWobble = 2f;
        [Range(0f, 1f)] public float vhsTracking = 0.25f;
        [Range(-4f, 4f)] public float vhsTrackingSpeed = 0.65f;
        [Range(0.005f, 0.25f)] public float vhsTrackingWidth = 0.055f;
        [Range(-12f, 12f)] public float vhsChromaBleed = 3f;
        [Range(0f, 12f)] public float vhsChromaBlur = 4f;
        [Range(0f, 1f)] public float vhsColorLoss = 0.15f;
        [Range(0f, 0.35f)] public float vhsLumaNoise = 0.055f;
        [Range(0f, 0.35f)] public float vhsChromaNoise = 0.025f;
        [Range(0f, 1f)] public float vhsDropout = 0.12f;
        [Range(0f, 1f)] public float vhsHeadSwitching = 0.2f;
        [Range(0.005f, 0.2f)] public float vhsHeadSwitchHeight = 0.045f;
        [Range(0f, 1f)] public float vhsInterlace = 0.12f;
        public CrowImageEffects.VhsStandard vhsStandard = CrowImageEffects.VhsStandard.NTSC;
        public CrowImageEffects.VhsTapeMode vhsTapeMode = CrowImageEffects.VhsTapeMode.SP;
        [Range(0, 8)] public int vhsGeneration = 0;
        [Range(0f, 1f)] public float vhsAgcInstability = 0.08f;
        [Range(0f, 4f)] public float vhsVerticalChromaBlur = 1f;
    }

    [Serializable]
    public sealed class CrowFXProfessionalSettings
    {
        public bool lensSensorEnabled; [Range(0f,1f)] public float lensSensorIntensity = 1f;
        [Range(-0.5f,0.5f)] public float lensDistortion; [Range(0f,8f)] public float lensChromaticAberration;
        [Range(0f,1f)] public float lensVignette; [Range(0f,2f)] public float lensBloom; [Range(0.5f,8f)] public float lensBloomRadius = 2f;
        [Range(0f,12f)] public float sensorRollingShutter; [Range(0f,0.25f)] public float sensorNoise; [Range(0f,1f)] public float sensorDeadPixels;

        public bool filmEnabled; [Range(0f,1f)] public float filmIntensity = 1f; [Range(0f,0.5f)] public float filmGrain = 0.08f;
        [Range(0.5f,4f)] public float filmGrainSize = 1f; [Range(0f,2f)] public float filmHalation = 0.15f; [Range(0.5f,8f)] public float filmHalationRadius = 2f;
        [Range(0f,6f)] public float filmGateWeave = 0.25f; [Range(0f,1f)] public float filmDust; [Range(0f,1f)] public float filmScratches; [Range(0f,0.2f)] public float filmFlicker = 0.01f;

        public bool motionGlitchEnabled; [Range(0f,1f)] public float motionGlitchIntensity = 0.6f; [Range(4f,128f)] public float motionBlockSize = 32f;
        [Range(0f,8f)] public float motionVectorDisplacement = 2f; [Range(0f,1f)] public float motionFreezeRate = 0.1f; [Range(0f,8f)] public float motionColorSplit;
        [Range(0.25f,1f)] public float motionHistoryScale = 0.5f; [Range(1f,60f)] public float motionHistoryFps = 24f;

        public bool digitalVideoEnabled; [Range(0f,1f)] public float digitalVideoIntensity = 0.7f; [Range(4f,64f)] public float digitalBlockSize = 16f;
        [Range(0f,1f)] public float digitalQuantization = 0.25f; [Range(0f,1f)] public float digitalRinging = 0.1f;
        [Range(0f,1f)] public float digitalChromaSubsampling = 0.35f; [Range(0f,1f)] public float digitalMosquitoNoise = 0.05f; [Range(0f,1f)] public float digitalBitratePumping;

        public bool compositeEnabled; [Range(0f,1f)] public float compositeIntensity = 0.7f; public CrowImageEffects.VhsStandard compositeStandard;
        [Range(0f,1f)] public float compositeDotCrawl = 0.25f; [Range(0f,1f)] public float compositeRainbow = 0.15f;
        [Range(0f,1f)] public float compositeChromaBandwidth = 0.55f; [Range(0f,1f)] public float compositePhaseError = 0.05f; [Range(0f,1f)] public float compositeCombFilter = 0.5f;

        public bool lcdEnabled; [Range(0f,1f)] public float lcdIntensity = 0.8f; [Range(1f,8f)] public float lcdPixelScale = 2f;
        [Range(0f,1f)] public float lcdSubpixelStrength = 0.35f; [Range(0f,1f)] public float lcdInversion = 0.03f; [Range(-1f,1f)] public float lcdViewingAngle;
        [Range(0f,1f)] public float lcdBacklightBleed = 0.05f; [Range(0f,4f)] public float lcdResponseSmear = 0.4f;
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
        [Tooltip("CRT display simulation settings shared by linked CrowFX components.")]
        public CrowFXCrtSettings crt = new CrowFXCrtSettings();
        [Tooltip("VHS tape simulation settings shared by linked CrowFX components.")]
        public CrowFXVhsSettings vhs = new CrowFXVhsSettings();
        [Tooltip("Lens, film, compression, composite, LCD and motion-processing settings.")]
        public CrowFXProfessionalSettings professional = new CrowFXProfessionalSettings();

        public void ApplyTo(CrowImageEffects fx)
        {
            if (fx == null) return;

            fx.masterBlend = master.masterBlend;
            fx.qualityTier = master.qualityTier;
            fx.maskPlacement = master.maskPlacement;
            fx.pixelSize = sampling.pixelSize;
            fx.useVirtualGrid = sampling.useVirtualGrid;
            fx.virtualResolution = sampling.virtualResolution;
            fx.samplingPhase = sampling.samplingPhase;
            fx.pixelAspect = sampling.pixelAspect;
            fx.samplingFilter = sampling.samplingFilter;
            fx.pregradeEnabled = pregrade.pregradeEnabled;
            fx.exposure = pregrade.exposure;
            fx.contrast = pregrade.contrast;
            fx.gamma = pregrade.gamma;
            fx.saturation = pregrade.saturation;
            fx.pregradeTint = pregrade.pregradeTint;
            fx.pregradeTintStrength = pregrade.pregradeTintStrength;
            fx.pregradeLift = pregrade.pregradeLift;
            fx.pregradeGain = pregrade.pregradeGain;
            fx.pregradeOffset = pregrade.pregradeOffset;
            fx.pregradeTemperature = pregrade.pregradeTemperature;
            fx.pregradeHighlightRolloff = pregrade.pregradeHighlightRolloff;
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
            fx.paletteMode = palette.paletteMode;
            fx.paletteTex = palette.paletteTex;
            fx.thresholdCurve = CloneCurve(palette.thresholdCurve);
            fx.paletteColorCount = palette.paletteColorCount;
            fx.palettePerceptual = palette.palettePerceptual;
            fx.useMask = textureMask.useMask;
            fx.maskTex = textureMask.maskTex;
            fx.maskThreshold = textureMask.maskThreshold;
            fx.maskSoftness = textureMask.maskSoftness;
            fx.maskOpacity = textureMask.maskOpacity;
            fx.maskInvert = textureMask.maskInvert;
            fx.maskChannel = textureMask.maskChannel;
            fx.maskTiling = textureMask.maskTiling;
            fx.maskOffset = textureMask.maskOffset;
            fx.useDepthMask = depthMask.useDepthMask;
            fx.depthThreshold = depthMask.depthThreshold;
            fx.depthFar = depthMask.depthFar;
            fx.depthSoftness = depthMask.depthSoftness;
            fx.depthOpacity = depthMask.depthOpacity;
            fx.depthInvert = depthMask.depthInvert;
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
            fx.ghostResolutionScale = ghost.ghostResolutionScale;
            fx.ghostFrameIntervalMs = ghost.ghostFrameIntervalMs;
            fx.ghostDecayMs = ghost.ghostDecayMs;
            fx.unsharpEnabled = unsharp.unsharpEnabled;
            fx.unsharpAmount = unsharp.unsharpAmount;
            fx.unsharpRadius = unsharp.unsharpRadius;
            fx.unsharpThreshold = unsharp.unsharpThreshold;
            fx.unsharpLumaOnly = unsharp.unsharpLumaOnly;
            fx.unsharpChroma = unsharp.unsharpChroma;
            fx.sharpenMode = unsharp.sharpenMode;
            fx.edgeEnabled = edges.edgeEnabled;
            fx.edgeStrength = edges.edgeStrength;
            fx.edgeThreshold = edges.edgeThreshold;
            fx.edgeBlend = edges.edgeBlend;
            fx.edgeColor = edges.edgeColor;
            fx.edgeThickness = edges.edgeThickness;
            fx.edgeUseNormals = edges.edgeUseNormals;
            fx.edgeNormalThreshold = edges.edgeNormalThreshold;
            fx.ditherMode = dither.ditherMode;
            fx.ditherStrength = dither.ditherStrength;
            fx.ditherAngle = dither.ditherAngle;
            fx.blueNoise = dither.blueNoise;
            fx.temporalDither = dither.temporalDither;
            fx.temporalDitherRate = dither.temporalDitherRate;
            fx.halftoneScale = dither.halftoneScale;
            fx.halftoneDotGain = dither.halftoneDotGain;
            fx.crtEnabled = crt.crtEnabled;
            fx.crtCurvature = crt.crtCurvature;
            fx.crtOverscan = crt.crtOverscan;
            fx.crtScanlineStrength = crt.crtScanlineStrength;
            fx.crtScanlineCount = crt.crtScanlineCount;
            fx.crtBeamWidth = crt.crtBeamWidth;
            fx.crtMaskMode = crt.crtMaskMode;
            fx.crtMaskStrength = crt.crtMaskStrength;
            fx.crtMaskScale = crt.crtMaskScale;
            fx.crtBloom = crt.crtBloom;
            fx.crtBloomRadius = crt.crtBloomRadius;
            fx.crtVignette = crt.crtVignette;
            fx.crtVignetteSoftness = crt.crtVignetteSoftness;
            fx.crtNoise = crt.crtNoise;
            fx.crtFlicker = crt.crtFlicker;
            fx.crtBrightness = crt.crtBrightness;
            fx.crtTubeEdge = crt.crtTubeEdge;
            fx.crtBloomThreshold = crt.crtBloomThreshold;
            fx.crtConvergencePx = crt.crtConvergencePx;
            fx.crtFocus = crt.crtFocus;
            fx.crtBlackLevel = crt.crtBlackLevel;
            fx.crtHumBar = crt.crtHumBar;
            fx.crtFlickerHz = crt.crtFlickerHz;
            fx.vhsEnabled = vhs.vhsEnabled;
            fx.vhsIntensity = vhs.vhsIntensity;
            fx.vhsTapeSpeed = vhs.vhsTapeSpeed;
            fx.vhsHorizontalJitter = vhs.vhsHorizontalJitter;
            fx.vhsLineWobble = vhs.vhsLineWobble;
            fx.vhsTracking = vhs.vhsTracking;
            fx.vhsTrackingSpeed = vhs.vhsTrackingSpeed;
            fx.vhsTrackingWidth = vhs.vhsTrackingWidth;
            fx.vhsChromaBleed = vhs.vhsChromaBleed;
            fx.vhsChromaBlur = vhs.vhsChromaBlur;
            fx.vhsColorLoss = vhs.vhsColorLoss;
            fx.vhsLumaNoise = vhs.vhsLumaNoise;
            fx.vhsChromaNoise = vhs.vhsChromaNoise;
            fx.vhsDropout = vhs.vhsDropout;
            fx.vhsHeadSwitching = vhs.vhsHeadSwitching;
            fx.vhsHeadSwitchHeight = vhs.vhsHeadSwitchHeight;
            fx.vhsInterlace = vhs.vhsInterlace;
            fx.vhsStandard = vhs.vhsStandard;
            fx.vhsTapeMode = vhs.vhsTapeMode;
            fx.vhsGeneration = vhs.vhsGeneration;
            fx.vhsAgcInstability = vhs.vhsAgcInstability;
            fx.vhsVerticalChromaBlur = vhs.vhsVerticalChromaBlur;
            ApplyProfessional(fx, professional);
        }

        public void CaptureFrom(CrowImageEffects fx)
        {
            if (fx == null) return;
            if (professional == null) professional = new CrowFXProfessionalSettings();

            master.masterBlend = fx.masterBlend;
            master.qualityTier = fx.qualityTier;
            master.maskPlacement = fx.maskPlacement;
            sampling.pixelSize = fx.pixelSize;
            sampling.useVirtualGrid = fx.useVirtualGrid;
            sampling.virtualResolution = fx.virtualResolution;
            sampling.samplingPhase = fx.samplingPhase;
            sampling.pixelAspect = fx.pixelAspect;
            sampling.samplingFilter = fx.samplingFilter;
            pregrade.pregradeEnabled = fx.pregradeEnabled;
            pregrade.exposure = fx.exposure;
            pregrade.contrast = fx.contrast;
            pregrade.gamma = fx.gamma;
            pregrade.saturation = fx.saturation;
            pregrade.pregradeTint = fx.pregradeTint;
            pregrade.pregradeTintStrength = fx.pregradeTintStrength;
            pregrade.pregradeLift = fx.pregradeLift;
            pregrade.pregradeGain = fx.pregradeGain;
            pregrade.pregradeOffset = fx.pregradeOffset;
            pregrade.pregradeTemperature = fx.pregradeTemperature;
            pregrade.pregradeHighlightRolloff = fx.pregradeHighlightRolloff;
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
            palette.paletteMode = fx.paletteMode;
            palette.paletteTex = fx.paletteTex;
            palette.thresholdCurve = CloneCurve(fx.thresholdCurve);
            palette.paletteColorCount = fx.paletteColorCount;
            palette.palettePerceptual = fx.palettePerceptual;
            textureMask.useMask = fx.useMask;
            textureMask.maskTex = fx.maskTex;
            textureMask.maskThreshold = fx.maskThreshold;
            textureMask.maskSoftness = fx.maskSoftness;
            textureMask.maskOpacity = fx.maskOpacity;
            textureMask.maskInvert = fx.maskInvert;
            textureMask.maskChannel = fx.maskChannel;
            textureMask.maskTiling = fx.maskTiling;
            textureMask.maskOffset = fx.maskOffset;
            depthMask.useDepthMask = fx.useDepthMask;
            depthMask.depthThreshold = fx.depthThreshold;
            depthMask.depthFar = fx.depthFar;
            depthMask.depthSoftness = fx.depthSoftness;
            depthMask.depthOpacity = fx.depthOpacity;
            depthMask.depthInvert = fx.depthInvert;
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
            ghost.ghostResolutionScale = fx.ghostResolutionScale;
            ghost.ghostFrameIntervalMs = fx.ghostFrameIntervalMs;
            ghost.ghostDecayMs = fx.ghostDecayMs;
            unsharp.unsharpEnabled = fx.unsharpEnabled;
            unsharp.unsharpAmount = fx.unsharpAmount;
            unsharp.unsharpRadius = fx.unsharpRadius;
            unsharp.unsharpThreshold = fx.unsharpThreshold;
            unsharp.unsharpLumaOnly = fx.unsharpLumaOnly;
            unsharp.unsharpChroma = fx.unsharpChroma;
            unsharp.sharpenMode = fx.sharpenMode;
            edges.edgeEnabled = fx.edgeEnabled;
            edges.edgeStrength = fx.edgeStrength;
            edges.edgeThreshold = fx.edgeThreshold;
            edges.edgeBlend = fx.edgeBlend;
            edges.edgeColor = fx.edgeColor;
            edges.edgeThickness = fx.edgeThickness;
            edges.edgeUseNormals = fx.edgeUseNormals;
            edges.edgeNormalThreshold = fx.edgeNormalThreshold;
            dither.ditherMode = fx.ditherMode;
            dither.ditherStrength = fx.ditherStrength;
            dither.ditherAngle = fx.ditherAngle;
            dither.blueNoise = fx.blueNoise;
            dither.temporalDither = fx.temporalDither;
            dither.temporalDitherRate = fx.temporalDitherRate;
            dither.halftoneScale = fx.halftoneScale;
            dither.halftoneDotGain = fx.halftoneDotGain;
            crt.crtEnabled = fx.crtEnabled;
            crt.crtCurvature = fx.crtCurvature;
            crt.crtOverscan = fx.crtOverscan;
            crt.crtScanlineStrength = fx.crtScanlineStrength;
            crt.crtScanlineCount = fx.crtScanlineCount;
            crt.crtBeamWidth = fx.crtBeamWidth;
            crt.crtMaskMode = fx.crtMaskMode;
            crt.crtMaskStrength = fx.crtMaskStrength;
            crt.crtMaskScale = fx.crtMaskScale;
            crt.crtBloom = fx.crtBloom;
            crt.crtBloomRadius = fx.crtBloomRadius;
            crt.crtVignette = fx.crtVignette;
            crt.crtVignetteSoftness = fx.crtVignetteSoftness;
            crt.crtNoise = fx.crtNoise;
            crt.crtFlicker = fx.crtFlicker;
            crt.crtBrightness = fx.crtBrightness;
            crt.crtTubeEdge = fx.crtTubeEdge;
            crt.crtBloomThreshold = fx.crtBloomThreshold;
            crt.crtConvergencePx = fx.crtConvergencePx;
            crt.crtFocus = fx.crtFocus;
            crt.crtBlackLevel = fx.crtBlackLevel;
            crt.crtHumBar = fx.crtHumBar;
            crt.crtFlickerHz = fx.crtFlickerHz;
            vhs.vhsEnabled = fx.vhsEnabled;
            vhs.vhsIntensity = fx.vhsIntensity;
            vhs.vhsTapeSpeed = fx.vhsTapeSpeed;
            vhs.vhsHorizontalJitter = fx.vhsHorizontalJitter;
            vhs.vhsLineWobble = fx.vhsLineWobble;
            vhs.vhsTracking = fx.vhsTracking;
            vhs.vhsTrackingSpeed = fx.vhsTrackingSpeed;
            vhs.vhsTrackingWidth = fx.vhsTrackingWidth;
            vhs.vhsChromaBleed = fx.vhsChromaBleed;
            vhs.vhsChromaBlur = fx.vhsChromaBlur;
            vhs.vhsColorLoss = fx.vhsColorLoss;
            vhs.vhsLumaNoise = fx.vhsLumaNoise;
            vhs.vhsChromaNoise = fx.vhsChromaNoise;
            vhs.vhsDropout = fx.vhsDropout;
            vhs.vhsHeadSwitching = fx.vhsHeadSwitching;
            vhs.vhsHeadSwitchHeight = fx.vhsHeadSwitchHeight;
            vhs.vhsInterlace = fx.vhsInterlace;
            vhs.vhsStandard = fx.vhsStandard;
            vhs.vhsTapeMode = fx.vhsTapeMode;
            vhs.vhsGeneration = fx.vhsGeneration;
            vhs.vhsAgcInstability = fx.vhsAgcInstability;
            vhs.vhsVerticalChromaBlur = fx.vhsVerticalChromaBlur;
            CaptureProfessional(fx, professional);
        }

        private static void ApplyProfessional(CrowImageEffects fx, CrowFXProfessionalSettings p)
        {
            if (p == null) return;
            fx.lensSensorEnabled=p.lensSensorEnabled; fx.lensSensorIntensity=p.lensSensorIntensity; fx.lensDistortion=p.lensDistortion;
            fx.lensChromaticAberration=p.lensChromaticAberration; fx.lensVignette=p.lensVignette; fx.lensBloom=p.lensBloom; fx.lensBloomRadius=p.lensBloomRadius;
            fx.sensorRollingShutter=p.sensorRollingShutter; fx.sensorNoise=p.sensorNoise; fx.sensorDeadPixels=p.sensorDeadPixels;
            fx.filmEnabled=p.filmEnabled; fx.filmIntensity=p.filmIntensity; fx.filmGrain=p.filmGrain; fx.filmGrainSize=p.filmGrainSize;
            fx.filmHalation=p.filmHalation; fx.filmHalationRadius=p.filmHalationRadius; fx.filmGateWeave=p.filmGateWeave; fx.filmDust=p.filmDust;
            fx.filmScratches=p.filmScratches; fx.filmFlicker=p.filmFlicker;
            fx.motionGlitchEnabled=p.motionGlitchEnabled; fx.motionGlitchIntensity=p.motionGlitchIntensity; fx.motionBlockSize=p.motionBlockSize;
            fx.motionVectorDisplacement=p.motionVectorDisplacement; fx.motionFreezeRate=p.motionFreezeRate; fx.motionColorSplit=p.motionColorSplit; fx.motionHistoryScale=p.motionHistoryScale; fx.motionHistoryFps=p.motionHistoryFps;
            fx.digitalVideoEnabled=p.digitalVideoEnabled; fx.digitalVideoIntensity=p.digitalVideoIntensity; fx.digitalBlockSize=p.digitalBlockSize;
            fx.digitalQuantization=p.digitalQuantization; fx.digitalRinging=p.digitalRinging; fx.digitalChromaSubsampling=p.digitalChromaSubsampling;
            fx.digitalMosquitoNoise=p.digitalMosquitoNoise; fx.digitalBitratePumping=p.digitalBitratePumping;
            fx.compositeEnabled=p.compositeEnabled; fx.compositeIntensity=p.compositeIntensity; fx.compositeStandard=p.compositeStandard;
            fx.compositeDotCrawl=p.compositeDotCrawl; fx.compositeRainbow=p.compositeRainbow; fx.compositeChromaBandwidth=p.compositeChromaBandwidth;
            fx.compositePhaseError=p.compositePhaseError; fx.compositeCombFilter=p.compositeCombFilter;
            fx.lcdEnabled=p.lcdEnabled; fx.lcdIntensity=p.lcdIntensity; fx.lcdPixelScale=p.lcdPixelScale; fx.lcdSubpixelStrength=p.lcdSubpixelStrength;
            fx.lcdInversion=p.lcdInversion; fx.lcdViewingAngle=p.lcdViewingAngle; fx.lcdBacklightBleed=p.lcdBacklightBleed; fx.lcdResponseSmear=p.lcdResponseSmear;
        }

        private static void CaptureProfessional(CrowImageEffects fx, CrowFXProfessionalSettings p)
        {
            if (p == null) return;
            p.lensSensorEnabled=fx.lensSensorEnabled; p.lensSensorIntensity=fx.lensSensorIntensity; p.lensDistortion=fx.lensDistortion;
            p.lensChromaticAberration=fx.lensChromaticAberration; p.lensVignette=fx.lensVignette; p.lensBloom=fx.lensBloom; p.lensBloomRadius=fx.lensBloomRadius;
            p.sensorRollingShutter=fx.sensorRollingShutter; p.sensorNoise=fx.sensorNoise; p.sensorDeadPixels=fx.sensorDeadPixels;
            p.filmEnabled=fx.filmEnabled; p.filmIntensity=fx.filmIntensity; p.filmGrain=fx.filmGrain; p.filmGrainSize=fx.filmGrainSize;
            p.filmHalation=fx.filmHalation; p.filmHalationRadius=fx.filmHalationRadius; p.filmGateWeave=fx.filmGateWeave; p.filmDust=fx.filmDust;
            p.filmScratches=fx.filmScratches; p.filmFlicker=fx.filmFlicker;
            p.motionGlitchEnabled=fx.motionGlitchEnabled; p.motionGlitchIntensity=fx.motionGlitchIntensity; p.motionBlockSize=fx.motionBlockSize;
            p.motionVectorDisplacement=fx.motionVectorDisplacement; p.motionFreezeRate=fx.motionFreezeRate; p.motionColorSplit=fx.motionColorSplit; p.motionHistoryScale=fx.motionHistoryScale; p.motionHistoryFps=fx.motionHistoryFps;
            p.digitalVideoEnabled=fx.digitalVideoEnabled; p.digitalVideoIntensity=fx.digitalVideoIntensity; p.digitalBlockSize=fx.digitalBlockSize;
            p.digitalQuantization=fx.digitalQuantization; p.digitalRinging=fx.digitalRinging; p.digitalChromaSubsampling=fx.digitalChromaSubsampling;
            p.digitalMosquitoNoise=fx.digitalMosquitoNoise; p.digitalBitratePumping=fx.digitalBitratePumping;
            p.compositeEnabled=fx.compositeEnabled; p.compositeIntensity=fx.compositeIntensity; p.compositeStandard=fx.compositeStandard;
            p.compositeDotCrawl=fx.compositeDotCrawl; p.compositeRainbow=fx.compositeRainbow; p.compositeChromaBandwidth=fx.compositeChromaBandwidth;
            p.compositePhaseError=fx.compositePhaseError; p.compositeCombFilter=fx.compositeCombFilter;
            p.lcdEnabled=fx.lcdEnabled; p.lcdIntensity=fx.lcdIntensity; p.lcdPixelScale=fx.lcdPixelScale; p.lcdSubpixelStrength=fx.lcdSubpixelStrength;
            p.lcdInversion=fx.lcdInversion; p.lcdViewingAngle=fx.lcdViewingAngle; p.lcdBacklightBleed=fx.lcdBacklightBleed; p.lcdResponseSmear=fx.lcdResponseSmear;
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
