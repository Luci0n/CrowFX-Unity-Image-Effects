#ifndef CROWFX_SCENEBUFFERS_INCLUDED
#define CROWFX_SCENEBUFFERS_INCLUDED

// Scene buffers are screen-space targets and become texture arrays under stereo, so this
// header depends on the stereo macros. Included here rather than assumed from the call site.
#include "CE_Stereo.cginc"

// Depth is the only scene buffer every render pipeline binds under the same
// name.  Normals and motion vectors differ in name and encoding, so CrowFX
// publishes the active pipeline through a global float and selects the correct
// buffer here.  Stages then degrade predictably instead of sampling a texture
// the running pipeline never bound.
//
//   0 = Built-in Render Pipeline  (depth + view-space normals + motion vectors)
//   1 = Universal Render Pipeline (depth + prepass normals + motion vectors)
//   2 = Unsupported pipeline      (depth only)
#define CROWFX_SCENE_BIRP 0.0
#define CROWFX_SCENE_URP  1.0
#define CROWFX_SCENE_NONE 2.0

float _CrowFXSceneBufferMode;

CROWFX_DECLARE_SCREEN_TEX(_CameraDepthNormalsTexture)   // Built-in RP: DecodeDepthNormal-encoded
CROWFX_DECLARE_SCREEN_TEX(_CameraNormalsTexture)        // URP DepthNormals prepass: unencoded
CROWFX_DECLARE_SCREEN_TEX(_CameraMotionVectorsTexture)  // Built-in RP
CROWFX_DECLARE_SCREEN_TEX(_MotionVectorTexture)         // URP

inline bool CrowFX_HasSceneNormals()
{
    return _CrowFXSceneBufferMode < 1.5;
}

// On D3D-style APIs Unity flips the blit projection when rendering into a RenderTexture, which
// it signals by making _MainTex_TexelSize.y negative. The stage source is flipped along with it,
// but the camera's depth, normal and motion buffers are bound externally and are not, so their V
// axis has to be inverted to line back up with the image.
//
// Without this the outline, depth mask and datamosh vectors are all mirrored about the horizontal
// axis relative to the picture they are drawn onto: the shapes are the right shapes, sitting in
// the wrong place.
inline bool CrowFX_SceneBufferFlipped(float4 mainTexTexelSize)
{
#if UNITY_UV_STARTS_AT_TOP
    return mainTexTexelSize.y < 0.0;
#else
    return false;
#endif
}

inline float2 CrowFX_SceneBufferUV(float2 uv, float4 mainTexTexelSize)
{
    if (CrowFX_SceneBufferFlipped(mainTexTexelSize))
        uv.y = 1.0 - uv.y;
    return uv;
}

// Returns a unit normal for edge comparison.  Callers only ever take dot
// products between neighbouring samples, so the reference space does not
// matter; only the encoding has to match the buffer the pipeline bound.
// An unbound texture reads as black and normalizes to a constant, which
// reports zero angular difference and therefore falls back to depth-only
// edges rather than producing noise.
inline float3 CrowFX_SampleSceneNormal(float2 uv)
{
    if (_CrowFXSceneBufferMode < 0.5)
    {
        float ignoredDepth;
        float3 viewNormal;
        DecodeDepthNormal(CROWFX_SAMPLE_SCREEN(_CameraDepthNormalsTexture, uv), ignoredDepth, viewNormal);
        return viewNormal;
    }

    if (_CrowFXSceneBufferMode < 1.5)
        return normalize(CROWFX_SAMPLE_SCREEN(_CameraNormalsTexture, uv).xyz * 2.0 - 1.0 + 1e-5);

    return float3(0.0, 0.0, 1.0);
}

// Screen-space motion in UV units. Unsupported pipelines report no motion,
// which leaves datamosh driven purely by its deterministic codec-vector
// fallback instead of smearing along an unbound buffer.
inline float2 CrowFX_SampleMotionVector(float2 uv)
{
    if (_CrowFXSceneBufferMode < 0.5)
        return CROWFX_SAMPLE_SCREEN(_CameraMotionVectorsTexture, uv).rg;

    if (_CrowFXSceneBufferMode < 1.5)
        return CROWFX_SAMPLE_SCREEN(_MotionVectorTexture, uv).rg;

    return float2(0.0, 0.0);
}

/// Motion vectors need the flip applied twice over: once to read the correct texel, and once to
/// the vector itself, because a displacement expressed in the buffer's V axis points the opposite
/// way in a flipped working space.
inline float2 CrowFX_SampleMotionVectorFlipAware(float2 uv, float4 mainTexTexelSize)
{
    float2 motion = CrowFX_SampleMotionVector(CrowFX_SceneBufferUV(uv, mainTexTexelSize));
    if (CrowFX_SceneBufferFlipped(mainTexTexelSize))
        motion.y = -motion.y;
    return motion;
}

#endif
