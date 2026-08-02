using System;
using UnityEngine;
using UnityEngine.Profiling;
using CrowFX.Helpers;
using SectionKeys = CrowFX.Helpers.CrowFXSectionKeys;

namespace CrowFX
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Image Effects/CrowFX/Crow Image Effects")]

    [EffectSectionMeta(SectionKeys.Master,      title: "Master",            icon: "d_Settings",             hint: "Global Blend",         order: 0,  defaultExpanded: true)]
    [EffectSectionMeta(SectionKeys.Presets,     title: "Look Library",      icon: "d_FolderOpened Icon",    hint: "Browse / Preview / Apply", order: -10, defaultExpanded: true)]
    [EffectSectionMeta(SectionKeys.Sampling,    title: "Sampling & Grid",   icon: "d_GridLayoutGroup Icon", hint: "Pixel Size / Grid",     order: 10, defaultExpanded: true)]
    [EffectSectionMeta(SectionKeys.Pregrade,    title: "Pre-Grade",         icon: "d_PreMatCube",           hint: "Exposure / Contrast",  order: 20, defaultExpanded: true)]
    [EffectSectionMeta(SectionKeys.Posterize,   title: "Posterize",         icon: "d_PreTextureRGB",        hint: "Levels / Animation",   order: 30, defaultExpanded: true)]
    [EffectSectionMeta(SectionKeys.Palette,     title: "Palette",           icon: "d_color_picker",         hint: "LUT / Curve",          order: 40, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.TextureMask, title: "Texture Mask",      icon: "d_RectTool",             hint: "Mask Texture",         order: 50, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.DepthMask,   title: "Depth Mask",        icon: "d_SceneViewOrtho",       hint: "Depth Threshold",      order: 60, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.Jitter,      title: "Channel Jitter",    icon: "d_Image Icon",           hint: "RGB Offset",           order: 70, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.Bleed,       title: "RGB Bleed",         icon: "d_PreTexRGB",            hint: "Chromatic Aberration", order: 80, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.Ghost,       title: "Ghosting",          icon: "d_CameraPreview",        hint: "Motion Trail",         order: 90, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.Edges,       title: "Edge Outline",      icon: "d_SceneViewFx",          hint: "Depth-based",          order: 100, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.Unsharp,     title: "Unsharp Mask",      icon: "d_Search Icon",          hint: "Sharpen",              order: 110, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.Dither,      title: "Dithering",         icon: "d_PreTextureMipMapHigh", hint: "Noise Pattern",         order: 120, defaultExpanded: true)]
    [EffectSectionMeta(SectionKeys.Vhs,         title: "VHS Tape",          icon: "d_PreTextureRGB",        hint: "Tracking / Chroma / Tape", order: 130, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.Crt,         title: "CRT Display",       icon: "d_CameraPreview",        hint: "Tube / Beam / Phosphors", order: 140, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.LensSensor,  title: "Lens & Sensor",     icon: "d_Camera Icon",          hint: "Optics / Sensor",       order: 25, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.Film,        title: "Film Emulation",    icon: "d_PreTextureMipMapHigh", hint: "Grain / Halation",      order: 28, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.MotionGlitch,title: "Motion & Datamosh", icon: "d_AnimationClip Icon",   hint: "Vectors / Frame Hold",  order: 95, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.DigitalVideo,title: "Digital Video",     icon: "d_GridLayoutGroup Icon", hint: "Blocks / Compression",  order: 125, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.Composite,   title: "Composite Signal",  icon: "d_PreTextureRGB",        hint: "NTSC / PAL Decode",     order: 135, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.Lcd,         title: "LCD Display",       icon: "d_RawImage Icon",        hint: "Pixels / Response",     order: 145, defaultExpanded: false)]
    [EffectSectionMeta(SectionKeys.Shaders,     title: "Shaders",           icon: "d_Shader Icon",          hint: "Advanced",             order: 1000, defaultExpanded: false)]
    public sealed class CrowImageEffects : MonoBehaviour
    {
        public enum DitherMode { None = 0, Ordered2x2 = 1, Ordered4x4 = 2, Ordered8x8 = 3, Noise = 4, BlueNoise = 5, Halftone = 6, Linear = 7, Diamond = 8 }
        public enum PaletteMode { Ramp = 0, Nearest = 1 }
        public enum GhostCombineMode { Mix = 0, Add = 1, Screen = 2, Max = 3 }
        public enum BleedMode { Manual = 0, Radial = 1 }
        public enum BleedBlendMode { Mix = 0, Add = 1, Screen = 2, Max = 3 }
        public enum CrtMaskMode { ApertureGrille = 0, SlotMask = 1, ShadowMask = 2 }
        public enum MaskChannel { Luminance = 0, Red = 1, Green = 2, Blue = 3, Alpha = 4 }
        public enum VhsStandard { NTSC = 0, PAL = 1 }
        public enum VhsTapeMode { SP = 0, LP = 1, EP = 2 }
        public enum SamplingFilter { Point = 0, Bilinear = 1 }
        public enum QualityTier { Low = 0, Balanced = 1, Reference = 2 }
        public enum MaskPlacement { EntireStack = 0, BeforeSignalAndDisplays = 1 }
        public enum SharpenMode { UnsharpMask = 0, ContrastAdaptive = 1 }
        private enum ProfessionalMode { LensSensor = 0, Film = 1, MotionGlitch = 2, DigitalVideo = 3, Composite = 4, Lcd = 5 }

        private static class ShaderProps
        {
            public static readonly int PixelSize = Shader.PropertyToID("_PixelSize");
            public static readonly int UseVirtualGrid = Shader.PropertyToID("_UseVirtualGrid");
            public static readonly int VirtualRes = Shader.PropertyToID("_VirtualRes");
            public static readonly int SamplingPhase = Shader.PropertyToID("_SamplingPhase");
            public static readonly int PixelAspect = Shader.PropertyToID("_PixelAspect");
            public static readonly int SamplingFilter = Shader.PropertyToID("_SamplingFilter");

            public static readonly int PregradeEnabled = Shader.PropertyToID("_PregradeEnabled");
            public static readonly int Exposure = Shader.PropertyToID("_Exposure");
            public static readonly int Contrast = Shader.PropertyToID("_Contrast");
            public static readonly int Gamma = Shader.PropertyToID("_Gamma");
            public static readonly int Saturation = Shader.PropertyToID("_Saturation");
            public static readonly int PregradeTint = Shader.PropertyToID("_PregradeTint");
            public static readonly int PregradeTintStrength = Shader.PropertyToID("_PregradeTintStrength");
            public static readonly int PregradeLift = Shader.PropertyToID("_Lift");
            public static readonly int PregradeGain = Shader.PropertyToID("_Gain");
            public static readonly int PregradeOffset = Shader.PropertyToID("_Offset");
            public static readonly int PregradeTemperature = Shader.PropertyToID("_Temperature");
            public static readonly int PregradeRolloff = Shader.PropertyToID("_HighlightRolloff");

            public static readonly int JitterEnabled = Shader.PropertyToID("_JitterEnabled");
            public static readonly int JitterStrength = Shader.PropertyToID("_JitterStrength");
            public static readonly int JitterMode = Shader.PropertyToID("_JitterMode");
            public static readonly int JitterAmountPx = Shader.PropertyToID("_JitterAmountPx");
            public static readonly int JitterSpeed = Shader.PropertyToID("_JitterSpeed");
            public static readonly int UseSeed = Shader.PropertyToID("_UseSeed");
            public static readonly int Seed = Shader.PropertyToID("_Seed");
            public static readonly int Scanline = Shader.PropertyToID("_Scanline");
            public static readonly int ScanlineDensity = Shader.PropertyToID("_ScanlineDensity");
            public static readonly int ScanlineAmp = Shader.PropertyToID("_ScanlineAmp");
            public static readonly int ChannelWeights = Shader.PropertyToID("_ChannelWeights");
            public static readonly int DirR = Shader.PropertyToID("_DirR");
            public static readonly int DirG = Shader.PropertyToID("_DirG");
            public static readonly int DirB = Shader.PropertyToID("_DirB");
            public static readonly int HashCellCount = Shader.PropertyToID("_HashCellCount");
            public static readonly int HashTimeSmooth = Shader.PropertyToID("_HashTimeSmooth");
            public static readonly int HashRotateDeg = Shader.PropertyToID("_HashRotateDeg");
            public static readonly int HashAniso = Shader.PropertyToID("_HashAniso");
            public static readonly int HashWarpAmpPx = Shader.PropertyToID("_HashWarpAmpPx");
            public static readonly int HashWarpCells = Shader.PropertyToID("_HashWarpCells");
            public static readonly int HashWarpSpeed = Shader.PropertyToID("_HashWarpSpeed");
            public static readonly int HashPerChannel = Shader.PropertyToID("_HashPerChannel");
            public static readonly int ClampUV = Shader.PropertyToID("_ClampUV");
            public static readonly int NoiseTex = Shader.PropertyToID("_NoiseTex");

            public static readonly int GhostEnabled = Shader.PropertyToID("_GhostEnabled");
            public static readonly int GhostBlend = Shader.PropertyToID("_GhostBlend");
            public static readonly int GhostOffsetPx = Shader.PropertyToID("_GhostOffsetPx");
            public static readonly int CombineMode = Shader.PropertyToID("_CombineMode");
            public static readonly int PrevTex = Shader.PropertyToID("_PrevTex");

            public static readonly int BleedBlend = Shader.PropertyToID("_BleedBlend");
            public static readonly int BleedIntensity = Shader.PropertyToID("_BleedIntensity");
            public static readonly int BleedMode = Shader.PropertyToID("_BleedMode");
            public static readonly int BlendMode = Shader.PropertyToID("_BlendMode");
            public static readonly int ShiftR = Shader.PropertyToID("_ShiftR");
            public static readonly int ShiftG = Shader.PropertyToID("_ShiftG");
            public static readonly int ShiftB = Shader.PropertyToID("_ShiftB");
            public static readonly int EdgeOnly = Shader.PropertyToID("_EdgeOnly");
            public static readonly int EdgeThreshold = Shader.PropertyToID("_EdgeThreshold");
            public static readonly int EdgePower = Shader.PropertyToID("_EdgePower");
            public static readonly int RadialCenter = Shader.PropertyToID("_RadialCenter");
            public static readonly int RadialStrength = Shader.PropertyToID("_RadialStrength");
            public static readonly int Samples = Shader.PropertyToID("_Samples");
            public static readonly int Smear = Shader.PropertyToID("_Smear");
            public static readonly int Falloff = Shader.PropertyToID("_Falloff");
            public static readonly int IntensityR = Shader.PropertyToID("_IntensityR");
            public static readonly int IntensityG = Shader.PropertyToID("_IntensityG");
            public static readonly int IntensityB = Shader.PropertyToID("_IntensityB");
            public static readonly int Anamorphic = Shader.PropertyToID("_Anamorphic");
            public static readonly int PreserveLuma = Shader.PropertyToID("_PreserveLuma");
            public static readonly int WobbleAmp = Shader.PropertyToID("_WobbleAmp");
            public static readonly int WobbleFreq = Shader.PropertyToID("_WobbleFreq");
            public static readonly int WobbleScanline = Shader.PropertyToID("_WobbleScanline");

            public static readonly int UnsharpEnabled = Shader.PropertyToID("_UnsharpEnabled");
            public static readonly int UnsharpAmount = Shader.PropertyToID("_UnsharpAmount");
            public static readonly int UnsharpRadius = Shader.PropertyToID("_UnsharpRadius");
            public static readonly int UnsharpThreshold = Shader.PropertyToID("_UnsharpThreshold");
            public static readonly int UnsharpLumaOnly = Shader.PropertyToID("_UnsharpLumaOnly");
            public static readonly int UnsharpChroma = Shader.PropertyToID("_UnsharpChroma");
            public static readonly int SharpenMode = Shader.PropertyToID("_SharpenMode");

            public static readonly int LuminanceOnly = Shader.PropertyToID("_LuminanceOnly");

            public static readonly int Levels = Shader.PropertyToID("_Levels");
            public static readonly int UsePerChannel = Shader.PropertyToID("_UsePerChannel");
            public static readonly int LevelsR = Shader.PropertyToID("_LevelsR");
            public static readonly int LevelsG = Shader.PropertyToID("_LevelsG");
            public static readonly int LevelsB = Shader.PropertyToID("_LevelsB");
            public static readonly int AnimateLevels = Shader.PropertyToID("_AnimateLevels");
            public static readonly int MinLevels = Shader.PropertyToID("_MinLevels");
            public static readonly int MaxLevels = Shader.PropertyToID("_MaxLevels");
            public static readonly int Speed = Shader.PropertyToID("_Speed");
            public static readonly int DitherMode = Shader.PropertyToID("_DitherMode");
            public static readonly int DitherStrength = Shader.PropertyToID("_DitherStrength");
            public static readonly int DitherAngle = Shader.PropertyToID("_DitherAngle");
            public static readonly int BlueNoise = Shader.PropertyToID("_BlueNoise");
            public static readonly int TemporalDither = Shader.PropertyToID("_TemporalDither");
            public static readonly int TemporalDitherRate = Shader.PropertyToID("_TemporalDitherRate");
            public static readonly int HalftoneScale = Shader.PropertyToID("_HalftoneScale");
            public static readonly int HalftoneDotGain = Shader.PropertyToID("_HalftoneDotGain");

            public static readonly int ThresholdTex = Shader.PropertyToID("_ThresholdTex");
            public static readonly int UseThreshold = Shader.PropertyToID("_UseThreshold");
            public static readonly int UsePalette = Shader.PropertyToID("_UsePalette");
            public static readonly int PaletteMode = Shader.PropertyToID("_PaletteMode");
            public static readonly int PaletteTex = Shader.PropertyToID("_PaletteTex");
            public static readonly int PaletteColorCount = Shader.PropertyToID("_PaletteColorCount");
            public static readonly int PalettePerceptual = Shader.PropertyToID("_PalettePerceptual");
            public static readonly int Invert = Shader.PropertyToID("_Invert");

            public static readonly int EdgeEnabled = Shader.PropertyToID("_EdgeEnabled");
            public static readonly int EdgeStrength = Shader.PropertyToID("_EdgeStrength");
            public static readonly int EdgeBlend = Shader.PropertyToID("_EdgeBlend");
            public static readonly int EdgeColor = Shader.PropertyToID("_EdgeColor");
            public static readonly int EdgeThickness = Shader.PropertyToID("_EdgeThickness");
            public static readonly int EdgeUseNormals = Shader.PropertyToID("_EdgeUseNormals");
            public static readonly int EdgeNormalThreshold = Shader.PropertyToID("_EdgeNormalThreshold");

            public static readonly int UseMask = Shader.PropertyToID("_UseMask");
            public static readonly int MaskTex = Shader.PropertyToID("_MaskTex");
            public static readonly int MaskThreshold = Shader.PropertyToID("_MaskThreshold");
            public static readonly int MaskSoftness = Shader.PropertyToID("_MaskSoftness");
            public static readonly int MaskOpacity = Shader.PropertyToID("_MaskOpacity");
            public static readonly int MaskInvert = Shader.PropertyToID("_MaskInvert");
            public static readonly int MaskChannel = Shader.PropertyToID("_MaskChannel");
            public static readonly int MaskTransform = Shader.PropertyToID("_MaskTransform");
            public static readonly int MaskedTex = Shader.PropertyToID("_MaskedTex");

            public static readonly int UseDepthMask = Shader.PropertyToID("_UseDepthMask");
            public static readonly int DepthThreshold = Shader.PropertyToID("_DepthThreshold");
            public static readonly int DepthFar = Shader.PropertyToID("_DepthFar");
            public static readonly int DepthSoftness = Shader.PropertyToID("_DepthSoftness");
            public static readonly int DepthOpacity = Shader.PropertyToID("_DepthOpacity");
            public static readonly int DepthInvert = Shader.PropertyToID("_DepthInvert");

            public static readonly int OriginalTex = Shader.PropertyToID("_OriginalTex");
            public static readonly int MasterBlend = Shader.PropertyToID("_MasterBlend");

            public static readonly int CrtCurvature = Shader.PropertyToID("_Curvature");
            public static readonly int CrtOverscan = Shader.PropertyToID("_Overscan");
            public static readonly int CrtScanlineStrength = Shader.PropertyToID("_ScanlineStrength");
            public static readonly int CrtScanlineCount = Shader.PropertyToID("_ScanlineCount");
            public static readonly int CrtBeamWidth = Shader.PropertyToID("_BeamWidth");
            public static readonly int CrtMaskMode = Shader.PropertyToID("_MaskMode");
            public static readonly int CrtMaskStrength = Shader.PropertyToID("_MaskStrength");
            public static readonly int CrtMaskScale = Shader.PropertyToID("_MaskScale");
            public static readonly int CrtBloom = Shader.PropertyToID("_Bloom");
            public static readonly int CrtBloomRadius = Shader.PropertyToID("_BloomRadius");
            public static readonly int CrtVignette = Shader.PropertyToID("_Vignette");
            public static readonly int CrtVignetteSoftness = Shader.PropertyToID("_VignetteSoftness");
            public static readonly int CrtNoise = Shader.PropertyToID("_Noise");
            public static readonly int CrtFlicker = Shader.PropertyToID("_Flicker");
            public static readonly int CrtBrightness = Shader.PropertyToID("_Brightness");
            public static readonly int CrtTubeEdge = Shader.PropertyToID("_TubeEdge");
            public static readonly int CrtBloomThreshold = Shader.PropertyToID("_BloomThreshold");
            public static readonly int CrtConvergence = Shader.PropertyToID("_ConvergencePx");
            public static readonly int CrtFocus = Shader.PropertyToID("_Focus");
            public static readonly int CrtBlackLevel = Shader.PropertyToID("_BlackLevel");
            public static readonly int CrtHumBar = Shader.PropertyToID("_HumBar");
            public static readonly int CrtFlickerHz = Shader.PropertyToID("_FlickerHz");
            public static readonly int VhsIntensity = Shader.PropertyToID("_Intensity");
            public static readonly int VhsTapeSpeed = Shader.PropertyToID("_TapeSpeed");
            public static readonly int VhsJitter = Shader.PropertyToID("_HorizontalJitter");
            public static readonly int VhsLineWobble = Shader.PropertyToID("_LineWobble");
            public static readonly int VhsTracking = Shader.PropertyToID("_Tracking");
            public static readonly int VhsTrackingSpeed = Shader.PropertyToID("_TrackingSpeed");
            public static readonly int VhsTrackingWidth = Shader.PropertyToID("_TrackingWidth");
            public static readonly int VhsChromaBleed = Shader.PropertyToID("_ChromaBleed");
            public static readonly int VhsChromaBlur = Shader.PropertyToID("_ChromaBlur");
            public static readonly int VhsColorLoss = Shader.PropertyToID("_ColorLoss");
            public static readonly int VhsLumaNoise = Shader.PropertyToID("_LumaNoise");
            public static readonly int VhsChromaNoise = Shader.PropertyToID("_ChromaNoise");
            public static readonly int VhsDropout = Shader.PropertyToID("_Dropout");
            public static readonly int VhsHeadSwitching = Shader.PropertyToID("_HeadSwitching");
            public static readonly int VhsHeadSwitchHeight = Shader.PropertyToID("_HeadSwitchHeight");
            public static readonly int VhsInterlace = Shader.PropertyToID("_Interlace");
            public static readonly int VhsStandard = Shader.PropertyToID("_Standard");
            public static readonly int VhsTapeMode = Shader.PropertyToID("_TapeMode");
            public static readonly int VhsGeneration = Shader.PropertyToID("_Generation");
            public static readonly int VhsAgc = Shader.PropertyToID("_AgcInstability");
            public static readonly int VhsVerticalChroma = Shader.PropertyToID("_VerticalChromaBlur");

            public static readonly int ProfessionalMode = Shader.PropertyToID("_ProfessionalMode");
            public static readonly int EffectIntensity = Shader.PropertyToID("_EffectIntensity");
            public static readonly int ParamA = Shader.PropertyToID("_ParamA");
            public static readonly int ParamB = Shader.PropertyToID("_ParamB");
            public static readonly int HistoryTex = Shader.PropertyToID("_HistoryTex");

            public static readonly int Count = Shader.PropertyToID("_Count");
            public static readonly int WeightCurve = Shader.PropertyToID("_WeightCurve");
            public static readonly int DecayPerTap = Shader.PropertyToID("_DecayPerTap");
            public static readonly int[] Hist = BuildHistoryIds();

            private static int[] BuildHistoryIds()
            {
                var ids = new int[16];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = Shader.PropertyToID("_Hist" + i);

                return ids;
            }
        }

        // -------------------- Master --------------------
        [EffectSection(SectionKeys.Master, 0)]
        [Tooltip("Global opacity for the entire CrowFX stack.")]
        [Range(0, 1)] public float masterBlend = 1f;

        [EffectSection(SectionKeys.Master, 10)]
        [Tooltip("Performance/quality ceiling for expensive temporal, smear, and palette searches. Reference honors all authored values.")]
        public QualityTier qualityTier = QualityTier.Balanced;

        [EffectSection(SectionKeys.Master, 20)]
        [Tooltip("Entire Stack masks the final result. Before Signal & Displays masks creative processing while tape, composite, CRT and LCD continue across the whole frame.")]
        public MaskPlacement maskPlacement = MaskPlacement.EntireStack;

        [Tooltip("Shared profile asset used to sync settings across cameras.")]
        public CrowFXProfile profile;
        [Tooltip("If enabled, this component live-syncs with its assigned profile.")]
        public bool autoApplyProfile = true;

        // -------------------- Sampling / Grid --------------------
        [EffectSection(SectionKeys.Sampling, 0)]
        [Tooltip("Size of each pixel block in screen pixels.")]
        [Range(1, 1024)] public int pixelSize = 1;

        [EffectSection(SectionKeys.Sampling, 10)]
        [Tooltip("Locks sampling & dithering to a fixed virtual pixel grid, independent of GameView/backbuffer size.\nDoes NOT replace Pixelation; it's an additional stabilizer.")]
        public bool useVirtualGrid = false;

        [EffectSection(SectionKeys.Sampling, 20)]
        [Tooltip("Typical vibes: 640x448, 640x480, 512x448, etc.")]
        public Vector2Int virtualResolution = new Vector2Int(720, 480);
        [EffectSection(SectionKeys.Sampling, 30)][Tooltip("Sub-pixel phase of the sampling lattice, measured in grid cells.")] public Vector2 samplingPhase = Vector2.zero;
        [EffectSection(SectionKeys.Sampling, 40)][Tooltip("Pixel width-to-height ratio. Values above one create wider pixels.")][Range(0.25f, 4f)] public float pixelAspect = 1f;
        [EffectSection(SectionKeys.Sampling, 50)][Tooltip("Point preserves hard texels; Bilinear preserves smooth source reconstruction.")] public SamplingFilter samplingFilter = SamplingFilter.Point;

        // -------------------- Pregrade --------------------
        [EffectSection(SectionKeys.Pregrade, 0)][Tooltip("Enable exposure, contrast, gamma, and saturation adjustments before posterization.")] public bool pregradeEnabled = false;
        [EffectSection(SectionKeys.Pregrade, 10)][Tooltip("Brightness adjustment applied before posterization.")][Range(-5f, 5f)] public float exposure = 0f;
        [EffectSection(SectionKeys.Pregrade, 20)][Tooltip("Endpoint-preserving luminance contrast curve. Values above 1 add contrast without hard black or white clipping.")][Range(0f, 2f)] public float contrast = 1f;
        [EffectSection(SectionKeys.Pregrade, 30)][Tooltip("Gamma correction applied before posterization.")][Range(0.1f, 3f)] public float gamma = 1f;
        [EffectSection(SectionKeys.Pregrade, 40)][Tooltip("Color saturation applied before posterization.")][Range(0f, 2f)] public float saturation = 1f;
        [EffectSection(SectionKeys.Pregrade, 50)][Tooltip("Color filter applied after saturation while approximately preserving luminance.")] public Color pregradeTint = Color.white;
        [EffectSection(SectionKeys.Pregrade, 60)][Tooltip("Strength of the pre-grade color filter.")][Range(0f, 1f)] public float pregradeTintStrength = 0f;
        [EffectSection(SectionKeys.Pregrade, 70)][Tooltip("Adds color primarily to shadows before contrast.")] public Color pregradeLift = new Color(0f, 0f, 0f, 0f);
        [EffectSection(SectionKeys.Pregrade, 80)][Tooltip("Per-channel gain applied before contrast.")] public Color pregradeGain = Color.white;
        [EffectSection(SectionKeys.Pregrade, 90)][Tooltip("Linear per-channel offset applied before contrast.")] public Color pregradeOffset = new Color(0f, 0f, 0f, 0f);
        [EffectSection(SectionKeys.Pregrade, 100)][Tooltip("Warm/cool white-balance adjustment.")][Range(-1f, 1f)] public float pregradeTemperature = 0f;
        [EffectSection(SectionKeys.Pregrade, 110)][Tooltip("Compress highlights above display white without hard clipping.")][Range(0f, 2f)] public float pregradeHighlightRolloff = 0.5f;

        // -------------------- Posterize --------------------
        [EffectSection(SectionKeys.Posterize, 0)][Tooltip("Shared number of quantization levels for all channels.")][Range(2, 512)] public int levels = 512;
        [EffectSection(SectionKeys.Posterize, 10)][Tooltip("Use independent quantization levels for red, green, and blue.")] public bool usePerChannel = false;
        [EffectSection(SectionKeys.Posterize, 20)][Tooltip("Quantization levels for the red channel.")][Range(2, 512)] public int levelsR = 512;
        [EffectSection(SectionKeys.Posterize, 30)][Tooltip("Quantization levels for the green channel.")][Range(2, 512)] public int levelsG = 512;
        [EffectSection(SectionKeys.Posterize, 40)][Tooltip("Quantization levels for the blue channel.")][Range(2, 512)] public int levelsB = 512;

        [EffectSection(SectionKeys.Posterize, 50)][Tooltip("Animate the shared quantization level count over time.")] public bool animateLevels = false;
        [EffectSection(SectionKeys.Posterize, 60)][Tooltip("Lower bound used when Animated Levels is enabled.")][Range(2, 512)] public int minLevels = 512;
        [EffectSection(SectionKeys.Posterize, 70)][Tooltip("Upper bound used when Animated Levels is enabled.")][Range(2, 512)] public int maxLevels = 512;
        [EffectSection(SectionKeys.Posterize, 80)][Tooltip("Animation speed for cycling quantization levels.")] public float speed = 1f;

        [EffectSection(SectionKeys.Posterize, 90)][Tooltip("Posterize luminance while preserving overall color relationships.")] public bool luminanceOnly = false;
        [EffectSection(SectionKeys.Posterize, 100)][Tooltip("Invert the posterized output colors.")] public bool invert = false;

        // -------------------- Palette / Curve --------------------
        [EffectSection(SectionKeys.Palette, 0)][Tooltip("Map final colors through a palette texture.")] public bool usePalette = false;
        [EffectSection(SectionKeys.Palette, 10)][Tooltip("Ramp follows tonal value along the palette strip. Nearest matches each pixel to the closest palette swatch.")] public PaletteMode paletteMode = PaletteMode.Nearest;
        [EffectSection(SectionKeys.Palette, 20)][Tooltip("Palette lookup texture used when palette mapping is enabled.")] public Texture2D paletteTex;
        [EffectSection(SectionKeys.Palette, 30)][Tooltip("Remap tonal values before palette lookup or nearest-color matching.")] public AnimationCurve thresholdCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [EffectSection(SectionKeys.Palette, 40)][Tooltip("Number of palette texels considered by nearest-color matching.")][Range(2, 64)] public int paletteColorCount = 16;
        [EffectSection(SectionKeys.Palette, 50)][Tooltip("Use perceptual Oklab distance instead of encoded RGB distance.")] public bool palettePerceptual = true;

        // -------------------- Masks --------------------
        [EffectSection(SectionKeys.TextureMask, 0)][Tooltip("Enable a texture mask to blend between processed and original image.")] public bool useMask = false;
        [EffectSection(SectionKeys.TextureMask, 10)][Tooltip("Grayscale mask texture. White keeps the effect; black restores the source.")] public Texture2D maskTex;
        [EffectSection(SectionKeys.TextureMask, 20)][Tooltip("Threshold used to cut between masked and unmasked areas.")][Range(0, 1)] public float maskThreshold = 0.5f;
        [EffectSection(SectionKeys.TextureMask, 30)][Tooltip("Feather width around the mask threshold.")][Range(0, 1)] public float maskSoftness = 0.1f;
        [EffectSection(SectionKeys.TextureMask, 40)][Tooltip("Overall opacity of the texture mask.")][Range(0, 1)] public float maskOpacity = 1f;
        [EffectSection(SectionKeys.TextureMask, 50)][Tooltip("Invert the selected mask channel.")] public bool maskInvert = false;
        [EffectSection(SectionKeys.TextureMask, 60)][Tooltip("Texture channel used to drive the mask.")] public MaskChannel maskChannel = MaskChannel.Luminance;
        [EffectSection(SectionKeys.TextureMask, 70)][Tooltip("Mask texture tiling in screen UV space.")] public Vector2 maskTiling = Vector2.one;
        [EffectSection(SectionKeys.TextureMask, 80)][Tooltip("Mask texture offset in screen UV space.")] public Vector2 maskOffset = Vector2.zero;

        [EffectSection(SectionKeys.DepthMask, 0)][Tooltip("Attenuate the effect based on scene depth.")] public bool useDepthMask = false;
        [EffectSection(SectionKeys.DepthMask, 10)][Tooltip("Depth distance where the mask starts attenuating the effect.")][Range(0.0f, 10.0f)] public float depthThreshold = 1.0f;
        [EffectSection(SectionKeys.DepthMask, 20)][Tooltip("Depth distance where the effect range ends. Set equal to Near for a one-sided mask.")][Range(0.0f, 1000.0f)] public float depthFar = 1000f;
        [EffectSection(SectionKeys.DepthMask, 30)][Tooltip("Feather distance at both depth boundaries.")][Range(0.0f, 50.0f)] public float depthSoftness = 0.25f;
        [EffectSection(SectionKeys.DepthMask, 40)][Tooltip("Overall opacity of the depth mask.")][Range(0, 1)] public float depthOpacity = 1f;
        [EffectSection(SectionKeys.DepthMask, 50)][Tooltip("Invert the depth range selection.")] public bool depthInvert = false;

        // -------------------- Channel Jitter --------------------
        public enum JitterMode
        {
            Static = 0,          // fixed offsets (no time)
            TimeSine = 1,        // smooth wobble over time
            HashNoise = 2,       // pseudo-random noise (no texture needed)
            BlueNoiseTex = 3     // uses a noise texture (blue noise recommended)
        }

        [EffectSection(SectionKeys.Jitter, 0)]
        [Tooltip("Enable per-channel sampling jitter.")]
        public bool jitterEnabled = false;

        [EffectSection(SectionKeys.Jitter, 10)]
        [Tooltip("Blend amount between the original image and jittered sampling.")]
        [Range(0f, 1f)] public float jitterStrength = 0f;

        [EffectSection(SectionKeys.Jitter, 20)]
        [Tooltip("Pattern used to generate jitter offsets.")]
        public JitterMode jitterMode = JitterMode.TimeSine;

        [EffectSection(SectionKeys.Jitter, 30)]
        [Tooltip("Scales offset in pixels (multiplied by texel size or virtual grid pixel size).")]
        [Range(0f, 8f)] public float jitterAmountPx = 1f;

        [EffectSection(SectionKeys.Jitter, 40)]
        [Tooltip("Speed for TimeSine/Noise modes.")]
        [Range(0f, 30f)] public float jitterSpeed = 8f;

        [EffectSection(SectionKeys.Jitter, 50)]
        [Tooltip("If enabled, randomizes using a stable seed (helps avoid identical look across cameras).")]
        public bool jitterUseSeed = false;

        [EffectSection(SectionKeys.Jitter, 60)]
        [Tooltip("Stable seed used when Use Stable Seed is enabled.")]
        [Range(0, 9999)] public int jitterSeed = 1337;

        [EffectSection(SectionKeys.Jitter, 70)]
        [Tooltip("Optional: vary jitter per scanline (VHS-like).")]
        public bool jitterScanline = false;

        [EffectSection(SectionKeys.Jitter, 80)]
        [Tooltip("Scanline density (lines per screen height). Typical: 240-720.")]
        [Range(32f, 2048f)] public float jitterScanlineDensity = 480f;

        [EffectSection(SectionKeys.Jitter, 90)]
        [Tooltip("How much scanline modulation affects the offset.")]
        [Range(0f, 2f)] public float jitterScanlineAmp = 0.35f;

        [EffectSection(SectionKeys.Jitter, 100)]
        [Tooltip("Per-channel intensity multipliers (R,G,B).")]
        public Vector3 jitterChannelWeights = new Vector3(1f, 1f, 1f);

        [EffectSection(SectionKeys.Jitter, 110)]
        [Tooltip("Per-channel direction in pixel space (R.xy, G.xy, B.xy).")]
        public Vector2 jitterDirR = new Vector2(1f, 0f);

        [EffectSection(SectionKeys.Jitter, 120)]
        [Tooltip("Per-channel direction in pixel space for the green channel.")]
        public Vector2 jitterDirG = new Vector2(0f, 1f);

        [EffectSection(SectionKeys.Jitter, 130)]
        [Tooltip("Per-channel direction in pixel space for the blue channel.")]
        public Vector2 jitterDirB = new Vector2(-1f, -1f);

        [EffectSection(SectionKeys.Jitter, 140)]
        [Tooltip("Optional noise texture for BlueNoiseTex mode (128x128+ recommended).")]
        public Texture2D jitterNoiseTex = null;

        [EffectSection(SectionKeys.Jitter, 150)]
        [Tooltip("Clamp UVs after offset (prevents sampling outside screen).")]
        public bool jitterClampUV = true;

        // -------------------- HashNoise controls (Jitter) --------------------
        [EffectSection(SectionKeys.Jitter, 160)]
        [Tooltip("HashNoise only: number of noise cells per axis (spatial frequency).")]
        [Range(4, 1024)] public int jitterHashCellCount = 256;

        [EffectSection(SectionKeys.Jitter, 170)]
        [Tooltip("HashNoise only: 0 = stepped time (poppy), 1 = smooth interpolation between steps.")]
        [Range(0f, 1f)] public float jitterHashTimeSmooth = 0f;

        [EffectSection(SectionKeys.Jitter, 180)]
        [Tooltip("HashNoise only: rotate the hash grid (reduces obvious axis-aligned grid look).")]
        [Range(-180f, 180f)] public float jitterHashRotateDeg = 0f;

        [EffectSection(SectionKeys.Jitter, 190)]
        [Tooltip("HashNoise only: anisotropic scaling of the hash domain (x/y stretch).")]
        public Vector2 jitterHashAniso = Vector2.one;

        [EffectSection(SectionKeys.Jitter, 200)]
        [Tooltip("HashNoise only: domain warp amplitude in pixels (adds organic crawling).")]
        [Range(0f, 8f)] public float jitterHashWarpAmpPx = 0f;

        [EffectSection(SectionKeys.Jitter, 210)]
        [Tooltip("HashNoise only: domain warp cell count (frequency).")]
        [Range(4, 1024)] public int jitterHashWarpCells = 64;

        [EffectSection(SectionKeys.Jitter, 220)]
        [Tooltip("HashNoise only: domain warp animation speed.")]
        [Range(0f, 30f)] public float jitterHashWarpSpeed = 6f;

        [EffectSection(SectionKeys.Jitter, 230)]
        [Tooltip("HashNoise only: if enabled, each channel gets its own independent hash vector.")]
        public bool jitterHashPerChannel = false;

        // -------------------- RGB Bleeding --------------------
        [EffectSection(SectionKeys.Bleed, 0)][Tooltip("Blend amount of the RGB bleed composite.")][Range(0f, 1f)] public float bleedBlend = 0f;
        [EffectSection(SectionKeys.Bleed, 10)][Tooltip("Base distance used for channel separation.")][Range(0f, 10f)] public float bleedIntensity = 0f;
        [EffectSection(SectionKeys.Bleed, 20)][Tooltip("Choose between manual per-channel shifts or radial shifting.")] public BleedMode bleedMode = BleedMode.Manual;
        [EffectSection(SectionKeys.Bleed, 30)][Tooltip("How the separated channels are combined back into the image.")] public BleedBlendMode bleedBlendMode = BleedBlendMode.Mix;
        [EffectSection(SectionKeys.Bleed, 40)][Tooltip("Manual screen-space shift for the red channel.")] public Vector2 shiftR = new Vector2(-0.5f, 0.5f);
        [EffectSection(SectionKeys.Bleed, 50)][Tooltip("Manual screen-space shift for the green channel.")] public Vector2 shiftG = new Vector2(0.5f, -0.5f);
        [EffectSection(SectionKeys.Bleed, 60)][Tooltip("Manual screen-space shift for the blue channel.")] public Vector2 shiftB = Vector2.zero;

        [EffectSection(SectionKeys.Bleed, 70)][Tooltip("Restrict bleed to higher-contrast edges.")] public bool bleedEdgeOnly = false;
        [EffectSection(SectionKeys.Bleed, 80)][Tooltip("Threshold for detecting edges when Edge Only is enabled.")][Range(0f, 1f)] public float bleedEdgeThreshold = 0.05f;
        [EffectSection(SectionKeys.Bleed, 90)][Tooltip("Sharpness and contrast of the edge mask.")][Range(0.25f, 8f)] public float bleedEdgePower = 2f;

        [EffectSection(SectionKeys.Bleed, 100)][Tooltip("Center point used by radial bleed mode.")] public Vector2 bleedRadialCenter = new Vector2(0.5f, 0.5f);
        [EffectSection(SectionKeys.Bleed, 110)][Tooltip("Signed radial shift strength. Positive pulls inward, negative pushes outward.")][Range(-5f, 5f)] public float bleedRadialStrength = 1f;

        [EffectSection(SectionKeys.Bleed, 120)][Tooltip("Number of taps used when smear is active.")][Range(1, 8)] public int bleedSamples = 1;
        [EffectSection(SectionKeys.Bleed, 130)][Tooltip("Additional trail length for multi-sample smear.")][Range(0f, 5f)] public float bleedSmear = 0f;
        [EffectSection(SectionKeys.Bleed, 140)][Tooltip("How quickly smear samples fade over distance.")][Range(0.25f, 6f)] public float bleedFalloff = 2f;

        [EffectSection(SectionKeys.Bleed, 150)][Tooltip("Per-channel multiplier for red shift strength.")][Range(0f, 2f)] public float bleedIntensityR = 1f;
        [EffectSection(SectionKeys.Bleed, 160)][Tooltip("Per-channel multiplier for green shift strength.")][Range(0f, 2f)] public float bleedIntensityG = 1f;
        [EffectSection(SectionKeys.Bleed, 170)][Tooltip("Per-channel multiplier for blue shift strength.")][Range(0f, 2f)] public float bleedIntensityB = 1f;
        [EffectSection(SectionKeys.Bleed, 180)][Tooltip("Horizontal and vertical stretch applied to the bleed shape.")] public Vector2 bleedAnamorphic = Vector2.one;

        [EffectSection(SectionKeys.Bleed, 190)][Tooltip("Clamp screen UVs to avoid sampling outside the source image.")] public bool bleedClampUV = false;
        [EffectSection(SectionKeys.Bleed, 200)][Tooltip("Preserve approximate brightness after channel separation.")] public bool bleedPreserveLuma = false;

        [EffectSection(SectionKeys.Bleed, 210)][Tooltip("Animated wobble amount added to bleed offsets.")][Range(0f, 2f)] public float bleedWobbleAmp = 0f;
        [EffectSection(SectionKeys.Bleed, 220)][Tooltip("Frequency of the bleed wobble animation.")][Range(0f, 20f)] public float bleedWobbleFreq = 4f;
        [EffectSection(SectionKeys.Bleed, 230)][Tooltip("Modulate wobble per scanline for a VHS-style drift.")] public bool bleedWobbleScanline = false;

        // -------------------- Ghosting --------------------
        [EffectSection(SectionKeys.Ghost, 0)][Tooltip("Enable motion-trail ghosting.")] public bool ghostEnabled = false;
        [EffectSection(SectionKeys.Ghost, 10)][Tooltip("Blend amount of the accumulated history. Overlay modes contribute temporal differences only, avoiding static double exposure.")][Range(0f, 1f)] public float ghostBlend = 0.20f;
        [EffectSection(SectionKeys.Ghost, 20)][Tooltip("Per-frame offset applied between stored history frames.")] public Vector2 ghostOffsetPx = Vector2.zero;

        [EffectSection(SectionKeys.Ghost, 30)][Tooltip("Number of previous frames to store in history.")][Range(1, 16)] public int ghostFrames = 4;
        [HideInInspector][Tooltip("Legacy frame-based capture interval retained for profile migration. Use Ghost Frame Interval (ms).")][Range(0, 8)] public int ghostCaptureInterval = 0;
        [EffectSection(SectionKeys.Ghost, 50)][Tooltip("Delay before the first ghost frame appears.")][Range(0, 8)] public int ghostStartDelay = 0;
        [EffectSection(SectionKeys.Ghost, 60)][Tooltip("Bias toward newer or older frames in the composite.")][Range(0.25f, 4f)] public float ghostWeightCurve = 1.5f;

        [EffectSection(SectionKeys.Ghost, 70)][Tooltip("How the history composite blends with the current frame.")] public GhostCombineMode ghostCombineMode = GhostCombineMode.Screen;
        [EffectSection(SectionKeys.Ghost, 80)][Tooltip("Resolution scale of temporal history buffers. Half resolution greatly reduces VRAM with minimal trail-quality loss.")][Range(0.25f, 1f)] public float ghostResolutionScale = 0.5f;
        [EffectSection(SectionKeys.Ghost, 90)][Tooltip("Time between discrete history samples in milliseconds.")][Range(8f, 250f)] public float ghostFrameIntervalMs = 33.333f;
        [EffectSection(SectionKeys.Ghost, 100)][Tooltip("Exponential history decay time in milliseconds.")][Range(16f, 2000f)] public float ghostDecayMs = 180f;

        // -------------------- Unsharp --------------------
        [EffectSection(SectionKeys.Unsharp, 0)][Tooltip("Enable the unsharp mask sharpening pass.")] public bool unsharpEnabled = false;
        [EffectSection(SectionKeys.Unsharp, 10)][Tooltip("Strength of the sharpening effect.")][Range(0f, 3f)] public float unsharpAmount = 0.5f;
        [EffectSection(SectionKeys.Unsharp, 20)][Tooltip("Radius of the blur used to build the sharpen mask.")][Range(0.25f, 4f)] public float unsharpRadius = 1.0f;
        [EffectSection(SectionKeys.Unsharp, 30)][Tooltip("Ignore smaller differences to reduce sharpening of noise.")][Range(0f, 0.25f)] public float unsharpThreshold = 0.0f;

        [EffectSection(SectionKeys.Unsharp, 40)][Tooltip("Sharpen luminance only and keep color sharpening separate.")] public bool unsharpLumaOnly = false;
        [EffectSection(SectionKeys.Unsharp, 50)][Tooltip("Additional sharpening applied to chroma when Luma Only is enabled.")][Range(0f, 1f)] public float unsharpChroma = 0.0f;
        [EffectSection(SectionKeys.Unsharp, 60)][Tooltip("Contrast Adaptive suppresses sharpening in flat/noisy areas and uses a smaller cross kernel to reduce halos.")] public SharpenMode sharpenMode = SharpenMode.ContrastAdaptive;

        // -------------------- Edge Outline --------------------
        [EffectSection(SectionKeys.Edges, 0)][Tooltip("Enable depth-based outlines.")] public bool edgeEnabled = false;
        [EffectSection(SectionKeys.Edges, 10)][Tooltip("Strength of the outline detection.")][Range(0f, 8f)] public float edgeStrength = 1f;
        [EffectSection(SectionKeys.Edges, 20)][Tooltip("Depth difference required to create an edge.")][Range(0f, 1f)] public float edgeThreshold = 0.02f;
        [EffectSection(SectionKeys.Edges, 30)][Tooltip("Blend amount of the outline pass.")][Range(0f, 1f)] public float edgeBlend = 1f;
        [EffectSection(SectionKeys.Edges, 40)][Tooltip("Tint used for the outline.")] public Color edgeColor = Color.black;
        [EffectSection(SectionKeys.Edges, 50)][Tooltip("Outline sampling radius in pixels.")][Range(0.5f, 4f)] public float edgeThickness = 1f;
        [EffectSection(SectionKeys.Edges, 60)][Tooltip("Also detect view-space normal discontinuities.")] public bool edgeUseNormals = true;
        [EffectSection(SectionKeys.Edges, 70)][Tooltip("Normal-angle discontinuity required to create an edge.")][Range(0f, 1f)] public float edgeNormalThreshold = 0.18f;

        // -------------------- Dithering --------------------
        [EffectSection(SectionKeys.Dither, 0)][Tooltip("Pattern used for dithering before final quantization.")] public DitherMode ditherMode = DitherMode.None;
        [EffectSection(SectionKeys.Dither, 10)][Tooltip("How strongly the dither pattern affects the image.")][Range(0f, 1f)] public float ditherStrength = 0.0f;
        [EffectSection(SectionKeys.Dither, 20)][Tooltip("Rotation in degrees for the Linear dither pattern.")][Range(0f, 180f)] public float ditherAngle = 45f;
        [EffectSection(SectionKeys.Dither, 30)][Tooltip("Blue-noise texture used by Blue Noise mode.")] public Texture2D blueNoise;
        [EffectSection(SectionKeys.Dither, 40)][Tooltip("Decorrelate noise patterns over time to reduce stationary texture.")] public bool temporalDither = false;
        [EffectSection(SectionKeys.Dither, 50)][Tooltip("Temporal dither updates per second.")][Range(1f, 120f)] public float temporalDitherRate = 30f;
        [EffectSection(SectionKeys.Dither, 60)][Tooltip("Print screen cell size in virtual pixels.")][Range(2f, 24f)] public float halftoneScale = 6f;
        [EffectSection(SectionKeys.Dither, 70)][Tooltip("Expands or contracts printed dots.")][Range(-0.5f, 0.5f)] public float halftoneDotGain = 0f;

        // -------------------- VHS tape --------------------
        [EffectSection(SectionKeys.Vhs, 0)][Tooltip("Enable analog VHS tape simulation.")] public bool vhsEnabled = false;
        [EffectSection(SectionKeys.Vhs, 10)][Tooltip("Overall blend with the clean input.")][Range(0f, 1f)] public float vhsIntensity = 0.8f;
        [EffectSection(SectionKeys.Vhs, 20)][Tooltip("Tape transport speed used by tracking and noise animation.")][Range(0f, 4f)] public float vhsTapeSpeed = 1f;
        [EffectSection(SectionKeys.Vhs, 30)][Tooltip("Random horizontal displacement per recorded scanline, in pixels.")][Range(0f, 12f)] public float vhsHorizontalJitter = 1.5f;
        [EffectSection(SectionKeys.Vhs, 40)][Tooltip("Smooth time-base error that bends vertical edges.")][Range(0f, 12f)] public float vhsLineWobble = 2f;
        [EffectSection(SectionKeys.Vhs, 50)][Tooltip("Strength of the traveling tracking-error band.")][Range(0f, 1f)] public float vhsTracking = 0.25f;
        [EffectSection(SectionKeys.Vhs, 60)][Tooltip("Vertical travel speed of tracking errors.")][Range(-4f, 4f)] public float vhsTrackingSpeed = 0.65f;
        [EffectSection(SectionKeys.Vhs, 70)][Tooltip("Height of the tracking-error band.")][Range(0.005f, 0.25f)] public float vhsTrackingWidth = 0.055f;
        [EffectSection(SectionKeys.Vhs, 80)][Tooltip("Horizontal delay between luminance and chrominance, in pixels.")][Range(-12f, 12f)] public float vhsChromaBleed = 3f;
        [EffectSection(SectionKeys.Vhs, 90)][Tooltip("Low-bandwidth chroma smear radius, in pixels.")][Range(0f, 12f)] public float vhsChromaBlur = 4f;
        [EffectSection(SectionKeys.Vhs, 100)][Tooltip("Loss of color saturation from repeated tape generations.")][Range(0f, 1f)] public float vhsColorLoss = 0.15f;
        [EffectSection(SectionKeys.Vhs, 110)][Tooltip("Uncorrelated luminance grain from the recorded signal.")][Range(0f, 0.35f)] public float vhsLumaNoise = 0.055f;
        [EffectSection(SectionKeys.Vhs, 120)][Tooltip("Colored speckle in the low-bandwidth chroma signal.")][Range(0f, 0.35f)] public float vhsChromaNoise = 0.025f;
        [EffectSection(SectionKeys.Vhs, 130)][Tooltip("Frequency of short horizontal RF dropouts.")][Range(0f, 1f)] public float vhsDropout = 0.12f;
        [EffectSection(SectionKeys.Vhs, 140)][Tooltip("Instability and tearing at the lower edge of the tape frame.")][Range(0f, 1f)] public float vhsHeadSwitching = 0.2f;
        [EffectSection(SectionKeys.Vhs, 150)][Tooltip("Height of the head-switching region at the bottom of the frame.")][Range(0.005f, 0.2f)] public float vhsHeadSwitchHeight = 0.045f;
        [EffectSection(SectionKeys.Vhs, 160)][Tooltip("Alternating field-line brightness modulation.")][Range(0f, 1f)] public float vhsInterlace = 0.12f;
        [EffectSection(SectionKeys.Vhs, 170)][Tooltip("Composite television timing family used by the tape signal.")] public VhsStandard vhsStandard = VhsStandard.NTSC;
        [EffectSection(SectionKeys.Vhs, 180)][Tooltip("Tape recording speed. LP and EP progressively reduce bandwidth and stability.")] public VhsTapeMode vhsTapeMode = VhsTapeMode.SP;
        [EffectSection(SectionKeys.Vhs, 190)][Tooltip("Number of analog copy generations.")][Range(0, 8)] public int vhsGeneration = 0;
        [EffectSection(SectionKeys.Vhs, 200)][Tooltip("Slow RF envelope and automatic-gain-control pumping.")][Range(0f, 1f)] public float vhsAgcInstability = 0.08f;
        [EffectSection(SectionKeys.Vhs, 210)][Tooltip("Vertical bandwidth loss in the recorded chroma signal.")][Range(0f, 4f)] public float vhsVerticalChromaBlur = 1f;

        // -------------------- CRT display --------------------
        [EffectSection(SectionKeys.Crt, 0)][Tooltip("Enable cathode-ray tube display simulation.")] public bool crtEnabled = false;
        [EffectSection(SectionKeys.Crt, 10)][Tooltip("Barrel distortion of the glass tube.")][Range(0f, 0.35f)] public float crtCurvature = 0.08f;
        [EffectSection(SectionKeys.Crt, 20)][Tooltip("Zoom after curvature to hide warped corners.")][Range(0.8f, 1.2f)] public float crtOverscan = 1.02f;
        [EffectSection(SectionKeys.Crt, 30)][Tooltip("Darkness between electron-beam scanlines.")][Range(0f, 1f)] public float crtScanlineStrength = 0.75f;
        [EffectSection(SectionKeys.Crt, 40)][Tooltip("Number of simulated raster lines. Set 0 to follow output height.")][Range(0, 1200)] public int crtScanlineCount = 240;
        [EffectSection(SectionKeys.Crt, 50)][Tooltip("Gaussian beam width; bright pixels naturally bloom into adjacent scanlines.")][Range(0.2f, 1.5f)] public float crtBeamWidth = 0.5f;
        [EffectSection(SectionKeys.Crt, 60)][Tooltip("Physical RGB phosphor layout.")] public CrtMaskMode crtMaskMode = CrtMaskMode.ApertureGrille;
        [EffectSection(SectionKeys.Crt, 70)][Tooltip("Visibility of the phosphor or shadow mask.")][Range(0f, 1f)] public float crtMaskStrength = 0.35f;
        [EffectSection(SectionKeys.Crt, 80)][Tooltip("Size of each phosphor triad in output pixels.")][Range(1f, 6f)] public float crtMaskScale = 1f;
        [EffectSection(SectionKeys.Crt, 90)][Tooltip("Local light spread that approximates phosphor halation.")][Range(0f, 1.5f)] public float crtBloom = 0.2f;
        [EffectSection(SectionKeys.Crt, 100)][Tooltip("Bloom sampling radius in output pixels.")][Range(0.5f, 4f)] public float crtBloomRadius = 1.5f;
        [EffectSection(SectionKeys.Crt, 110)][Tooltip("Darkening near the rounded screen edges.")][Range(0f, 1f)] public float crtVignette = 0.35f;
        [EffectSection(SectionKeys.Crt, 120)][Tooltip("Width of the vignette transition.")][Range(0.01f, 0.5f)] public float crtVignetteSoftness = 0.18f;
        [EffectSection(SectionKeys.Crt, 130)][Tooltip("Animated electronic signal noise.")][Range(0f, 0.2f)] public float crtNoise = 0.015f;
        [EffectSection(SectionKeys.Crt, 140)][Tooltip("Subtle 60 Hz brightness instability.")][Range(0f, 0.2f)] public float crtFlicker = 0.015f;
        [EffectSection(SectionKeys.Crt, 150)][Tooltip("Compensates for light lost to scanlines and the phosphor mask.")][Range(0.5f, 3f)] public float crtBrightness = 1.2f;
        [EffectSection(SectionKeys.Crt, 160)][Tooltip("Opacity of the rounded glass tube boundary. Set zero for borderless output.")][Range(0f, 1f)] public float crtTubeEdge = 1f;
        [EffectSection(SectionKeys.Crt, 170)][Tooltip("Only pixels brighter than this value feed phosphor bloom.")][Range(0f, 2f)] public float crtBloomThreshold = 0.55f;
        [EffectSection(SectionKeys.Crt, 180)][Tooltip("RGB beam convergence error in output pixels.")][Range(0f, 3f)] public float crtConvergencePx = 0f;
        [EffectSection(SectionKeys.Crt, 190)][Tooltip("Horizontal electron-beam focus softness.")][Range(0f, 1f)] public float crtFocus = 0.35f;
        [EffectSection(SectionKeys.Crt, 200)][Tooltip("Display black-level pedestal.")][Range(0f, 0.2f)] public float crtBlackLevel = 0f;
        [EffectSection(SectionKeys.Crt, 210)][Tooltip("Slow vertical mains-hum brightness bar.")][Range(0f, 0.2f)] public float crtHumBar = 0f;
        [EffectSection(SectionKeys.Crt, 220)][Tooltip("Flicker frequency in hertz.")][Range(24f, 120f)] public float crtFlickerHz = 60f;

        // -------------------- Lens & sensor --------------------
        [EffectSection(SectionKeys.LensSensor, 0)] public bool lensSensorEnabled = false;
        [EffectSection(SectionKeys.LensSensor, 10)][Range(0f, 1f)] public float lensSensorIntensity = 1f;
        [EffectSection(SectionKeys.LensSensor, 20)][Tooltip("Barrel (positive) or pincushion (negative) distortion.")][Range(-0.5f, 0.5f)] public float lensDistortion = 0f;
        [EffectSection(SectionKeys.LensSensor, 30)][Tooltip("Lateral lens chromatic aberration in pixels.")][Range(0f, 8f)] public float lensChromaticAberration = 0f;
        [EffectSection(SectionKeys.LensSensor, 40)][Range(0f, 1f)] public float lensVignette = 0f;
        [EffectSection(SectionKeys.LensSensor, 50)][Range(0f, 2f)] public float lensBloom = 0f;
        [EffectSection(SectionKeys.LensSensor, 60)][Range(0.5f, 8f)] public float lensBloomRadius = 2f;
        [EffectSection(SectionKeys.LensSensor, 70)][Tooltip("Rolling-shutter horizontal skew in pixels.")][Range(0f, 12f)] public float sensorRollingShutter = 0f;
        [EffectSection(SectionKeys.LensSensor, 80)][Range(0f, 0.25f)] public float sensorNoise = 0f;
        [EffectSection(SectionKeys.LensSensor, 90)][Range(0f, 1f)] public float sensorDeadPixels = 0f;

        // -------------------- Film --------------------
        [EffectSection(SectionKeys.Film, 0)] public bool filmEnabled = false;
        [EffectSection(SectionKeys.Film, 10)][Range(0f, 1f)] public float filmIntensity = 1f;
        [EffectSection(SectionKeys.Film, 20)][Range(0f, 0.5f)] public float filmGrain = 0.08f;
        [EffectSection(SectionKeys.Film, 30)][Range(0.5f, 4f)] public float filmGrainSize = 1f;
        [EffectSection(SectionKeys.Film, 40)][Range(0f, 2f)] public float filmHalation = 0.15f;
        [EffectSection(SectionKeys.Film, 50)][Range(0.5f, 8f)] public float filmHalationRadius = 2f;
        [EffectSection(SectionKeys.Film, 60)][Tooltip("Mechanical gate weave in pixels.")][Range(0f, 6f)] public float filmGateWeave = 0.25f;
        [EffectSection(SectionKeys.Film, 70)][Range(0f, 1f)] public float filmDust = 0f;
        [EffectSection(SectionKeys.Film, 80)][Range(0f, 1f)] public float filmScratches = 0f;
        [EffectSection(SectionKeys.Film, 90)][Range(0f, 0.2f)] public float filmFlicker = 0.01f;

        // -------------------- Motion & datamosh --------------------
        [EffectSection(SectionKeys.MotionGlitch, 0)] public bool motionGlitchEnabled = false;
        [EffectSection(SectionKeys.MotionGlitch, 10)][Range(0f, 1f)] public float motionGlitchIntensity = 0.6f;
        [EffectSection(SectionKeys.MotionGlitch, 20)][Range(4f, 128f)] public float motionBlockSize = 32f;
        [EffectSection(SectionKeys.MotionGlitch, 30)][Range(0f, 8f)] public float motionVectorDisplacement = 2f;
        [EffectSection(SectionKeys.MotionGlitch, 40)][Range(0f, 1f)] public float motionFreezeRate = 0.1f;
        [EffectSection(SectionKeys.MotionGlitch, 50)][Range(0f, 8f)] public float motionColorSplit = 0f;
        [EffectSection(SectionKeys.MotionGlitch, 60)][Range(0.25f, 1f)] public float motionHistoryScale = 0.5f;
        [EffectSection(SectionKeys.MotionGlitch, 70)][Tooltip("Temporal history capture cadence. Lower rates create held-frame stop motion and longer datamosh persistence.")][Range(1f, 60f)] public float motionHistoryFps = 24f;

        // -------------------- Digital video --------------------
        [EffectSection(SectionKeys.DigitalVideo, 0)] public bool digitalVideoEnabled = false;
        [EffectSection(SectionKeys.DigitalVideo, 10)][Range(0f, 1f)] public float digitalVideoIntensity = 0.7f;
        [EffectSection(SectionKeys.DigitalVideo, 20)][Range(4f, 64f)] public float digitalBlockSize = 16f;
        [EffectSection(SectionKeys.DigitalVideo, 30)][Range(0f, 1f)] public float digitalQuantization = 0.25f;
        [EffectSection(SectionKeys.DigitalVideo, 40)][Range(0f, 1f)] public float digitalRinging = 0.1f;
        [EffectSection(SectionKeys.DigitalVideo, 50)][Range(0f, 1f)] public float digitalChromaSubsampling = 0.35f;
        [EffectSection(SectionKeys.DigitalVideo, 60)][Range(0f, 1f)] public float digitalMosquitoNoise = 0.05f;
        [EffectSection(SectionKeys.DigitalVideo, 70)][Range(0f, 1f)] public float digitalBitratePumping = 0f;

        // -------------------- Composite signal --------------------
        [EffectSection(SectionKeys.Composite, 0)] public bool compositeEnabled = false;
        [EffectSection(SectionKeys.Composite, 10)][Range(0f, 1f)] public float compositeIntensity = 0.7f;
        [EffectSection(SectionKeys.Composite, 20)] public VhsStandard compositeStandard = VhsStandard.NTSC;
        [EffectSection(SectionKeys.Composite, 30)][Range(0f, 1f)] public float compositeDotCrawl = 0.25f;
        [EffectSection(SectionKeys.Composite, 40)][Range(0f, 1f)] public float compositeRainbow = 0.15f;
        [EffectSection(SectionKeys.Composite, 50)][Range(0f, 1f)] public float compositeChromaBandwidth = 0.55f;
        [EffectSection(SectionKeys.Composite, 60)][Range(0f, 1f)] public float compositePhaseError = 0.05f;
        [EffectSection(SectionKeys.Composite, 70)][Tooltip("Decoder comb-filter quality. One is cleaner; zero leaves more crosstalk.")][Range(0f, 1f)] public float compositeCombFilter = 0.5f;

        // -------------------- LCD display --------------------
        [EffectSection(SectionKeys.Lcd, 0)] public bool lcdEnabled = false;
        [EffectSection(SectionKeys.Lcd, 10)][Range(0f, 1f)] public float lcdIntensity = 0.8f;
        [EffectSection(SectionKeys.Lcd, 20)][Range(1f, 8f)] public float lcdPixelScale = 2f;
        [EffectSection(SectionKeys.Lcd, 30)][Range(0f, 1f)] public float lcdSubpixelStrength = 0.35f;
        [EffectSection(SectionKeys.Lcd, 40)][Range(0f, 1f)] public float lcdInversion = 0.03f;
        [EffectSection(SectionKeys.Lcd, 50)][Range(-1f, 1f)] public float lcdViewingAngle = 0f;
        [EffectSection(SectionKeys.Lcd, 60)][Range(0f, 1f)] public float lcdBacklightBleed = 0.05f;
        [EffectSection(SectionKeys.Lcd, 70)][Range(0f, 4f)] public float lcdResponseSmear = 0.4f;

        // -------------------- Stage shaders --------------------
        [EffectSection(SectionKeys.Shaders, 0)][Tooltip("Optional override for the sampling and grid shader. Leave empty to auto-find by name.")] public Shader samplingGridShader;
        [EffectSection(SectionKeys.Shaders, 10)][Tooltip("Optional override for the pregrade shader. Leave empty to auto-find by name.")] public Shader pregradeShader;
        [EffectSection(SectionKeys.Shaders, 20)][Tooltip("Optional override for the channel jitter shader. Leave empty to auto-find by name.")] public Shader channelJitterShader;
        [EffectSection(SectionKeys.Shaders, 30)][Tooltip("Optional override for the ghosting shader. Leave empty to auto-find by name.")] public Shader ghostingShader;
        [EffectSection(SectionKeys.Shaders, 40)][Tooltip("Optional override for the RGB bleed shader. Leave empty to auto-find by name.")] public Shader rgbBleedingShader;
        [EffectSection(SectionKeys.Shaders, 50)][Tooltip("Optional override for the unsharp mask shader. Leave empty to auto-find by name.")] public Shader unsharpMaskShader;
        [EffectSection(SectionKeys.Shaders, 60)][Tooltip("Optional override for the posterize tone shader. Leave empty to auto-find by name.")] public Shader posterizeToneShader;
        [EffectSection(SectionKeys.Shaders, 70)][Tooltip("Optional override for the dithering shader. Leave empty to auto-find by name.")] public Shader ditheringShader;
        [EffectSection(SectionKeys.Shaders, 80)][Tooltip("Optional override for the palette mapping shader. Leave empty to auto-find by name.")] public Shader paletteMappingShader;
        [EffectSection(SectionKeys.Shaders, 90)][Tooltip("Optional override for the edge outline shader. Leave empty to auto-find by name.")] public Shader edgeOutlineShader;
        [EffectSection(SectionKeys.Shaders, 100)][Tooltip("Optional override for the final present shader. Leave empty to auto-find by name.")] public Shader masterPresentShader;
        [EffectSection(SectionKeys.Shaders, 110)][Tooltip("Optional override for the texture mask shader. Leave empty to auto-find by name.")] public Shader textureMaskShader;
        [EffectSection(SectionKeys.Shaders, 120)][Tooltip("Optional override for the depth mask shader. Leave empty to auto-find by name.")] public Shader depthMaskShader;
        [EffectSection(SectionKeys.Shaders, 130)][Tooltip("Optional override for the ghost composite shader. Leave empty to auto-find by name.")] public Shader ghostCompositeShader;
        [EffectSection(SectionKeys.Shaders, 140)][Tooltip("Optional override for the CRT display shader. Leave empty to auto-find by name.")] public Shader crtDisplayShader;
        [EffectSection(SectionKeys.Shaders, 150)][Tooltip("Optional override for the VHS tape shader. Leave empty to auto-find by name.")] public Shader vhsTapeShader;
        [EffectSection(SectionKeys.Shaders, 160)][Tooltip("Optional override for the professional effect-family shader.")] public Shader professionalEffectsShader;

        // -------------------- Materials --------------------
        [SerializeField, HideInInspector] private int _analogSettingsVersion;
        private const int CurrentAnalogSettingsVersion = 1;

        private Material _mSampling, _mPregrade, _mJitter, _mGhosting, _mBleed, _mUnsharp, _mPosterize, _mDither, _mPalette, _mEdges, _mVhs, _mCrt, _mPresent;
        private Material _mTexMask, _mDepthMask;
        private Material _mGhostComposite;
        private Material _mProfessional;

        // -------------------- Curve texture --------------------
        private Texture2D _curveTex;

        // -------------------- Ghost history --------------------
        private RenderTexture[] _ghostRing;
        private int _ghostWriteIndex;
        private float _ghostCaptureElapsed;
        private RenderTexture _ghostCompositeTex;
        private bool _ghostSeeded;
        private RenderTexture _motionHistoryTex;
        private bool _motionHistorySeeded;
        private float _motionHistoryNextCaptureTime;

        private void OnEnable()
        {
            AutoAssignShadersIfMissing();
            ApplyAssignedProfileIfEnabled();
            UpgradeAnalogSettings();
            ClampAndSanitize();
            UpdateCurveTexture();
            EnsureDepthModeIfNeeded();
            EnsureGhostResources(null);
            _ghostSeeded = false;
        }

        private void OnValidate()
        {
            AutoAssignShadersIfMissing();
            UpgradeAnalogSettings();
            ClampAndSanitize();
            UpdateCurveTexture();
            EnsureDepthModeIfNeeded();
        }

        private void OnDisable()
        {
            DestroyMat(ref _mSampling);
            DestroyMat(ref _mPregrade);
            DestroyMat(ref _mJitter);
            DestroyMat(ref _mGhosting);
            DestroyMat(ref _mBleed);
            DestroyMat(ref _mUnsharp);
            DestroyMat(ref _mPosterize);
            DestroyMat(ref _mDither);
            DestroyMat(ref _mPalette);
            DestroyMat(ref _mEdges);
            DestroyMat(ref _mCrt);
            DestroyMat(ref _mVhs);
            DestroyMat(ref _mPresent);
            DestroyMat(ref _mTexMask);
            DestroyMat(ref _mDepthMask);
            DestroyMat(ref _mGhostComposite);
            DestroyMat(ref _mProfessional);

            if (_curveTex) DestroyImmediate(_curveTex);
            ReleaseGhostResources();
            SafeReleaseAndDestroyRT(ref _motionHistoryTex);
            _motionHistorySeeded = false;
            _motionHistoryNextCaptureTime = 0f;
        }

        private void ClampAndSanitize()
        {
            virtualResolution.x = Mathf.Max(1, virtualResolution.x);
            virtualResolution.y = Mathf.Max(1, virtualResolution.y);
            pixelAspect = Mathf.Clamp(pixelAspect, 0.25f, 4f);

            gamma = Mathf.Max(0.1f, gamma);

            ghostFrames = Mathf.Clamp(ghostFrames, 1, 16);
            ghostCaptureInterval = Mathf.Clamp(ghostCaptureInterval, 0, 8);
            ghostStartDelay = Mathf.Clamp(ghostStartDelay, 0, 8);
            ghostWeightCurve = Mathf.Clamp(ghostWeightCurve, 0.25f, 4f);
            ghostResolutionScale = Mathf.Clamp(ghostResolutionScale, 0.25f, 1f);
            ghostFrameIntervalMs = Mathf.Clamp(ghostFrameIntervalMs, 8f, 250f);
            ghostDecayMs = Mathf.Clamp(ghostDecayMs, 16f, 2000f);

            minLevels = Mathf.Clamp(minLevels, 2, 512);
            maxLevels = Mathf.Clamp(maxLevels, 2, 512);
            levels = Mathf.Clamp(levels, 2, 512);
            levelsR = Mathf.Clamp(levelsR, 2, 512);
            levelsG = Mathf.Clamp(levelsG, 2, 512);
            levelsB = Mathf.Clamp(levelsB, 2, 512);

            bleedSamples = Mathf.Clamp(bleedSamples, 1, 8);
            bleedFalloff = Mathf.Clamp(bleedFalloff, 0.25f, 6f);
            bleedSmear = Mathf.Max(0f, bleedSmear);

            bleedIntensityR = Mathf.Max(0f, bleedIntensityR);
            bleedIntensityG = Mathf.Max(0f, bleedIntensityG);
            bleedIntensityB = Mathf.Max(0f, bleedIntensityB);

            bleedAnamorphic.x = Mathf.Max(0.0001f, bleedAnamorphic.x);
            bleedAnamorphic.y = Mathf.Max(0.0001f, bleedAnamorphic.y);

            bleedRadialStrength = Mathf.Clamp(bleedRadialStrength, -5f, 5f);

            bleedWobbleAmp = Mathf.Max(0f, bleedWobbleAmp);
            bleedWobbleFreq = Mathf.Max(0f, bleedWobbleFreq);

            jitterSeed = Mathf.Clamp(jitterSeed, 0, 9999);
            jitterAmountPx = Mathf.Max(0f, jitterAmountPx);
            jitterSpeed = Mathf.Max(0f, jitterSpeed);
            jitterScanlineDensity = Mathf.Max(32f, jitterScanlineDensity);
            jitterScanlineAmp = Mathf.Max(0f, jitterScanlineAmp);
            jitterChannelWeights.x = Mathf.Max(0f, jitterChannelWeights.x);
            jitterChannelWeights.y = Mathf.Max(0f, jitterChannelWeights.y);
            jitterChannelWeights.z = Mathf.Max(0f, jitterChannelWeights.z);
            jitterHashCellCount = Mathf.Clamp(jitterHashCellCount, 4, 1024);
            jitterHashWarpCells = Mathf.Clamp(jitterHashWarpCells, 4, 1024);
            jitterHashTimeSmooth = Mathf.Clamp01(jitterHashTimeSmooth);
            jitterHashRotateDeg = Mathf.Clamp(jitterHashRotateDeg, -180f, 180f);
            jitterHashAniso.x = Mathf.Max(0.0001f, jitterHashAniso.x);
            jitterHashAniso.y = Mathf.Max(0.0001f, jitterHashAniso.y);
            jitterHashWarpAmpPx = Mathf.Max(0f, jitterHashWarpAmpPx);
            jitterHashWarpSpeed = Mathf.Max(0f, jitterHashWarpSpeed);
            crtScanlineCount = Mathf.Clamp(crtScanlineCount, 0, 1200);
            crtMaskScale = Mathf.Max(1f, crtMaskScale);
            crtBeamWidth = Mathf.Max(0.2f, crtBeamWidth);
            vhsTrackingWidth = Mathf.Max(0.005f, vhsTrackingWidth);
            vhsHeadSwitchHeight = Mathf.Max(0.005f, vhsHeadSwitchHeight);
            motionHistoryFps = Mathf.Clamp(motionHistoryFps, 1f, 60f);

        }

        public void ApplyProfile(CrowFXProfile source)
        {
            if (source == null) return;

            source.ApplyTo(this);
            ClampAndSanitize();
            UpdateCurveTexture();
            EnsureDepthModeIfNeeded();
        }

        public void SaveToProfile(CrowFXProfile target)
        {
            if (target == null) return;
            target.CaptureFrom(this);
        }

        private void ApplyAssignedProfileIfEnabled()
        {
            if (!autoApplyProfile || profile == null) return;
            profile.ApplyTo(this);
        }

        private float GetPixelSizeParam() => Mathf.Max(1, pixelSize);

        private Vector4 GetVirtualResolutionParam()
            => new Vector4(Mathf.Max(1, virtualResolution.x), Mathf.Max(1, virtualResolution.y), 0f, 0f);

        private void SetVirtualGridParams(Material material)
        {
            material.SetFloat(ShaderProps.UseVirtualGrid, useVirtualGrid ? 1f : 0f);
            material.SetVector(ShaderProps.VirtualRes, GetVirtualResolutionParam());
        }

        private void SetPixelGridParams(Material material)
        {
            material.SetFloat(ShaderProps.PixelSize, GetPixelSizeParam());
            SetVirtualGridParams(material);
        }

        private static Vector4 ToShaderVector(Vector2 value) => new Vector4(value.x, value.y, 0f, 0f);

        private static Vector4 ToShaderVector(Vector3 value) => new Vector4(value.x, value.y, value.z, 0f);

        private void OnRenderImage(RenderTexture src, RenderTexture dest) => RenderStack(src, dest);

        /// <summary>Renders the complete active CrowFX graph. Render-pipeline adapters can call this
        /// when their source and destination are backed by RenderTextures.</summary>
        public void RenderStack(RenderTexture src, RenderTexture dest)
        {
            if (src == null) { Graphics.Blit(src, dest); return; }

            if (masterBlend <= 0.0001f)
            {
                Graphics.Blit(src, dest);
                return;
            }

            EnsureDepthModeIfNeeded();
            EnsureGhostResources(src);
            EnsureMotionHistory(src);

            var desc = src.descriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                desc.colorFormat = RenderTextureFormat.ARGBHalf;

            RenderTexture a = null;
            RenderTexture b = null;
            Profiler.BeginSample("CrowFX.Stack");
            try
            {
                a = RenderTexture.GetTemporary(desc);
                b = RenderTexture.GetTemporary(desc);

                Graphics.Blit(src, a);

                if (IsSamplingActive()) { RunSamplingGrid(a, b); Swap(ref a, ref b); }
                if (pregradeEnabled) { RunPregrade(a, b); Swap(ref a, ref b); }
                if (lensSensorEnabled && lensSensorIntensity > 0f) { RunProfessional(a, b, ProfessionalMode.LensSensor); Swap(ref a, ref b); }
                if (filmEnabled && filmIntensity > 0f) { RunProfessional(a, b, ProfessionalMode.Film); Swap(ref a, ref b); }
                if (IsJitterActive()) { RunChannelJitter(a, b); Swap(ref a, ref b); }

                if (IsGhostActive())
                {
                    // Composite only previously captured frames. Capture the signal at the
                    // ghost injection point after rendering so display/tape damage is never
                    // recursively fed through the stack on the following frame.
                    // Seed the ring from the current signal so enabling ghosting never
                    // produces a one-frame black flash.
                    if (!_ghostSeeded) CaptureGhostFrame(a);
                    BuildGhostComposite();
                    RunGhosting(a, b);
                    CaptureGhostFrame(a);
                    Swap(ref a, ref b);
                }

                if (IsBleedActive()) { RunBleed(a, b); Swap(ref a, ref b); }
                if (IsUnsharpActive()) { RunUnsharp(a, b); Swap(ref a, ref b); }
                if (motionGlitchEnabled && motionGlitchIntensity > 0f)
                {
                    RunProfessional(a, b, ProfessionalMode.MotionGlitch);
                    CaptureMotionHistory(a);
                    Swap(ref a, ref b);
                }
                if (IsQuantizationActive()) { RunDithering(a, b); Swap(ref a, ref b); }
                if (IsPaletteActive()) { RunPaletteMapping(a, b); Swap(ref a, ref b); }
                if (IsEdgeActive()) { RunEdges(a, b); Swap(ref a, ref b); }

                if (maskPlacement == MaskPlacement.BeforeSignalAndDisplays)
                    ApplyStackMasks(src, ref a, ref b);

                if (digitalVideoEnabled && digitalVideoIntensity > 0f) { RunProfessional(a, b, ProfessionalMode.DigitalVideo); Swap(ref a, ref b); }
                if (IsVhsActive()) { RunVhs(a, b); Swap(ref a, ref b); }
                if (compositeEnabled && compositeIntensity > 0f) { RunProfessional(a, b, ProfessionalMode.Composite); Swap(ref a, ref b); }
                if (IsCrtActive()) { RunCrt(a, b); Swap(ref a, ref b); }
                if (lcdEnabled && lcdIntensity > 0f) { RunProfessional(a, b, ProfessionalMode.Lcd); Swap(ref a, ref b); }

                // Stack masks are final by default: white/deep areas receive the complete
                // processed stack, while black/near areas restore the true camera source.
                if (maskPlacement == MaskPlacement.EntireStack)
                    ApplyStackMasks(src, ref a, ref b);

                RunPresent(src, a, dest);
            }
            finally
            {
                if (a != null) RenderTexture.ReleaseTemporary(a);
                if (b != null) RenderTexture.ReleaseTemporary(b);
                Profiler.EndSample();
            }
        }

        private void ApplyStackMasks(RenderTexture original, ref RenderTexture current, ref RenderTexture scratch)
        {
            if (useMask && maskTex != null) { RunTextureMask(original, current, scratch); Swap(ref current, ref scratch); }
            if (useDepthMask) { RunDepthMask(original, current, scratch); Swap(ref current, ref scratch); }
        }

        private bool IsSamplingActive() => pixelSize > 1 || useVirtualGrid;
        private bool IsJitterActive() => jitterEnabled && jitterStrength > 0.0001f && jitterAmountPx > 0.0001f;
        private bool IsGhostActive() => ghostEnabled && ghostBlend > 0.0001f;
        private bool IsBleedActive() => bleedBlend > 0.0001f && bleedIntensity > 0.0001f;
        private bool IsUnsharpActive() => unsharpEnabled && unsharpAmount > 0.0001f;
        private bool IsEdgeActive() => edgeEnabled && edgeBlend > 0.0001f && edgeStrength > 0.0001f;
        private bool IsVhsActive() => vhsEnabled && vhsIntensity > 0.0001f;
        private bool IsCrtActive() => crtEnabled;

        public int GetActivePassCount()
        {
            if (masterBlend <= 0.0001f) return 0;
            int count = 2; // source staging + final alpha-safe present
            if (IsSamplingActive()) count++;
            if (pregradeEnabled) count++;
            if (lensSensorEnabled && lensSensorIntensity > 0f) count++;
            if (filmEnabled && filmIntensity > 0f) count++;
            if (IsJitterActive()) count++;
            if (IsGhostActive()) count += 3; // history composite, effect, capture
            if (IsBleedActive()) count++;
            if (IsUnsharpActive()) count++;
            if (motionGlitchEnabled && motionGlitchIntensity > 0f) count += 2;
            if (IsQuantizationActive()) count++;
            if (IsPaletteActive()) count++;
            if (IsEdgeActive()) count++;
            if (digitalVideoEnabled && digitalVideoIntensity > 0f) count++;
            if (IsVhsActive()) count++;
            if (compositeEnabled && compositeIntensity > 0f) count++;
            if (IsCrtActive()) count++;
            if (lcdEnabled && lcdIntensity > 0f) count++;
            if (useMask && maskTex != null) count++;
            if (useDepthMask) count++;
            return count;
        }

        public int GetEstimatedSamplesPerPixel()
        {
            int samples = 2;
            if (IsSamplingActive()) samples += 1;
            if (pregradeEnabled) samples += 1;
            if (lensSensorEnabled && lensSensorIntensity > 0f) samples += 8;
            if (filmEnabled && filmIntensity > 0f) samples += 5;
            if (IsJitterActive()) samples += 4;
            if (IsGhostActive()) samples += Mathf.Clamp(ghostFrames, 1, 16) + 2;
            if (IsBleedActive()) samples += 1 + GetEffectiveBleedSamples() * 3 + (bleedEdgeOnly ? 4 : 0);
            if (IsUnsharpActive()) samples += sharpenMode == SharpenMode.ContrastAdaptive ? 6 : 10;
            if (motionGlitchEnabled && motionGlitchIntensity > 0f) samples += 7;
            if (IsQuantizationActive()) samples += ditherMode == DitherMode.BlueNoise ? 2 : 1;
            if (IsPaletteActive()) samples += usePalette && paletteMode == PaletteMode.Nearest ? GetEffectivePaletteColorCount() + 1 : 2;
            if (IsEdgeActive()) samples += edgeUseNormals ? 11 : 6;
            if (digitalVideoEnabled && digitalVideoIntensity > 0f) samples += 5;
            if (IsVhsActive()) samples += 10;
            if (compositeEnabled && compositeIntensity > 0f) samples += 5;
            if (IsCrtActive()) samples += 20;
            if (lcdEnabled && lcdIntensity > 0f) samples += 4;
            return samples;
        }

        public long GetEstimatedHistoryBytes(int width, int height)
        {
            long bytes = 0;
            if (IsGhostActive())
            {
                float scale = GetEffectiveGhostResolutionScale();
                long pixels = (long)Mathf.Max(1, Mathf.RoundToInt(width * scale)) *
                              Mathf.Max(1, Mathf.RoundToInt(height * scale));
                bytes += pixels * 8L * (Mathf.Clamp(ghostFrames, 1, 16) + 1L);
            }
            if (motionGlitchEnabled)
            {
                long pixels = (long)Mathf.Max(1, Mathf.RoundToInt(width * motionHistoryScale)) *
                              Mathf.Max(1, Mathf.RoundToInt(height * motionHistoryScale));
                bytes += pixels * 8L;
            }
            return bytes;
        }

        private int GetEffectiveBleedSamples()
        {
            int ceiling = qualityTier == QualityTier.Low ? 3 : qualityTier == QualityTier.Balanced ? 6 : 8;
            return Mathf.Clamp(bleedSamples, 1, ceiling);
        }

        private int GetEffectivePaletteColorCount()
        {
            int ceiling = qualityTier == QualityTier.Low ? 16 : qualityTier == QualityTier.Balanced ? 32 : 64;
            return Mathf.Clamp(paletteColorCount, 2, ceiling);
        }

        private float GetEffectiveGhostResolutionScale()
        {
            float ceiling = qualityTier == QualityTier.Low ? 0.25f : qualityTier == QualityTier.Balanced ? 0.5f : 1f;
            return Mathf.Min(ghostResolutionScale, ceiling);
        }

        private bool IsQuantizationActive()
        {
            if (ditherMode != DitherMode.None || animateLevels) return true;
            if (usePerChannel) return levelsR < 512 || levelsG < 512 || levelsB < 512;
            return levels < 512;
        }

        private bool IsPaletteActive()
        {
            if (invert || (usePalette && paletteTex != null)) return true;
            return IsThresholdCurveActive();
        }

        private bool IsThresholdCurveActive()
        {
            if (thresholdCurve == null) return false;

            const int samples = 12;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                if (Mathf.Abs(thresholdCurve.Evaluate(t) - t) > 0.001f) return true;
            }
            return false;
        }

        private void RunSamplingGrid(RenderTexture src, RenderTexture dst)
        {
            var m = MSampling;
            if (!m) { Graphics.Blit(src, dst); return; }

            SetPixelGridParams(m);
            m.SetVector(ShaderProps.SamplingPhase, ToShaderVector(samplingPhase));
            m.SetFloat(ShaderProps.PixelAspect, pixelAspect);
            m.SetFloat(ShaderProps.SamplingFilter, (float)samplingFilter);

            Graphics.Blit(src, dst, m);
        }

        private void RunPregrade(RenderTexture src, RenderTexture dst)
        {
            var m = MPregrade;
            if (!m) { Graphics.Blit(src, dst); return; }

            m.SetFloat(ShaderProps.PregradeEnabled, pregradeEnabled ? 1f : 0f);
            m.SetFloat(ShaderProps.Exposure, exposure);
            m.SetFloat(ShaderProps.Contrast, contrast);
            m.SetFloat(ShaderProps.Gamma, gamma);
            m.SetFloat(ShaderProps.Saturation, saturation);
            m.SetColor(ShaderProps.PregradeTint, pregradeTint);
            m.SetFloat(ShaderProps.PregradeTintStrength, pregradeTintStrength);
            m.SetColor(ShaderProps.PregradeLift, pregradeLift);
            m.SetColor(ShaderProps.PregradeGain, pregradeGain);
            m.SetColor(ShaderProps.PregradeOffset, pregradeOffset);
            m.SetFloat(ShaderProps.PregradeTemperature, pregradeTemperature);
            m.SetFloat(ShaderProps.PregradeRolloff, pregradeHighlightRolloff);

            Graphics.Blit(src, dst, m);
        }

        private void RunChannelJitter(RenderTexture src, RenderTexture dst)
        {
            var m = MJitter;

            if (!m || !jitterEnabled || jitterStrength <= 0f)
            {
                Graphics.Blit(src, dst);
                return;
            }

            m.SetFloat(ShaderProps.JitterEnabled, 1f);
            m.SetFloat(ShaderProps.JitterStrength, jitterStrength);
            m.SetFloat(ShaderProps.JitterMode, (float)jitterMode);

            m.SetFloat(ShaderProps.JitterAmountPx, jitterAmountPx);
            m.SetFloat(ShaderProps.JitterSpeed, jitterSpeed);

            m.SetFloat(ShaderProps.UseSeed, jitterUseSeed ? 1f : 0f);
            m.SetFloat(ShaderProps.Seed, jitterSeed);

            m.SetFloat(ShaderProps.Scanline, jitterScanline ? 1f : 0f);
            m.SetFloat(ShaderProps.ScanlineDensity, jitterScanlineDensity);
            m.SetFloat(ShaderProps.ScanlineAmp, jitterScanlineAmp);

            m.SetVector(ShaderProps.ChannelWeights, ToShaderVector(jitterChannelWeights));
            m.SetVector(ShaderProps.DirR, ToShaderVector(jitterDirR));
            m.SetVector(ShaderProps.DirG, ToShaderVector(jitterDirG));
            m.SetVector(ShaderProps.DirB, ToShaderVector(jitterDirB));

            m.SetFloat(ShaderProps.HashCellCount, Mathf.Clamp(jitterHashCellCount, 4, 1024));
            m.SetFloat(ShaderProps.HashTimeSmooth, Mathf.Clamp01(jitterHashTimeSmooth));
            m.SetFloat(ShaderProps.HashRotateDeg, Mathf.Clamp(jitterHashRotateDeg, -180f, 180f));
            m.SetVector(ShaderProps.HashAniso, new Vector4(
                Mathf.Max(0.0001f, jitterHashAniso.x),
                Mathf.Max(0.0001f, jitterHashAniso.y), 0f, 0f));

            m.SetFloat(ShaderProps.HashWarpAmpPx, Mathf.Max(0f, jitterHashWarpAmpPx));
            m.SetFloat(ShaderProps.HashWarpCells, Mathf.Clamp(jitterHashWarpCells, 4, 1024));
            m.SetFloat(ShaderProps.HashWarpSpeed, Mathf.Max(0f, jitterHashWarpSpeed));
            m.SetFloat(ShaderProps.HashPerChannel, jitterHashPerChannel ? 1f : 0f);

            m.SetFloat(ShaderProps.ClampUV, jitterClampUV ? 1f : 0f);

            // Optional noise texture (used in BlueNoiseTex mode)
            m.SetTexture(ShaderProps.NoiseTex, jitterNoiseTex != null ? jitterNoiseTex : Texture2D.grayTexture);

            // Anchor to virtual grid if enabled (so jitter is resolution-stable)
            SetPixelGridParams(m);

            Graphics.Blit(src, dst, m);
        }

        private void RunGhosting(RenderTexture src, RenderTexture dst)
        {
            var m = MGhosting;

            if (!m || !ghostEnabled || ghostBlend <= 0f || _ghostCompositeTex == null || !_ghostSeeded)
            {
                Graphics.Blit(src, dst);
                return;
            }

            m.SetFloat(ShaderProps.GhostEnabled, 1f);
            m.SetFloat(ShaderProps.GhostBlend, ghostBlend);
            m.SetVector(ShaderProps.GhostOffsetPx, ToShaderVector(ghostOffsetPx));
            m.SetFloat(ShaderProps.CombineMode, (float)ghostCombineMode);

            SetVirtualGridParams(m);
            m.SetTexture(ShaderProps.PrevTex, _ghostCompositeTex);

            Graphics.Blit(src, dst, m);
        }

        private void RunBleed(RenderTexture src, RenderTexture dst)
        {
            var m = MBleed;
            if (!m || bleedBlend <= 0f || bleedIntensity <= 0f) { Graphics.Blit(src, dst); return; }

            m.SetFloat(ShaderProps.BleedBlend, bleedBlend);
            m.SetFloat(ShaderProps.BleedIntensity, bleedIntensity);
            m.SetFloat(ShaderProps.BleedMode, (float)bleedMode);
            m.SetFloat(ShaderProps.BlendMode, (float)bleedBlendMode);
            
            m.SetVector(ShaderProps.ShiftR, ToShaderVector(shiftR));
            m.SetVector(ShaderProps.ShiftG, ToShaderVector(shiftG));
            m.SetVector(ShaderProps.ShiftB, ToShaderVector(shiftB));

            m.SetFloat(ShaderProps.EdgeOnly, bleedEdgeOnly ? 1f : 0f);
            m.SetFloat(ShaderProps.EdgeThreshold, bleedEdgeThreshold);
            m.SetFloat(ShaderProps.EdgePower, bleedEdgePower);

            m.SetVector(ShaderProps.RadialCenter, ToShaderVector(bleedRadialCenter));
            m.SetFloat(ShaderProps.RadialStrength, bleedRadialStrength);

            m.SetFloat(ShaderProps.Samples, GetEffectiveBleedSamples());
            m.SetFloat(ShaderProps.Smear, bleedSmear);
            m.SetFloat(ShaderProps.Falloff, bleedFalloff);

            m.SetFloat(ShaderProps.IntensityR, bleedIntensityR);
            m.SetFloat(ShaderProps.IntensityG, bleedIntensityG);
            m.SetFloat(ShaderProps.IntensityB, bleedIntensityB);
            m.SetVector(ShaderProps.Anamorphic, ToShaderVector(bleedAnamorphic));

            m.SetFloat(ShaderProps.ClampUV, bleedClampUV ? 1f : 0f);
            m.SetFloat(ShaderProps.PreserveLuma, bleedPreserveLuma ? 1f : 0f);

            m.SetFloat(ShaderProps.WobbleAmp, bleedWobbleAmp);
            m.SetFloat(ShaderProps.WobbleFreq, bleedWobbleFreq);
            m.SetFloat(ShaderProps.WobbleScanline, bleedWobbleScanline ? 1f : 0f);

            SetPixelGridParams(m);

            Graphics.Blit(src, dst, m);
        }

        private void RunUnsharp(RenderTexture src, RenderTexture dst)
        {
            var m = MUnsharp;
            if (!m || !unsharpEnabled || unsharpAmount <= 0f) { Graphics.Blit(src, dst); return; }

            m.SetFloat(ShaderProps.UnsharpEnabled, 1f);
            m.SetFloat(ShaderProps.UnsharpAmount, unsharpAmount);
            m.SetFloat(ShaderProps.UnsharpRadius, Mathf.Max(0.25f, unsharpRadius));
            m.SetFloat(ShaderProps.UnsharpThreshold, Mathf.Max(0f, unsharpThreshold));
            m.SetFloat(ShaderProps.UnsharpLumaOnly, unsharpLumaOnly ? 1f : 0f);
            m.SetFloat(ShaderProps.UnsharpChroma, unsharpChroma);
            m.SetFloat(ShaderProps.SharpenMode, (float)sharpenMode);

            SetVirtualGridParams(m);

            Graphics.Blit(src, dst, m);
        }

        private void RunPosterizeTone(RenderTexture src, RenderTexture dst)
        {
            var m = MPosterize;
            if (!m) { Graphics.Blit(src, dst); return; }

            m.SetFloat(ShaderProps.LuminanceOnly, luminanceOnly ? 1f : 0f);

            Graphics.Blit(src, dst, m);
        }

        private void RunDithering(RenderTexture src, RenderTexture dst)
        {
            var m = MDither;
            if (!m) { Graphics.Blit(src, dst); return; }

            m.SetFloat(ShaderProps.Levels, Mathf.Max(2, levels));
            m.SetFloat(ShaderProps.UsePerChannel, usePerChannel ? 1f : 0f);
            m.SetFloat(ShaderProps.LevelsR, Mathf.Max(2, levelsR));
            m.SetFloat(ShaderProps.LevelsG, Mathf.Max(2, levelsG));
            m.SetFloat(ShaderProps.LevelsB, Mathf.Max(2, levelsB));

            m.SetFloat(ShaderProps.AnimateLevels, animateLevels ? 1f : 0f);
            m.SetFloat(ShaderProps.MinLevels, Mathf.Max(2, minLevels));
            m.SetFloat(ShaderProps.MaxLevels, Mathf.Max(2, maxLevels));
            m.SetFloat(ShaderProps.Speed, speed);
            m.SetFloat(ShaderProps.LuminanceOnly, luminanceOnly ? 1f : 0f);

            m.SetFloat(ShaderProps.DitherMode, (float)ditherMode);
            m.SetFloat(ShaderProps.DitherStrength, (ditherMode == DitherMode.None) ? 0f : ditherStrength);
            m.SetFloat(ShaderProps.DitherAngle, Mathf.Repeat(ditherAngle, 180f));

            m.SetTexture(ShaderProps.BlueNoise,
                (ditherMode == DitherMode.BlueNoise && blueNoise != null) ? blueNoise : Texture2D.grayTexture);
            m.SetFloat(ShaderProps.TemporalDither, temporalDither ? 1f : 0f);
            m.SetFloat(ShaderProps.TemporalDitherRate, temporalDitherRate);
            m.SetFloat(ShaderProps.HalftoneScale, halftoneScale);
            m.SetFloat(ShaderProps.HalftoneDotGain, halftoneDotGain);

            SetPixelGridParams(m);

            Graphics.Blit(src, dst, m);
        }

        private void RunPaletteMapping(RenderTexture src, RenderTexture dst)
        {
            var m = MPalette;
            if (!m) { Graphics.Blit(src, dst); return; }

            m.SetTexture(ShaderProps.ThresholdTex, _curveTex ? _curveTex : Texture2D.whiteTexture);
            m.SetFloat(ShaderProps.UseThreshold, IsThresholdCurveActive() ? 1f : 0f);

            bool paletteOn = usePalette && paletteTex != null;
            m.SetFloat(ShaderProps.UsePalette, paletteOn ? 1f : 0f);
            m.SetFloat(ShaderProps.PaletteMode, (float)paletteMode);
            m.SetTexture(ShaderProps.PaletteTex, paletteTex != null ? paletteTex : Texture2D.whiteTexture);
            m.SetInt(ShaderProps.PaletteColorCount, GetEffectivePaletteColorCount());
            m.SetFloat(ShaderProps.PalettePerceptual, palettePerceptual ? 1f : 0f);

            m.SetFloat(ShaderProps.Invert, invert ? 1f : 0f);

            Graphics.Blit(src, dst, m);
        }

        private void RunEdges(RenderTexture src, RenderTexture dst)
        {
            var m = MEdges;
            if (!m || !edgeEnabled || edgeBlend <= 0f) { Graphics.Blit(src, dst); return; }

            m.SetFloat(ShaderProps.EdgeEnabled, 1f);
            m.SetFloat(ShaderProps.EdgeStrength, edgeStrength);
            m.SetFloat(ShaderProps.EdgeThreshold, edgeThreshold);
            m.SetFloat(ShaderProps.EdgeBlend, edgeBlend);
            m.SetColor(ShaderProps.EdgeColor, edgeColor);
            m.SetFloat(ShaderProps.EdgeThickness, edgeThickness);
            m.SetFloat(ShaderProps.EdgeUseNormals, edgeUseNormals ? 1f : 0f);
            m.SetFloat(ShaderProps.EdgeNormalThreshold, edgeNormalThreshold);

            SetVirtualGridParams(m);

            Graphics.Blit(src, dst, m);
        }

        private void RunTextureMask(RenderTexture baseTex, RenderTexture fxTex, RenderTexture dst)
        {
            var m = MTextureMask;
            if (!m) { Graphics.Blit(fxTex, dst); return; }

            bool enabled = useMask && maskTex != null;
            m.SetFloat(ShaderProps.UseMask, enabled ? 1f : 0f);

            m.SetTexture(ShaderProps.MaskTex, maskTex != null ? maskTex : Texture2D.whiteTexture);
            m.SetFloat(ShaderProps.MaskThreshold, maskThreshold);
            m.SetFloat(ShaderProps.MaskSoftness, maskSoftness);
            m.SetFloat(ShaderProps.MaskOpacity, maskOpacity);
            m.SetFloat(ShaderProps.MaskInvert, maskInvert ? 1f : 0f);
            m.SetFloat(ShaderProps.MaskChannel, (float)maskChannel);
            m.SetVector(ShaderProps.MaskTransform, new Vector4(maskTiling.x, maskTiling.y, maskOffset.x, maskOffset.y));
            m.SetTexture(ShaderProps.MaskedTex, fxTex);

            Graphics.Blit(baseTex, dst, m);
        }

        private void RunDepthMask(RenderTexture baseTex, RenderTexture fxTex, RenderTexture dst)
        {
            var m = MDepthMask;
            if (!m) { Graphics.Blit(fxTex, dst); return; }

            m.SetFloat(ShaderProps.UseDepthMask, useDepthMask ? 1f : 0f);
            m.SetFloat(ShaderProps.DepthThreshold, Mathf.Max(0f, depthThreshold));
            m.SetFloat(ShaderProps.DepthFar, Mathf.Max(depthThreshold, depthFar));
            m.SetFloat(ShaderProps.DepthSoftness, Mathf.Max(0f, depthSoftness));
            m.SetFloat(ShaderProps.DepthOpacity, depthOpacity);
            m.SetFloat(ShaderProps.DepthInvert, depthInvert ? 1f : 0f);
            m.SetTexture(ShaderProps.MaskedTex, fxTex);

            Graphics.Blit(baseTex, dst, m);
        }

        private void RunProfessional(RenderTexture src, RenderTexture dst, ProfessionalMode mode)
        {
            var m = MProfessional;
            if (!m) { Graphics.Blit(src, dst); return; }

            float intensity = 0f;
            Vector4 a = Vector4.zero;
            Vector4 b = Vector4.zero;

            switch (mode)
            {
                case ProfessionalMode.LensSensor:
                    intensity = lensSensorIntensity;
                    a = new Vector4(lensDistortion, lensChromaticAberration, lensVignette, lensBloom);
                    b = new Vector4(sensorRollingShutter, sensorNoise, sensorDeadPixels, lensBloomRadius);
                    break;
                case ProfessionalMode.Film:
                    intensity = filmIntensity;
                    a = new Vector4(filmGrain, filmGrainSize, filmHalation, filmHalationRadius);
                    b = new Vector4(filmGateWeave, filmDust, filmScratches, filmFlicker);
                    break;
                case ProfessionalMode.MotionGlitch:
                    intensity = motionGlitchIntensity;
                    a = new Vector4(motionBlockSize, motionVectorDisplacement, motionFreezeRate, motionColorSplit);
                    b = new Vector4(motionHistoryScale, 0f, 0f, 0f);
                    break;
                case ProfessionalMode.DigitalVideo:
                    intensity = digitalVideoIntensity;
                    a = new Vector4(digitalBlockSize, digitalQuantization, digitalRinging, digitalChromaSubsampling);
                    b = new Vector4(digitalMosquitoNoise, digitalBitratePumping, 0f, 0f);
                    break;
                case ProfessionalMode.Composite:
                    intensity = compositeIntensity;
                    a = new Vector4(compositeDotCrawl, compositeRainbow, compositeChromaBandwidth, compositePhaseError);
                    b = new Vector4(compositeCombFilter, (float)compositeStandard, 0f, 0f);
                    break;
                case ProfessionalMode.Lcd:
                    intensity = lcdIntensity;
                    a = new Vector4(lcdPixelScale, lcdSubpixelStrength, lcdInversion, lcdViewingAngle);
                    b = new Vector4(lcdBacklightBleed, lcdResponseSmear, 0f, 0f);
                    break;
            }

            m.SetFloat(ShaderProps.ProfessionalMode, (float)mode);
            m.SetFloat(ShaderProps.EffectIntensity, intensity);
            m.SetVector(ShaderProps.ParamA, a);
            m.SetVector(ShaderProps.ParamB, b);
            m.SetTexture(ShaderProps.HistoryTex, _motionHistorySeeded && _motionHistoryTex != null ? _motionHistoryTex : src);
            Graphics.Blit(src, dst, m);
        }

        private void RunPresent(RenderTexture originalSrc, RenderTexture processed, RenderTexture dst)
        {
            var m = MPresent;
            if (!m) { Graphics.Blit(processed, dst); return; }

            m.SetTexture(ShaderProps.OriginalTex, originalSrc);
            m.SetFloat(ShaderProps.MasterBlend, masterBlend);

            Graphics.Blit(processed, dst, m);
        }

        private void UpgradeAnalogSettings()
        {
            if (_analogSettingsVersion >= CurrentAnalogSettingsVersion) return;

            // Migrate the weak v1.2 CRT defaults without overwriting an authored setup.
            if (Mathf.Approximately(crtScanlineStrength, 0.45f) && Mathf.Approximately(crtBeamWidth, 0.7f))
            {
                crtScanlineStrength = 0.75f;
                crtBeamWidth = 0.5f;
            }

            _analogSettingsVersion = CurrentAnalogSettingsVersion;
        }

        private void RunCrt(RenderTexture src, RenderTexture dst)
        {
            var m = MCrt;
            if (!m || !crtEnabled) { Graphics.Blit(src, dst); return; }

            m.SetFloat(ShaderProps.CrtCurvature, crtCurvature);
            m.SetFloat(ShaderProps.CrtOverscan, crtOverscan);
            m.SetFloat(ShaderProps.CrtScanlineStrength, crtScanlineStrength);
            m.SetFloat(ShaderProps.CrtScanlineCount, crtScanlineCount);
            m.SetFloat(ShaderProps.CrtBeamWidth, crtBeamWidth);
            m.SetFloat(ShaderProps.CrtMaskMode, (float)crtMaskMode);
            m.SetFloat(ShaderProps.CrtMaskStrength, crtMaskStrength);
            m.SetFloat(ShaderProps.CrtMaskScale, crtMaskScale);
            m.SetFloat(ShaderProps.CrtBloom, crtBloom);
            m.SetFloat(ShaderProps.CrtBloomRadius, crtBloomRadius);
            m.SetFloat(ShaderProps.CrtVignette, crtVignette);
            m.SetFloat(ShaderProps.CrtVignetteSoftness, crtVignetteSoftness);
            m.SetFloat(ShaderProps.CrtNoise, crtNoise);
            m.SetFloat(ShaderProps.CrtFlicker, crtFlicker);
            m.SetFloat(ShaderProps.CrtBrightness, crtBrightness);
            m.SetFloat(ShaderProps.CrtTubeEdge, crtTubeEdge);
            m.SetFloat(ShaderProps.CrtBloomThreshold, crtBloomThreshold);
            m.SetFloat(ShaderProps.CrtConvergence, crtConvergencePx);
            m.SetFloat(ShaderProps.CrtFocus, crtFocus);
            m.SetFloat(ShaderProps.CrtBlackLevel, crtBlackLevel);
            m.SetFloat(ShaderProps.CrtHumBar, crtHumBar);
            m.SetFloat(ShaderProps.CrtFlickerHz, crtFlickerHz);
            Graphics.Blit(src, dst, m);
        }

        private void RunVhs(RenderTexture src, RenderTexture dst)
        {
            var m = MVhs;
            if (!m || !vhsEnabled || vhsIntensity <= 0f) { Graphics.Blit(src, dst); return; }

            m.SetFloat(ShaderProps.VhsIntensity, vhsIntensity);
            m.SetFloat(ShaderProps.VhsTapeSpeed, vhsTapeSpeed);
            m.SetFloat(ShaderProps.VhsJitter, vhsHorizontalJitter);
            m.SetFloat(ShaderProps.VhsLineWobble, vhsLineWobble);
            m.SetFloat(ShaderProps.VhsTracking, vhsTracking);
            m.SetFloat(ShaderProps.VhsTrackingSpeed, vhsTrackingSpeed);
            m.SetFloat(ShaderProps.VhsTrackingWidth, vhsTrackingWidth);
            m.SetFloat(ShaderProps.VhsChromaBleed, vhsChromaBleed);
            m.SetFloat(ShaderProps.VhsChromaBlur, vhsChromaBlur);
            m.SetFloat(ShaderProps.VhsColorLoss, vhsColorLoss);
            m.SetFloat(ShaderProps.VhsLumaNoise, vhsLumaNoise);
            m.SetFloat(ShaderProps.VhsChromaNoise, vhsChromaNoise);
            m.SetFloat(ShaderProps.VhsDropout, vhsDropout);
            m.SetFloat(ShaderProps.VhsHeadSwitching, vhsHeadSwitching);
            m.SetFloat(ShaderProps.VhsHeadSwitchHeight, vhsHeadSwitchHeight);
            m.SetFloat(ShaderProps.VhsInterlace, vhsInterlace);
            m.SetFloat(ShaderProps.VhsStandard, (float)vhsStandard);
            m.SetFloat(ShaderProps.VhsTapeMode, (float)vhsTapeMode);
            m.SetFloat(ShaderProps.VhsGeneration, vhsGeneration);
            m.SetFloat(ShaderProps.VhsAgc, vhsAgcInstability);
            m.SetFloat(ShaderProps.VhsVerticalChroma, vhsVerticalChromaBlur);
            Graphics.Blit(src, dst, m);
        }

        private void EnsureGhostResources(RenderTexture src)
        {
            if (!ghostEnabled)
            {
                ReleaseGhostResources();
                return;
            }

            int sourceW = Mathf.Max(1, src ? src.width : Screen.width);
            int sourceH = Mathf.Max(1, src ? src.height : Screen.height);
            float scale = GetEffectiveGhostResolutionScale();
            int w = Mathf.Max(1, Mathf.RoundToInt(sourceW * scale));
            int h = Mathf.Max(1, Mathf.RoundToInt(sourceH * scale));

            int n = Mathf.Clamp(ghostFrames, 1, 16);
            var ghostDesc = BuildGhostDescriptor(src, w, h);

            bool needsRing = (_ghostRing == null || _ghostRing.Length != n);
            bool needsResize = false;

            if (!needsRing && _ghostRing != null && _ghostRing.Length > 0 && _ghostRing[0] != null)
                needsResize =
                    (_ghostRing[0].width != w || _ghostRing[0].height != h) ||
                    (_ghostRing[0].graphicsFormat != ghostDesc.graphicsFormat);

            if (!needsRing && !needsResize && _ghostCompositeTex != null)
                needsResize =
                    (_ghostCompositeTex.width != w || _ghostCompositeTex.height != h) ||
                    (_ghostCompositeTex.graphicsFormat != ghostDesc.graphicsFormat);

            if (!needsRing && !needsResize) return;

            ReleaseGhostResources();

            _ghostRing = new RenderTexture[n];
            for (int i = 0; i < n; i++)
            {
                _ghostRing[i] = CreateGhostRT(ghostDesc);
                Graphics.Blit(Texture2D.blackTexture, _ghostRing[i]);
            }

            _ghostCompositeTex = CreateGhostRT(ghostDesc);
            Graphics.Blit(Texture2D.blackTexture, _ghostCompositeTex);

            _ghostWriteIndex = 0;
            _ghostCaptureElapsed = 0f;
            _ghostSeeded = false;
        }

        private void EnsureMotionHistory(RenderTexture src)
        {
            if (!motionGlitchEnabled || src == null)
            {
                SafeReleaseAndDestroyRT(ref _motionHistoryTex);
                _motionHistorySeeded = false;
                _motionHistoryNextCaptureTime = 0f;
                return;
            }

            int w = Mathf.Max(1, Mathf.RoundToInt(src.width * Mathf.Clamp(motionHistoryScale, 0.25f, 1f)));
            int h = Mathf.Max(1, Mathf.RoundToInt(src.height * Mathf.Clamp(motionHistoryScale, 0.25f, 1f)));
            bool recreate = _motionHistoryTex == null || _motionHistoryTex.width != w || _motionHistoryTex.height != h;
            if (!recreate) return;

            SafeReleaseAndDestroyRT(ref _motionHistoryTex);
            var desc = BuildGhostDescriptor(src, w, h);
            _motionHistoryTex = CreateGhostRT(desc);
            Graphics.Blit(Texture2D.blackTexture, _motionHistoryTex);
            _motionHistorySeeded = false;
            _motionHistoryNextCaptureTime = 0f;
        }

        private void CaptureMotionHistory(RenderTexture source)
        {
            if (_motionHistoryTex == null || source == null) return;
            float now = Time.realtimeSinceStartup;
            if (_motionHistorySeeded && now < _motionHistoryNextCaptureTime) return;
            Graphics.Blit(source, _motionHistoryTex);
            _motionHistorySeeded = true;
            _motionHistoryNextCaptureTime = now + 1f / Mathf.Clamp(motionHistoryFps, 1f, 60f);
        }

        private static RenderTextureDescriptor BuildGhostDescriptor(RenderTexture src, int w, int h)
        {
            var desc = src != null
                ? src.descriptor
                : new RenderTextureDescriptor(w, h, RenderTextureFormat.ARGB32, 0);

            desc.width = w;
            desc.height = h;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.useMipMap = false;
            desc.autoGenerateMips = false;
            desc.enableRandomWrite = false;
            desc.bindMS = false;
            desc.memoryless = RenderTextureMemoryless.None;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                desc.colorFormat = RenderTextureFormat.ARGBHalf;

            return desc;
        }

        private static RenderTexture CreateGhostRT(RenderTextureDescriptor desc)
        {
            var rt = new RenderTexture(desc)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            rt.Create();
            return rt;
        }

        private static void SafeReleaseAndDestroyRT(ref RenderTexture rt)
        {
            if (rt == null) return;
            if (RenderTexture.active == rt) RenderTexture.active = null;
            rt.Release();
            DestroyImmediate(rt);
            rt = null;
        }

        private void ReleaseGhostResources()
        {
            if (_ghostRing != null)
            {
                for (int i = 0; i < _ghostRing.Length; i++)
                    SafeReleaseAndDestroyRT(ref _ghostRing[i]);
                _ghostRing = null;
            }

            SafeReleaseAndDestroyRT(ref _ghostCompositeTex);
            _ghostSeeded = false;
        }

        private void BuildGhostComposite()
        {
            if (!_ghostSeeded || !ghostEnabled || ghostBlend <= 0f || _ghostRing == null || _ghostRing.Length == 0 || _ghostCompositeTex == null)
            {
                if (_ghostCompositeTex != null) Graphics.Blit(Texture2D.blackTexture, _ghostCompositeTex);
                return;
            }

            var m = MGhostComposite;
            if (!m) { Graphics.Blit(Texture2D.blackTexture, _ghostCompositeTex); return; }

            int n = _ghostRing.Length;
            int start = Mathf.Clamp(ghostStartDelay, 0, n - 1);

            for (int i = 0; i < ShaderProps.Hist.Length; i++)
                m.SetTexture(ShaderProps.Hist[i], Texture2D.blackTexture);

            int count = 0;
            for (int k = start; k < n && count < ShaderProps.Hist.Length; k++)
            {
                int idx = WrapIndex(_ghostWriteIndex - 1 - k, n);
                var rt = _ghostRing[idx];
                m.SetTexture(ShaderProps.Hist[count], rt != null ? rt : Texture2D.blackTexture);
                count++;
            }

            m.SetInt(ShaderProps.Count, count);
            m.SetFloat(ShaderProps.WeightCurve, ghostWeightCurve);
            float decayPerTap = Mathf.Exp(-ghostFrameIntervalMs / Mathf.Max(ghostDecayMs, 1f));
            m.SetFloat(ShaderProps.DecayPerTap, decayPerTap);

            Graphics.Blit(Texture2D.blackTexture, _ghostCompositeTex, m);
        }

        private void CaptureGhostFrame(RenderTexture frameTex)
        {
            if (!ghostEnabled || _ghostRing == null || _ghostRing.Length == 0 || frameTex == null)
                return;

            if (!_ghostSeeded)
            {
                for (int i = 0; i < _ghostRing.Length; i++)
                    if (_ghostRing[i] != null) Graphics.Blit(frameTex, _ghostRing[i]);

                if (_ghostCompositeTex != null) Graphics.Blit(frameTex, _ghostCompositeTex);

                _ghostWriteIndex = 0;
                _ghostCaptureElapsed = 0f;
                _ghostSeeded = true;
                return;
            }

            float deltaMs = Mathf.Max(Time.unscaledDeltaTime, 1f / 240f) * 1000f;
            _ghostCaptureElapsed += deltaMs;
            if (_ghostCaptureElapsed < ghostFrameIntervalMs) return;
            _ghostCaptureElapsed %= ghostFrameIntervalMs;

            var rt = _ghostRing[_ghostWriteIndex];
            if (rt != null) Graphics.Blit(frameTex, rt);

            _ghostWriteIndex = WrapIndex(_ghostWriteIndex + 1, _ghostRing.Length);
        }

        private static int WrapIndex(int x, int n)
        {
            if (n <= 0) return 0;
            x %= n;
            if (x < 0) x += n;
            return x;
        }

        private void EnsureDepthModeIfNeeded()
        {
            var cam = GetComponent<Camera>();
            if (!cam) return;
            if (useDepthMask || edgeEnabled) cam.depthTextureMode |= DepthTextureMode.Depth;
            if (edgeEnabled && edgeUseNormals) cam.depthTextureMode |= DepthTextureMode.DepthNormals;
            if (motionGlitchEnabled) cam.depthTextureMode |= DepthTextureMode.MotionVectors;
        }

        private void UpdateCurveTexture()
        {
            if (_curveTex == null || _curveTex.width != 256)
            {
                _curveTex = new Texture2D(256, 1, TextureFormat.RFloat, false, true)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            if (thresholdCurve == null)
                thresholdCurve = AnimationCurve.Linear(0, 0, 1, 1);

            for (int i = 0; i < 256; i++)
            {
                float t = i / 255f;
                float v = Mathf.Clamp01(thresholdCurve.Evaluate(t));
                _curveTex.SetPixel(i, 0, new Color(v, 0, 0, 0));
            }

            _curveTex.Apply(false, false);
        }

        private Material MSampling => GetOrCreate(ref _mSampling, samplingGridShader, "Hidden/CrowFX/Stages/SamplingGrid");
        private Material MPregrade => GetOrCreate(ref _mPregrade, pregradeShader, "Hidden/CrowFX/Stages/Pregrade");
        private Material MJitter => GetOrCreate(ref _mJitter, channelJitterShader, "Hidden/CrowFX/Stages/ChannelJitter");
        private Material MGhosting => GetOrCreate(ref _mGhosting, ghostingShader, "Hidden/CrowFX/Stages/Ghosting");
        private Material MBleed => GetOrCreate(ref _mBleed, rgbBleedingShader, "Hidden/CrowFX/Stages/RGBBleeding");
        private Material MUnsharp => GetOrCreate(ref _mUnsharp, unsharpMaskShader, "Hidden/CrowFX/Stages/UnsharpMask");
        private Material MPosterize => GetOrCreate(ref _mPosterize, posterizeToneShader, "Hidden/CrowFX/Stages/PosterizeTone");
        private Material MDither => GetOrCreate(ref _mDither, ditheringShader, "Hidden/CrowFX/Stages/Dithering");
        private Material MPalette => GetOrCreate(ref _mPalette, paletteMappingShader, "Hidden/CrowFX/Stages/PaletteMapping");
        private Material MEdges => GetOrCreate(ref _mEdges, edgeOutlineShader, "Hidden/CrowFX/Stages/EdgeOutline");
        private Material MCrt => GetOrCreate(ref _mCrt, crtDisplayShader, "Hidden/CrowFX/Stages/CRTDisplay");
        private Material MVhs => GetOrCreate(ref _mVhs, vhsTapeShader, "Hidden/CrowFX/Stages/VHSTape");
        private Material MPresent => GetOrCreate(ref _mPresent, masterPresentShader, "Hidden/CrowFX/Stages/MasterPresent");

        private Material MTextureMask => GetOrCreate(ref _mTexMask, textureMaskShader, "Hidden/CrowFX/Stages/TextureMask");
        private Material MDepthMask => GetOrCreate(ref _mDepthMask, depthMaskShader, "Hidden/CrowFX/Stages/DepthMask");

        private Material MGhostComposite => GetOrCreate(ref _mGhostComposite, ghostCompositeShader, "Hidden/CrowFX/Helpers/GhostComposite");
        private Material MProfessional => GetOrCreate(ref _mProfessional, professionalEffectsShader, "Hidden/CrowFX/Stages/ProfessionalEffects");

        private static Material GetOrCreate(ref Material mat, Shader assigned, string fallbackName)
        {
            if (mat) return mat;

            Shader s = assigned ? assigned : Shader.Find(fallbackName);
            if (!s) return null;

            mat = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
            return mat;
        }

        private static void DestroyMat(ref Material m)
        {
            if (m) { DestroyImmediate(m); m = null; }
        }

        private static void Swap(ref RenderTexture a, ref RenderTexture b)
        {
            var tmp = a; a = b; b = tmp;
        }

        private void AutoAssignShadersIfMissing()
        {
            samplingGridShader = AutoShader(samplingGridShader, "Hidden/CrowFX/Stages/SamplingGrid");
            pregradeShader = AutoShader(pregradeShader, "Hidden/CrowFX/Stages/Pregrade");
            channelJitterShader = AutoShader(channelJitterShader, "Hidden/CrowFX/Stages/ChannelJitter");
            ghostingShader = AutoShader(ghostingShader, "Hidden/CrowFX/Stages/Ghosting");
            rgbBleedingShader = AutoShader(rgbBleedingShader, "Hidden/CrowFX/Stages/RGBBleeding");
            unsharpMaskShader = AutoShader(unsharpMaskShader, "Hidden/CrowFX/Stages/UnsharpMask");
            posterizeToneShader = AutoShader(posterizeToneShader, "Hidden/CrowFX/Stages/PosterizeTone");
            ditheringShader = AutoShader(ditheringShader, "Hidden/CrowFX/Stages/Dithering");
            paletteMappingShader = AutoShader(paletteMappingShader, "Hidden/CrowFX/Stages/PaletteMapping");
            edgeOutlineShader = AutoShader(edgeOutlineShader, "Hidden/CrowFX/Stages/EdgeOutline");
            masterPresentShader = AutoShader(masterPresentShader, "Hidden/CrowFX/Stages/MasterPresent");

            textureMaskShader = AutoShader(textureMaskShader, "Hidden/CrowFX/Stages/TextureMask");
            depthMaskShader = AutoShader(depthMaskShader, "Hidden/CrowFX/Stages/DepthMask");

            ghostCompositeShader = AutoShader(ghostCompositeShader, "Hidden/CrowFX/Helpers/GhostComposite");
            crtDisplayShader = AutoShader(crtDisplayShader, "Hidden/CrowFX/Stages/CRTDisplay");
            vhsTapeShader = AutoShader(vhsTapeShader, "Hidden/CrowFX/Stages/VHSTape");
            professionalEffectsShader = AutoShader(professionalEffectsShader, "Hidden/CrowFX/Stages/ProfessionalEffects");
        }

        private static Shader AutoShader(Shader current, string shaderName)
        {
            if (current != null) return current;
            return Shader.Find(shaderName);
        }
    }
}
