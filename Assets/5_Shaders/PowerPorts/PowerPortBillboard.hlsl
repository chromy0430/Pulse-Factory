#ifndef POWER_PORT_BILLBOARD_INCLUDED
#define POWER_PORT_BILLBOARD_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D(_Atlas);
SAMPLER(sampler_Atlas);
CBUFFER_START(UnityPerMaterial)
float4 _AtlasRect;
float _UseAtlas;
CBUFFER_END
float _PortTime;
struct PortVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 tint : TEXCOORD1; };
PortVaryings PortVertex(float3 center, float size, float2 uv, float4 tint, float4 motion)
{
    PortVaryings o;
    float t = motion.w <= 0 ? 1 : saturate((_PortTime - motion.z) / motion.w);
    center.y += lerp(motion.x, motion.y, t*t*(3-2*t));
    float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
    float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
    float3 world = center + (right * (uv.x-0.5) + up * (uv.y-0.5)) * size;
    o.positionCS = TransformWorldToHClip(world);
    o.uv = uv;
    o.tint = tint;
    return o;
}
half4 PortFragment(PortVaryings i) : SV_Target
{
    if (_UseAtlas > 0.5)
    {
        half4 tex = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, _AtlasRect.xy + i.uv * _AtlasRect.zw);
        clip(tex.a - 0.1);
        return tex * i.tint;
    }
    // Three faces of a small cube, in a replaceable billboard material.
    float x = i.uv.x, y = i.uv.y;
    float edge = abs(x-0.5)*0.5;
    clip(y-edge);
    clip(1-edge-y);
    float shade = y >= 0.5+edge ? 1.0 : x < 0.5 ? 0.68 : 0.84;
    return half4(i.tint.rgb * shade, 1);
}
#endif
