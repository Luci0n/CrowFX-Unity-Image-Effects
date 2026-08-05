Shader "Hidden/CrowFX/Stages/PaletteMapping"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _ThresholdTex ("Threshold Curve", 2D) = "white" {}
        _UsePalette ("Use Palette", Float) = 0
        _PaletteMode ("Palette Mode", Float) = 1
        _PaletteTex ("Palette", 2D) = "white" {}
        _Invert ("Invert", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma vertex CrowFX_Vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "CE_Stereo.cginc"

            CROWFX_DECLARE_SCREEN_TEX(_MainTex)
            sampler2D _ThresholdTex;

            float _UsePalette;
            float _UseThreshold;
            float _PaletteMode;
            sampler2D _PaletteTex;
            float4 _PaletteTex_TexelSize;

            float _Invert;
            int _PaletteColorCount;
            float _PalettePerceptual;

            inline float3 LinearToOklab(float3 c)
            {
                float3 lms = float3(
                    dot(c, float3(0.4122214708, 0.5363325363, 0.0514459929)),
                    dot(c, float3(0.2119034982, 0.6806995451, 0.1073969566)),
                    dot(c, float3(0.0883024619, 0.2817188376, 0.6299787005)));
                lms = pow(max(lms, 0.0), 1.0 / 3.0);
                return float3(
                    dot(lms, float3(0.2104542553, 0.7936177850, -0.0040720468)),
                    dot(lms, float3(1.9779984951, -2.4285922050, 0.4505937099)),
                    dot(lms, float3(0.0259040371, 0.7827717662, -0.8086757660)));
            }

            inline float3 SamplePaletteRamp(float value, int width, int height)
            {
                if (width >= height)
                    return tex2D(_PaletteTex, float2(value, 0.5)).rgb;

                return tex2D(_PaletteTex, float2(0.5, value)).rgb;
            }

            inline float3 SamplePaletteNearest(float3 color, int width, int height)
            {
                const int MAX_PALETTE_SAMPLES = 64;

                int safeWidth = max(width, 1);
                int safeHeight = max(height, 1);
                int total = min(min(safeWidth * safeHeight, MAX_PALETTE_SAMPLES), max(_PaletteColorCount, 2));

                float3 comparisonColor = color;
                if (_PalettePerceptual > 0.5)
                {
                    #if defined(UNITY_COLORSPACE_GAMMA)
                        comparisonColor = GammaToLinearSpace(max(color, 0.0));
                    #endif
                    comparisonColor = LinearToOklab(comparisonColor);
                }

                float bestDistance = 1e9;
                float3 bestColor = color;

                [loop]
                for (int idx = 0; idx < MAX_PALETTE_SAMPLES; idx++)
                {
                    if (idx >= total)
                        break;

                    int x = idx % safeWidth;
                    int y = idx / safeWidth;
                    if (y >= safeHeight)
                        break;

                    float2 uv = float2((x + 0.5) / safeWidth, (y + 0.5) / safeHeight);
                    float3 candidate = tex2D(_PaletteTex, uv).rgb;
                    float3 candidateComparison = candidate;
                    if (_PalettePerceptual > 0.5)
                    {
                        #if defined(UNITY_COLORSPACE_GAMMA)
                            candidateComparison = GammaToLinearSpace(max(candidate, 0.0));
                        #endif
                        candidateComparison = LinearToOklab(candidateComparison);
                    }
                    float3 delta = comparisonColor - candidateComparison;
                    float distance = dot(delta, delta);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestColor = candidate;
                    }
                }

                return bestColor;
            }

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                float3 c = CROWFX_SAMPLE_SCREEN(_MainTex, i.uv).rgb;

                if (_UseThreshold > 0.5)
                {
                    c.r = tex2D(_ThresholdTex, float2(saturate(c.r), 0.5)).r;
                    c.g = tex2D(_ThresholdTex, float2(saturate(c.g), 0.5)).r;
                    c.b = tex2D(_ThresholdTex, float2(saturate(c.b), 0.5)).r;
                }

                // Palette lookup (tonal ramp or nearest swatch)
                if (_UsePalette > 0.5)
                {
                    int width = max(1, (int)round(1.0 / max(_PaletteTex_TexelSize.x, 1e-5)));
                    int height = max(1, (int)round(1.0 / max(_PaletteTex_TexelSize.y, 1e-5)));

                    if (_PaletteMode < 0.5)
                    {
                        float v = dot(c, float3(0.2126, 0.7152, 0.0722));
                        c = SamplePaletteRamp(v, width, height);
                    }
                    else
                    {
                        c = SamplePaletteNearest(c, width, height);
                    }
                }

                // Inversion (perceptual in Linear projects, plain in Gamma projects)
                if (_Invert > 0.5)
                {
                #if defined(UNITY_COLORSPACE_GAMMA)
                    c = 1.0 - c;
                #else
                    float3 g = LinearToGammaSpace(c);
                    g = 1.0 - g;
                    c = GammaToLinearSpace(g);
                #endif
                }

                return float4(c, 1);
            }
            ENDCG
        }
    }
}
