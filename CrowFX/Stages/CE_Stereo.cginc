#ifndef CROWFX_STEREO_INCLUDED
#define CROWFX_STEREO_INCLUDED

// Single-pass instanced stereo renders both eyes into one 2D texture array, with the slice
// chosen by unity_StereoEyeIndex. A shader that declares its source as a plain sampler2D reads
// slice 0 for both eyes, so the right eye shows the left eye's image.
//
// Everything here funnels through Unity's own SCREENSPACE macros, which collapse to sampler2D
// and tex2D whenever no stereo keyword is set. The monoscopic path is therefore unchanged by
// construction: it compiles to exactly what these shaders had before.
//
// Only screen-space render targets - the stage source, history buffers, and camera depth,
// normal and motion buffers - become arrays. Author-supplied textures such as palettes, masks
// and blue noise stay ordinary 2D textures in both modes and must keep using sampler2D/tex2D.

#define CROWFX_DECLARE_SCREEN_TEX(name) UNITY_DECLARE_SCREENSPACE_TEXTURE(name);
#define CROWFX_SAMPLE_SCREEN(name, uv) UNITY_SAMPLE_SCREENSPACE_TEXTURE(name, uv)

// Explicit-LOD sampling of a screen-space target. Unity ships no screenspace equivalent, so the
// two forms are spelled out. Needed where a sample sits in divergent flow control and the
// implicit derivative would be undefined.
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define CROWFX_SAMPLE_SCREEN_LOD(name, uv) \
        UNITY_SAMPLE_TEX2DARRAY_LOD(name, float3((uv).xy, (float)unity_StereoEyeIndex), 0)
#else
    #define CROWFX_SAMPLE_SCREEN_LOD(name, uv) tex2Dlod(name, float4((uv).xy, 0, 0))
#endif

// Replaces appdata_img / v2f_img. The stereo eye index has to travel from the vertex stage to
// the fragment stage, which the built-in image structs make no room for.
struct CrowFX_VertInput
{
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct CrowFX_V2F
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

CrowFX_V2F CrowFX_Vert(CrowFX_VertInput v)
{
    CrowFX_V2F o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(CrowFX_V2F, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord;
    return o;
}

// Must be the first statement of every fragment function: it resolves unity_StereoEyeIndex,
// which every CROWFX_SAMPLE_SCREEN below it depends on.
#define CROWFX_SETUP_STEREO(i) UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i)

#endif
