Shader "PulseFactory/PowerPortIndirect"
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
            Name "PowerPortIcons"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment PortFragment
            #include "PowerPortBillboard.hlsl"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
            struct PortData { float4 positionSize; float4 tint; float4 motion; };
            StructuredBuffer<PortData> _PortData;
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; uint instanceID : SV_InstanceID; };
            PortVaryings Vert(Attributes input)
            {
                InitIndirectDrawArgs(0);
                PortData port = _PortData[GetIndirectInstanceID(input.instanceID)];
                return PortVertex(port.positionSize.xyz, port.positionSize.w, input.uv, port.tint, port.motion);
            }
            ENDHLSL
        }
    }
}
