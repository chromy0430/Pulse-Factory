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
 // Screen-space effects need the same geometry as the visible model.
 // Keep these passes when updating the imported model shader.
 Pass
 {
     Name "DepthOnly"
     Tags { "LightMode"="DepthOnly" }
     ZWrite [_ZWrite] ZTest LEqual Cull Back ColorMask 0
     HLSLPROGRAM
     #pragma target 3.5
     #pragma vertex DepthVertex
     #pragma fragment DepthFragment
     #pragma multi_compile_instancing
     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
     struct DepthAttributes
     {
         float4 positionOS : POSITION;
         UNITY_VERTEX_INPUT_INSTANCE_ID
     };
     struct DepthVaryings
     {
         float4 positionCS : SV_POSITION;
         UNITY_VERTEX_OUTPUT_STEREO
     };
     DepthVaryings DepthVertex(DepthAttributes input)
     {
         DepthVaryings output = (DepthVaryings)0;
         UNITY_SETUP_INSTANCE_ID(input);
         UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
         output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
         return output;
     }
     half4 DepthFragment(DepthVaryings input) : SV_Target
     {
         UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
         return 0;
     }
     ENDHLSL
 }
 Pass
 {
     Name "DepthNormals"
     Tags { "LightMode"="DepthNormals" }
     ZWrite [_ZWrite] ZTest LEqual Cull Back
     HLSLPROGRAM
     #pragma target 3.5
     #pragma vertex ModelNormalsVertex
     #pragma fragment ModelNormalsFragment
     #pragma multi_compile_instancing
     #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
     #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
     struct ModelNormalsAttributes
     {
         float4 positionOS : POSITION;
         float3 normalOS : NORMAL;
         UNITY_VERTEX_INPUT_INSTANCE_ID
     };
     struct ModelNormalsVaryings
     {
         float4 positionCS : SV_POSITION;
         float3 normalWS : TEXCOORD0;
         UNITY_VERTEX_INPUT_INSTANCE_ID
         UNITY_VERTEX_OUTPUT_STEREO
     };
     ModelNormalsVaryings ModelNormalsVertex(ModelNormalsAttributes input)
     {
         ModelNormalsVaryings output = (ModelNormalsVaryings)0;
         UNITY_SETUP_INSTANCE_ID(input);
         UNITY_TRANSFER_INSTANCE_ID(input, output);
         UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
         output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
         output.normalWS = TransformObjectToWorldNormal(input.normalOS);
         return output;
     }
     void ModelNormalsFragment(ModelNormalsVaryings input, out half4 outNormalWS : SV_Target0
     #ifdef _WRITE_RENDERING_LAYERS
         , out uint outRenderingLayers : SV_Target1
     #endif
     )
     {
         UNITY_SETUP_INSTANCE_ID(input);
         UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
         float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
     #if defined(_GBUFFER_NORMALS_OCT)
         float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
         outNormalWS = half4(PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5)), 0);
     #else
         outNormalWS = half4(normalWS, 0);
     #endif
     #ifdef _WRITE_RENDERING_LAYERS
         outRenderingLayers = EncodeMeshRenderingLayer();
     #endif
     }
     ENDHLSL
 }

 }
}
