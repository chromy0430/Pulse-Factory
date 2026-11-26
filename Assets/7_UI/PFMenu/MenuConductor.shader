Shader "PulseFactory/Menu/Conductor"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10"}
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 Vert(float4 p:POSITION):SV_POSITION { return TransformObjectToHClip(p.xyz); }
            half4 Frag():SV_Target { return half4(.16,.93,.62,1); }
            ENDHLSL
        }
    }
}
