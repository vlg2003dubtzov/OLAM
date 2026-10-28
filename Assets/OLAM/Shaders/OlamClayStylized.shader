Shader "OLAM/Clay Stylized"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.62, 0.42, 0.28, 1)
        _RampSoftness ("Ramp Softness", Range(0.01, 1)) = 0.35
        _RimColor ("Rim", Color) = (1, 0.85, 0.6, 0.35)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _Dust ("Dust Amount", Range(0,1)) = 0.25
        _DustColor ("Dust Color", Color) = (0.78, 0.68, 0.48, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _RampSoftness;
                half4 _RimColor;
                half _RimPower;
                half _Dust;
                half4 _DustColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs vi = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs ni = GetVertexNormalInputs(v.normalOS);
                o.positionCS = vi.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.normalWS = ni.normalWS;
                o.viewWS = GetWorldSpaceViewDir(vi.positionWS);
                o.fogFactor = ComputeFogFactor(vi.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                Light light = GetMainLight();
                half ndl = saturate(dot(n, light.direction));
                half ramp = smoothstep(0, _RampSoftness, ndl);

                // пыль сверху (как глина в пустыне)
                half up = saturate(n.y);
                half3 baseCol = lerp(albedo.rgb, _DustColor.rgb, up * _Dust);

                half3 col = baseCol * (0.25h + 0.75h * ramp) * light.color;
                half rim = pow(1.0h - saturate(dot(n, v)), _RimPower) * _RimColor.a;
                col += _RimColor.rgb * rim;
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
