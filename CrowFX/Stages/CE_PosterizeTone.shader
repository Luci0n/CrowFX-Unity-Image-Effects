Shader "Hidden/CrowFX/Stages/PosterizeTone"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _LuminanceOnly ("Luminance Only", Float) = 0
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
            float _LuminanceOnly;

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                float3 col = CROWFX_SAMPLE_SCREEN(_MainTex, i.uv).rgb;

                // Luminance-only quantization is performed in CE_Dithering so the
                // quantized luminance can be recombined with the original chroma.
                // Keeping this legacy stage as a pass-through preserves the stack layout.

                return float4(col, 1);
            }
            ENDCG
        }
    }
}
