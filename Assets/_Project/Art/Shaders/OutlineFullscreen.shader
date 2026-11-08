// Vents & Vending — screen-space "ink" outlines (Rick & Morty-ish, toned down). Used by a Full Screen Pass Renderer
// Feature on the URP renderer (requirements: Depth + Normal, fetch colour buffer). Edges come from depth jumps
// (silhouettes) and normal changes (creases); they fade out with distance and skip the sky.
Shader "VentsAndVending/Outline Fullscreen"
{
    Properties
    {
        _OutlineColor ("Outline Colour", Color) = (0.06, 0.05, 0.1, 1)
        _Thickness ("Thickness (pixels at 1080p)", Range(0.5, 4)) = 1.4
        _DepthSensitivity ("Depth Sensitivity", Range(0, 10)) = 2.5
        _NormalSensitivity ("Normal Sensitivity", Range(0, 4)) = 1.2
        _Strength ("Strength", Range(0, 1)) = 0.85
        _FadeStart ("Fade Start (m)", Float) = 18
        _FadeEnd ("Fade End (m)", Float) = 55
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Outline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4 _OutlineColor;
            float _Thickness;
            float _DepthSensitivity;
            float _NormalSensitivity;
            half _Strength;
            float _FadeStart;
            float _FadeEnd;

            float EyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            bool IsSky(float2 uv)
            {
                float raw = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    return raw <= 0.00001;
                #else
                    return raw >= 0.99999;
                #endif
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                if (IsSky(uv))
                {
                    return color;
                }

                // Scale thickness with resolution so lines look the same at 1080p and 4K.
                float2 texel = (_Thickness * (_ScreenParams.y / 1080.0)) / _ScreenParams.xy;
                float2 o1 = float2(texel.x, 0);
                float2 o2 = float2(0, texel.y);

                float d = EyeDepth(uv);
                float dDiff = abs(EyeDepth(uv + o1) - d) + abs(EyeDepth(uv - o1) - d)
                            + abs(EyeDepth(uv + o2) - d) + abs(EyeDepth(uv - o2) - d);
                float depthEdge = smoothstep(0.06, 0.16, (dDiff / max(d, 0.1)) * _DepthSensitivity);

                float3 n = SampleSceneNormals(uv);
                float nDiff = (1.0 - dot(n, SampleSceneNormals(uv + o1))) + (1.0 - dot(n, SampleSceneNormals(uv - o1)))
                            + (1.0 - dot(n, SampleSceneNormals(uv + o2))) + (1.0 - dot(n, SampleSceneNormals(uv - o2)));
                float normalEdge = smoothstep(0.35, 0.75, nDiff * _NormalSensitivity);

                float fade = 1.0 - saturate((d - _FadeStart) / max(_FadeEnd - _FadeStart, 0.01));
                float edge = saturate(max(depthEdge, normalEdge)) * fade * _Strength;
                return half4(lerp(color.rgb, _OutlineColor.rgb, edge), color.a);
            }
            ENDHLSL
        }
    }
}
