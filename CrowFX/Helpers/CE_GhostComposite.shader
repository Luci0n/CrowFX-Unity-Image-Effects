Shader "Hidden/CrowFX/Helpers/GhostComposite"
{
    Properties
    {
        _MainTex ("Unused", 2D) = "black" {} // required by Unity blit, not used
        _Count ("Count", Int) = 0
        _WeightCurve ("WeightCurve", Float) = 1.5
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
            // Include paths resolve relative to this shader, and this is the one stage that
            // lives outside Stages/.
            #include "../Stages/CE_Stereo.cginc"

            CROWFX_DECLARE_SCREEN_TEX(_MainTex)

            // History textures (newest -> oldest as bound by C#)
            CROWFX_DECLARE_SCREEN_TEX(_Hist0)
            CROWFX_DECLARE_SCREEN_TEX(_Hist1)
            CROWFX_DECLARE_SCREEN_TEX(_Hist2)
            CROWFX_DECLARE_SCREEN_TEX(_Hist3)
            CROWFX_DECLARE_SCREEN_TEX(_Hist4)
            CROWFX_DECLARE_SCREEN_TEX(_Hist5)
            CROWFX_DECLARE_SCREEN_TEX(_Hist6)
            CROWFX_DECLARE_SCREEN_TEX(_Hist7)
            CROWFX_DECLARE_SCREEN_TEX(_Hist8)
            CROWFX_DECLARE_SCREEN_TEX(_Hist9)
            CROWFX_DECLARE_SCREEN_TEX(_Hist10)
            CROWFX_DECLARE_SCREEN_TEX(_Hist11)
            CROWFX_DECLARE_SCREEN_TEX(_Hist12)
            CROWFX_DECLARE_SCREEN_TEX(_Hist13)
            CROWFX_DECLARE_SCREEN_TEX(_Hist14)
            CROWFX_DECLARE_SCREEN_TEX(_Hist15)

            int _Count;
            float _WeightCurve;
            float _DecayPerTap;

            float3 SampleHist(int idx, float2 uv)
            {
                if (idx == 0)  return CROWFX_SAMPLE_SCREEN(_Hist0,  uv).rgb;
                if (idx == 1)  return CROWFX_SAMPLE_SCREEN(_Hist1,  uv).rgb;
                if (idx == 2)  return CROWFX_SAMPLE_SCREEN(_Hist2,  uv).rgb;
                if (idx == 3)  return CROWFX_SAMPLE_SCREEN(_Hist3,  uv).rgb;
                if (idx == 4)  return CROWFX_SAMPLE_SCREEN(_Hist4,  uv).rgb;
                if (idx == 5)  return CROWFX_SAMPLE_SCREEN(_Hist5,  uv).rgb;
                if (idx == 6)  return CROWFX_SAMPLE_SCREEN(_Hist6,  uv).rgb;
                if (idx == 7)  return CROWFX_SAMPLE_SCREEN(_Hist7,  uv).rgb;
                if (idx == 8)  return CROWFX_SAMPLE_SCREEN(_Hist8,  uv).rgb;
                if (idx == 9)  return CROWFX_SAMPLE_SCREEN(_Hist9,  uv).rgb;
                if (idx == 10) return CROWFX_SAMPLE_SCREEN(_Hist10, uv).rgb;
                if (idx == 11) return CROWFX_SAMPLE_SCREEN(_Hist11, uv).rgb;
                if (idx == 12) return CROWFX_SAMPLE_SCREEN(_Hist12, uv).rgb;
                if (idx == 13) return CROWFX_SAMPLE_SCREEN(_Hist13, uv).rgb;
                if (idx == 14) return CROWFX_SAMPLE_SCREEN(_Hist14, uv).rgb;
                return CROWFX_SAMPLE_SCREEN(_Hist15, uv).rgb;
            }

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                int count = clamp(_Count, 0, 16);

                // If no history, output black (C# will avoid using it once we seed)
                if (count <= 0)
                    return float4(0,0,0,1);

                float3 acc = 0;
                float wsum = 0;

                // Weight newest -> oldest
                // w_i = pow(1 - i/(count-1), curve), normalized by wsum
                for (int k = 0; k < 16; k++)
                {
                    if (k >= count) break;

                    float t = (count <= 1) ? 1.0 : (1.0 - (k / (float)count));
                    float w = pow(saturate(t), max(_WeightCurve, 0.0001)) *
                              pow(saturate(_DecayPerTap), (float)k);

                    acc += SampleHist(k, i.uv) * w;
                    wsum += w;
                }

                acc /= max(wsum, 1e-6);
                return float4(acc, 1);
            }
            ENDCG
        }
    }
}
