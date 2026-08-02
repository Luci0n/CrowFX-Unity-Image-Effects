Shader "Hidden/CrowFX/Stages/EdgeOutline"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}

        _EdgeEnabled ("Edge Enabled", Float) = 0
        _EdgeStrength ("Edge Strength", Range(0,8)) = 1
        _EdgeThreshold ("Edge Threshold (Linear)", Range(0,1)) = 0.02
        _EdgeBlend ("Edge Blend", Range(0,1)) = 1
        _EdgeColor ("Edge Color", Color) = (0,0,0,1)
        _EdgeThickness ("Edge Thickness", Range(0.5,4)) = 1
        _EdgeUseNormals ("Use Normals", Float) = 1
        _EdgeNormalThreshold ("Normal Threshold", Range(0,1)) = 0.18

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

            // camera depth
            sampler2D_float _CameraDepthTexture;
            sampler2D _CameraDepthNormalsTexture;

            float _EdgeEnabled, _EdgeStrength, _EdgeThreshold, _EdgeBlend;
            float _EdgeThickness, _EdgeUseNormals, _EdgeNormalThreshold;
            float4 _EdgeColor;

            float _UseVirtualGrid;
            float4 _VirtualRes;

            inline float2 StepUV()
            {
                return CrowFX_GetStepUV(_UseVirtualGrid, _VirtualRes, _MainTex_TexelSize);
            }

            inline float EdgeFromDepth(float2 uv)
            {
                float2 stepUV = StepUV() * max(_EdgeThickness, 0.5);

                float dC = LinearEyeDepth(tex2D(_CameraDepthTexture, uv).r);
                float dR = LinearEyeDepth(tex2D(_CameraDepthTexture, uv + float2(stepUV.x, 0)).r);
                float dL = LinearEyeDepth(tex2D(_CameraDepthTexture, uv - float2(stepUV.x, 0)).r);
                float dU = LinearEyeDepth(tex2D(_CameraDepthTexture, uv + float2(0, stepUV.y)).r);
                float dD = LinearEyeDepth(tex2D(_CameraDepthTexture, uv - float2(0, stepUV.y)).r);

                float diff = max(max(abs(dR - dC), abs(dL - dC)), max(abs(dU - dC), abs(dD - dC)));
                // Relative depth is stable as the camera and subject move through the scene.
                diff /= max(abs(dC), 0.01);

                float normalEdge = 0.0;
                if (_EdgeUseNormals > 0.5)
                {
                    float depthDummy;
                    float3 nC, nR, nL, nU, nD;
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv), depthDummy, nC);
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv + float2(stepUV.x, 0)), depthDummy, nR);
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv - float2(stepUV.x, 0)), depthDummy, nL);
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv + float2(0, stepUV.y)), depthDummy, nU);
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv - float2(0, stepUV.y)), depthDummy, nD);
                    float nd = max(max(1.0 - dot(nC, nR), 1.0 - dot(nC, nL)),
                                   max(1.0 - dot(nC, nU), 1.0 - dot(nC, nD)));
                    normalEdge = smoothstep(_EdgeNormalThreshold, _EdgeNormalThreshold + 0.08, nd);
                }

                float depthEdge = smoothstep(_EdgeThreshold, _EdgeThreshold + max(0.002, _EdgeThreshold * 0.5), diff);
                float e = saturate(max(depthEdge, normalEdge) * _EdgeStrength);
                return e;
            }

            float4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float3 c = tex2D(_MainTex, uv).rgb;

                if (_EdgeEnabled > 0.5 && _EdgeBlend > 0.0)
                {
                    float e = EdgeFromDepth(uv);
                    c = lerp(c, _EdgeColor.rgb, saturate(e * _EdgeBlend));
                }

                return float4(c, 1);
            }
            ENDCG
        }
    }
}
