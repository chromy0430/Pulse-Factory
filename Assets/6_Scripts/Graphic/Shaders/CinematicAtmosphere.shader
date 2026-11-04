Shader "Hidden/EnhancedRendering/CinematicAtmosphere"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #pragma target 4.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        TEXTURE2D_X_FLOAT(_ERSceneDepth);
        TEXTURE2D_X(_ERFog);
        TEXTURE2D_X_FLOAT(_ERFogDistance);
        float4 _ERMedium; // extinction, distance, base height, height falloff
        float4 _ERLighting; // anisotropy, step count, shadow enabled, cascade count
        float4 _ERAmbient, _ERAlbedo, _ERLightDirection, _ERLightColor, _ERFogSize;

        void Ray(float2 uv, out float3 origin, out float3 direction, out float rayLength)
        {
            float raw = SAMPLE_TEXTURE2D_X(_ERSceneDepth, sampler_PointClamp, uv).r;
            #if UNITY_REVERSED_Z
                float depth = max(raw, 0.000001);
                float nearDepth = 1;
            #else
                float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, min(raw, 0.999999));
                float nearDepth = UNITY_NEAR_CLIP_VALUE;
            #endif
            float3 surface = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
            origin = unity_OrthoParams.w > 0.5
                ? ComputeWorldSpacePosition(uv, nearDepth, UNITY_MATRIX_I_VP) : GetCameraPositionWS();
            float3 offset = surface - origin;
            float distance = max(length(offset), 0.00001);
            direction = offset / distance;
            rayLength = min(distance, _ERMedium.y);
        }

        float Shadow(float3 positionWS)
        {
            if (_ERLighting.z < 0.5) return 1;
            // Sample the directional atlas in world space, never a screen-space shadow mask.
            int cascade = _ERLighting.w > 1 ? (int)ComputeCascadeIndex(positionWS) : 0;
            float3 coord = mul(_MainLightWorldToShadow[cascade], float4(positionWS, 1)).xyz;
            if (coord.z <= 0 || coord.z >= 1) return 1;
            float visibility = SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_LinearClampCompare, coord);
            float4 parameters = GetMainLightShadowParams();
            visibility = lerp(1, visibility, parameters.x);
            return lerp(visibility, 1, GetMainLightShadowFade(positionWS));
        }

        float4 Integrate(float2 uv, out float distance)
        {
            float3 origin, direction;
            Ray(uv, origin, direction, distance);
            int count = clamp((int)_ERLighting.y, 8, 64);
            float stepLength = distance / count;
            float g = _ERLighting.x;
            float cosine = dot(direction, normalize(_ERLightDirection.xyz));
            float phase = (1-g*g) / (4*PI*pow(max(1+g*g-2*g*cosine, 0.001), 1.5));
            float3 scattered = 0;
            float transmittance = 1;
            [loop] for (int i=0; i<count; i++)
            {
                float3 positionWS = origin + direction * ((i+0.5)*stepLength);
                float density = _ERMedium.x * exp(-max(0,positionWS.y-_ERMedium.z)*_ERMedium.w);
                float segmentT = exp(-density*stepLength);
                float3 source = _ERAmbient.rgb + _ERLightColor.rgb * phase * Shadow(positionWS);
                scattered += transmittance * (1-segmentT) * _ERAlbedo.rgb * source;
                transmittance *= segmentT;
            }
            return float4(scattered, transmittance);
        }

        struct FogOutput { float4 fog:SV_Target0; float distance:SV_Target1; };
        FogOutput FragIntegrate(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            FogOutput output;
            output.fog = Integrate(input.texcoord, output.distance);
            return output;
        }

        float4 FragComposite(Varyings input):SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float3 origin, direction;
            float distance;
            Ray(input.texcoord, origin, direction, distance);
            float2 position = input.texcoord * _ERFogSize.xy - 0.5;
            int2 basePixel = (int2)floor(position);
            float2 fraction = frac(position);
            float4 fog = 0;
            float total = 0;
            float nearest = 1e20;
            float tolerance = max(0.1, distance*0.02);
            [unroll] for (int i=0; i<4; i++)
            {
                int2 offset = int2(i&1,i>>1);
                int2 pixel = clamp(basePixel+offset, int2(0,0), (int2)_ERFogSize.xy-1);
                float sampleDistance = LOAD_TEXTURE2D_X(_ERFogDistance,pixel).r;
                float difference = abs(sampleDistance-distance);
                nearest = min(nearest,difference);
                float2 weights = lerp(1-fraction,fraction,float2(offset));
                float weight = weights.x*weights.y*exp2(-difference/max(tolerance,0.00001));
                fog += LOAD_TEXTURE2D_X(_ERFog,pixel)*weight;
                total += weight;
            }
            // Thin foreground geometry can be absent from all coarse samples. Reintegrate only those edges.
            if (nearest > tolerance || total < 0.00001) fog = Integrate(input.texcoord,distance);
            else fog /= total;
            float4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,input.texcoord);
            return float4(scene.rgb*saturate(fog.a)+max(fog.rgb,0), scene.a);
        }
        ENDHLSL
        Pass
        {
            Name "Integrate Scattering"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragIntegrate
            ENDHLSL
        }
        Pass
        {
            Name "Depth Aware Composite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            ENDHLSL
        }
    }
    Fallback Off
}
