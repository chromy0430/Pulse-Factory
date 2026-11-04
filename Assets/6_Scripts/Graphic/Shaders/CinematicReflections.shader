Shader "Hidden/EnhancedRendering/CinematicReflections"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #pragma target 4.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        TEXTURE2D_X_FLOAT(_CameraDepthTexture);
        TEXTURE2D_X(_ERReflectiveProperties);
        TEXTURE2D_X(_ERReflection);
        float4 _ERRay, _ERSurface, _EROutput, _ERSize;

        bool Sky(float depth)
        {
            #if UNITY_REVERSED_Z
                return depth <= 0.000001;
            #else
                return depth >= 0.999999;
            #endif
        }

        float3 World(float2 uv, float depth)
        {
            #if !UNITY_REVERSED_Z
                depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
            #endif
            return ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
        }

        bool Project(float3 position, out float2 uv, out float delta)
        {
            float viewDepth = -TransformWorldToView(position).z;
            uv = ComputeNormalizedDeviceCoordinates(position, UNITY_MATRIX_VP);
            delta = -1;
            if (viewDepth <= _ProjectionParams.y || any(uv <= 0) || any(uv >= 1)) return false;
            float depth = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_PointClamp, uv, 0).r;
            if (Sky(depth)) return true;
            float sceneDepth = -TransformWorldToView(World(uv, depth)).z;
            delta = viewDepth - sceneDepth;
            return true;
        }

        float4 FragTrace(Varyings input):SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            uint2 pixel = min((uint2)(input.texcoord * _ERSize.xy), (uint2)_ERSize.xy - 1);
            float4 surface = LOAD_TEXTURE2D_X(_ERReflectiveProperties, pixel);
            float depth = LOAD_TEXTURE2D_X(_CameraDepthTexture, pixel).r;
            if (Sky(depth) || surface.a <= _ERSurface.x) return 0;
            float3 position = World(input.texcoord, depth);
            float3 normal = SafeNormalize(LoadSceneNormals(pixel));
            float3 incident = unity_OrthoParams.w > 0.5
                ? -UNITY_MATRIX_V[2].xyz : normalize(position - GetCameraPositionWS());
            if (dot(normal, -incident) <= 0) return 0;
            float3 direction = normalize(reflect(incident, normal));
            float3 origin = position + normal * _ERRay.w;
            int count = clamp((int)_ERRay.y, 16, 128);
            float stepLength = _ERRay.x / count;
            float previousT = 0;
            float previousDelta = -1;
            [loop] for (int i = 1; i <= count; i++)
            {
                float t = stepLength * i;
                float2 uv;
                float delta;
                if (!Project(origin + direction*t, uv, delta)) break;
                if (delta >= 0 && previousDelta < 0)
                {
                    float lower = previousT, upper = t;
                    [unroll] for (int j = 0; j < 6; j++)
                    {
                        float middle = (lower + upper) * 0.5;
                        float2 testUV; float testDelta;
                        Project(origin + direction*middle, testUV, testDelta);
                        if (testDelta >= 0) upper = middle; else lower = middle;
                    }
                    Project(origin + direction*upper, uv, delta);
                    if (delta >= 0 && delta < _ERRay.z && upper > _ERRay.w*2)
                    {
                        uint2 hitPixel = min((uint2)(uv*_ERSize.xy), (uint2)_ERSize.xy-1);
                        float3 hitNormal = LoadSceneNormals(hitPixel);
                        if (dot(hitNormal, direction) >= -0.01) return 0;
                        float edge = min(min(uv.x,uv.y),min(1-uv.x,1-uv.y));
                        float confidence = saturate(edge/_ERSurface.y) * saturate(1-upper/_ERRay.x);
                        confidence *= smoothstep(_ERSurface.x,1,surface.a);
                        float fresnel = pow(1-saturate(dot(normal,-incident)),5);
                        float3 weight = lerp(surface.rgb,1,fresnel);
                        float3 radiance = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,uv,0).rgb;
                        return float4(clamp(radiance,0,_ERSurface.z)*weight*confidence,confidence);
                    }
                    // A failed crossing is usually a depth discontinuity, not a valid solid hit.
                    return 0;
                }
                previousDelta = delta;
                previousT = t;
            }
            return 0;
        }

        float4 FragComposite(Varyings input):SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,input.texcoord);
            float4 reflection = SAMPLE_TEXTURE2D_X(_ERReflection,sampler_PointClamp,input.texcoord);
            if (_EROutput.y > 0.5) return float4(reflection.aaa,scene.a);
            // URP probe lighting is retained. This module adds a bounded SSR contribution.
            return float4(scene.rgb+reflection.rgb*_EROutput.x,scene.a);
        }
        ENDHLSL
        Pass
        {
            Name "Trace Reflections"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragTrace
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
        Pass
        {
            Name "Composite Reflections"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            ENDHLSL
        }
    }
    Fallback Off
}
