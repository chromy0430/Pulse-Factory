Shader "PulseFactory/PowerPortParticle"
{
    Properties { _MainTex ("Effect Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Input { float4 position:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Output { float4 position:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            Output Vert(Input i) { Output o; o.position=TransformObjectToHClip(i.position.xyz); o.color=i.color; o.uv=i.uv; return o; }
            half4 Frag(Output i):SV_Target
            {
                float2 p=i.uv*2-1;
                float r=length(p);
                float angle=atan2(p.y,p.x);
                float pulse=0.9+0.1*sin(_Time.y*4);
                float glow=saturate(1-r)*0.35;
                float core=1-smoothstep(0.12,0.3* pulse,r);
                float ring=(1-smoothstep(0.03,0.12,abs(r-0.57* pulse)))*0.65;
                float rays=pow(saturate(cos(angle*6+_Time.y*1.5)),16)*(1-smoothstep(0.3,0.95,r));
                half4 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                return half4(i.color.rgb*tex.rgb, saturate(glow+core+ring+rays*0.4)*i.color.a*tex.a);
            }
            ENDHLSL
        }
    }
}
