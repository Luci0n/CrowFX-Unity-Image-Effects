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
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _ThresholdTex;

            float _UsePalette;
            float _PaletteMode;
            sampler2D _PaletteTex;
            float4 _PaletteTex_TexelSize;

            float _Invert;

            inline float3 SamplePaletteRamp(float value, int width, int height)
            {
                if (width >= height)
                    return tex2D(_PaletteTex, float2(value, 0.5)).rgb;

                return tex2D(_PaletteTex, float2(0.5, value)).rgb;
            }

            inline float3 SamplePaletteNearest(float3 color, int width, int height)
            {
                const int MAX_PALETTE_SAMPLES = 256;

                int safeWidth = max(width, 1);
                int safeHeight = max(height, 1);
                int total = min(safeWidth * safeHeight, MAX_PALETTE_SAMPLES);

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
                    float3 delta = color - candidate;
                    float distance = dot(delta * delta, float3(0.2126, 0.7152, 0.0722));

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestColor = candidate;
                    }
                }

                return bestColor;
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;

                // Threshold curve remap per channel (use y=0 for a 256x1 curve texture)
                c.r = tex2D(_ThresholdTex, float2(c.r, 0.0)).r;
                c.g = tex2D(_ThresholdTex, float2(c.g, 0.0)).r;
                c.b = tex2D(_ThresholdTex, float2(c.b, 0.0)).r;

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

                return float4(saturate(c), 1);
            }
            ENDCG
        }
    }
}
