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
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma vertex CrowFX_Vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "CE_Stereo.cginc"
            #include "CE_Common.cginc"

            CROWFX_DECLARE_SCREEN_TEX(_MainTex)
            float4 _MainTex_TexelSize;

            float _PixelSize;
            float _UseVirtualGrid;
            float4 _VirtualRes;
            float4 _SamplingPhase;
            float _PixelAspect, _SamplingFilter;

            // Highest supported box-filter footprint per axis. Bilinear taps each cover
            // a 2x2 texel neighbourhood, so 8 positions resolve blocks up to ~16 source
            // texels wide before the estimate starts to under-sample.
            #define CROWFX_MAX_BOX_TAPS 8

            // Averages the source texels a destination cell actually covers. Point
            // sampling a single texel per cell is what makes heavy pixelation crawl
            // and shimmer: thin geometry and speculars fall between sample points and
            // pop in and out as the camera moves. Integrating the cell removes that
            // temporal aliasing at its source rather than hiding it with a blur.
            float4 SampleCellAverage(float2 cellCenterUv, float2 cellSizeUv)
            {
                float2 cellTexels = cellSizeUv * _MainTex_TexelSize.zw;
                float2 tapsF = clamp(ceil(cellTexels * 0.5), 1.0, (float)CROWFX_MAX_BOX_TAPS);
                int2 taps = (int2)tapsF;

                float2 origin = cellCenterUv - cellSizeUv * 0.5;
                float2 stride = cellSizeUv / tapsF;

                // Explicit LOD rather than tex2D: neighbouring pixels can take different
                // numbers of iterations, which leaves screen-space derivatives (and so the
                // implicit mip selection tex2D performs) undefined inside these loops.
                // The source is a non-mipped intermediate target, so level 0 is the only
                // level and sampling it directly is both correct and cheaper.
                float4 sum = 0.0;
                [loop]
                for (int y = 0; y < CROWFX_MAX_BOX_TAPS; y++)
                {
                    if (y >= taps.y) break;

                    [loop]
                    for (int x = 0; x < CROWFX_MAX_BOX_TAPS; x++)
                    {
                        if (x >= taps.x) break;
                        float2 tapUv = origin + (float2(x, y) + 0.5) * stride;
                        sum += CROWFX_SAMPLE_SCREEN_LOD(_MainTex, tapUv);
                    }
                }

                return sum / max(tapsF.x * tapsF.y, 1.0);
            }

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                float2 uv = i.uv;
                bool gridActive = (_PixelSize > 1.0 || _UseVirtualGrid > 0.5);
                float2 cellSizeUv = 0.0;

                if (gridActive)
                {
                    float2 grid = CrowFX_GetBaseResolution(_UseVirtualGrid, _VirtualRes, _MainTex_TexelSize);
                    grid.x /= max(_PixelAspect, 0.001);
                    float block = max(_PixelSize, 1.0);
                    float2 phase = clamp(_SamplingPhase.xy, -0.5, 0.5);
                    cellSizeUv = block / grid;
                    uv = (floor(uv * grid / block + phase) - phase + 0.5) * cellSizeUv;
                }

                // Box integrates the cell; Point snaps to one texel center for hard
                // texel edges; Bilinear leaves reconstruction to the sampler.
                if (_SamplingFilter > 1.5)
                {
                    if (!gridActive)
                        cellSizeUv = _MainTex_TexelSize.xy;

                    return SampleCellAverage(uv, cellSizeUv);
                }

                if (_SamplingFilter < 0.5)
                {
                    float2 sourceSize = _MainTex_TexelSize.zw;
                    uv = (floor(uv * sourceSize) + 0.5) / sourceSize;
                }

                return CROWFX_SAMPLE_SCREEN(_MainTex, uv);
            }
            ENDCG
        }
    }
}
