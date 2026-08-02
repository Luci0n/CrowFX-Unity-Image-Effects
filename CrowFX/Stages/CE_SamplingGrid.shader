Shader "Hidden/CrowFX/Stages/SamplingGrid"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _PixelSize ("Pixel Size", Float) = 1
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

            float _PixelSize;
            float _UseVirtualGrid;
            float4 _VirtualRes;
            float4 _SamplingPhase;
            float _PixelAspect, _SamplingFilter;

            float4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                if (_PixelSize > 1.0 || _UseVirtualGrid > 0.5)
                {
                    float2 grid = CrowFX_GetBaseResolution(_UseVirtualGrid, _VirtualRes, _MainTex_TexelSize);
                    grid.x /= max(_PixelAspect, 0.001);
                    float block = max(_PixelSize, 1.0);
                    float2 phase = clamp(_SamplingPhase.xy, -0.5, 0.5);
                    uv = (floor(uv * grid / block + phase) - phase + 0.5) * (block / grid);
                }

                if (_SamplingFilter < 0.5)
                {
                    float2 sourceSize = _MainTex_TexelSize.zw;
                    uv = (floor(uv * sourceSize) + 0.5) / sourceSize;
                }

                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
