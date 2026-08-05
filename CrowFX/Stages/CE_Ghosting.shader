Shader "Hidden/CrowFX/Stages/Ghosting"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _PrevTex ("Prev", 2D) = "black" {}

        _GhostEnabled ("Ghost Enabled", Float) = 0
        _GhostBlend ("Ghost Amount", Range(0,1)) = 0
        _GhostOffsetPx ("Ghost Offset (px)", Vector) = (0,0,0,0)

        // 0=Mix (lerp), 1=Add, 2=Screen, 3=Max
        _CombineMode ("Combine Mode", Float) = 2

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
            CROWFX_DECLARE_SCREEN_TEX(_PrevTex)
            float4 _MainTex_TexelSize;

            float _GhostEnabled, _GhostBlend;
            float4 _GhostOffsetPx;
            float _CombineMode;

            float _UseVirtualGrid;
            float4 _VirtualRes;

            // Compute step only if we actually need an offset.
            inline float2 StepUVFast()
            {
                return CrowFX_GetStepUV(_UseVirtualGrid, _VirtualRes, _MainTex_TexelSize);
            }

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                float2 uv = i.uv;

                // Always sample current.
                float3 cur = CROWFX_SAMPLE_SCREEN(_MainTex, uv).rgb;

                // EARLY OUT: if ghost is off, we do exactly 1 sample total.
                // Using <= 0.5 / <= 0 keeps it stable, and avoids extra work.
                if (_GhostEnabled <= 0.5 || _GhostBlend <= 0.0)
                    return float4(cur, 1.0);

                // Clamp blend once.
                float amt = saturate(_GhostBlend);

                // If offset is zero (common), don’t compute StepUV or add.
                float2 offPx = _GhostOffsetPx.xy;
                float2 uvPrev = uv;

                if (dot(offPx, offPx) > 0.0)
                {
                    float2 stepUV = StepUVFast();
                    uvPrev = uv + offPx * stepUV;
                }

                float3 prev = CROWFX_SAMPLE_SCREEN(_PrevTex, uvPrev).rgb;

                // Combine mode selection.
                float m = _CombineMode;

                // 0: Mix (lerp)
                if (m < 0.5)
                {
                    cur = lerp(cur, prev, amt);
                    return float4(cur, 1.0);
                }

                // Overlay modes should contribute only temporal residue. Compositing the
                // complete previous frame brightens an unchanged image and makes every preset
                // look permanently double-exposed even when nothing is moving.
                float3 positiveResidue = max(prev - cur, 0.0);
                float3 residueScaled = positiveResidue * amt;

                // 1: Add
                if (m < 1.5)
                {
                    cur = cur + residueScaled;
                    return float4(cur, 1.0);
                }

                // 2: Screen (result is already in [0,1] if inputs are)
                if (m < 2.5)
                {
                    cur = 1.0 - (1.0 - cur) * (1.0 - residueScaled);
                    return float4(cur, 1.0);
                }

                // 3: Max
                cur = max(cur, lerp(cur, prev, amt));
                return float4(cur, 1.0);
            }
            ENDCG
        }
    }
}
