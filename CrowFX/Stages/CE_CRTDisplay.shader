Shader "Hidden/CrowFX/Stages/CRTDisplay"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
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

            CROWFX_DECLARE_SCREEN_TEX(_MainTex)
            float4 _MainTex_TexelSize;
            float _Curvature, _Overscan;
            float _ScanlineStrength, _ScanlineCount, _BeamWidth;
            float _MaskMode, _MaskStrength, _MaskScale;
            float _Bloom, _BloomRadius;
            float _Vignette, _VignetteSoftness;
            float _Noise, _Flicker, _Brightness;
            float _TubeEdge, _BloomThreshold, _ConvergencePx, _Focus;
            float _BlackLevel, _HumBar, _FlickerHz;
            float _DisplaySignalDomain;

            // Resolution of the render target actually being processed. _ScreenParams
            // describes the backbuffer, which diverges from the target under render
            // scale, dynamic resolution, split screen, and off-screen cameras.
            #define CROWFX_TARGET_RES (_MainTex_TexelSize.zw)

            // Scanline weighting, phosphor masks, vignette and hum all scale encoded
            // drive values rather than radiometric intensity.  Applying them to linear
            // color makes every one of them substantially weaker than authored in a
            // Linear-space project, so the tube runs on signal like the tape and
            // composite stages do.
            float3 ToSignal(float3 c)
            {
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return max(c, 0.0);
                #else
                    return (_DisplaySignalDomain > 0.5) ? LinearToGammaSpace(max(c, 0.0)) : max(c, 0.0);
                #endif
            }

            float3 FromSignal(float3 c)
            {
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return c;
                #else
                    return (_DisplaySignalDomain > 0.5) ? GammaToLinearSpace(max(c, 0.0)) : max(c, 0.0);
                #endif
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            float GaussianNoise(float2 id, float salt)
            {
                float sum = Hash21(id + salt * 11.17) + Hash21(id.yx + salt * 29.41);
                sum += Hash21(id * float2(0.7549, 1.3719) + salt * 53.73);
                sum += Hash21(id * float2(1.9317, 0.6183) + salt * 89.13);
                return (sum - 2.0) * 0.5;
            }

            float2 Warp(float2 uv)
            {
                float2 p = uv * 2.0 - 1.0;
                p *= 1.0 + _Curvature * p.yx * p.yx;
                p /= max(0.8, _Overscan);
                return p * 0.5 + 0.5;
            }

            float3 SampleSignal(float2 uv)
            {
                return ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, uv).rgb);
            }

            float3 SampleSoft(float2 uv)
            {
                float2 dx = float2(_MainTex_TexelSize.x, 0.0);
                float3 center = SampleSignal(uv);
                float3 soft = center * 0.50 +
                              (SampleSignal(uv - dx) + SampleSignal(uv + dx)) * 0.25;
                return lerp(center, soft, saturate(_Focus));
            }

            float BeamWeight(float distanceToLine, float luminance)
            {
                // Generalized Gaussian beam. Brighter phosphor excitation widens the beam.
                float width = max(0.12, _BeamWidth * lerp(0.72, 1.28, saturate(luminance)));
                return exp2(-3.0 * distanceToLine * distanceToLine / (width * width));
            }

            float3 RasterBeam(float2 uv)
            {
                float lines = _ScanlineCount > 0.5 ? _ScanlineCount : max(1.0, _MainTex_TexelSize.w * 0.5);
                float linePosition = uv.y * lines - 0.5;
                float baseLine = floor(linePosition);
                float fractionOnLine = frac(linePosition);
                float2 lineUv = float2(uv.x, (baseLine + 0.5) / lines);

                float3 previous = SampleSoft(lineUv - float2(0.0, 1.0 / lines));
                float3 current = SampleSoft(lineUv);
                float3 next = SampleSoft(lineUv + float2(0.0, 1.0 / lines));
                float wp = BeamWeight(fractionOnLine + 1.0, dot(previous, float3(0.2126, 0.7152, 0.0722)));
                float wc = BeamWeight(fractionOnLine, dot(current, float3(0.2126, 0.7152, 0.0722)));
                float wn = BeamWeight(fractionOnLine - 1.0, dot(next, float3(0.2126, 0.7152, 0.0722)));
                float weightSum = max(wp + wc + wn, 1e-5);
                return (previous * wp + current * wc + next * wn) / weightSum;
            }

            float3 PhosphorMask(float2 pixel)
            {
                float2 cell = floor(pixel / max(1.0, _MaskScale));
                float row = cell.y;
                float column = cell.x;

                if (_MaskMode > 0.5)
                    column += fmod(floor(row / (_MaskMode < 1.5 ? 2.0 : 1.0)), 2.0);

                float phase = fmod(column, 3.0);
                float3 mask = phase < 0.5 ? float3(1.45, 0.55, 0.55) :
                              phase < 1.5 ? float3(0.55, 1.45, 0.55) : float3(0.55, 0.55, 1.45);
                mask /= 0.85; // preserve average phosphor energy before separator losses

                // Slot masks contain dark horizontal separator wires.
                if (_MaskMode > 0.5 && _MaskMode < 1.5 && fmod(row, 4.0) > 2.5)
                    mask *= 0.62;
                // Shadow-mask dots have a softer vertical aperture.
                if (_MaskMode > 1.5)
                    mask *= lerp(0.72, 1.0, 1.0 - abs(frac(pixel.y / max(1.0, _MaskScale)) - 0.5) * 2.0);
                return mask;
            }

            float3 Halation(float2 uv)
            {
                float2 r = _MainTex_TexelSize.xy * _BloomRadius;
                float3 nearGlow = 0.0;
                nearGlow += SampleSignal(uv + float2( r.x, 0.0));
                nearGlow += SampleSignal(uv + float2(-r.x, 0.0));
                nearGlow += SampleSignal(uv + float2(0.0,  r.y));
                nearGlow += SampleSignal(uv + float2(0.0, -r.y));
                float3 farGlow = 0.0;
                farGlow += SampleSignal(uv + r * 2.5);
                farGlow += SampleSignal(uv - r * 2.5);
                farGlow += SampleSignal(uv + float2(r.x, -r.y) * 2.5);
                farGlow += SampleSignal(uv + float2(-r.x, r.y) * 2.5);
                float3 glow = nearGlow * 0.175 + farGlow * 0.075;
                float luminance = dot(glow, float3(0.2126, 0.7152, 0.0722));
                float gate = smoothstep(_BloomThreshold, _BloomThreshold + 0.25, luminance);
                return glow * gate;
            }

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                float2 uv = Warp(i.uv);
                float2 clampedUv = saturate(uv);
                float3 clean = SampleSignal(clampedUv);
                float3 beam = RasterBeam(clampedUv);
                float lines = _ScanlineCount > 0.5 ? _ScanlineCount : max(1.0, _MainTex_TexelSize.w * 0.5);
                float pixelsPerLine = CROWFX_TARGET_RES.y / max(lines, 1.0);
                float rasterAA = saturate((pixelsPerLine - 0.45) / 1.35);
                float3 color = lerp(clean, beam, _ScanlineStrength * rasterAA);

                if (_ConvergencePx > 0.001)
                {
                    float2 convergence = float2(_MainTex_TexelSize.x * _ConvergencePx, 0.0);
                    float red = SampleSignal(saturate(clampedUv + convergence)).r;
                    float blue = SampleSignal(saturate(clampedUv - convergence)).b;
                    float convergenceMix = saturate(_ConvergencePx / 3.0);
                    color.r = lerp(color.r, red, convergenceMix);
                    color.b = lerp(color.b, blue, convergenceMix);
                }

                float3 glow = Halation(clampedUv);
                color += max(0.0, glow - color * 0.35) * _Bloom;
                float maskAA = saturate((_MaskScale - 0.65) / 1.35);
                color *= lerp(float3(1.0, 1.0, 1.0), PhosphorMask(i.uv * CROWFX_TARGET_RES), _MaskStrength * maskAA);

                float2 p = abs(uv * 2.0 - 1.0);
                float roundedEdge = length(max(p - float2(0.88, 0.84), 0.0));
                float tube = 1.0 - smoothstep(0.08, 0.08 + _VignetteSoftness, roundedEdge);
                float radial = 1.0 - _Vignette * smoothstep(0.28, 1.25, dot(p, p));
                float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                float tubeControl = lerp(1.0, tube, saturate(_TubeEdge));
                color *= tubeControl * radial * inside * _Brightness;
                color = max(0.0, color - _BlackLevel);

                float humCenter = frac(_Time.y * 0.18);
                float humDistance = min(abs(i.uv.y - humCenter), 1.0 - abs(i.uv.y - humCenter));
                color *= 1.0 + exp2(-humDistance * humDistance * 180.0) * _HumBar;

                // Frame-decorrelated grain: no spatial translation, hence no scrolling pattern.
                float frame = floor(_Time.y * 60.0);
                float2 noisePixel = floor(i.uv * CROWFX_TARGET_RES);
                float grain = GaussianNoise(noisePixel + float2(frame * 17.17, frame * 113.1), 5.0);
                float signalNoise = grain * _Noise * lerp(1.35, 0.45, saturate(dot(clean, float3(0.2126, 0.7152, 0.0722))));
                color += signalNoise * inside;
                color *= 1.0 + sin(_Time.y * 6.28318530718 * max(_FlickerHz, 1.0)) * _Flicker;
                return float4(FromSignal(max(0.0, color)), 1.0);
            }
            ENDCG
        }
    }
}
