Shader "Hidden/CrowFX/Stages/Dithering"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}

        _Levels   ("Levels", Range(2,512)) = 64
        _LevelsR  ("Levels R", Range(2,512)) = 64
        _LevelsG  ("Levels G", Range(2,512)) = 64
        _LevelsB  ("Levels B", Range(2,512)) = 64
        _UsePerChannel ("Use Per Channel", Float) = 0
        _LuminanceOnly ("Luminance Only", Float) = 0

        _AnimateLevels ("Animate Levels", Float) = 0
        _MinLevels ("Min Levels", Float) = 64
        _MaxLevels ("Max Levels", Float) = 64
        _Speed ("Speed", Float) = 1

        _DitherMode ("Dither Mode", Float) = 0
        _DitherStrength ("Dither Strength", Range(0,1)) = 0
        _DitherSize ("Dither Pattern Size", Range(1,16)) = 1
        _DitherAngle ("Dither Angle", Range(0,180)) = 45
        _BlueNoise ("Blue Noise (128x128)", 2D) = "gray" {}
        _HalftoneAreaModulation ("Halftone Area-Modulated Dots", Float) = 0
        _HalftoneColorMode ("Halftone Color Separation", Float) = 0

        _PixelSize ("Pixel Size", Float) = 1
        _UseVirtualGrid ("Use Virtual Grid", Float) = 0
        _VirtualRes ("Virtual Resolution (xy)", Vector) = (640,448,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "CE_Common.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _Levels, _LevelsR, _LevelsG, _LevelsB, _UsePerChannel, _LuminanceOnly;
            float _AnimateLevels, _MinLevels, _MaxLevels, _Speed;

            float _DitherMode, _DitherStrength, _DitherSize;
            float _DitherAngle;
            sampler2D _BlueNoise;
            float4 _BlueNoise_TexelSize;
            float _TemporalDither, _TemporalDitherRate, _HalftoneScale, _HalftoneDotGain, _HalftoneAreaModulation, _HalftoneColorMode;

            float _PixelSize;
            float _UseVirtualGrid;
            float4 _VirtualRes;

            static const float bayer2[4] = { 0.0/4.0, 2.0/4.0, 3.0/4.0, 1.0/4.0 };

            static const float bayer4[16] =
            {
                 0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
                 3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
            };

            static const float bayer8[64] =
            {
                 0.0/64.0, 32.0/64.0,  8.0/64.0, 40.0/64.0,  2.0/64.0, 34.0/64.0, 10.0/64.0, 42.0/64.0,
                48.0/64.0, 16.0/64.0, 56.0/64.0, 24.0/64.0, 50.0/64.0, 18.0/64.0, 58.0/64.0, 26.0/64.0,
                12.0/64.0, 44.0/64.0,  4.0/64.0, 36.0/64.0, 14.0/64.0, 46.0/64.0,  6.0/64.0, 38.0/64.0,
                60.0/64.0, 28.0/64.0, 52.0/64.0, 20.0/64.0, 62.0/64.0, 30.0/64.0, 54.0/64.0, 22.0/64.0,
                 3.0/64.0, 35.0/64.0, 11.0/64.0, 43.0/64.0,  1.0/64.0, 33.0/64.0,  9.0/64.0, 41.0/64.0,
                51.0/64.0, 19.0/64.0, 59.0/64.0, 27.0/64.0, 49.0/64.0, 17.0/64.0, 57.0/64.0, 25.0/64.0,
                15.0/64.0, 47.0/64.0,  7.0/64.0, 39.0/64.0, 13.0/64.0, 45.0/64.0,  5.0/64.0, 37.0/64.0,
                63.0/64.0, 31.0/64.0, 55.0/64.0, 23.0/64.0, 61.0/64.0, 29.0/64.0, 53.0/64.0, 21.0/64.0
            };

            inline float D2(int2 p) { p = p & int2(1,1); return bayer2[p.y*2 + p.x]; }
            inline float D4(int2 p) { p = p & int2(3,3); return bayer4[p.y*4 + p.x]; }
            inline float D8(int2 p) { p = p & int2(7,7); return bayer8[p.y*8 + p.x]; }

            inline uint DHash(uint x)
            {
                x ^= x >> 16u;
                x *= 0x7feb352du;
                x ^= x >> 15u;
                x *= 0x846ca68bu;
                return x ^ (x >> 16u);
            }

            inline int DTemporalFrame()
            {
                return (_TemporalDither > 0.5) ? (int)floor(_Time.y * _TemporalDitherRate) : 0;
            }

            inline float DNoise(int2 p)
            {
                // Frame id is mixed as an independent hash component.  This regenerates
                // the threshold field instead of translating a fixed 2D noise pattern.
                uint x = (uint)p.x;
                uint y = (uint)p.y;
                uint frame = (uint)DTemporalFrame();
                uint h = DHash(x + 0x9e3779b9u);
                h ^= DHash(y + 0x85ebca6bu);
                h ^= DHash(frame + 0xc2b2ae35u);
                return (float)DHash(h) * 2.3283064365386963e-10;
            }

            inline float DBlue(int2 p)
            {
                int frame = DTemporalFrame();
                int2 size = max((int2)_BlueNoise_TexelSize.zw, int2(1, 1));
                uint h = DHash((uint)frame + 0x9e3779b9u);
                int2 pp = p;
                if ((h & 1u) != 0u) pp = pp.yx;
                if ((h & 2u) != 0u) pp.x = size.x - 1 - pp.x;
                if ((h & 4u) != 0u) pp.y = size.y - 1 - pp.y;
                pp += int2((int)(h % (uint)size.x), (int)(DHash(h) % (uint)size.y));
                pp = int2(pp.x % size.x, pp.y % size.y);
                pp += int2(pp.x < 0 ? size.x : 0, pp.y < 0 ? size.y : 0);
                float2 uv = (float2(pp) + 0.5) / float2(size);
                return tex2D(_BlueNoise, uv).r;
            }

            inline float2 Rotate2D(float2 p, float radiansAngle)
            {
                float s = sin(radiansAngle);
                float c = cos(radiansAngle);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            inline float DHalftone(float2 gridPos, float angle)
            {
                float cellSize = max(_HalftoneScale, 2.0);
                float2 local = frac(Rotate2D(gridPos, radians(angle)) / cellSize) - 0.5;
                float dist = length(local);
                return saturate(pow(saturate(1.0 - dist / 0.5), 1.35) + _HalftoneDotGain);
            }

            // Clustered-dot screen rank: 0 at a dot center, 0.5 where adjacent
            // cells meet, and 1 at the four-cell junction. Thresholding this by
            // ink coverage produces isolated highlight dots, connected shadow
            // ink, and shrinking paper-colored holes above the midtone.
            inline float HalftoneAreaRank(float2 gridPos, float angle)
            {
                float cellSize = max(_HalftoneScale, 2.0);
                float2 local = frac(Rotate2D(gridPos, radians(angle)) / cellSize) - 0.5;
                return saturate(2.0 * dot(local, local));
            }

            inline float HalftoneCoverageMask(float coverage, float rank)
            {
                coverage = saturate(coverage);
                // Dot gain changes ink that exists on a plate; it must not invent
                // cyan/magenta/yellow specks where that separation has zero ink.
                if (coverage <= 0.0) return 0.0;
                coverage = saturate(coverage + _HalftoneDotGain);
                float antialiasWidth = max(fwidth(rank), 0.002);
                if (coverage <= 0.0) return 0.0;
                if (coverage >= 1.0) return 1.0;
                return 1.0 - smoothstep(coverage - antialiasWidth, coverage + antialiasWidth, rank);
            }

            inline float HalftoneInkMask(float reflectance, float rank)
            {
                return HalftoneCoverageMask(1.0 - reflectance, rank);
            }

            inline float DLinear(float2 gridPos)
            {
                float radiansAngle = radians(_DitherAngle);
                float2 dir = float2(cos(radiansAngle), sin(radiansAngle));
                return frac(dot(gridPos, dir) * 0.25);
            }

            inline float DDiamond(float2 gridPos)
            {
                const float cellSize = 6.0;
                float2 local = frac(gridPos / cellSize) - 0.5;
                float dist = abs(local.x) + abs(local.y);
                return saturate(1.0 - dist / 0.5);
            }

            inline float3 Quantize(float3 rgb, float3 levels, float2 gridPos)
            {
                rgb = saturate(rgb);
                levels = max(levels, 2.0);

                int2 pix = (int2)floor(gridPos);

                int mode = (int)(_DitherMode + 0.5);
                float3 scaled = rgb * (levels - 1.0);

                if (mode == 6 && _HalftoneAreaModulation > 0.5)
                {
                    float3 posterized = clamp(floor(scaled + 0.5), 0.0, levels - 1.0) / (levels - 1.0);
                    int colorMode = (int)(_HalftoneColorMode + 0.5);
                    float3 screened;

                    if (colorMode == 1)
                    {
                        // Convert RGB to under-color-removed CMYK, screen each ink
                        // at a conventional print angle, then mix the inks
                        // subtractively on white paper.
                        float blackCoverage = 1.0 - max(posterized.r, max(posterized.g, posterized.b));
                        float colorRange = max(1.0 - blackCoverage, 0.0001);
                        float3 cmyCoverage = saturate((1.0 - posterized - blackCoverage) / colorRange);
                        if (blackCoverage >= 0.9999)
                            cmyCoverage = 0.0;

                        float cyanInk = HalftoneCoverageMask(cmyCoverage.x, HalftoneAreaRank(gridPos, 15.0));
                        float magentaInk = HalftoneCoverageMask(cmyCoverage.y, HalftoneAreaRank(gridPos, 75.0));
                        float yellowInk = HalftoneCoverageMask(cmyCoverage.z, HalftoneAreaRank(gridPos, 0.0));
                        float blackInk = HalftoneCoverageMask(blackCoverage, HalftoneAreaRank(gridPos, 45.0));

                        screened = 1.0;
                        screened *= lerp(float3(1.0, 1.0, 1.0), float3(0.06, 0.80, 0.86), cyanInk);
                        screened *= lerp(float3(1.0, 1.0, 1.0), float3(0.88, 0.06, 0.70), magentaInk);
                        screened *= lerp(float3(1.0, 1.0, 1.0), float3(0.97, 0.88, 0.08), yellowInk);
                        screened *= lerp(float3(1.0, 1.0, 1.0), float3(0.055, 0.05, 0.045), blackInk);
                    }
                    else if (colorMode == 2)
                    {
                        // Deliberately vivid additive RGB rosette. This preserves
                        // the former halftone appearance as an explicit option.
                        float3 rank = float3(
                            HalftoneAreaRank(gridPos, 15.0),
                            HalftoneAreaRank(gridPos, 75.0),
                            HalftoneAreaRank(gridPos, 0.0));
                        float3 ink = float3(
                            HalftoneInkMask(posterized.r, rank.r),
                            HalftoneInkMask(posterized.g, rank.g),
                            HalftoneInkMask(posterized.b, rank.b));
                        screened = 1.0 - ink;
                    }
                    else
                    {
                        // A single luminance-driven screen avoids colored channel
                        // fringes while retaining a restrained trace of source hue.
                        float luminance = CrowFX_Luma(posterized);
                        float ink = HalftoneInkMask(luminance, HalftoneAreaRank(gridPos, 45.0));
                        float3 inkTint = saturate(posterized / max(luminance, 0.001)) * 0.10;
                        screened = lerp(float3(1.0, 1.0, 1.0), inkTint, ink);
                    }

                    return lerp(posterized, screened, saturate(_DitherStrength));
                }

                float thr = 0.5;
                if (mode == 1) thr = D2(pix);
                else if (mode == 2) thr = D4(pix);
                else if (mode == 3) thr = D8(pix);
                else if (mode == 4) thr = DNoise(pix);
                else if (mode == 5) thr = DBlue(pix);
                else if (mode == 6) thr = DHalftone(gridPos, 45.0);
                else if (mode == 7) thr = DLinear(gridPos);
                else if (mode == 8) thr = DDiamond(gridPos);

                if (_DitherStrength > 0.0)
                {
                    float3 threshold = thr.xxx;
                    // Separated print-screen angles prevent every color channel from
                    // collapsing into the same monochrome dot lattice.
                    if (mode == 6)
                        threshold = float3(DHalftone(gridPos, 15.0), DHalftone(gridPos, 75.0), DHalftone(gridPos, 0.0));
                    scaled += (threshold - 0.5) * _DitherStrength;
                }

                float3 q = clamp(floor(scaled + 0.5), 0.0, levels - 1.0);
                return q / (levels - 1.0);
            }

            float4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float3 col = tex2D(_MainTex, uv).rgb;

                float levelsAnim = _Levels;
                if (_AnimateLevels > 0.5)
                {
                    float t = 0.5 + 0.5 * sin(_Time.y * _Speed);
                    levelsAnim = lerp(_MinLevels, _MaxLevels, t);
                }

                float3 perChLevels = float3(levelsAnim, levelsAnim, levelsAnim);
                if (_UsePerChannel > 0.5)
                    perChLevels = float3(_LevelsR, _LevelsG, _LevelsB);

                float pxBlock = max(_PixelSize, 1.0) * max(_DitherSize, 1.0);
                float2 gridPos = uv * (CrowFX_GetBaseResolution(_UseVirtualGrid, _VirtualRes, _MainTex_TexelSize) / pxBlock);

                float3 quant;
                if (_LuminanceOnly > 0.5)
                {
                    float lum = CrowFX_Luma(col);
                    float quantLum = Quantize(lum.xxx, levelsAnim.xxx, gridPos).r;
                    quant = (lum > 1e-5) ? col * (quantLum / lum) : quantLum.xxx;
                    quant = saturate(quant);
                }
                else
                {
                    quant = Quantize(col, perChLevels, gridPos);
                }
                return float4(quant, 1.0);
            }
            ENDCG
        }
    }
}
