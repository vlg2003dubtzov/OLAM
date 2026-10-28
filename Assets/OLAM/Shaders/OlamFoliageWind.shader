Shader "OLAM/Foliage Wind"
{
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (0.25, 0.55, 0.22, 1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.35
        _WindStrength ("Wind Strength", Range(0,1)) = 0.25
        _WindSpeed ("Wind Speed", Range(0,5)) = 1.2
        _DarkSide ("Shadow Tint", Color) = (0.12, 0.22, 0.1, 1)
        _FadeStart ("Fade Start (m)", Float) = 18
        _FadeEnd ("Fade End (m)", Float) = 32
        [Toggle] _GPUInstancing ("Enable GPU Instancing", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }
        Cull Off
        ZWrite On

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
            #include "OlamWind.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _WindStrength;
                half _WindSpeed;
                half4 _DarkSide;
                half _FadeStart;
                half _FadeEnd;
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
                float fogFactor : TEXCOORD2;
                half fade : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float h = saturate(v.positionOS.y);
                float3 pos = OlamApplyWind(v.positionOS.xyz, posWS, h, _WindStrength, _WindSpeed);

                VertexPositionInputs vi = GetVertexPositionInputs(pos);
                VertexNormalInputs ni = GetVertexNormalInputs(v.normalOS);
                o.positionCS = vi.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.normalWS = ni.normalWS;
                o.fogFactor = ComputeFogFactor(vi.positionCS.z);
                o.fade = OlamDistanceFade(vi.positionWS, _FadeStart, _FadeEnd);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                clip(i.fade - 0.01h);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                clip(albedo.a * i.fade - _Cutoff);

                Light light = GetMainLight();
                half ndl = saturate(dot(normalize(i.normalWS), light.direction));
                // двухсторонний lit без доп. прохода
                ndl = max(ndl, saturate(dot(-normalize(i.normalWS), light.direction)) * 0.55h);
                half3 col = lerp(_DarkSide.rgb, albedo.rgb, 0.35h + 0.65h * ndl);
                col *= light.color;
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        // DepthOnly — дешевле для depth prepass / occlusion в URP
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "OlamWind.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _WindStrength;
                half _WindSpeed;
                half4 _DarkSide;
                half _FadeStart;
                half _FadeEnd;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fade : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthVert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float h = saturate(v.positionOS.y);
                float3 pos = OlamApplyWind(v.positionOS.xyz, posWS, h, _WindStrength, _WindSpeed);
                o.positionCS = TransformObjectToHClip(pos);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fade = OlamDistanceFade(TransformObjectToWorld(pos), _FadeStart, _FadeEnd);
                return o;
            }

            half4 DepthFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a * i.fade;
                clip(a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
