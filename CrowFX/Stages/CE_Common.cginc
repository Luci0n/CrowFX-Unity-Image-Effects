#ifndef CROWFX_COMMON_INCLUDED
#define CROWFX_COMMON_INCLUDED

inline float CrowFX_Luma601(float3 c)
{
    return dot(c, float3(0.299, 0.587, 0.114));
}

inline float CrowFX_Luma709(float3 c)
{
    return dot(c, float3(0.2126, 0.7152, 0.0722));
}

inline float CrowFX_Luma(float3 c) { return CrowFX_Luma709(c); }

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

// Shared Lens & Sensor coordinate transform.
//
// This maps an output pixel back into the source image exactly as the Lens & Sensor
// stage does. Any stage that samples camera-space buffers after Lens & Sensor must use
// this transform or its depth/normals will remain in the undistorted coordinate space.
inline float2 CrowFX_LensSensorWarpUV(
    float2 uv,
    float lensDistortion,
    float rollingShutterPx,
    float effectIntensity,
    float overscan,
    float lensEdgeMode,
    float4 mainTexTexelSize,
    out float2 distortedP,
    out float2 aspectLensP,
    out float radius01,
    out float coverage)
{
    float lensMix = saturate(effectIntensity);
    float distortion = lensDistortion * lensMix;
    float rolling = rollingShutterPx * lensMix;

    float2 p = uv * 2.0 - 1.0;
    float rollingWave = sin(_Time.y * 9.0 + uv.y * 13.0) * rolling * mainTexTexelSize.x;
    p.x += rollingWave * 2.0;

    float2 targetRes = max(mainTexTexelSize.zw, 1.0);
    float aspect = max(targetRes.x / max(targetRes.y, 1.0), 1e-4);
    float2 lensP = float2(p.x * aspect, p.y);
    float cornerRadius = sqrt(aspect * aspect + 1.0);
    radius01 = saturate(length(lensP) / max(cornerRadius, 1e-4));

    float radialScale = max(0.25, 1.0 - distortion * 1.10 * (1.0 - radius01 * radius01));
    lensP *= radialScale;
    p = float2(lensP.x / aspect, lensP.y);

    float2 rawWarped = p * 0.5 + 0.5;
    rawWarped = (rawWarped - 0.5) / max(overscan, 1e-4) + 0.5;

    float2 halfTexel = mainTexTexelSize.xy * 0.5;
    float2 outside = max(abs(rawWarped - 0.5) - 0.5, 0.0);

    int edgeMode = (int)(lensEdgeMode + 0.5);
    float2 warped = (edgeMode == 2)
        ? 1.0 - abs(1.0 - fmod(abs(rawWarped), 2.0))
        : rawWarped;
    warped = clamp(warped, halfTexel, 1.0 - halfTexel);

    coverage = 1.0;
    if (edgeMode == 3)
    {
        float aaWidth = max(mainTexTexelSize.x, mainTexTexelSize.y) * 1.5;
        coverage = 1.0 - smoothstep(0.0, aaWidth, max(outside.x, outside.y));
    }

    distortedP = p;
    aspectLensP = lensP;
    return warped;
}

#endif
