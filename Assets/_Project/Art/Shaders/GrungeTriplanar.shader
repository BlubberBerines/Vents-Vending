// Vents & Vending — "Stylized grunge" world-space material (see Docs/Design/ArtStyleGuide.md).
// Triplanar base texture + normal map (no UVs needed, so scaled greybox primitives never stretch), plus procedural
// panel seams, floor-level grime, rust patches and chipped paint on box edges. URP Forward+/SSAO/shadows/fog aware.
Shader "VentsAndVending/Grunge Triplanar"
{
    Properties
    {
        [Header(Base)]
        _BaseColor ("Base Colour", Color) = (0.5, 0.5, 0.5, 1)
        [NoScaleOffset] _BaseMap ("Base Texture (tileable)", 2D) = "white" {}
        _TextureStrength ("Texture Strength", Range(0, 1)) = 1
        [NoScaleOffset][Normal] _BumpMap ("Normal Map (tileable)", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1
        _TileSize ("Texture Tile Size (m)", Float) = 2
        _BlendSharpness ("Triplanar Blend Sharpness", Range(1, 16)) = 6
        _Metallic ("Metallic", Range(0, 1)) = 0.2
        _Smoothness ("Smoothness", Range(0, 1)) = 0.35

        [Header(Panel Seams)]
        _PanelSize ("Panel Size (m)", Float) = 1.5
        _SeamWidth ("Seam Width (m)", Range(0, 0.05)) = 0.012
        _SeamDarkness ("Seam Darkness", Range(0, 1)) = 0

        [Header(Grime)]
        _GrimeColor ("Grime Colour (multiplied)", Color) = (0.35, 0.3, 0.24, 1)
        _GrimeHeight ("Floor Grime Height (m)", Float) = 1.2
        _GrimeAmount ("Floor Grime Amount", Range(0, 1)) = 0.6
        _GrimePatches ("Grime Patches", Range(0, 1)) = 0.35
        _GrimeScale ("Grime Noise Scale", Float) = 0.8

        [Header(Rust)]
        _RustColor ("Rust Colour", Color) = (0.42, 0.2, 0.08, 1)
        _RustAmount ("Rust Amount", Range(0, 1)) = 0
        _RustScale ("Rust Noise Scale", Float) = 1.6

        [Header(Edge Wear  box meshes)]
        _EdgeColor ("Bare Edge Colour", Color) = (0.62, 0.6, 0.56, 1)
        _EdgeWear ("Edge Wear Amount", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width (m)", Range(0.005, 0.2)) = 0.04
        [Header(Edge Wear  any mesh  bevel curvature)]
        _CurveWear ("Curvature Chip Amount", Range(0, 1)) = 0
        _CurveThreshold ("Curvature Threshold (1/m)", Range(1, 60)) = 9

        [Header(Emission)]
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)

        [Header(Stylized Lighting  cartoony but not cel comic)]
        _RampThreshold ("Light Ramp Threshold", Range(-1, 1)) = 0.05
        _RampSoftness ("Light Ramp Softness", Range(0.001, 0.5)) = 0.12
        _ShadowTint ("Shadow Tint", Color) = (0.8, 0.76, 0.95, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 6)) = 2.6
        _SpecStrength ("Highlight Strength", Range(0, 2)) = 0.45
        _RimStrength ("Rim Light Strength", Range(0, 1)) = 0.18
        _WearCrispness ("Wear Crispness (painted blobs)", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _TextureStrength;
            half _BumpScale;
            float _TileSize;
            half _BlendSharpness;
            half _Metallic;
            half _Smoothness;
            float _PanelSize;
            float _SeamWidth;
            half _SeamDarkness;
            half4 _GrimeColor;
            float _GrimeHeight;
            half _GrimeAmount;
            half _GrimePatches;
            float _GrimeScale;
            half4 _RustColor;
            half _RustAmount;
            float _RustScale;
            half4 _EdgeColor;
            half _EdgeWear;
            float _EdgeWidth;
            half _CurveWear;
            float _CurveThreshold;
            half4 _EmissionColor;
            half _RampThreshold;
            half _RampSoftness;
            half4 _ShadowTint;
            half _AmbientStrength;
            half _SpecStrength;
            half _RimStrength;
            half _WearCrispness;
        CBUFFER_END

        TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);  SAMPLER(sampler_BumpMap);

        // ---------------------------------------------------------------- Noise
        float VV_Hash13(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.zyx + 31.32);
            return frac((p.x + p.y) * p.z);
        }

        float VV_ValueNoise(float3 p)
        {
            float3 i = floor(p);
            float3 f = frac(p);
            float3 u = f * f * (3.0 - 2.0 * f);
            float n000 = VV_Hash13(i);
            float n100 = VV_Hash13(i + float3(1, 0, 0));
            float n010 = VV_Hash13(i + float3(0, 1, 0));
            float n110 = VV_Hash13(i + float3(1, 1, 0));
            float n001 = VV_Hash13(i + float3(0, 0, 1));
            float n101 = VV_Hash13(i + float3(1, 0, 1));
            float n011 = VV_Hash13(i + float3(0, 1, 1));
            float n111 = VV_Hash13(i + float3(1, 1, 1));
            float x00 = lerp(n000, n100, u.x);
            float x10 = lerp(n010, n110, u.x);
            float x01 = lerp(n001, n101, u.x);
            float x11 = lerp(n011, n111, u.x);
            return lerp(lerp(x00, x10, u.y), lerp(x01, x11, u.y), u.z);
        }

        float VV_Fbm(float3 p)
        {
            float sum = 0.0;
            float amp = 0.5;
            [unroll] for (int o = 0; o < 4; o++)
            {
                sum += VV_ValueNoise(p) * amp;
                p = p * 2.03 + 17.17;
                amp *= 0.5;
            }
            return sum; // ~0..0.94
        }

        float3 VV_TriWeights(float3 normalWS)
        {
            float3 w = pow(abs(normalWS), _BlendSharpness);
            return w / max(w.x + w.y + w.z, 1e-4);
        }
        ENDHLSL

        // ================================================================== Forward
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // One light, stylized: soft two-tone ramp, crisp shadows, gently posterized point-light falloff
            // (cartoon light pools) and a banded highlight that only glossy/metal surfaces get.
            half3 VV_ToonLight(Light light, half3 n, half3 v, half3 albedo, half smoothness, half metallic, bool isMain)
            {
                half ndl = dot(n, light.direction);
                half lit = smoothstep(_RampThreshold - _RampSoftness, _RampThreshold + _RampSoftness, ndl);
                half shadow = smoothstep(0.35, 0.65, light.shadowAttenuation);
                half atten = light.distanceAttenuation;
                if (!isMain)
                {
                    atten = lerp(atten, floor(atten * 4.0 + 0.5) * 0.25, 0.45);
                }

                half amount = lit * shadow * atten;
                half3 diffuse = albedo * light.color * amount;

                half3 h = SafeNormalize(light.direction + v);
                half spec = pow(saturate(dot(n, h)), exp2(smoothness * 9.0 + 1.0));
                // Only glossy surfaces get a highlight — rough paint and concrete stay flat (cartoon read)
                half band = smoothstep(0.45, 0.55, spec) * smoothstep(0.45, 0.7, smoothness) * _SpecStrength;
                half3 specColor = lerp(half3(1.0, 1.0, 1.0), albedo * 2.0, metallic);
                return diffuse + specColor * light.color * band * amount;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                float3 normalOS   : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionOS = input.positionOS.xyz;
                o.normalOS = input.normalOS;
                o.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 posWS = input.positionWS;
                float3 geoN = normalize(input.normalWS);
                float3 w = VV_TriWeights(geoN);
                float tile = 1.0 / max(_TileSize, 0.01);

                float2 uvX = posWS.zy * tile;
                float2 uvY = posWS.xz * tile;
                float2 uvZ = posWS.xy * tile;

                // Base texture (triplanar)
                half3 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb * w.x
                          + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY).rgb * w.y
                          + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb * w.z;
                half3 albedo = _BaseColor.rgb * lerp(half3(1, 1, 1), tex * 2.0, _TextureStrength);

                // Normal map (triplanar, whiteout blend)
                half3 tnX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvX), _BumpScale);
                half3 tnY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvY), _BumpScale);
                half3 tnZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvZ), _BumpScale);
                tnX = half3(tnX.xy + geoN.zy, abs(tnX.z) * geoN.x);
                tnY = half3(tnY.xy + geoN.xz, abs(tnY.z) * geoN.y);
                tnZ = half3(tnZ.xy + geoN.xy, abs(tnZ.z) * geoN.z);
                float3 normalWS = normalize(tnX.zyx * w.x + tnY.xzy * w.y + tnZ.xyz * w.z);

                half metallic = _Metallic;
                half smoothness = _Smoothness;

                // Panel seams on the dominant projection plane
                if (_SeamDarkness > 0.001)
                {
                    float2 puv = w.x > w.y && w.x > w.z ? posWS.zy : (w.y > w.z ? posWS.xz : posWS.xy);
                    float2 cell = abs(frac(puv / max(_PanelSize, 0.05) + 0.5) - 0.5) * _PanelSize;
                    float seam = 1.0 - smoothstep(_SeamWidth * 0.5, _SeamWidth, min(cell.x, cell.y));
                    albedo *= 1.0 - seam * _SeamDarkness;
                    smoothness *= 1.0 - seam * 0.6;
                }

                // Edge wear (box meshes only: distance from the nearest edge in metres).
                // Only faces with axis-aligned object normals inside the unit cube qualify, so round meshes are left alone.
                float edge = 0.0;
                float3 nOS = abs(normalize(input.normalOS));
                bool boxFace = max(nOS.x, max(nOS.y, nOS.z)) > 0.995 && all(abs(input.positionOS) <= 0.5005);
                if (_EdgeWear > 0.001 && boxFace)
                {
                    float3 scale = float3(length(UNITY_MATRIX_M._m00_m10_m20), length(UNITY_MATRIX_M._m01_m11_m21), length(UNITY_MATRIX_M._m02_m12_m22));
                    float3 d = (0.5 - abs(input.positionOS)) * scale;
                    float mn = min(d.x, min(d.y, d.z));
                    float mx = max(d.x, max(d.y, d.z));
                    float mid = d.x + d.y + d.z - mn - mx;
                    float crispE = lerp(0.1, 0.02, _WearCrispness);
                    float chip = smoothstep(0.45 - crispE, 0.45 + crispE, VV_Fbm(posWS * 6.0 + 3.7));
                    float band = lerp(saturate((1.0 - saturate(mid / _EdgeWidth)) * 1.6), step(mid, _EdgeWidth * 0.7), _WearCrispness);
                    edge = band * chip * _EdgeWear;
                    albedo = lerp(albedo, _EdgeColor.rgb, edge);
                    metallic = lerp(metallic, 0.85, edge);
                    smoothness = lerp(smoothness, 0.6, edge);
                }

                // Edge wear on any mesh: curvature from screen-space normal derivatives (1/m). Kit pieces have smooth-shaded
                // bevels, so curvature is high exactly on their edges and ~0 on flat faces; noise breaks it into chips.
                if (_CurveWear > 0.001)
                {
                    float3 nW = normalize(input.normalWS);
                    float curv = length(fwidth(nW)) / max(length(fwidth(posWS)), 1e-4);
                    float bandC = saturate((curv - _CurveThreshold) / max(_CurveThreshold, 0.01));
                    float crispC = lerp(0.1, 0.02, _WearCrispness);
                    float chipC = smoothstep(0.42 - crispC, 0.42 + crispC, VV_Fbm(posWS * 9.0 + 7.1));
                    float e2 = bandC * chipC * _CurveWear;
                    albedo = lerp(albedo, _EdgeColor.rgb, e2);
                    metallic = lerp(metallic, 0.25, e2);     // low: with no reflections to show, metal chips render dark
                    smoothness = lerp(smoothness, 0.45, e2);
                    edge = max(edge, e2);
                }

                // Rust patches, biased toward low areas and edges
                float height = saturate(1.0 - posWS.y / max(_GrimeHeight * 2.0, 0.01));
                if (_RustAmount > 0.001)
                {
                    float rn = VV_Fbm(posWS * _RustScale + 11.3);
                    float threshold = 1.0 - _RustAmount * 0.9;
                    float rustEdge = lerp(0.12, 0.02, _WearCrispness);
                    float rust = smoothstep(threshold, threshold + rustEdge, rn + height * 0.12 + edge * 0.3);
                    // Two flat rust tones instead of noisy gradients (painted look)
                    half3 rustCol = lerp(_RustColor.rgb, _RustColor.rgb * 0.62, step(0.55, VV_ValueNoise(posWS * 3.0)) * _WearCrispness
                                         + VV_ValueNoise(posWS * 7.0) * (1.0 - _WearCrispness));
                    albedo = lerp(albedo, rustCol, rust);
                    metallic = lerp(metallic, 0.1, rust);
                    smoothness = lerp(smoothness, 0.12, rust);
                }

                // Grime: creeps up walls from the floor, plus patchy dirt everywhere.
                // Crisp mode turns it into a wavy painted band and flat blotches (cartoon grime).
                float upFacing = saturate(geoN.y);
                float wave = VV_Fbm(posWS * 1.3 + 2.2);
                float softFloor = saturate(1.0 - posWS.y / max(_GrimeHeight, 0.01));
                softFloor *= softFloor;
                float crispFloor = step(posWS.y, _GrimeHeight * (0.35 + 0.65 * wave)) * 0.75;
                float floorGrime = lerp(softFloor, crispFloor, _WearCrispness) * (1.0 - upFacing) * _GrimeAmount;
                float pn = VV_Fbm(posWS * _GrimeScale + 5.1);
                float softPatch = saturate((pn - 0.42) * 3.0);
                float crispPatch = smoothstep(0.5, 0.52, pn) * 0.8;
                float patches = lerp(softPatch, crispPatch, _WearCrispness) * _GrimePatches;
                float grime = saturate(max(floorGrime, patches) * lerp(lerp(0.7, 1.15, VV_ValueNoise(posWS * 3.1)), 1.0, _WearCrispness));
                albedo = lerp(albedo, albedo * _GrimeColor.rgb, grime);
                smoothness *= 1.0 - grime * 0.7;

                InputData inputData = (InputData)0;
                inputData.positionWS = posWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(posWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(posWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = saturate(albedo);
                surface.metallic = metallic;
                surface.smoothness = smoothness;
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = 1.0;
                surface.emission = _EmissionColor.rgb;
                surface.alpha = 1.0;

                // ---------------- Stylized (cel-ish) lighting ----------------
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surface);
                half3 baseAlbedo = surface.albedo * lerp(1.0, 0.8, metallic); // metals read a touch darker in toon shading

                // Ambient: tinted toward the cool shadow colour so unlit sides read as coloured shade, not grey
                half3 color = baseAlbedo * inputData.bakedGI * _AmbientStrength * _ShadowTint.rgb * aoFactor.indirectAmbientOcclusion;

                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
                color += VV_ToonLight(mainLight, inputData.normalWS, inputData.viewDirectionWS, baseAlbedo, smoothness, metallic, true);

                uint pixelLightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light dirLight = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                    color += VV_ToonLight(dirLight, inputData.normalWS, inputData.viewDirectionWS, baseAlbedo, smoothness, metallic, false);
                }
                #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                    color += VV_ToonLight(light, inputData.normalWS, inputData.viewDirectionWS, baseAlbedo, smoothness, metallic, false);
                LIGHT_LOOP_END

                // Rim light (subtle cartoon edge glow, picks up the main light colour)
                half rim = smoothstep(0.62, 0.7, 1.0 - saturate(dot(inputData.normalWS, inputData.viewDirectionWS)));
                color += baseAlbedo * rim * _RimStrength * (mainLight.color * 0.5 + 0.5);

                color += surface.emission;
                color = MixFog(color, inputData.fogCoord);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ================================================================== Shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, lightDir));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = positionCS;
                return o;
            }

            half4 ShadowFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        // ================================================================== Depth
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings DepthVert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half DepthFrag(Varyings input) : SV_Target { return input.positionCS.z; }
            ENDHLSL
        }

        // ================================================================== Depth + normals (SSAO)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DNVert
            #pragma fragment DNFrag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings DNVert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 DNFrag(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
