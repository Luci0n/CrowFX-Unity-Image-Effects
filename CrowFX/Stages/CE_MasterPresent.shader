Shader "Hidden/CrowFX/Stages/MasterPresent"
{
    Properties
    {
        _MainTex ("Processed", 2D) = "white" {}
        _OriginalTex ("Original", 2D) = "white" {}
        _MasterBlend ("Master Blend", Range(0,1)) = 1
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

            CROWFX_DECLARE_SCREEN_TEX(_MainTex)      // processed (comes from Blit source)
            CROWFX_DECLARE_SCREEN_TEX(_OriginalTex)
            float _MasterBlend;

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                float4 processed = CROWFX_SAMPLE_SCREEN(_MainTex, i.uv);
                float4 original  = CROWFX_SAMPLE_SCREEN(_OriginalTex, i.uv);

                float3 outc = lerp(original.rgb, processed.rgb, saturate(_MasterBlend));
                return float4(outc, original.a);
            }
            ENDCG
        }
    }
}
