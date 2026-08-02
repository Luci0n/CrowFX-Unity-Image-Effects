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
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _LuminanceOnly;

            float4 frag(v2f_img i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).rgb;

                // Luminance-only quantization is performed in CE_Dithering so the
                // quantized luminance can be recombined with the original chroma.
                // Keeping this legacy stage as a pass-through preserves the stack layout.

                return float4(col, 1);
            }
            ENDCG
        }
    }
}
