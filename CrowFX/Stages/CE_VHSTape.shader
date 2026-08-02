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
                float rfNoise = Hash21(float2(floor(uv.x * _ScreenParams.x * 0.42) + salt * 37.0,
                                              outputLine + epoch * 103.0)) - 0.5;
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
                float lineNoise = Hash21(float2(outputLine, floor(time * frameRate)));
                float fineJitter = (lineNoise - 0.5) * _HorizontalJitter * modeLoss;
                float wobble = (ValueNoise(float2(uv.y * 18.0, time * 2.1)) - 0.5) * _LineWobble * modeLoss;

                float trackingY = frac(time * _TrackingSpeed * 0.16 + 0.31);
                float trackingDistance = WrapDistance(uv.y, trackingY);
                float trackingBand = exp2(-trackingDistance * trackingDistance /
                                          max(0.00001, _TrackingWidth * _TrackingWidth) * 3.0) * _Tracking;
                float trackingTear = (Hash21(float2(outputLine, floor(time * 8.0))) - 0.5) * 42.0 * trackingBand;

                float headMask = 1.0 - smoothstep(0.0, max(0.005, _HeadSwitchHeight), 1.0 - uv.y);
                float headWave = sin(uv.y * 920.0 + time * 37.0) * 7.0 * headMask * _HeadSwitching;
                float xOffsetPx = fineJitter + wobble + trackingTear + headWave;
                float2 warpedUv = saturate(uv + float2(xOffsetPx * _MainTex_TexelSize.x, 0.0));

                // Sample a real alternating field lattice, then blend toward it. This creates
                // weave/comb behavior rather than merely darkening alternate output lines.
                float fieldIndex = fmod(floor(time * fieldRate), 2.0);
                float fieldLine = floor(outputLine * 0.5) * 2.0 + fieldIndex;
                float fieldY = (fieldLine + 0.5) * _MainTex_TexelSize.y;
                warpedUv.y = lerp(warpedUv.y, saturate(fieldY), _Interlace);

                float3 clean = tex2D(_MainTex, uv).rgb;
                float luma = RGBtoYIQ(ToSignalRGB(tex2D(_MainTex, warpedUv).rgb)).x;
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

                float frame = floor(time * fieldRate);
                float2 pixel = floor(uv * _ScreenParams.xy);
                float grain = Hash21(pixel + float2(frame * 71.0, frame * 19.0)) - 0.5;
                float chromaGrainI = Hash21(pixel * 0.53 + float2(frame * 13.0, frame * 47.0)) - 0.5;
                float chromaGrainQ = Hash21(pixel * 0.37 + float2(frame * 31.0, frame * 7.0)) - 0.5;
                luma += grain * _LumaNoise * modeLoss * lerp(1.25, 0.45, saturate(luma));
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

                luma += (Hash21(pixel + frame * 3.7) - 0.5) * trackingBand * 0.45;
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
