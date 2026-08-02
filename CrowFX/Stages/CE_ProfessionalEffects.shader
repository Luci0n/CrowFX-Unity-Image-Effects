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
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _HistoryTex;
            sampler2D _CameraMotionVectorsTexture;
            float4 _MainTex_TexelSize;
            float _ProfessionalMode, _EffectIntensity;
            float4 _ParamA, _ParamB;

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
                float distortion = _ParamA.x * lensMix;
                float chromaPx = _ParamA.y * lensMix;
                float vignette = _ParamA.z * lensMix;
                float bloom = _ParamA.w * lensMix;
                float rolling = _ParamB.x * lensMix;
                float noiseAmount = _ParamB.y * lensMix;
                float deadRate = _ParamB.z * lensMix;
                float bloomRadius = _ParamB.w;

                float2 p = uv * 2.0 - 1.0;
                float rollingWave = sin(_Time.y * 9.0 + uv.y * 13.0) * rolling * _MainTex_TexelSize.x;
                p.x += rollingWave * 2.0;

                // Aspect-correct, bounded radial mapping. It keeps positive barrel/fisheye
                // distortion inside the image instead of saturating many UVs onto one edge texel.
                float aspect = max(_ScreenParams.x / max(_ScreenParams.y, 1.0), 1e-4);
                float2 lensP = float2(p.x * aspect, p.y);
                float cornerRadius = sqrt(aspect * aspect + 1.0);
                float radius01 = saturate(length(lensP) / max(cornerRadius, 1e-4));
                float radialScale = max(0.25, 1.0 - distortion * 1.10 * (1.0 - radius01 * radius01));
                lensP *= radialScale;
                p = float2(lensP.x / aspect, lensP.y);

                float2 rawWarped = p * 0.5 + 0.5;
                float2 halfTexel = _MainTex_TexelSize.xy * 0.5;
                float2 warped = clamp(rawWarped, halfTexel, 1.0 - halfTexel);
                float2 outside = max(abs(rawWarped - 0.5) - 0.5, 0.0);
                float edgeValidity = 1.0 - smoothstep(0.0, max(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * 2.0,
                                                      max(outside.x, outside.y));
                float2 radialDir = lensP / max(length(lensP), 1e-5);
                radialDir.x /= aspect;
                float2 radial = radialDir * chromaPx * _MainTex_TexelSize.xy * radius01;

                float3 color;
                color.r = tex2D(_MainTex, clamp(warped + radial, halfTexel, 1.0 - halfTexel)).r;
                color.g = tex2D(_MainTex, saturate(warped)).g;
                color.b = tex2D(_MainTex, clamp(warped - radial, halfTexel, 1.0 - halfTexel)).b;
                color = lerp(tex2D(_MainTex, uv).rgb, color, edgeValidity);

                float2 br = _MainTex_TexelSize.xy * bloomRadius;
                float3 glow = (tex2D(_MainTex, saturate(warped + float2(br.x, 0))).rgb +
                               tex2D(_MainTex, saturate(warped - float2(br.x, 0))).rgb +
                               tex2D(_MainTex, saturate(warped + float2(0, br.y))).rgb +
                               tex2D(_MainTex, saturate(warped - float2(0, br.y))).rgb) * 0.25;
                float glowGate = smoothstep(0.7, 1.15, dot(glow, float3(0.2126, 0.7152, 0.0722)));
                color += glow * glowGate * bloom;

                float radialVignette = smoothstep(0.2, 1.3, dot(p, p));
                color *= 1.0 - vignette * radialVignette;
                float frame = floor(_Time.y * 60.0);
                float2 pixel = floor(uv * _ScreenParams.xy);
                float grain = Hash21(pixel + frame * float2(17.0, 71.0)) - 0.5;
                color += grain * noiseAmount * lerp(1.3, 0.45, saturate(dot(color, float3(0.333, 0.333, 0.333))));
                float dead = step(1.0 - deadRate * 0.0015, Hash21(pixel + 317.0));
                float deadValue = Hash21(pixel + 811.0) > 0.5 ? 0.0 : 1.0;
                color = lerp(color, float3(deadValue, deadValue, deadValue), dead);
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
                float3 color = tex2D(_MainTex, sampleUv).rgb;

                float2 r = _MainTex_TexelSize.xy * halationRadius;
                float3 surround = (tex2D(_MainTex, saturate(sampleUv + float2(r.x, 0))).rgb +
                                   tex2D(_MainTex, saturate(sampleUv - float2(r.x, 0))).rgb +
                                   tex2D(_MainTex, saturate(sampleUv + float2(0, r.y))).rgb +
                                   tex2D(_MainTex, saturate(sampleUv - float2(0, r.y))).rgb) * 0.25;
                float hot = smoothstep(0.62, 1.05, dot(surround, float3(0.2126, 0.7152, 0.0722)));
                color += surround * float3(1.0, 0.22, 0.08) * hot * halation;

                float2 grainPixel = floor(uv * _ScreenParams.xy / grainSize);
                float grain = Hash21(grainPixel + frame * float2(37.0, 19.0)) - 0.5;
                float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
                color += grain * grainAmount * lerp(0.55, 1.25, 1.0 - saturate(abs(luma - 0.45) * 1.8));

                float2 dustCell = floor(uv * float2(180.0, 100.0));
                float dustSeed = Hash21(dustCell + floor(frame / 3.0));
                float2 dustLocal = frac(uv * float2(180.0, 100.0)) - 0.5;
                float dust = step(1.0 - dustAmount * 0.035, dustSeed) * smoothstep(0.32, 0.02, length(dustLocal));
                float scratchX = abs(frac(uv.x * 317.0 + Hash21(float2(frame, frame)) * 0.13) - 0.5);
                float scratch = step(1.0 - scratchAmount * 0.025, Hash21(float2(floor(uv.x * 317.0), floor(frame / 8.0)))) * smoothstep(0.035, 0.0, scratchX);
                color = lerp(color, float3(0.02, 0.02, 0.02), dust * 0.85);
                color += scratch * 0.32;
                color *= 1.0 + (Noise21(float2(_Time.y * 4.0, 7.1)) - 0.5) * flicker;
                return color;
            }

            float3 MotionGlitch(float2 uv)
            {
                float blockSize = max(_ParamA.x, 4.0);
                float displacement = _ParamA.y;
                float freezeRate = _ParamA.z;
                float colorSplit = _ParamA.w;
                float2 block = floor(uv * _ScreenParams.xy / blockSize);
                float2 blockUv = (block * blockSize + blockSize * 0.5) / _ScreenParams.xy;
                float2 motion = tex2D(_CameraMotionVectorsTexture, blockUv).rg;
                float epoch = floor(_Time.y * 12.0);
                float eventNoise = Hash21(block + epoch * float2(13.0, 47.0));
                float frozen = step(1.0 - freezeRate, eventNoise);
                // Real motion vectors drive the smear. A small deterministic codec-vector
                // fallback keeps authored datamosh visible in static previews and paused cameras.
                float2 codecVector = (float2(Hash21(block + epoch * 7.13), Hash21(block + epoch * 19.71 + 83.0)) * 2.0 - 1.0)
                                     * displacement * _MainTex_TexelSize.xy;
                float quietMotion = 1.0 - saturate(length(motion) * 256.0);
                float2 shifted = saturate(uv - motion * displacement + codecVector * quietMotion * frozen);
                float3 current = tex2D(_MainTex, shifted).rgb;
                float3 history = tex2D(_HistoryTex, shifted).rgb;
                float3 color = lerp(current, history, frozen);
                float split = colorSplit * _MainTex_TexelSize.x * frozen;
                float splitMix = frozen * saturate(colorSplit * 0.25);
                color.r = lerp(color.r, tex2D(_MainTex, saturate(shifted + float2(split, 0))).r, splitMix);
                color.b = lerp(color.b, tex2D(_HistoryTex, saturate(shifted - float2(split, 0))).b, splitMix);
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

                float2 pixel = uv * _ScreenParams.xy;
                float2 blockUv = (floor(pixel / blockSize) * blockSize + blockSize * 0.5) / _ScreenParams.xy;
                float3 source = ToSignal(tex2D(_MainTex, uv).rgb);
                float3 blockColor = ToSignal(tex2D(_MainTex, blockUv).rgb);
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
                float3 left = ToSignal(tex2D(_MainTex, saturate(uv - dx)).rgb);
                float3 right = ToSignal(tex2D(_MainTex, saturate(uv + dx)).rgb);
                float3 edge = source * 2.0 - left - right;
                compressed += edge * ringing * 0.18;
                float edgeGate = saturate(length(edge) * 4.0);
                compressed += (Hash21(floor(pixel) + floor(_Time.y * 24.0)) - 0.5) * mosquito * edgeGate * 0.12;
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
                float3 center = RGBtoYIQ(ToSignal(tex2D(_MainTex, uv).rgb));
                float radius = lerp(5.0, 0.75, bandwidth) * _MainTex_TexelSize.x;
                float3 left = RGBtoYIQ(ToSignal(tex2D(_MainTex, saturate(uv - float2(radius, 0))).rgb));
                float3 right = RGBtoYIQ(ToSignal(tex2D(_MainTex, saturate(uv + float2(radius, 0))).rgb));
                float2 chroma = center.yz * 0.5 + (left.yz + right.yz) * 0.25;

                float lineIndex = floor(uv.y * _MainTex_TexelSize.w);
                float cycles = standard < 0.5 ? 227.5 : 283.75;
                float phase = (uv.x * cycles + lineIndex * (standard < 0.5 ? 0.5 : 0.25) + _Time.y * 0.75) * 6.2831853;
                phase += (Noise21(float2(lineIndex * 0.05, _Time.y * 2.0)) - 0.5) * phaseError;
                float lumaEdge = abs(left.x - right.x);
                chroma += float2(cos(phase), sin(phase)) * lumaEdge * rainbow * 0.22;
                center.x += dot(chroma, float2(cos(phase), sin(phase))) * dotCrawl * 0.12;

                float3 above = RGBtoYIQ(ToSignal(tex2D(_MainTex, saturate(uv - float2(0, _MainTex_TexelSize.y))).rgb));
                chroma = lerp(chroma, (chroma + above.yz) * 0.5, comb);
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
                float2 pixel = floor(uv * _ScreenParams.xy / scale) * scale + scale * 0.5;
                float2 sampleUv = pixel / _ScreenParams.xy;
                float3 color = tex2D(_MainTex, sampleUv).rgb;
                float3 response = (tex2D(_MainTex, saturate(sampleUv - float2(_MainTex_TexelSize.x * smear, 0))).rgb +
                                   tex2D(_MainTex, saturate(sampleUv + float2(_MainTex_TexelSize.x * smear, 0))).rgb) * 0.5;
                color = lerp(color, response, saturate(smear * 0.2));

                float phase = fmod(floor(uv.x * _ScreenParams.x), 3.0);
                float3 mask = phase < 0.5 ? float3(1.35, 0.82, 0.82) :
                              phase < 1.5 ? float3(0.82, 1.35, 0.82) : float3(0.82, 0.82, 1.35);
                color *= lerp(float3(1.0, 1.0, 1.0), mask, subpixel);
                float polarity = fmod(floor(pixel.x / scale) + floor(pixel.y / scale) + floor(_Time.y * 60.0), 2.0) * 2.0 - 1.0;
                color *= 1.0 + polarity * inversion;
                color *= float3(1.0 + viewAngle * 0.08, 1.0 - abs(viewAngle) * 0.05, 1.0 - viewAngle * 0.08);
                float2 corner = abs(uv * 2.0 - 1.0);
                color += backlight * pow(saturate(max(corner.x, corner.y)), 5.0) * 0.12;
                return color;
            }

            float4 frag(v2f_img i) : SV_Target
            {
                float3 original = tex2D(_MainTex, i.uv).rgb;
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
