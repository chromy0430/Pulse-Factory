Shader "HoloTactical/URP/WireframeGlow"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color (HDR)", Color) = (0.20, 0.95, 1.0, 1.0)
        _GlowIntensity ("Glow Intensity", Range(0, 20)) = 5
        _Opacity ("Opacity", Range(0, 1)) = 0.85
        _ScanDensity ("Scanlines per World Unit", Range(0, 100)) = 8
        _ScanSpeed ("Scanline Speed", Range(-30, 30)) = 3
        _ScanStrength ("Scanline Strength", Range(0, 1)) = 0.15
        _FlickerSpeed ("Flicker Speed", Range(0, 60)) = 21
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.12
        _PulseSpeed ("Pulse Speed", Range(0, 20)) = 2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.08
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth Test (4=LEqual, 8=Always)", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Hologram Lines"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            // Additive blending: overlapping lines become brighter.
            Blend SrcAlpha One
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float _GlowIntensity;
                float _Opacity;
                float _ScanDensity;
                float _ScanSpeed;
                float _ScanStrength;
                float _FlickerSpeed;
                float _FlickerAmount;
                float _PulseSpeed;
                float _PulseAmount;
                float _ZTest;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float scanWave = 0.5 + 0.5 * sin(input.positionWS.y * _ScanDensity - t * _ScanSpeed);
                float scan = lerp(1.0, scanWave, _ScanStrength);
                float flickerWave = 0.5 + 0.5 * sin(t * _FlickerSpeed);
                float flicker = lerp(1.0, flickerWave, _FlickerAmount);
                float pulseWave = 0.5 + 0.5 * sin(t * _PulseSpeed);
                float pulse = lerp(1.0, pulseWave, _PulseAmount);

                float3 colour = _GlowColor.rgb * _GlowIntensity * scan * flicker * pulse;
                float alpha = saturate(_GlowColor.a * _Opacity);
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
