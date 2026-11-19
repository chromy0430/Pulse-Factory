Shader "Pulse Factory/Build Ghost Surface"
{
    Properties
    {
        [MainColor] _BaseColor ("Hologram Color", Color) = (0.15, 1, 0.55, 0.42)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS, false);
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 n = SafeNormalize(input.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = pow(1.0h - saturate(abs(dot(n, v))), 2.0h);
                half shade = .18h + .52h * saturate(dot(n, normalize(float3(-.4, .85, .3))));
                float band = input.positionWS.y * 4.0 - _Time.y * .7;
                half scan = (.5 + .5 * cos(band * 6.2831853)) * saturate(1.0 - fwidth(band) * 2.0);
                // A shared screen-door pattern keeps the rear of a multipart factory from
                // accumulating opacity through the front. Covered pixels write depth.
                uint2 p = uint2(input.positionCS.xy) & 3u;
                uint2 low = p & 1u, high = p >> 1u;
                uint threshold = 4u * (2u * (low.x ^ low.y) + low.y)
                               + 2u * (high.x ^ high.y) + high.y;
                half coverage = saturate(_BaseColor.a + .18h + rim * .22h);
                clip(coverage - (threshold + .5) / 16.0);
                return half4(_BaseColor.rgb * (shade + rim * .42h + scan * .15h), 1);
            }
            ENDHLSL
        }
    }
}
