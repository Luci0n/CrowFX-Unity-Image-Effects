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
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;      // processed (comes from Blit source)
            sampler2D _OriginalTex;
            float _MasterBlend;

            float4 frag(v2f_img i) : SV_Target
            {
                float4 processed = tex2D(_MainTex, i.uv);
                float4 original  = tex2D(_OriginalTex, i.uv);

                float3 outc = lerp(original.rgb, processed.rgb, saturate(_MasterBlend));
                return float4(outc, original.a);
            }
            ENDCG
        }
    }
}
