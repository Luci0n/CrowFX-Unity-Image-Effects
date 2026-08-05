Shader "Hidden/CrowFX/Stages/UnsharpMask"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}

        _UnsharpEnabled ("Unsharp Enabled", Float) = 0
        _UnsharpAmount ("Amount", Range(0,3)) = 0.5
        _UnsharpRadius ("Radius (px)", Float) = 1.0
        _UnsharpThreshold ("Threshold", Range(0,0.25)) = 0.0

        _UnsharpLumaOnly ("Luma Only", Float) = 0
        _UnsharpChroma ("Chroma Sharpen", Range(0,1)) = 0.0
        _SharpenMode ("Sharpen Mode", Float) = 1

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
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma vertex CrowFX_Vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "CE_Stereo.cginc"
            #include "CE_Common.cginc"

            CROWFX_DECLARE_SCREEN_TEX(_MainTex)
            float4 _MainTex_TexelSize;

            float _UnsharpEnabled, _UnsharpAmount, _UnsharpRadius, _UnsharpThreshold;
            float _UnsharpLumaOnly, _UnsharpChroma;
            float _SharpenMode;

            float _UseVirtualGrid;
            float4 _VirtualRes;

            inline float2 StepUV()
            {
                return CrowFX_GetStepUV(_UseVirtualGrid, _VirtualRes, _MainTex_TexelSize);
            }

            // 3x3 blur (gaussian-ish)
            float3 Blur3x3(float2 uv, float2 texelStep, out float3 localMin, out float3 localMax)
            {
                float2 o = texelStep;
                float3 c  = CROWFX_SAMPLE_SCREEN(_MainTex, uv).rgb;
                float3 r  = CROWFX_SAMPLE_SCREEN(_MainTex, uv + float2( o.x,  0)).rgb;
                float3 l  = CROWFX_SAMPLE_SCREEN(_MainTex, uv + float2(-o.x,  0)).rgb;
                float3 u  = CROWFX_SAMPLE_SCREEN(_MainTex, uv + float2( 0,   o.y)).rgb;
                float3 d  = CROWFX_SAMPLE_SCREEN(_MainTex, uv + float2( 0,  -o.y)).rgb;
                float3 ru = CROWFX_SAMPLE_SCREEN(_MainTex, uv + float2( o.x,  o.y)).rgb;
                float3 lu = CROWFX_SAMPLE_SCREEN(_MainTex, uv + float2(-o.x,  o.y)).rgb;
                float3 rd = CROWFX_SAMPLE_SCREEN(_MainTex, uv + float2( o.x, -o.y)).rgb;
                float3 ld = CROWFX_SAMPLE_SCREEN(_MainTex, uv + float2(-o.x, -o.y)).rgb;

                localMin = min(c, min(min(r, l), min(u, d)));
                localMin = min(localMin, min(min(ru, lu), min(rd, ld)));
                localMax = max(c, max(max(r, l), max(u, d)));
                localMax = max(localMax, max(max(ru, lu), max(rd, ld)));
                return (c * 4.0 + (r + l + u + d) * 2.0 + ru + lu + rd + ld) / 16.0;
            }

            float3 ContrastAdaptive(float2 uv, float2 texelStep, float3 col)
            {
                float3 r = CROWFX_SAMPLE_SCREEN(_MainTex, uv + float2(texelStep.x, 0)).rgb;
                float3 l = CROWFX_SAMPLE_SCREEN(_MainTex, uv - float2(texelStep.x, 0)).rgb;
                float3 u = CROWFX_SAMPLE_SCREEN(_MainTex, uv + float2(0, texelStep.y)).rgb;
                float3 d = CROWFX_SAMPLE_SCREEN(_MainTex, uv - float2(0, texelStep.y)).rgb;
                float3 localMin = min(col, min(min(r, l), min(u, d)));
                float3 localMax = max(col, max(max(r, l), max(u, d)));
                float3 detail = col - (r + l + u + d) * 0.25;

                float detailLuma = CrowFX_Luma(abs(detail));
                float gate = smoothstep(_UnsharpThreshold, _UnsharpThreshold + 0.035, detailLuma);
                float contrast = max(localMax.r - localMin.r, max(localMax.g - localMin.g, localMax.b - localMin.b));
                float adaptive = lerp(0.30, 1.0, smoothstep(0.015, 0.22, contrast));
                float3 sharpened = col + detail * (_UnsharpAmount * adaptive * gate);

                // Permit a small amount of local overshoot without the bright/dark
                // contours produced by an unconstrained unsharp mask.
                float3 haloRoom = max((localMax - localMin) * 0.055, 1e-4);
                return clamp(sharpened, localMin - haloRoom, localMax + haloRoom);
            }

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                float2 uv = i.uv;
                float3 col = CROWFX_SAMPLE_SCREEN(_MainTex, uv).rgb;

                if (_UnsharpEnabled < 0.5 || _UnsharpAmount <= 0.0)
                    return float4(col, 1);

                float radius = max(_UnsharpRadius, 0.25);
                float2 texelStep = StepUV() * radius;

                if (_SharpenMode > 0.5)
                    return float4(ContrastAdaptive(uv, texelStep, col), 1);

                float3 localMin, localMax;
                float3 blurred = Blur3x3(uv, texelStep, localMin, localMax);

                float3 detail = col - blurred;

                float thr = max(_UnsharpThreshold, 0.0);
                if (thr > 0.0)
                {
                    float thresholdHigh = thr * 2.0 + 1e-5;
                    float3 mask = smoothstep(thr.xxx, thresholdHigh.xxx, abs(detail));
                    detail *= mask;
                }

                float3 rgbSharpen = col + _UnsharpAmount * detail;
                float3 haloRoom = max((localMax - localMin) * 0.12, 1e-4);
                rgbSharpen = clamp(rgbSharpen, localMin - haloRoom, localMax + haloRoom);

                if (_UnsharpLumaOnly > 0.5)
                {
                    float yC = CrowFX_Luma(col);
                    float yB = CrowFX_Luma(blurred);
                    float yD = yC - yB;

                    if (thr > 0.0)
                        yD *= smoothstep(thr, thr * 2.0 + 1e-5, abs(yD));

                    float ySharp = yC + _UnsharpAmount * yD;
                    float3 lumaSharpen = col + (ySharp - yC).xxx;

                    float3 combined = lerp(lumaSharpen, rgbSharpen, saturate(_UnsharpChroma));
                    return float4(combined, 1);
                }

                return float4(rgbSharpen, 1);
            }
            ENDCG
        }
    }
}
