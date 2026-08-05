Shader "Hidden/CrowFX/Stages/TextureMask"
{
    Properties
    {
        _MainTex ("Base (Unmasked)", 2D) = "white" {}
        _MaskedTex ("Masked Result", 2D) = "white" {}

        _UseMask ("Use Mask", Float) = 0
        _MaskTex ("Mask", 2D) = "white" {}
        _MaskThreshold ("Mask Threshold", Range(0,1)) = 0.5
        _MaskSoftness ("Mask Softness", Range(0,1)) = 0.1
        _MaskOpacity ("Mask Opacity", Range(0,1)) = 1
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
            CROWFX_DECLARE_SCREEN_TEX(_MaskedTex)

            float _UseMask;
            sampler2D _MaskTex;
            float _MaskThreshold;
            float _MaskSoftness, _MaskOpacity, _MaskInvert, _MaskChannel;
            float4 _MaskTransform;

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                float2 uv = i.uv;

                float3 baseCol = CROWFX_SAMPLE_SCREEN(_MainTex, uv).rgb;
                float3 fxCol   = CROWFX_SAMPLE_SCREEN(_MaskedTex, uv).rgb;

                if (_UseMask < 0.5)
                    return float4(fxCol, 1); // no masking -> just pass processed

                float4 maskSample = tex2D(_MaskTex, uv * _MaskTransform.xy + _MaskTransform.zw);
                float m = dot(maskSample.rgb, float3(0.2126, 0.7152, 0.0722));
                if (_MaskChannel > 0.5 && _MaskChannel < 1.5) m = maskSample.r;
                else if (_MaskChannel < 2.5 && _MaskChannel > 1.5) m = maskSample.g;
                else if (_MaskChannel < 3.5 && _MaskChannel > 2.5) m = maskSample.b;
                else if (_MaskChannel > 3.5) m = maskSample.a;
                if (_MaskInvert > 0.5) m = 1.0 - m;

                float halfFeather = max(_MaskSoftness * 0.5, 0.00001);
                float a = smoothstep(_MaskThreshold - halfFeather, _MaskThreshold + halfFeather, m);
                a *= saturate(_MaskOpacity);

                float3 outc = lerp(baseCol, fxCol, a);
                return float4(outc, 1);
            }
            ENDCG
        }
    }
}
