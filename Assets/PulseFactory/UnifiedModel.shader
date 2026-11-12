Shader "PulseFactory/UnifiedModel" {
 Properties { _BaseMap("Model Atlas",2D)="white"{} _SrcBlend("Src",Float)=1 _DstBlend("Dst",Float)=0 _ZWrite("Depth",Float)=1 }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {Tags {"LightMode"="UniversalForward"} Blend [_SrcBlend] [_DstBlend] ZWrite [_ZWrite]
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.5
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 struct A{float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float2 surface:TEXCOORD1;float2 effect:TEXCOORD2;float2 progress:TEXCOORD3;float4 color:COLOR;};
 struct V{float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;float2 surface:TEXCOORD3;float2 effect:TEXCOORD4;float2 progress:TEXCOORD5;float4 color:COLOR;};
 V vert(A i){V o;o.pos=TransformObjectToHClip(i.vertex.xyz);o.world=TransformObjectToWorld(i.vertex.xyz);o.normal=TransformObjectToWorldNormal(i.normal);o.uv=i.uv;o.surface=i.surface;o.effect=i.effect;o.progress=i.progress;o.color=i.color;return o;}
 half4 frag(V i):SV_Target {
 float strength=i.effect.x;int mode=(int)round(i.effect.y);float t=_Time.y;
 if(mode==1)strength=.6+5*pow(.5-.5*cos(fmod(t,3)/3*6.283185),2);
 if(mode==2)strength=1.6*(1-smoothstep(saturate(fmod(t,4)/3.3)-.035,saturate(fmod(t,4)/3.3)+.035,i.progress.x));
 if(mode>=3&&mode<=5){float duration=mode==3?2.25:(mode==4?3.75:3);float fill=saturate(fmod(t,5)/duration);strength=1.6*(1-smoothstep(fill-.035,fill+.035,i.progress.x));}
 if(mode>=10&&mode<=24){int cell=(mode-10)/5;int level=(mode-10)%5;float duration=cell==0?2.25:(cell==1?3.75:3);float fill=saturate(fmod(t,5)/duration);strength=2*smoothstep(level*.2,level*.2+.16,fill);}
 if(mode==30)strength=1.6*pow(saturate(.5+.5*cos((i.progress.y+fmod(t,2.4)/2.4)*6.283185)),12);
 InputData d=(InputData)0;d.positionWS=i.world;d.normalWS=normalize(i.normal);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);d.bakedGI=SampleSH(d.normalWS);d.shadowCoord=TransformWorldToShadowCoord(i.world);
 SurfaceData s=(SurfaceData)0;s.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;s.metallic=i.surface.x;s.smoothness=i.surface.y;s.normalTS=half3(0,0,1);s.occlusion=1;s.alpha=i.color.a;s.emission=i.color.rgb*strength;
 return UniversalFragmentPBR(d,s);
 }
 ENDHLSL }
 }
}
