Shader "PulseFactory/CableFlow" {
 Properties { _BaseMap("Cable Atlas",2D)="white"{} _Strength("Light Strength",Float)=7 _Period("Period Seconds",Float)=4 }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float _Strength,_Period;
 CBUFFER_END
 struct A {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float2 flow:TEXCOORD2;};
 struct V {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;float2 flow:TEXCOORD3;};
 V vert(A i){V o;o.pos=TransformObjectToHClip(i.vertex.xyz);o.world=TransformObjectToWorld(i.vertex.xyz);o.normal=TransformObjectToWorldNormal(i.normal);o.uv=i.uv;o.flow=i.flow;return o;}
 half4 frag(V i):SV_Target {half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);float light=saturate((cos((i.flow.x-_Time.y/max(.01,_Period))*6.283185)-.85)/.15)*saturate(i.flow.y)*_Strength;
 InputData d=(InputData)0;d.positionWS=i.world;d.normalWS=normalize(i.normal);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);d.bakedGI=SampleSH(d.normalWS);d.shadowCoord=TransformWorldToShadowCoord(i.world);
 SurfaceData s=(SurfaceData)0;s.albedo=c.rgb;s.metallic=c.a;s.smoothness=.54;s.normalTS=half3(0,0,1);s.occlusion=1;s.alpha=1;s.emission=half3(1,.65,.04)*light;return UniversalFragmentPBR(d,s);}
 ENDHLSL }
 }
}
