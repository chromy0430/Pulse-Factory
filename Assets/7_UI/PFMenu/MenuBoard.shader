Shader "PulseFactory/Menu/Board"
{
    Properties { _MainTex("Board atlas",2D)="white"{} _EmissionMap("Board circuits",2D)="black"{} }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            struct A {float4 positionOS:POSITION; float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0;};
            V Vert(A i){V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv*3;return o;}
            half4 Frag(V i):SV_Target
            {
                half4 board=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                half glow=.5h+.5h*sin(_Time.y*1.25h+i.uv.x*2.4h+i.uv.y);
                half circuit=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).g;
                float2 cells=i.uv*9;
                float2 edges=abs(frac(cells-.5)-.5)/max(fwidth(cells),.0001);
                half grid=1-saturate(min(edges.x,edges.y));
                return half4(half3(.014,.021,.027)+board.rgb*.25h+grid*.042h+
                             circuit*half3(.032,.20,.16)*(.25h+.75h*glow),1);
            }
            ENDHLSL
        }
    }
}
