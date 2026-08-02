Shader "Hidden/CrowFX/Stages/DepthMask"
{
    Properties
    {
        _MainTex ("Base (Unmasked)", 2D) = "white" {}
        _MaskedTex ("Masked Result", 2D) = "white" {}

        _UseDepthMask ("Use Depth Mask", Float) = 0
        _DepthThreshold ("Depth Threshold (Linear)", Float) = 1.0
        _DepthFar ("Far Depth", Float) = 1000.0
        _DepthSoftness ("Depth Softness", Float) = 0.25
        _DepthOpacity ("Depth Opacity", Range(0,1)) = 1
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
            sampler2D _MaskedTex;

            sampler2D_float _CameraDepthTexture;

            float _UseDepthMask;
            float _DepthThreshold;
            float _DepthFar, _DepthSoftness, _DepthOpacity, _DepthInvert;

            float4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                float3 baseCol = tex2D(_MainTex, uv).rgb;
                float3 fxCol   = tex2D(_MaskedTex, uv).rgb;

                if (_UseDepthMask < 0.5)
                    return float4(fxCol, 1); // no depth mask -> just pass processed

                float raw = tex2D(_CameraDepthTexture, uv).r;
                float sceneDepth = LinearEyeDepth(raw);

                float feather = max(_DepthSoftness, 0.00001);
                float nearMask = smoothstep(_DepthThreshold - feather, _DepthThreshold + feather, sceneDepth);
                float farMask = 1.0 - smoothstep(_DepthFar - feather, _DepthFar + feather, sceneDepth);
                float a = nearMask * farMask;
                if (_DepthInvert > 0.5) a = 1.0 - a;
                a *= saturate(_DepthOpacity);

                float3 outc = lerp(baseCol, fxCol, a);
                return float4(outc, 1);
            }
            ENDCG
        }
    }
}
