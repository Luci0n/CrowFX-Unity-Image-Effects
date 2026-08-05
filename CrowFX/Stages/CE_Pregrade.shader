Shader "Hidden/CrowFX/Stages/Pregrade"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _PregradeEnabled ("Enabled", Float) = 0
        _Exposure ("Exposure (EV)", Float) = 0
        _Contrast ("Contrast", Float) = 1
        _Gamma ("Gamma", Float) = 1
        _Saturation ("Saturation", Float) = 1
        _PregradeTint ("Color Filter", Color) = (1,1,1,1)
        _PregradeTintStrength ("Color Filter Strength", Range(0,1)) = 0
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
            float _PregradeEnabled, _Exposure, _Contrast, _Gamma, _Saturation;
            float4 _PregradeTint;
            float _PregradeTintStrength;
            float4 _Lift, _Gain, _Offset;
            float _Temperature, _HighlightRolloff;

            inline float ApplyContrastCurve(float value, float contrastAmount)
            {
                if (contrastAmount <= 0.001) return 0.5;

                float centered = value * 2.0 - 1.0;
                float magnitude = pow(abs(centered), rcp(contrastAmount));
                return saturate(0.5 + 0.5 * sign(centered) * magnitude);
            }

            inline float3 ApplyPregrade(float3 c)
            {
                if (_PregradeEnabled < 0.5) return c;

                c *= exp2(_Exposure);
                c = (c + _Lift.rgb + _Offset.rgb) * max(_Gain.rgb, 0.0);
                float temperature = clamp(_Temperature, -1.0, 1.0);
                c *= float3(1.0 + temperature * 0.10, 1.0, 1.0 - temperature * 0.10);

                // Apply contrast to luminance with an endpoint-preserving S-curve.
                // Unlike a linear pivot followed by saturate, this does not create
                // expanding crushed-black and clipped-white regions above 1.0.
                float sourceLuma = CrowFX_Luma(c);
                float boundedLuma = saturate(sourceLuma);
                float contrastLuma = ApplyContrastCurve(boundedLuma, max(_Contrast, 0.0));
                if (sourceLuma > 1.0) contrastLuma += sourceLuma - 1.0;
                if (sourceLuma < 0.0) contrastLuma += sourceLuma;
                c = (sourceLuma > 1e-5) ? c * (contrastLuma / sourceLuma) : contrastLuma.xxx;
                float3 positive = max(c, 0.0);
                c = pow(positive, 1.0 / max(_Gamma, 0.001)) + min(c, 0.0);

                float l = CrowFX_Luma(c);
                c = lerp(float3(l,l,l), c, _Saturation);

                float3 filterColor = max(_PregradeTint.rgb, 0.001);
                float filterLuma = max(CrowFX_Luma(filterColor), 0.001);
                float3 filtered = c * (filterColor / filterLuma);
                c = lerp(c, filtered, saturate(_PregradeTintStrength));

                float3 overWhite = max(c - 1.0, 0.0);
                float3 rolled = min(c, 1.0) + overWhite / (1.0 + max(_HighlightRolloff, 0.0) * overWhite);
                c = lerp(c, rolled, step(0.0001, _HighlightRolloff));

                return c;
            }

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                float3 c = CROWFX_SAMPLE_SCREEN(_MainTex, i.uv).rgb;
                c = ApplyPregrade(c);
                return float4(c, 1);
            }
            ENDCG
        }
    }
}
