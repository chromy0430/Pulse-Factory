Shader "Hidden/EnhancedRendering/CinematicBloom"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #pragma target 3.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        TEXTURE2D_X(_BloomLow);
        float4 _SourceTexel;
        float4 _BloomParams;
        float4 _BloomTint;

        float3 Source(float2 uv)
        {
            return min(max(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb, 0), _BloomParams.w);
        }

        float3 Downsample(float2 uv)
        {
            float2 t = _SourceTexel.xy;
            float3 c = Source(uv) * 0.125;
            c += (Source(uv + t * float2(-2,-2)) + Source(uv + t * float2(2,-2))
                + Source(uv + t * float2(-2,2)) + Source(uv + t * float2(2,2))) * 0.03125;
            c += (Source(uv + t * float2(-2,0)) + Source(uv + t * float2(2,0))
                + Source(uv + t * float2(0,-2)) + Source(uv + t * float2(0,2))) * 0.0625;
            c += (Source(uv + t * float2(-1,-1)) + Source(uv + t * float2(1,-1))
                + Source(uv + t * float2(-1,1)) + Source(uv + t * float2(1,1))) * 0.125;
            return c;
        }

        float3 Low(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X(_BloomLow, sampler_LinearClamp, uv).rgb;
        }

        float3 Tent(float2 uv)
        {
            float2 t = _SourceTexel.xy;
            float3 c = Low(uv) * 4;
            c += (Low(uv + t * float2(-1,0)) + Low(uv + t * float2(1,0))
                + Low(uv + t * float2(0,-1)) + Low(uv + t * float2(0,1))) * 2;
            c += Low(uv + t * float2(-1,-1)) + Low(uv + t * float2(1,-1))
                + Low(uv + t * float2(-1,1)) + Low(uv + t * float2(1,1));
            return c * (1.0 / 16.0);
        }

        float4 Prefilter(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float3 color = Downsample(input.texcoord);
            float brightness = max(color.r, max(color.g, color.b));
            float knee = max(_BloomParams.x * _BloomParams.y, 0.00001);
            float soft = clamp(brightness - _BloomParams.x + knee, 0, 2 * knee);
            soft = soft * soft / (4 * knee + 0.00001);
            float contribution = max(brightness - _BloomParams.x, soft) / max(brightness, 0.00001);
            return float4(color * contribution, 1);
        }

        float4 Down(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return float4(Downsample(input.texcoord), 1);
        }

        float4 Up(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            // Normalized interpolation prevents brightness from growing with the pyramid depth.
            return float4(lerp(Source(input.texcoord), Tent(input.texcoord), _BloomParams.z), 1);
        }

        float4 Composite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
            return float4(scene.rgb + Low(input.texcoord) * _BloomTint.rgb, scene.a);
        }
        ENDHLSL
        Pass
        {
            Name "HDR Prefilter"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Prefilter
            ENDHLSL
        }
        Pass
        {
            Name "Downsample"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Down
            ENDHLSL
        }
        Pass
        {
            Name "Upsample"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Up
            ENDHLSL
        }
        Pass
        {
            Name "HDR Composite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Composite
            ENDHLSL
        }
    }
    Fallback Off
}
