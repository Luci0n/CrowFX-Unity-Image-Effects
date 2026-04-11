using System;
using UnityEngine;
using CrowFX.Helpers;
using SectionKeys = CrowFX.Helpers.CrowFXSectionKeys;

namespace CrowFX
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Image Effects/CrowFX/Crow Image Effects")]

    [EffectSectionMeta(SectionKeys.Master,      title: "Master",            icon: "d_Settings",             hint: "Global Blend",         order: 0,  defaultExpanded: true)]
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
    [EffectSectionMeta(SectionKeys.Shaders,     title: "Shaders",           icon: "d_Shader Icon",          hint: "Advanced",             order: 1000, defaultExpanded: false)]
    public sealed class CrowImageEffects : MonoBehaviour
    {
        public enum DitherMode { None = 0, Ordered2x2 = 1, Ordered4x4 = 2, Ordered8x8 = 3, Noise = 4, BlueNoise = 5 }
        public enum GhostCombineMode { Mix = 0, Add = 1, Screen = 2, Max = 3 }
        public enum BleedMode { Manual = 0, Radial = 1 }
        public enum BleedBlendMode { Mix = 0, Add = 1, Screen = 2, Max = 3 }

        private static class ShaderProps
        {
            public static readonly int PixelSize = Shader.PropertyToID("_PixelSize");
            public static readonly int UseVirtualGrid = Shader.PropertyToID("_UseVirtualGrid");
            public static readonly int VirtualRes = Shader.PropertyToID("_VirtualRes");

            public static readonly int PregradeEnabled = Shader.PropertyToID("_PregradeEnabled");
            public static readonly int Exposure = Shader.PropertyToID("_Exposure");
            public static readonly int Contrast = Shader.PropertyToID("_Contrast");
            public static readonly int Gamma = Shader.PropertyToID("_Gamma");
            public static readonly int Saturation = Shader.PropertyToID("_Saturation");

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
            public static readonly int BlueNoise = Shader.PropertyToID("_BlueNoise");

            public static readonly int ThresholdTex = Shader.PropertyToID("_ThresholdTex");
            public static readonly int UsePalette = Shader.PropertyToID("_UsePalette");
            public static readonly int PaletteTex = Shader.PropertyToID("_PaletteTex");
            public static readonly int Invert = Shader.PropertyToID("_Invert");

            public static readonly int EdgeEnabled = Shader.PropertyToID("_EdgeEnabled");
            public static readonly int EdgeStrength = Shader.PropertyToID("_EdgeStrength");
            public static readonly int EdgeBlend = Shader.PropertyToID("_EdgeBlend");
            public static readonly int EdgeColor = Shader.PropertyToID("_EdgeColor");

            public static readonly int UseMask = Shader.PropertyToID("_UseMask");
            public static readonly int MaskTex = Shader.PropertyToID("_MaskTex");
            public static readonly int MaskThreshold = Shader.PropertyToID("_MaskThreshold");
            public static readonly int MaskedTex = Shader.PropertyToID("_MaskedTex");

            public static readonly int UseDepthMask = Shader.PropertyToID("_UseDepthMask");
            public static readonly int DepthThreshold = Shader.PropertyToID("_DepthThreshold");

            public static readonly int OriginalTex = Shader.PropertyToID("_OriginalTex");
            public static readonly int MasterBlend = Shader.PropertyToID("_MasterBlend");

            public static readonly int Count = Shader.PropertyToID("_Count");
            public static readonly int WeightCurve = Shader.PropertyToID("_WeightCurve");
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

        // -------------------- Pregrade --------------------
        [EffectSection(SectionKeys.Pregrade, 0)][Tooltip("Enable exposure, contrast, gamma, and saturation adjustments before posterization.")] public bool pregradeEnabled = false;
        [EffectSection(SectionKeys.Pregrade, 10)][Tooltip("Brightness adjustment applied before posterization.")][Range(-5f, 5f)] public float exposure = 0f;
        [EffectSection(SectionKeys.Pregrade, 20)][Tooltip("Contrast multiplier applied before posterization.")][Range(0f, 2f)] public float contrast = 1f;
        [EffectSection(SectionKeys.Pregrade, 30)][Tooltip("Gamma correction applied before posterization.")][Range(0.1f, 3f)] public float gamma = 1f;
        [EffectSection(SectionKeys.Pregrade, 40)][Tooltip("Color saturation applied before posterization.")][Range(0f, 2f)] public float saturation = 1f;

        // -------------------- Posterize --------------------
        [EffectSection(SectionKeys.Posterize, 0)][Tooltip("Shared number of quantization levels for all channels.")][Range(2, 512)] public int levels = 64;
        [EffectSection(SectionKeys.Posterize, 10)][Tooltip("Use independent quantization levels for red, green, and blue.")] public bool usePerChannel = false;
        [EffectSection(SectionKeys.Posterize, 20)][Tooltip("Quantization levels for the red channel.")][Range(2, 512)] public int levelsR = 64;
        [EffectSection(SectionKeys.Posterize, 30)][Tooltip("Quantization levels for the green channel.")][Range(2, 512)] public int levelsG = 64;
        [EffectSection(SectionKeys.Posterize, 40)][Tooltip("Quantization levels for the blue channel.")][Range(2, 512)] public int levelsB = 64;

        [EffectSection(SectionKeys.Posterize, 50)][Tooltip("Animate the shared quantization level count over time.")] public bool animateLevels = false;
        [EffectSection(SectionKeys.Posterize, 60)][Tooltip("Lower bound used when Animated Levels is enabled.")][Range(2, 512)] public int minLevels = 64;
        [EffectSection(SectionKeys.Posterize, 70)][Tooltip("Upper bound used when Animated Levels is enabled.")][Range(2, 512)] public int maxLevels = 64;
        [EffectSection(SectionKeys.Posterize, 80)][Tooltip("Animation speed for cycling quantization levels.")] public float speed = 1f;

        [EffectSection(SectionKeys.Posterize, 90)][Tooltip("Posterize luminance while preserving overall color relationships.")] public bool luminanceOnly = false;
        [EffectSection(SectionKeys.Posterize, 100)][Tooltip("Invert the posterized output colors.")] public bool invert = false;

        // -------------------- Palette / Curve --------------------
        [EffectSection(SectionKeys.Palette, 0)][Tooltip("Map final colors through a palette texture.")] public bool usePalette = false;
        [EffectSection(SectionKeys.Palette, 10)][Tooltip("Palette lookup texture used when palette mapping is enabled.")] public Texture2D paletteTex;
        [EffectSection(SectionKeys.Palette, 20)][Tooltip("Remap tonal values before palette lookup.")] public AnimationCurve thresholdCurve = AnimationCurve.Linear(0, 0, 1, 1);

        // -------------------- Masks --------------------
        [EffectSection(SectionKeys.TextureMask, 0)][Tooltip("Enable a texture mask to blend between processed and original image.")] public bool useMask = false;
        [EffectSection(SectionKeys.TextureMask, 10)][Tooltip("Grayscale mask texture. White keeps the effect; black restores the source.")] public Texture2D maskTex;
        [EffectSection(SectionKeys.TextureMask, 20)][Tooltip("Threshold used to cut between masked and unmasked areas.")][Range(0, 1)] public float maskThreshold = 0.5f;

        [EffectSection(SectionKeys.DepthMask, 0)][Tooltip("Attenuate the effect based on scene depth.")] public bool useDepthMask = false;
        [EffectSection(SectionKeys.DepthMask, 10)][Tooltip("Depth distance where the mask starts attenuating the effect.")][Range(0.0f, 10.0f)] public float depthThreshold = 1.0f;

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
        [EffectSection(SectionKeys.Ghost, 10)][Tooltip("Blend amount of the accumulated history.")][Range(0f, 1f)] public float ghostBlend = 0.35f;
        [EffectSection(SectionKeys.Ghost, 20)][Tooltip("Per-frame offset applied between stored history frames.")] public Vector2 ghostOffsetPx = Vector2.zero;

        [EffectSection(SectionKeys.Ghost, 30)][Tooltip("Number of previous frames to store in history.")][Range(1, 16)] public int ghostFrames = 4;
        [EffectSection(SectionKeys.Ghost, 40)][Tooltip("Frames to skip between history captures.")][Range(0, 8)] public int ghostCaptureInterval = 0;
        [EffectSection(SectionKeys.Ghost, 50)][Tooltip("Delay before the first ghost frame appears.")][Range(0, 8)] public int ghostStartDelay = 0;
        [EffectSection(SectionKeys.Ghost, 60)][Tooltip("Bias toward newer or older frames in the composite.")][Range(0.25f, 4f)] public float ghostWeightCurve = 1.5f;

        [EffectSection(SectionKeys.Ghost, 70)][Tooltip("How the history composite blends with the current frame.")] public GhostCombineMode ghostCombineMode = GhostCombineMode.Screen;

        // -------------------- Unsharp --------------------
        [EffectSection(SectionKeys.Unsharp, 0)][Tooltip("Enable the unsharp mask sharpening pass.")] public bool unsharpEnabled = false;
        [EffectSection(SectionKeys.Unsharp, 10)][Tooltip("Strength of the sharpening effect.")][Range(0f, 3f)] public float unsharpAmount = 0.5f;
        [EffectSection(SectionKeys.Unsharp, 20)][Tooltip("Radius of the blur used to build the sharpen mask.")][Range(0.25f, 4f)] public float unsharpRadius = 1.0f;
        [EffectSection(SectionKeys.Unsharp, 30)][Tooltip("Ignore smaller differences to reduce sharpening of noise.")][Range(0f, 0.25f)] public float unsharpThreshold = 0.0f;

        [EffectSection(SectionKeys.Unsharp, 40)][Tooltip("Sharpen luminance only and keep color sharpening separate.")] public bool unsharpLumaOnly = false;
        [EffectSection(SectionKeys.Unsharp, 50)][Tooltip("Additional sharpening applied to chroma when Luma Only is enabled.")][Range(0f, 1f)] public float unsharpChroma = 0.0f;

        // -------------------- Edge Outline --------------------
        [EffectSection(SectionKeys.Edges, 0)][Tooltip("Enable depth-based outlines.")] public bool edgeEnabled = false;
        [EffectSection(SectionKeys.Edges, 10)][Tooltip("Strength of the outline detection.")][Range(0f, 8f)] public float edgeStrength = 1f;
        [EffectSection(SectionKeys.Edges, 20)][Tooltip("Depth difference required to create an edge.")][Range(0f, 1f)] public float edgeThreshold = 0.02f;
        [EffectSection(SectionKeys.Edges, 30)][Tooltip("Blend amount of the outline pass.")][Range(0f, 1f)] public float edgeBlend = 1f;
        [EffectSection(SectionKeys.Edges, 40)][Tooltip("Tint used for the outline.")] public Color edgeColor = Color.black;

        // -------------------- Dithering --------------------
        [EffectSection(SectionKeys.Dither, 0)][Tooltip("Pattern used for dithering before final quantization.")] public DitherMode ditherMode = DitherMode.None;
        [EffectSection(SectionKeys.Dither, 10)][Tooltip("How strongly the dither pattern affects the image.")][Range(0f, 1f)] public float ditherStrength = 0.0f;
        [EffectSection(SectionKeys.Dither, 20)][Tooltip("Blue-noise texture used by Blue Noise mode.")] public Texture2D blueNoise;

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

        // -------------------- Materials --------------------
        private Material _mSampling, _mPregrade, _mJitter, _mGhosting, _mBleed, _mUnsharp, _mPosterize, _mDither, _mPalette, _mEdges, _mPresent;
        private Material _mTexMask, _mDepthMask;
        private Material _mGhostComposite;

        // -------------------- Curve texture --------------------
        private Texture2D _curveTex;

        // -------------------- Ghost history --------------------
        private RenderTexture[] _ghostRing;
        private int _ghostWriteIndex;
        private int _ghostCaptureCounter;
        private RenderTexture _ghostCompositeTex;
        private bool _ghostSeeded;

        private void OnEnable()
        {
            AutoAssignShadersIfMissing();
            ApplyAssignedProfileIfEnabled();
            ClampAndSanitize();
            UpdateCurveTexture();
            EnsureDepthModeIfNeeded();
            EnsureGhostResources(null);
            _ghostSeeded = false;
        }

        private void OnValidate()
        {
            AutoAssignShadersIfMissing();
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
            DestroyMat(ref _mPresent);
            DestroyMat(ref _mTexMask);
            DestroyMat(ref _mDepthMask);
            DestroyMat(ref _mGhostComposite);

            if (_curveTex) DestroyImmediate(_curveTex);
            ReleaseGhostResources();
        }

        private void ClampAndSanitize()
        {
            virtualResolution.x = Mathf.Max(1, virtualResolution.x);
            virtualResolution.y = Mathf.Max(1, virtualResolution.y);

            gamma = Mathf.Max(0.1f, gamma);

            ghostFrames = Mathf.Clamp(ghostFrames, 1, 16);
            ghostCaptureInterval = Mathf.Clamp(ghostCaptureInterval, 0, 8);
            ghostStartDelay = Mathf.Clamp(ghostStartDelay, 0, 8);
            ghostWeightCurve = Mathf.Clamp(ghostWeightCurve, 0.25f, 4f);

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

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (src == null) { Graphics.Blit(src, dest); return; }

            EnsureDepthModeIfNeeded();
            EnsureGhostResources(src);

            var desc = src.descriptor;
            desc.depthBufferBits = 0;

            RenderTexture a = null;
            RenderTexture b = null;
            RenderTexture baseAnchor = null;
            bool needsMaskedBase = useMask || useDepthMask;

            try
            {
                a = RenderTexture.GetTemporary(desc);
                b = RenderTexture.GetTemporary(desc);

                Graphics.Blit(src, a);

                RunSamplingGrid(a, b); Swap(ref a, ref b);
                RunPregrade(a, b); Swap(ref a, ref b);

                if (needsMaskedBase)
                {
                    baseAnchor = RenderTexture.GetTemporary(desc);
                    Graphics.Blit(a, baseAnchor);
                }

                RunChannelJitter(a, b); Swap(ref a, ref b);

                BuildGhostComposite();
                RunGhosting(a, b); Swap(ref a, ref b);

                RunBleed(a, b); Swap(ref a, ref b);
                RunUnsharp(a, b); Swap(ref a, ref b);

                RunPosterizeTone(a, b); Swap(ref a, ref b);
                RunDithering(a, b); Swap(ref a, ref b);
                RunPaletteMapping(a, b); Swap(ref a, ref b);
                RunEdges(a, b); Swap(ref a, ref b);

                if (useMask) { RunTextureMask(baseAnchor, a, b); Swap(ref a, ref b); }
                if (useDepthMask) { RunDepthMask(baseAnchor, a, b); Swap(ref a, ref b); }

                RunPresent(src, a, dest);
                CaptureGhostFrame(a);
            }
            finally
            {
                if (baseAnchor != null) RenderTexture.ReleaseTemporary(baseAnchor);
                if (a != null) RenderTexture.ReleaseTemporary(a);
                if (b != null) RenderTexture.ReleaseTemporary(b);
            }
        }

        private void RunSamplingGrid(RenderTexture src, RenderTexture dst)
        {
            var m = MSampling;
            if (!m) { Graphics.Blit(src, dst); return; }

            SetPixelGridParams(m);

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

            m.SetFloat(ShaderProps.Samples, bleedSamples);
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

            m.SetFloat(ShaderProps.DitherMode, (float)ditherMode);
            m.SetFloat(ShaderProps.DitherStrength, (ditherMode == DitherMode.None) ? 0f : ditherStrength);

            m.SetTexture(ShaderProps.BlueNoise,
                (ditherMode == DitherMode.BlueNoise && blueNoise != null) ? blueNoise : Texture2D.grayTexture);

            SetPixelGridParams(m);

            Graphics.Blit(src, dst, m);
        }

        private void RunPaletteMapping(RenderTexture src, RenderTexture dst)
        {
            var m = MPalette;
            if (!m) { Graphics.Blit(src, dst); return; }

            m.SetTexture(ShaderProps.ThresholdTex, _curveTex ? _curveTex : Texture2D.whiteTexture);

            bool paletteOn = usePalette && paletteTex != null;
            m.SetFloat(ShaderProps.UsePalette, paletteOn ? 1f : 0f);
            m.SetTexture(ShaderProps.PaletteTex, paletteTex != null ? paletteTex : Texture2D.whiteTexture);

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
            m.SetTexture(ShaderProps.MaskedTex, fxTex);

            Graphics.Blit(baseTex, dst, m);
        }

        private void RunDepthMask(RenderTexture baseTex, RenderTexture fxTex, RenderTexture dst)
        {
            var m = MDepthMask;
            if (!m) { Graphics.Blit(fxTex, dst); return; }

            m.SetFloat(ShaderProps.UseDepthMask, useDepthMask ? 1f : 0f);
            m.SetFloat(ShaderProps.DepthThreshold, Mathf.Max(0f, depthThreshold));
            m.SetTexture(ShaderProps.MaskedTex, fxTex);

            Graphics.Blit(baseTex, dst, m);
        }

        private void RunPresent(RenderTexture originalSrc, RenderTexture processed, RenderTexture dst)
        {
            var m = MPresent;
            if (!m) { Graphics.Blit(processed, dst); return; }

            m.SetTexture(ShaderProps.OriginalTex, originalSrc);
            m.SetFloat(ShaderProps.MasterBlend, masterBlend);

            Graphics.Blit(processed, dst, m);
        }

        private void EnsureGhostResources(RenderTexture src)
        {
            if (!ghostEnabled)
            {
                ReleaseGhostResources();
                return;
            }

            int w = Mathf.Max(1, src ? src.width : Screen.width);
            int h = Mathf.Max(1, src ? src.height : Screen.height);

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
            _ghostCaptureCounter = 0;
            _ghostSeeded = false;
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
                _ghostCaptureCounter = 0;
                _ghostSeeded = true;
                return;
            }

            int interval = Mathf.Max(0, ghostCaptureInterval);
            if (_ghostCaptureCounter < interval) { _ghostCaptureCounter++; return; }

            _ghostCaptureCounter = 0;

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
        private Material MPresent => GetOrCreate(ref _mPresent, masterPresentShader, "Hidden/CrowFX/Stages/MasterPresent");

        private Material MTextureMask => GetOrCreate(ref _mTexMask, textureMaskShader, "Hidden/CrowFX/Stages/TextureMask");
        private Material MDepthMask => GetOrCreate(ref _mDepthMask, depthMaskShader, "Hidden/CrowFX/Stages/DepthMask");

        private Material MGhostComposite => GetOrCreate(ref _mGhostComposite, ghostCompositeShader, "Hidden/CrowFX/Helpers/GhostComposite");

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
        }

        private static Shader AutoShader(Shader current, string shaderName)
        {
            if (current != null) return current;
            return Shader.Find(shaderName);
        }
    }
}
