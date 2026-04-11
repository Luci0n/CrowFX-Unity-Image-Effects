#ifndef CROWFX_COMMON_INCLUDED
#define CROWFX_COMMON_INCLUDED

inline float CrowFX_Luma(float3 c)
{
    return dot(c, float3(0.299, 0.587, 0.114));
}

inline float2 CrowFX_GetScreenResolution(float4 mainTexTexelSize)
{
    return float2(1.0 / mainTexTexelSize.x, 1.0 / mainTexTexelSize.y);
}

inline float2 CrowFX_GetBaseResolution(float useVirtualGrid, float4 virtualRes, float4 mainTexTexelSize)
{
    return (useVirtualGrid > 0.5)
        ? max(virtualRes.xy, 1.0)
        : CrowFX_GetScreenResolution(mainTexTexelSize);
}

inline float2 CrowFX_GetStepUV(float useVirtualGrid, float4 virtualRes, float4 mainTexTexelSize)
{
    return rcp(CrowFX_GetBaseResolution(useVirtualGrid, virtualRes, mainTexTexelSize));
}

inline float2 CrowFX_GetPixelStepUV(float pixelSize, float useVirtualGrid, float4 virtualRes, float4 mainTexTexelSize)
{
    return CrowFX_GetStepUV(useVirtualGrid, virtualRes, mainTexTexelSize) * max(pixelSize, 1.0);
}

inline float2 CrowFX_SnapToPixelBlocks(float2 uv, float pixelSize, float useVirtualGrid, float4 virtualRes, float4 mainTexTexelSize)
{
    float block = max(pixelSize, 1.0);
    if (block <= 1.0 && useVirtualGrid <= 0.5)
        return uv;

    float2 res = CrowFX_GetBaseResolution(useVirtualGrid, virtualRes, mainTexTexelSize);
    return floor(uv * res / block) * (block / res) + (0.5 * block / res);
}

inline float2 CrowFX_SnapToVirtualGrid(float2 uv, float4 virtualRes)
{
    float2 grid = max(virtualRes.xy, 1.0);
    return (floor(uv * grid) + 0.5) / grid;
}

inline float2 CrowFX_SafeUV(float2 uv, float clampUv)
{
    return (clampUv > 0.5) ? saturate(uv) : uv;
}

#endif
