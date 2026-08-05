Shader "Hidden/CrowFX/Stages/ProfessionalEffects"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _HistoryTex ("Previous Frame", 2D) = "black" {}
    }
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
            #include "CE_Common.cginc"
            #include "CE_SceneBuffers.cginc"

            CROWFX_DECLARE_SCREEN_TEX(_MainTex)
            CROWFX_DECLARE_SCREEN_TEX(_HistoryTex)
            float4 _MainTex_TexelSize;
            float _ProfessionalMode, _EffectIntensity;
            float4 _ParamA, _ParamB, _ParamC;
            float _DisplaySignalDomain;

            // Resolution of the render target actually being processed. _ScreenParams
            // describes the backbuffer, which diverges from the target under render
            // scale, dynamic resolution, split screen, and off-screen cameras.
            #define CROWFX_TARGET_RES (_MainTex_TexelSize.zw)

            float Hash21(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            float Noise21(float2 p)
            {
                float2 id = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(id), Hash21(id + float2(1, 0)), f.x),
                            lerp(Hash21(id + float2(0, 1)), Hash21(id + 1.0), f.x), f.y);
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            // Time is a separate hash dimension, not an offset added to the image plane.
            // Every film frame therefore receives a newly exposed grain field with no
            // coherent translation for the eye to track.
            float TemporalGaussianNoise(float2 id, float frameId, float salt)
            {
                float3 key = float3(id, frameId);
                float sum = Hash31(key + float3(salt * 11.17, salt * 7.31, salt * 3.13));
                sum += Hash31(key.yxz + float3(salt * 29.41, salt * 17.03, salt * 5.71));
                sum += Hash31(key * float3(0.7549, 1.3719, 0.9173) + salt * 53.73);
                sum += Hash31(key * float3(1.9317, 0.6183, 1.2371) + salt * 89.13);
                return (sum - 2.0) * 0.5;
            }

            float3 ToSignal(float3 c)
            {
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return max(c, 0.0);
                #else
                    return LinearToGammaSpace(max(c, 0.0));
                #endif
            }

            float3 FromSignal(float3 c)
            {
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return c;
                #else
                    return GammaToLinearSpace(max(c, 0.0));
                #endif
            }

            // Panel simulation multiplies and offsets encoded drive values, not
            // radiometric intensity.  Running it in linear makes subpixel masks and
            // inversion far weaker than intended in Linear-space projects, so the
            // LCD stage matches the tape and composite stages and works on signal.
            float3 ToDisplaySignal(float3 c)
            {
                return (_DisplaySignalDomain > 0.5) ? ToSignal(c) : max(c, 0.0);
            }

            float3 FromDisplaySignal(float3 c)
            {
                return (_DisplaySignalDomain > 0.5) ? FromSignal(c) : c;
            }

            float3 RGBtoYIQ(float3 c)
            {
                return float3(dot(c, float3(0.299, 0.587, 0.114)),
                              dot(c, float3(0.596, -0.274, -0.322)),
                              dot(c, float3(0.211, -0.523, 0.312)));
            }

            float3 YIQtoRGB(float3 c)
            {
                return float3(c.x + 0.956*c.y + 0.621*c.z,
                              c.x - 0.272*c.y - 0.647*c.z,
                              c.x - 1.106*c.y + 1.703*c.z);
            }

            float3 LensSensor(float2 uv)
            {
                // Lens mix scales physical parameters instead of cross-fading two different
                // geometries. Cross-fading a warped and unwarped frame creates false double edges.
                float lensMix = saturate(_EffectIntensity);
                float chromaPx = _ParamA.y * lensMix;
                float vignette = _ParamA.z * lensMix;
                float bloom = _ParamA.w * lensMix;
                float noiseAmount = _ParamB.y * lensMix;
                float deadRate = _ParamB.z * lensMix;
                float bloomRadius = _ParamB.w;

                // Keep this mapping shared with stages that read scene depth/normals after
                // Lens & Sensor. In particular, Edge Outline must transform every kernel
                // sample through the same nonlinear mapping or the line remains undistorted.
                float2 p;
                float2 lensP;
                float radius01;
                float coverage;
                float2 warped = CrowFX_LensSensorWarpUV(
                    uv,
                    _ParamA.x,
                    _ParamB.x,
                    _EffectIntensity,
                    _ParamC.x,
                    _ParamC.y,
                    _MainTex_TexelSize,
                    p,
                    lensP,
                    radius01,
                    coverage);

                float2 halfTexel = _MainTex_TexelSize.xy * 0.5;
                float aspect = max(CROWFX_TARGET_RES.x / max(CROWFX_TARGET_RES.y, 1.0), 1e-4);

                float2 radialDir = lensP / max(length(lensP), 1e-5);
                radialDir.x /= aspect;
                float2 radial = radialDir * chromaPx * _MainTex_TexelSize.xy * radius01;

                float3 color;
                color.r = CROWFX_SAMPLE_SCREEN(_MainTex, clamp(warped + radial, halfTexel, 1.0 - halfTexel)).r;
                color.g = CROWFX_SAMPLE_SCREEN(_MainTex, warped).g;
                color.b = CROWFX_SAMPLE_SCREEN(_MainTex, clamp(warped - radial, halfTexel, 1.0 - halfTexel)).b;

                float2 br = _MainTex_TexelSize.xy * bloomRadius;
                float3 glow = (CROWFX_SAMPLE_SCREEN(_MainTex, saturate(warped + float2(br.x, 0))).rgb +
                               CROWFX_SAMPLE_SCREEN(_MainTex, saturate(warped - float2(br.x, 0))).rgb +
                               CROWFX_SAMPLE_SCREEN(_MainTex, saturate(warped + float2(0, br.y))).rgb +
                               CROWFX_SAMPLE_SCREEN(_MainTex, saturate(warped - float2(0, br.y))).rgb) * 0.25;
                float glowGate = smoothstep(0.7, 1.15, dot(glow, float3(0.2126, 0.7152, 0.0722)));
                color += glow * glowGate * bloom;

                float radialVignette = smoothstep(0.2, 1.3, dot(p, p));
                color *= 1.0 - vignette * radialVignette;
                float frame = floor(_Time.y * 60.0);
                float2 pixel = floor(uv * CROWFX_TARGET_RES);
                float grain = TemporalGaussianNoise(pixel, frame, 3.0);
                color += grain * noiseAmount * lerp(1.3, 0.45, saturate(dot(color, float3(0.333, 0.333, 0.333))));
                float dead = step(1.0 - deadRate * 0.0015, Hash21(pixel + 317.0));
                float deadValue = Hash21(pixel + 811.0) > 0.5 ? 0.0 : 1.0;
                color = lerp(color, float3(deadValue, deadValue, deadValue), dead);

                // Applied last so the uncovered region carries no bloom, grain or sensor
                // defects either: outside the image circle nothing reached the sensor.
                return color * coverage;
            }

            // -----------------------------------------------------------------------------
            // Film dust
            //
            // Dust on film is a sparse population of hard-edged, irregular particles that is
            // redrawn every frame as fresh film passes the gate. Most of it reads BRIGHT:
            // a particle sitting on the negative blocks printing light, leaving unexposed
            // white on the print. Dark specks are the minority - debris on the print itself
            // or in the gate - and some of that gate dirt persists for a fraction of a second
            // while everything else is replaced frame to frame.
            //
            // Each layer scatters at most one particle per cell of its own lattice, but the
            // particle is jittered freely inside the cell, sized with a cubic bias toward
            // small, rotated, and optionally stretched into a fibre. Three non-harmonic
            // densities are summed, which leaves no perceptible lattice while costing one
            // cell lookup per layer rather than a neighbourhood search.
            //
            // Returns bright coverage in .x and dark coverage in .y.
            // -----------------------------------------------------------------------------
            float2 FilmDustLayer(float2 p, float frame, float amount, float opacity, float polarity,
                                 float density, float hitRate, float minRadius, float maxRadius,
                                 float maxStretch, float salt, float targetHeight)
            {
                float2 scaled = p * density;
                float2 cell = floor(scaled);
                float2 local = frac(scaled);

                // A frame-independent draw marks a minority of positions as gate dirt, which
                // holds for roughly two thirds of a second instead of being replaced.
                float sticky = step(0.86, Hash31(float3(cell, 0.0) + salt * 3.17));
                float timeIndex = lerp(frame, floor(frame * 0.06), sticky);

                // Time is its own hash dimension. Adding it to the cell id, as the previous
                // implementation did, translates the entire field instead of regenerating it.
                float3 key = float3(cell, timeIndex) + salt;

                float present = step(1.0 - saturate(amount) * hitRate, Hash31(key));

                float sizeRand = Hash31(key + 41.1);
                float stretchRand = Hash31(key + 63.9);
                float angleRand = Hash31(key + 87.3);

                // Bright particles are dust on the negative blocking printing light, which
                // leaves the print unexposed. Dark particles are dirt on the print or in the
                // gate blocking projected light.
                float bright = step(1.0 - saturate(polarity), Hash31(key + 103.7));

                // Real particles are not uniformly opaque: fibres, emulsion chips and grit
                // block light completely, while fine dust, oil and moisture only attenuate it.
                // Taking the larger of an independent draw and the size draw makes big debris
                // skew opaque, so lowering the control mostly thins out the fine specks.
                float opacityRand = max(Hash31(key + 131.7), sizeRand);
                float particleOpacity = saturate(opacity) * lerp(0.5, 1.0, opacityRand);

                // Cubic bias: mostly fine grit, occasionally something large.
                float radius = lerp(minRadius, maxRadius, sizeRand * sizeRand * sizeRand);
                float stretch = lerp(1.0, maxStretch, stretchRand * stretchRand);
                float2 shape = float2(radius * stretch, radius * rsqrt(stretch));

                // Fit the particle inside its cell so no edge is ever clipped, then spend
                // whatever room is left on scattering it. Small particles - the majority -
                // end up almost freely placed, which is what hides the lattice.
                float extent = max(shape.x, shape.y);
                float room = max(0.0, 0.5 - extent);
                float2 jitter = float2(Hash31(key + 11.3), Hash31(key + 27.7)) * 2.0 - 1.0;

                float2 d = local - (0.5 + jitter * room);
                float angle = angleRand * 6.2831853;
                float s = sin(angle);
                float c = cos(angle);
                d = float2(c * d.x - s * d.y, s * d.x + c * d.y);

                float dist = length(d / max(shape, 1e-5));

                // Analytic edge width: one output pixel expressed in cell units over the
                // narrowest axis of the particle. Screen-space derivatives cannot be used
                // here because cell and local are discontinuous at every cell boundary.
                float pixelInCells = density / max(targetHeight, 1.0);
                float aa = saturate(pixelInCells / max(min(shape.x, shape.y), 1e-5));
                float mask = present * particleOpacity * (1.0 - smoothstep(1.0 - aa, 1.0 + aa, dist));

                return float2(mask * bright, mask * (1.0 - bright));
            }

            float3 FilmDust(float3 color, float2 uv, float amount, float opacity, float polarity,
                            float frame, float aspect, float targetHeight)
            {
                if (amount <= 0.0001 || opacity <= 0.0001) return color;

                // Square-ish working space so particles stay round instead of inheriting
                // the frame's aspect ratio the way a fixed uv lattice does.
                float2 p = float2(uv.x * aspect, uv.y);

                float2 fine   = FilmDustLayer(p, frame, amount, opacity, polarity, 41.0, 0.10, 0.035, 0.130, 1.6,  7.0, targetHeight);
                float2 medium = FilmDustLayer(p, frame, amount, opacity, polarity, 23.0, 0.08, 0.050, 0.170, 2.2, 29.0, targetHeight);
                float2 fibres = FilmDustLayer(p, frame, amount, opacity, polarity,  9.0, 0.07, 0.040, 0.065, 6.0, 53.0, targetHeight);

                float brightCoverage = saturate(fine.x + medium.x + fibres.x);
                float darkCoverage   = saturate(fine.y + medium.y + fibres.y);

                // Dark debris first: a bright particle is opaque on the negative, so where
                // the two overlap the unexposed highlight wins.
                color = lerp(color, float3(0.015, 0.014, 0.012), darkCoverage);
                color = lerp(color, float3(0.97, 0.96, 0.92), brightCoverage);
                return color;
            }

            float3 Film(float2 uv)
            {
                float grainAmount = _ParamA.x;
                float grainSize = max(_ParamA.y, 0.5);
                float halation = _ParamA.z;
                float halationRadius = _ParamA.w;
                float weave = _ParamB.x;
                float dustAmount = _ParamB.y;
                float scratchAmount = _ParamB.z;
                float flicker = _ParamB.w;

                float frame = floor(_Time.y * 24.0);
                float2 weavePx = float2(sin(frame * 1.618), cos(frame * 1.173)) * weave;
                float2 sampleUv = saturate(uv + weavePx * _MainTex_TexelSize.xy);
                float3 color = CROWFX_SAMPLE_SCREEN(_MainTex, sampleUv).rgb;

                float2 r = _MainTex_TexelSize.xy * halationRadius;
                float3 surround = (CROWFX_SAMPLE_SCREEN(_MainTex, saturate(sampleUv + float2(r.x, 0))).rgb +
                                   CROWFX_SAMPLE_SCREEN(_MainTex, saturate(sampleUv - float2(r.x, 0))).rgb +
                                   CROWFX_SAMPLE_SCREEN(_MainTex, saturate(sampleUv + float2(0, r.y))).rgb +
                                   CROWFX_SAMPLE_SCREEN(_MainTex, saturate(sampleUv - float2(0, r.y))).rgb) * 0.25;
                float hot = smoothstep(0.62, 1.05, dot(surround, float3(0.2126, 0.7152, 0.0722)));
                color += surround * float3(1.0, 0.22, 0.08) * hot * halation;

                float2 grainPixel = floor(uv * CROWFX_TARGET_RES / grainSize);
                float fineGrain = TemporalGaussianNoise(grainPixel, frame, 7.0);
                float coarseGrain = TemporalGaussianNoise(floor(grainPixel * 0.43), frame, 11.0);
                float grain = fineGrain * 0.78 + coarseGrain * 0.22;
                float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
                color += grain * grainAmount * lerp(0.55, 1.25, 1.0 - saturate(abs(luma - 0.45) * 1.8));

                float scratchX = abs(frac(uv.x * 317.0 + Hash21(float2(frame, frame)) * 0.13) - 0.5);
                float scratch = step(1.0 - scratchAmount * 0.025, Hash21(float2(floor(uv.x * 317.0), floor(frame / 8.0)))) * smoothstep(0.035, 0.0, scratchX);
                color += scratch * 0.32;

                float aspect = max(CROWFX_TARGET_RES.x / max(CROWFX_TARGET_RES.y, 1.0), 1e-4);
                color = FilmDust(color, uv, dustAmount, _ParamC.x, _ParamC.y, frame, aspect, CROWFX_TARGET_RES.y);
                color *= 1.0 + (Noise21(float2(_Time.y * 4.0, 7.1)) - 0.5) * flicker;
                return color;
            }

            float3 MotionGlitch(float2 uv)
            {
                float blockSize = max(_ParamA.x, 4.0);
                float displacement = _ParamA.y;
                float freezeRate = _ParamA.z;
                float colorSplit = _ParamA.w;
                float2 block = floor(uv * CROWFX_TARGET_RES / blockSize);
                float2 blockUv = (block * blockSize + blockSize * 0.5) / CROWFX_TARGET_RES;
                float2 motion = CrowFX_SampleMotionVectorFlipAware(blockUv, _MainTex_TexelSize);
                float epoch = floor(_Time.y * 12.0);
                float eventNoise = Hash21(block + epoch * float2(13.0, 47.0));
                float frozen = step(1.0 - freezeRate, eventNoise);
                // Real motion vectors drive the smear. A small deterministic codec-vector
                // fallback keeps authored datamosh visible in static previews and paused cameras.
                float2 codecVector = (float2(Hash21(block + epoch * 7.13), Hash21(block + epoch * 19.71 + 83.0)) * 2.0 - 1.0)
                                     * displacement * _MainTex_TexelSize.xy;
                float quietMotion = 1.0 - saturate(length(motion) * 256.0);
                float2 shifted = saturate(uv - motion * displacement + codecVector * quietMotion * frozen);
                float3 current = CROWFX_SAMPLE_SCREEN(_MainTex, shifted).rgb;
                float3 history = CROWFX_SAMPLE_SCREEN(_HistoryTex, shifted).rgb;
                float3 color = lerp(current, history, frozen);
                float split = colorSplit * _MainTex_TexelSize.x * frozen;
                float splitMix = frozen * saturate(colorSplit * 0.25);
                color.r = lerp(color.r, CROWFX_SAMPLE_SCREEN(_MainTex, saturate(shifted + float2(split, 0))).r, splitMix);
                color.b = lerp(color.b, CROWFX_SAMPLE_SCREEN(_HistoryTex, saturate(shifted - float2(split, 0))).b, splitMix);
                return color;
            }

            float3 DigitalVideo(float2 uv)
            {
                float blockSize = max(_ParamA.x, 4.0);
                float quantization = _ParamA.y;
                float ringing = _ParamA.z;
                float chromaSub = _ParamA.w;
                float mosquito = _ParamB.x;
                float pumping = _ParamB.y;

                float2 pixel = uv * CROWFX_TARGET_RES;
                float2 blockUv = (floor(pixel / blockSize) * blockSize + blockSize * 0.5) / CROWFX_TARGET_RES;
                float3 source = ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, uv).rgb);
                float3 blockColor = ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, blockUv).rgb);
                float y = dot(source, float3(0.299, 0.587, 0.114));
                float yBlock = dot(blockColor, float3(0.299, 0.587, 0.114));
                float2 chroma = float2(source.r - y, source.b - y);
                float2 blockChroma = float2(blockColor.r - yBlock, blockColor.b - yBlock);
                chroma = lerp(chroma, blockChroma, chromaSub);

                float pump = 1.0 + (0.5 + 0.5 * sin(_Time.y * 2.3)) * pumping * 2.5;
                float levels = lerp(255.0, 8.0, saturate(quantization * pump));
                y = floor(y * levels + 0.5) / levels;
                chroma = floor(chroma * levels + 0.5) / levels;
                float3 compressed = float3(y + chroma.x, y - chroma.x * 0.51 - chroma.y * 0.19, y + chroma.y);

                float2 dx = float2(_MainTex_TexelSize.x * 2.0, 0.0);
                float3 left = ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, saturate(uv - dx)).rgb);
                float3 right = ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, saturate(uv + dx)).rgb);
                float3 edge = source * 2.0 - left - right;
                compressed += edge * ringing * 0.18;
                float edgeGate = saturate(length(edge) * 4.0);
                compressed += TemporalGaussianNoise(floor(pixel), floor(_Time.y * 24.0), 17.0) * mosquito * edgeGate * 0.12;
                return FromSignal(compressed);
            }

            float3 CompositeSignal(float2 uv)
            {
                float dotCrawl = _ParamA.x;
                float rainbow = _ParamA.y;
                float bandwidth = _ParamA.z;
                float phaseError = _ParamA.w;
                float comb = _ParamB.x;
                float standard = _ParamB.y;

                float3 center = RGBtoYIQ(ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, uv).rgb));
                float radius = lerp(10.0, 0.65, bandwidth) * _MainTex_TexelSize.x;
                float2 dx = float2(radius, 0.0);
                float3 leftNear = RGBtoYIQ(ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, saturate(uv - dx * 0.5)).rgb));
                float3 rightNear = RGBtoYIQ(ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, saturate(uv + dx * 0.5)).rgb));
                float3 leftFar = RGBtoYIQ(ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, saturate(uv - dx)).rgb));
                float3 rightFar = RGBtoYIQ(ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, saturate(uv + dx)).rgb));

                // Composite chroma has far less horizontal bandwidth than luma.  A five-tap
                // approximation gives the bandwidth control a useful soft-to-smeared range.
                float2 chroma = center.yz * 0.28 + (leftNear.yz + rightNear.yz) * 0.24 +
                                (leftFar.yz + rightFar.yz) * 0.12;

                float lineIndex = floor(uv.y * _MainTex_TexelSize.w);
                float cycles = standard < 0.5 ? 227.5 : 283.75;
                float fieldRate = standard < 0.5 ? 59.94 : 50.0;
                float fieldIndex = floor(_Time.y * fieldRate);
                float fieldSequence = standard < 0.5 ? fmod(fieldIndex, 4.0) * 0.25 :
                                                       fmod(fieldIndex, 8.0) * 0.125;
                float phase = (uv.x * cycles + lineIndex * (standard < 0.5 ? 0.5 : 0.25) + fieldSequence) * 6.2831853;
                float2 carrier = float2(cos(phase), sin(phase));

                // Time-base/subcarrier error changes decoded hue per scanline.  The old code
                // only nudged the artifact carrier by a few hundredths of a radian, making the
                // control effectively invisible.
                float phaseRandom = Hash21(float2(lineIndex + 17.0, fieldIndex + 53.0)) * 2.0 - 1.0;
                float phaseRadians = phaseRandom * phaseError * (standard < 0.5 ? 1.25 : 0.80);
                float phaseSin = sin(phaseRadians);
                float phaseCos = cos(phaseRadians);
                chroma = float2(phaseCos * chroma.x - phaseSin * chroma.y,
                                phaseSin * chroma.x + phaseCos * chroma.y);

                // A better comb filter rejects more Y/C crosstalk.  At zero quality the
                // controls can now reach severe consumer-decoder artifacts; at one they leave
                // only a small residual instead of silently disabling the whole stage.
                float decoderLeak = lerp(1.0, 0.08, saturate(comb));
                float lumaHigh = center.x - (leftNear.x + rightNear.x) * 0.5;
                float chromaLeak = dot(center.yz, carrier);
                chroma += carrier * lumaHigh * rainbow * decoderLeak * 1.35;
                center.x += chromaLeak * dotCrawl * decoderLeak * 0.55;

                float3 above = RGBtoYIQ(ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, saturate(uv - float2(0, _MainTex_TexelSize.y))).rgb));
                float3 below = RGBtoYIQ(ToSignal(CROWFX_SAMPLE_SCREEN(_MainTex, saturate(uv + float2(0, _MainTex_TexelSize.y))).rgb));
                float2 verticalComb = chroma * 0.5 + (above.yz + below.yz) * 0.25;
                chroma = lerp(chroma, verticalComb, saturate(comb) * 0.45);
                return FromSignal(YIQtoRGB(float3(center.x, chroma)));
            }

            float3 Lcd(float2 uv)
            {
                float scale = max(_ParamA.x, 1.0);
                float subpixel = _ParamA.y;
                float inversion = _ParamA.z;
                float viewAngle = _ParamA.w;
                float backlight = _ParamB.x;
                float smear = _ParamB.y;
                float2 pixel = floor(uv * CROWFX_TARGET_RES / scale) * scale + scale * 0.5;
                float2 sampleUv = pixel / CROWFX_TARGET_RES;
                float3 color = ToDisplaySignal(CROWFX_SAMPLE_SCREEN(_MainTex, sampleUv).rgb);
                float3 response = (ToDisplaySignal(CROWFX_SAMPLE_SCREEN(_MainTex, saturate(sampleUv - float2(_MainTex_TexelSize.x * smear, 0))).rgb) +
                                   ToDisplaySignal(CROWFX_SAMPLE_SCREEN(_MainTex, saturate(sampleUv + float2(_MainTex_TexelSize.x * smear, 0))).rgb)) * 0.5;
                color = lerp(color, response, saturate(smear * 0.2));

                float phase = fmod(floor(uv.x * CROWFX_TARGET_RES.x), 3.0);
                float3 mask = phase < 0.5 ? float3(1.35, 0.82, 0.82) :
                              phase < 1.5 ? float3(0.82, 1.35, 0.82) : float3(0.82, 0.82, 1.35);
                color *= lerp(float3(1.0, 1.0, 1.0), mask, subpixel);
                float polarity = fmod(floor(pixel.x / scale) + floor(pixel.y / scale) + floor(_Time.y * 60.0), 2.0) * 2.0 - 1.0;
                color *= 1.0 + polarity * inversion;
                color *= float3(1.0 + viewAngle * 0.08, 1.0 - abs(viewAngle) * 0.05, 1.0 - viewAngle * 0.08);
                float2 corner = abs(uv * 2.0 - 1.0);
                color += backlight * pow(saturate(max(corner.x, corner.y)), 5.0) * 0.12;
                return FromDisplaySignal(color);
            }

            float4 frag(CrowFX_V2F i) : SV_Target
            {
                CROWFX_SETUP_STEREO(i);
                float3 original = CROWFX_SAMPLE_SCREEN(_MainTex, i.uv).rgb;
                float3 effected = original;
                int mode = (int)(_ProfessionalMode + 0.5);
                if (mode == 0) effected = LensSensor(i.uv);
                else if (mode == 1) effected = Film(i.uv);
                else if (mode == 2) effected = MotionGlitch(i.uv);
                else if (mode == 3) effected = DigitalVideo(i.uv);
                else if (mode == 4) effected = CompositeSignal(i.uv);
                else if (mode == 5) effected = Lcd(i.uv);
                // LensSensor already applies intensity to its parameters; returning it directly
                // prevents a warped/unwarped double exposure. Other stages remain mix-based.
                return float4(mode == 0 ? effected : lerp(original, effected, saturate(_EffectIntensity)), 1.0);
            }
            ENDCG
        }
    }
}
