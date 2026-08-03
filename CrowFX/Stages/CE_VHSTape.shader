Shader "Hidden/CrowFX/Stages/VHSTape"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Intensity, _TapeSpeed, _HorizontalJitter, _LineWobble;
            float _Tracking, _TrackingSpeed, _TrackingWidth;
            float _ChromaBleed, _ChromaBlur, _ColorLoss;
            float _LumaNoise, _ChromaNoise, _Dropout;
            float _HeadSwitching, _HeadSwitchHeight, _Interlace;
            float _Standard, _TapeMode, _Generation, _AgcInstability, _VerticalChromaBlur;

            float3 ToSignalRGB(float3 c)
            {
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return max(c, 0.0);
                #else
                    return LinearToGammaSpace(max(c, 0.0));
                #endif
            }

            float3 FromSignalRGB(float3 c)
            {
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return c;
                #else
                    return GammaToLinearSpace(max(c, 0.0));
                #endif
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            float ValueNoise(float2 p)
            {
                float2 id = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(id), Hash21(id + float2(1,0)), f.x),
                            lerp(Hash21(id + float2(0,1)), Hash21(id + 1.0), f.x), f.y);
            }

            // Four independent uniforms approximate the bell-shaped amplitude statistics
            // of electronic/RF noise much better than a single flat hash sample.
            float GaussianNoise(float2 id, float salt)
            {
                float sum = Hash21(id + salt * 11.17);
                sum += Hash21(id.yx + salt * 29.41);
                sum += Hash21(id * float2(0.7549, 1.3719) + salt * 53.73);
                sum += Hash21(id * float2(1.9317, 0.6183) + salt * 89.13);
                return (sum - 2.0) * 0.5;
            }

            // A spatially correlated random process which is regenerated per field.  Time
            // is an event id, never an axis through the noise domain, so it changes rather
            // than visibly scrolling.
            float LineNoise(float scanLine, float field, float wavelength, float salt)
            {
                float x = scanLine / max(wavelength, 1.0);
                float cell = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(float2(cell + salt * 13.7, field + salt * 47.3));
                float b = Hash21(float2(cell + 1.0 + salt * 13.7, field + salt * 47.3));
                return lerp(a, b, f) * 2.0 - 1.0;
            }

            float2 RotateChroma(float2 c, float phase)
            {
                float s = sin(phase);
                float co = cos(phase);
                return float2(co * c.x - s * c.y, s * c.x + co * c.y);
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

            float WrapDistance(float a, float b)
            {
                float d = abs(a - b);
                return min(d, 1.0 - d);
            }

            // One stochastic oxide-loss event bank. Events arrive in time buckets, occupy
            // 1-3 adjacent recorded lines, start sharply, and decay horizontally like the
            // characteristic firing-line/comet artifacts of off-tape RF loss.
            float4 DropoutLayer(float2 uv, float outputLine, float time, float groupSize, float salt)
            {
                float epoch = floor(time * (13.0 + salt * 0.37));
                float lineGroup = floor(outputLine / groupSize);
                float2 eventId = float2(lineGroup + salt * 19.1, epoch + salt * 71.7);
                float arrival = Hash21(eventId);
                float active = step(1.0 - _Dropout * 0.16, arrival);

                float centerLine = Hash21(eventId + 11.3) * (groupSize - 1.0);
                float localLine = fmod(outputLine, groupSize);
                float lineDistance = abs(localLine - centerLine);
                float lineEnvelope = exp2(-1.8 * lineDistance * lineDistance);

                float startX = Hash21(eventId + 29.7) * 0.94;
                float durationSeed = Hash21(eventId + 47.1);
                float length = lerp(0.008, 0.22, durationSeed * durationSeed);
                float x = uv.x - startX;
                float startEdge = smoothstep(-0.0015, 0.0015, x);
                float body = startEdge * (1.0 - smoothstep(length * 0.55, length, x));
                float tail = startEdge * exp2(-max(0.0, x) * 5.0 / max(0.008, length));
                float envelope = active * lineEnvelope * max(body, tail * 0.58);

                float leadingEdge = active * lineEnvelope * exp2(-x * x * 18000.0);
                float rfNoise = GaussianNoise(float2(floor(uv.x * _ScreenParams.x * 0.32),
                                                     outputLine + epoch * 103.0), salt);
                float polarity = step(0.5, Hash21(eventId + 83.9));
                return float4(envelope, leadingEdge, rfNoise, polarity);
            }

            float4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _TapeSpeed;
                float frameRate = _Standard < 0.5 ? 29.97 : 25.0;
                float fieldRate = frameRate * 2.0;
                float modeLoss = 1.0 + _TapeMode * 0.38 + _Generation * 0.075;
                float outputLine = floor(uv.y * _MainTex_TexelSize.w);
                float field = floor(time * fieldRate);

                // Tape-speed error is a hierarchy of field-held processes: nearly independent
                // line jitter, short correlated runs, and slow flagging over dozens of lines.
                float lineJitter = GaussianNoise(float2(outputLine, field), 3.0);
                float shortTbe = LineNoise(outputLine, field, 3.5, 7.0);
                float longTbe = LineNoise(outputLine, floor(field * 0.25), 42.0, 13.0);
                float fineJitter = (lineJitter * 0.72 + shortTbe * 0.28) * _HorizontalJitter * modeLoss;
                float wobble = (longTbe * 0.72 + LineNoise(outputLine, field, 11.0, 19.0) * 0.28)
                               * _LineWobble * modeLoss;

                float trackingY = frac(time * _TrackingSpeed * 0.16 + 0.31);
                float trackingDistance = WrapDistance(uv.y, trackingY);
                float trackingBand = exp2(-trackingDistance * trackingDistance /
                                          max(0.00001, _TrackingWidth * _TrackingWidth) * 3.0) * _Tracking;
                float trackingTear = (LineNoise(outputLine, field, 2.0, 31.0) * 28.0 +
                                      GaussianNoise(float2(outputLine, field), 37.0) * 22.0) * trackingBand;

                float headMask = 1.0 - smoothstep(0.0, max(0.005, _HeadSwitchHeight), 1.0 - uv.y);
                float headLocal = saturate((uv.y - (1.0 - max(0.005, _HeadSwitchHeight))) /
                                           max(0.005, _HeadSwitchHeight));
                float headJump = GaussianNoise(float2(field, floor(field * 0.5)), 43.0);
                float headSkew = (headJump * 26.0 * headLocal * headLocal +
                                  LineNoise(outputLine, field, 1.5, 47.0) * 7.0) * headMask * _HeadSwitching;
                float xOffsetPx = fineJitter + wobble + trackingTear + headSkew;
                float2 warpedUv = saturate(uv + float2(xOffsetPx * _MainTex_TexelSize.x, 0.0));
                // The track transition also produces a small vertical discontinuity/fold at
                // the bottom of a field, rather than a stationary sinusoidal overlay.
                float headVerticalJump = (0.12 + abs(headJump) * 0.16) * _HeadSwitchHeight;
                warpedUv.y = saturate(warpedUv.y - headMask * _HeadSwitching * headVerticalJump);

                // Sample a real alternating field lattice, then blend toward it. This creates
                // weave/comb behavior rather than merely darkening alternate output lines.
                float fieldIndex = fmod(floor(time * fieldRate), 2.0);
                float fieldLine = floor(outputLine * 0.5) * 2.0 + fieldIndex;
                float fieldY = (fieldLine + 0.5) * _MainTex_TexelSize.y;
                warpedUv.y = lerp(warpedUv.y, saturate(fieldY), _Interlace);

                float3 clean = tex2D(_MainTex, uv).rgb;
                float lumaRadius = (0.75 + _TapeMode * 0.65 + _Generation * 0.10) * _MainTex_TexelSize.x;
                float3 lumaCenter = RGBtoYIQ(ToSignalRGB(tex2D(_MainTex, warpedUv).rgb));
                float lumaLeft = RGBtoYIQ(ToSignalRGB(tex2D(_MainTex, saturate(warpedUv - float2(lumaRadius, 0))).rgb)).x;
                float lumaRight = RGBtoYIQ(ToSignalRGB(tex2D(_MainTex, saturate(warpedUv + float2(lumaRadius, 0))).rgb)).x;
                // FM luminance retains much more horizontal bandwidth than color, but it is
                // still bandwidth-limited and becomes softer at slow tape speeds/generations.
                float luma = lumaCenter.x * 0.56 + (lumaLeft + lumaRight) * 0.22;
                float chromaOffset = _ChromaBleed * _MainTex_TexelSize.x;
                float chromaRadius = _ChromaBlur * modeLoss * _MainTex_TexelSize.x;

                // VHS stores chroma at much lower bandwidth than luma: delayed, broad, one-sided smear.
                float2 chromaUv = saturate(warpedUv - float2(chromaOffset, 0.0));
                float3 yiq0 = RGBtoYIQ(ToSignalRGB(tex2D(_MainTex, chromaUv).rgb));
                float3 yiq1 = RGBtoYIQ(ToSignalRGB(tex2D(_MainTex, saturate(chromaUv - float2(chromaRadius * 0.5, 0))).rgb));
                float3 yiq2 = RGBtoYIQ(ToSignalRGB(tex2D(_MainTex, saturate(chromaUv - float2(chromaRadius, 0))).rgb));
                float verticalRadius = _VerticalChromaBlur * modeLoss * _MainTex_TexelSize.y;
                float3 yiqUp = RGBtoYIQ(ToSignalRGB(tex2D(_MainTex, saturate(chromaUv + float2(0, verticalRadius))).rgb));
                float3 yiqDn = RGBtoYIQ(ToSignalRGB(tex2D(_MainTex, saturate(chromaUv - float2(0, verticalRadius))).rgb));
                float2 chroma = yiq0.yz * 0.38 + yiq1.yz * 0.25 + yiq2.yz * 0.13 + (yiqUp.yz + yiqDn.yz) * 0.12;
                chroma *= 1.0 - saturate(_ColorLoss + _Generation * 0.045);

                float2 pixel = floor(uv * _ScreenParams.xy);
                float rf0 = GaussianNoise(pixel + float2(field * 71.0, field * 19.0), 53.0);
                float rfL = GaussianNoise(pixel + float2(-1.0 + field * 71.0, field * 19.0), 53.0);
                float rfR = GaussianNoise(pixel + float2(1.0 + field * 71.0, field * 19.0), 53.0);
                float highBandRf = rf0 - (rfL + rfR) * 0.5;
                float shortRf = GaussianNoise(float2(floor(pixel.x * 0.28), outputLine + field * 103.0), 59.0);
                float grain = highBandRf * 0.62 + shortRf * 0.38;
                luma += grain * _LumaNoise * modeLoss * lerp(1.30, 0.42, saturate(luma));

                // Color-under noise is low-bandwidth and line-correlated.  Phase error rotates
                // I/Q into colored horizontal streaks instead of independent RGB confetti.
                float chromaCell = floor(pixel.x / max(3.0, 2.0 + _ChromaBlur * 0.8));
                float chromaGrainI = GaussianNoise(float2(chromaCell, outputLine + field * 131.0), 61.0);
                float chromaGrainQ = GaussianNoise(float2(chromaCell, outputLine + field * 149.0), 67.0);
                float chromaPhase = LineNoise(outputLine, field, 5.0, 71.0) * _ChromaNoise * modeLoss * 1.8;
                chroma = RotateChroma(chroma, chromaPhase);
                chroma += float2(chromaGrainI, chromaGrainQ) * _ChromaNoise * modeLoss;

                float4 dropoutA = DropoutLayer(uv, outputLine, time, 4.0, 1.0);
                float4 dropoutB = DropoutLayer(uv, outputLine, time, 7.0, 7.0);
                float dropout = saturate(dropoutA.x + dropoutB.x);
                float leadingEdge = saturate(dropoutA.y + dropoutB.y);
                float rfNoise = (dropoutA.z * dropoutA.x + dropoutB.z * dropoutB.x) /
                                max(0.0001, dropoutA.x + dropoutB.x);
                float polarity = dropoutA.x > dropoutB.x ? dropoutA.w : dropoutB.w;

                // Most decks conceal detected loss with the preceding scanline. Detection is
                // imperfect, leaving an inverted flash, granular RF body, and desaturated tail.
                float previousLineY = RGBtoYIQ(ToSignalRGB(tex2D(_MainTex,
                    saturate(warpedUv - float2(0.0, _MainTex_TexelSize.y))).rgb)).x;
                float rawLoss = lerp(0.04, 0.92, polarity) + rfNoise * 0.52 + leadingEdge * 0.55;
                float concealedLoss = lerp(rawLoss, previousLineY + rfNoise * 0.12, 0.32);
                luma = lerp(luma, concealedLoss, dropout);
                chroma *= 1.0 - dropout * 0.94;

                luma += GaussianNoise(pixel + field * 3.7, 73.0) * trackingBand * 0.45;
                chroma *= 1.0 - trackingBand * 0.75;
                float agcWave = ValueNoise(float2(time * 0.75, 91.7)) - 0.5;
                luma = (luma - 0.5) * (1.0 + agcWave * _AgcInstability * modeLoss) + 0.5;

                float3 tape = FromSignalRGB(YIQtoRGB(float3(luma, chroma)));
                tape = lerp(tape, FromSignalRGB(luma.xxx), headMask * _HeadSwitching * 0.35);
                return float4(lerp(clean, tape, saturate(_Intensity)), 1.0);
            }
            ENDCG
        }
    }
}
