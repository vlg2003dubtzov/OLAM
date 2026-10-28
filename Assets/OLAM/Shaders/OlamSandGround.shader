Shader "OLAM/Sand Ground"
{
    Properties
    {
        _BaseColor ("Sand Color", Color) = (0.76, 0.64, 0.42, 1)
        _ShadowColor ("Shadow Color", Color) = (0.45, 0.35, 0.22, 1)
        _Scale ("Noise Scale", Range(0.1, 20)) = 4
        _Contrast ("Contrast", Range(0.1, 3)) = 1.2
        _Grain ("Fine Grain", Range(0,1)) = 0.15
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

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half _Scale;
                half _Contrast;
                half _Grain;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 posWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Дешёвый value-noise (1 октава + мелкое зерно)
            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs vi = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs ni = GetVertexNormalInputs(v.normalOS);
                o.positionCS = vi.positionCS;
                o.normalWS = ni.normalWS;
                o.posWS = vi.positionWS;
                o.fogFactor = ComputeFogFactor(vi.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float2 xz = i.posWS.xz;
                float n = noise(xz * _Scale);
                n = saturate((n - 0.5) * _Contrast + 0.5);
                // мелкое зерно без второй октавы — дешевле
                float g = hash21(floor(xz * _Scale * 8.0));
                n = lerp(n, g, _Grain * 0.35);

                half3 albedo = lerp(_ShadowColor.rgb, _BaseColor.rgb, n);
                Light light = GetMainLight();
                half ndl = saturate(dot(normalize(i.normalWS), light.direction));
                half3 col = albedo * (0.35h + 0.65h * ndl) * light.color;
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthVert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }
            half4 DepthFrag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}
