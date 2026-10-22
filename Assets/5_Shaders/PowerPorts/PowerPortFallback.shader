Shader "PulseFactory/PowerPortFallback"
{
    Properties
    {
        _Atlas ("Icon Atlas", 2D) = "white" {}
        _AtlasRect ("Atlas UV Rect (xy origin, zw size)", Vector) = (0,0,1,1)
        [Toggle] _UseAtlas ("Use Atlas Instead Of Cube", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="TransparentCutout" }
        Pass
        {
            Name "PowerPortIconsFallback"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment PortFragment
            #include "PowerPortBillboard.hlsl"
            struct Attributes { float3 center : POSITION; float2 uv : TEXCOORD0; float4 motion : TEXCOORD1; float2 size : TEXCOORD2; float4 tint : COLOR; };
            PortVaryings Vert(Attributes input)
            {
                return PortVertex(input.center, input.size.x, input.uv, input.tint, input.motion);
            }
            ENDHLSL
        }
    }
}
