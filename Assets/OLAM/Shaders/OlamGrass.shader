Shader "OLAM/Grass"
{
    // Лёгкая трава для VR: cutout, wind, GPU instancing, distance fade. Без теней.
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (0.28, 0.52, 0.18, 1)
        _TipColor ("Tip Color", Color) = (0.55, 0.72, 0.28, 1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.4
        _WindStrength ("Wind Strength", Range(0,1)) = 0.4
        _WindSpeed ("Wind Speed", Range(0,5)) = 1.6
        _DarkSide ("Shadow Tint", Color) = (0.1, 0.18, 0.08, 1)
        _FadeStart ("Fade Start (m)", Float) = 12
        _FadeEnd ("Fade End (m)", Float) = 22
        _HeightTint ("Height Tip Blend", Range(0,1)) = 0.65
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
                half4 _TipColor;
                half _Cutoff;
                half _WindStrength;
                half _WindSpeed;
                half4 _DarkSide;
                half _FadeStart;
                half _FadeEnd;
                half _HeightTint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                half fade : TEXCOORD3;
                half tip : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // tip: UV.y или vertex color.a / position.y
                half tip = max(v.uv.y, saturate(v.positionOS.y));
                tip = max(tip, v.color.a);

                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 pos = OlamApplyWind(v.positionOS.xyz, posWS, tip, _WindStrength, _WindSpeed);

                VertexPositionInputs vi = GetVertexPositionInputs(pos);
                VertexNormalInputs ni = GetVertexNormalInputs(v.normalOS);
                o.positionCS = vi.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.normalWS = ni.normalWS;
                o.fogFactor = ComputeFogFactor(vi.positionCS.z);
                o.fade = OlamDistanceFade(vi.positionWS, _FadeStart, _FadeEnd);
                o.tip = tip;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                clip(i.fade - 0.01h);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half4 albedo = tex * _BaseColor;
                albedo.rgb = lerp(albedo.rgb, _TipColor.rgb, i.tip * _HeightTint);
                clip(albedo.a * i.fade - _Cutoff);

                Light light = GetMainLight();
                half3 n = normalize(i.normalWS);
                half ndl = saturate(dot(n, light.direction));
                ndl = max(ndl, saturate(dot(-n, light.direction)) * 0.5h);
                // ambient чуть выше — ночь Чаткала + фонарик
                half3 lit = lerp(_DarkSide.rgb, albedo.rgb, 0.4h + 0.6h * ndl);
                lit *= light.color;
                lit = MixFog(lit, i.fogFactor);
                return half4(lit, 1);
            }
            ENDHLSL
        }

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
                half4 _TipColor;
                half _Cutoff;
                half _WindStrength;
                half _WindSpeed;
                half4 _DarkSide;
                half _FadeStart;
                half _FadeEnd;
                half _HeightTint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
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
                half tip = max(v.uv.y, saturate(v.positionOS.y));
                tip = max(tip, v.color.a);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 pos = OlamApplyWind(v.positionOS.xyz, posWS, tip, _WindStrength, _WindSpeed);
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
