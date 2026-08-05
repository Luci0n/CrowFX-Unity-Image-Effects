Shader "Hidden/CrowFX/Stages/EdgeOutline"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}

        _EdgeEnabled ("Edge Enabled", Float) = 0
        _EdgeStrength ("Edge Strength", Range(0,8)) = 1
        _EdgeThreshold ("Edge Threshold (Linear)", Range(0,1)) = 0.02
        _EdgeBlend ("Edge Blend", Range(0,1)) = 1
        _EdgeColor ("Edge Color", Color) = (0,0,0,1)
        _EdgeThickness ("Edge Thickness", Range(0.5,4)) = 1
        _EdgeUseNormals ("Use Normals", Float) = 1
        _EdgeNormalThreshold ("Normal Threshold", Range(0,1)) = 0.18

        _UseVirtualGrid ("Use Virtual Grid", Float) = 0
        _VirtualRes ("Virtual Resolution (xy)", Vector) = (640,448,0,0)

        // Hidden stack state copied from Lens & Sensor so scene buffers use the
        // same coordinates as the already-distorted color image.
        [HideInInspector] _LensWarpEnabled ("Lens Warp Enabled", Float) = 0
        [HideInInspector] _LensWarpIntensity ("Lens Warp Intensity", Float) = 0
        [HideInInspector] _LensDistortion ("Lens Distortion", Float) = 0
        [HideInInspector] _LensRollingShutter ("Lens Rolling Shutter", Float) = 0
        [HideInInspector] _LensOverscan ("Lens Overscan", Float) = 1
        [HideInInspector] _LensEdgeMode ("Lens Edge Mode", Float) = 0
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
            #include "CE_SceneBuffers.cginc"

            CROWFX_DECLARE_SCREEN_TEX(_MainTex)
            float4 _MainTex_TexelSize;

            // Depth is bound under this name by every supported pipeline.
            // Normals come from CE_SceneBuffers, which resolves the correct
            // buffer for the active pipeline.
            CROWFX_DECLARE_SCREEN_TEX(_CameraDepthTexture)

            float _EdgeEnabled, _EdgeStrength, _EdgeThreshold, _EdgeBlend;
            float _EdgeThickness, _EdgeUseNormals, _EdgeNormalThreshold;
            float4 _EdgeColor;

            float _UseVirtualGrid;
            float4 _VirtualRes;
            float _PixelSize;

            float _LensWarpEnabled;
            float _LensWarpIntensity;
            float _LensDistortion;
            float _LensRollingShutter;
            float _LensOverscan;
            float _LensEdgeMode;

            inline float2 StepUV()
            {
                // One destination cell, not one source texel. At a pixel size or virtual grid
                // above 1:1 a texel-wide kernel finds detail the quantized image cannot show.
                return CrowFX_GetPixelStepUV(_PixelSize, _UseVirtualGrid, _VirtualRes, _MainTex_TexelSize);
            }

            inline float2 EdgeSceneBufferUV(float2 outputUV, out float coverage)
            {
                float2 sourceUV = outputUV;
                coverage = 1.0;

                if (_LensWarpEnabled > 0.5)
                {
                    float2 ignoredP;
                    float2 ignoredLensP;
                    float ignoredRadius;
                    sourceUV = CrowFX_LensSensorWarpUV(
                        outputUV,
                        _LensDistortion,
                        _LensRollingShutter,
                        _LensWarpIntensity,
                        _LensOverscan,
                        _LensEdgeMode,
                        _MainTex_TexelSize,
                        ignoredP,
                        ignoredLensP,
                        ignoredRadius,
                        coverage);
                }

                // Sampling & Grid runs before Lens & Sensor. Therefore the scene-buffer
                // coordinate must be warped first and snapped to that pre-lens lattice second.
                sourceUV = CrowFX_SnapToPixelBlocks(
                    sourceUV, _PixelSize, _UseVirtualGrid, _VirtualRes, _MainTex_TexelSize);

                // Camera depth/normals are external to the blit chain and may use the opposite V.
                return CrowFX_SceneBufferUV(sourceUV, _MainTex_TexelSize);
            }

            inline float EdgeFromDepth(float2 uv)
            {
                float2 stepUV = StepUV() * max(_EdgeThickness, 0.5);

                // The kernel lives in output space so its visible thickness stays stable.
                // Every tap is then mapped independently through the nonlinear lens transform.
                // Warping only the center and adding offsets afterwards would still make the
                // outline diverge near the corners.
                float coverageC, coverageR, coverageL, coverageU, coverageD;
                float2 uvC = EdgeSceneBufferUV(uv, coverageC);
                float2 uvR = EdgeSceneBufferUV(uv + float2(stepUV.x, 0), coverageR);
                float2 uvL = EdgeSceneBufferUV(uv - float2(stepUV.x, 0), coverageL);
                float2 uvU = EdgeSceneBufferUV(uv + float2(0, stepUV.y), coverageU);
                float2 uvD = EdgeSceneBufferUV(uv - float2(0, stepUV.y), coverageD);

                if (coverageC <= 0.0001)
                    return 0.0;

                float dC = LinearEyeDepth(CROWFX_SAMPLE_SCREEN(_CameraDepthTexture, uvC).r);
                float dR = LinearEyeDepth(CROWFX_SAMPLE_SCREEN(_CameraDepthTexture, uvR).r);
                float dL = LinearEyeDepth(CROWFX_SAMPLE_SCREEN(_CameraDepthTexture, uvL).r);
                float dU = LinearEyeDepth(CROWFX_SAMPLE_SCREEN(_CameraDepthTexture, uvU).r);
                float dD = LinearEyeDepth(CROWFX_SAMPLE_SCREEN(_CameraDepthTexture, uvD).r);

                // Black edge mode has no scene outside the image circle. Fade invalid taps
                // toward the center value so the sensor boundary itself is not mistaken for
                // a mesh silhouette.
                dR = lerp(dC, dR, coverageR);
                dL = lerp(dC, dL, coverageL);
                dU = lerp(dC, dU, coverageU);
                dD = lerp(dC, dD, coverageD);

                float diff = max(max(abs(dR - dC), abs(dL - dC)), max(abs(dU - dC), abs(dD - dC)));
                // Relative depth is stable as the camera and subject move through the scene.
                diff /= max(abs(dC), 0.01);

                float normalEdge = 0.0;
                if (_EdgeUseNormals > 0.5 && CrowFX_HasSceneNormals())
                {
                    float3 nC = CrowFX_SampleSceneNormal(uvC);
                    float3 nR = normalize(lerp(nC, CrowFX_SampleSceneNormal(uvR), coverageR));
                    float3 nL = normalize(lerp(nC, CrowFX_SampleSceneNormal(uvL), coverageL));
                    float3 nU = normalize(lerp(nC, CrowFX_SampleSceneNormal(uvU), coverageU));
                    float3 nD = normalize(lerp(nC, CrowFX_SampleSceneNormal(uvD), coverageD));
                    float nd = max(max(1.0 - dot(nC, nR), 1.0 - dot(nC, nL)),
                                   max(1.0 - dot(nC, nU), 1.0 - dot(nC, nD)));
                    normalEdge = smoothstep(_EdgeNormalThreshold, _EdgeNormalThreshold + 0.08, nd);
                }

                float depthEdge = smoothstep(_EdgeThreshold, _EdgeThreshold + max(0.002, _EdgeThreshold * 0.5), diff);
                float e = saturate(max(depthEdge, normalEdge) * _EdgeStrength);
                return e * coverageC;
            }

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                float2 uv = i.uv;
                float3 c = CROWFX_SAMPLE_SCREEN(_MainTex, uv).rgb;

                if (_EdgeEnabled > 0.5 && _EdgeBlend > 0.0)
                {
                    float e = EdgeFromDepth(uv);
                    c = lerp(c, _EdgeColor.rgb, saturate(e * _EdgeBlend));
                }

                return float4(c, 1);
            }
            ENDCG
        }
    }
}
