Shader "PulseFactory/ChargePreview" {
 Properties { _BaseColor("Base",Color)=(.025,.06,.07,1) _GlowColor("Glow",Color)=(0,1,1,1) _Fill("Fill",Float)=0 _YMin("Min",Float)=0 _YMax("Max",Float)=1 _Mode("Mode",Float)=0 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass { Tags {"LightMode"="UniversalForward"} HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_GlowColor;float _Fill,_YMin,_YMax,_Mode;
 CBUFFER_END
 struct A{float4 vertex:POSITION;float3 normal:NORMAL;};struct V{float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
 V vert(A i){V o;o.pos=TransformObjectToHClip(i.vertex.xyz);o.world=TransformObjectToWorld(i.vertex.xyz);o.normal=TransformObjectToWorldNormal(i.normal);return o;}
 half4 frag(V i):SV_Target {float h=saturate((( _Mode<.5?i.world.y:i.world.z)-_YMin)/max(.001,_YMax-_YMin));float v=_Mode<.5?1-smoothstep(_Fill-.035,_Fill+.035,h):pow(saturate(.5+.5*cos((h+_Fill)*6.283185)),12);Light l=GetMainLight();float diffuse=.3+.7*saturate(dot(normalize(i.normal),l.direction));return half4(_BaseColor.rgb*diffuse+_GlowColor.rgb*v*1.6,1);}
 ENDHLSL }
 }
}
