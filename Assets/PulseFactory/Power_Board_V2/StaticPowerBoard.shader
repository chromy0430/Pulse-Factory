Shader "PulseFactory/StaticPowerBoard" {
 Properties { _MainTex("Base RGB and Emission A",2D)="white"{} }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct V {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A i){V o;o.pos=TransformObjectToHClip(i.vertex.xyz);o.uv=i.uv*6;return o;}
 half4 frag(V i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);return half4(c.rgb+c.a*half3(.01,.35,.5),1);}
 ENDHLSL }
 }
}
