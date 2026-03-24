Shader "Custom/ToonShader"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _AmbientColor ("Ambient Color", Color) = (0.2,0.2,0.2,1)
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _Glossiness ("Glossiness", Float) = 32
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimThreshold ("Rim Threshold", Range(0,1)) = 0.1
        _Bands ("Light Bands", Range(1,10)) = 3
        _ColorBands ("Color Bands", Range(1,32)) = 8
        _ShadowSharpness ("Shadow Sharpness", Range(0.01, 0.5)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP shadow keywords — critical for receiving shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOW_CASCADE_BLEND
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _AmbientColor;
                half4 _SpecularColor;
                float _Glossiness;
                half4 _RimColor;
                float _RimThreshold;
                float _Bands;
                float _ColorBands;
                float _ShadowSharpness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogCoord : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogCoord = ComputeFogFactor(posInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(posInputs);

                return output;
            }

            float Bandify(float value, float bands)
            {
                return floor(value * bands) / bands;
            }

            half3 Posterize(half3 color, float bands)
            {
                return floor(color * bands) / bands;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                albedo.rgb = Posterize(albedo.rgb, _ColorBands);

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // Biased shadow sampling
                Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, half4(1, 1, 1, 1));

                float rawShadow = mainLight.shadowAttenuation;
                float NdotL = dot(normalWS, mainLight.direction);

                // Mask shadow on back faces to prevent acne on dark side
                float shadowMask = step(0.0, NdotL);
                float shadow = smoothstep(0.5 - _ShadowSharpness, 0.5 + _ShadowSharpness, rawShadow) * shadowMask;

                float toonDiffuse = Bandify(saturate(NdotL), _Bands) * shadow;
                half3 diffuse = mainLight.color * toonDiffuse;

                // Specular
                float3 halfVec = normalize(mainLight.direction + viewDirWS);
                float NdotH = dot(normalWS, halfVec);
                float specular = pow(saturate(NdotH), _Glossiness * _Glossiness);
                float toonSpec = step(0.5, specular) * shadow;
                half3 specularColor = _SpecularColor.rgb * toonSpec;

                // Rim
                float NdotV = dot(normalWS, viewDirWS);
                float rimVal = 1.0 - NdotV;
                float rimMask = step(_RimThreshold, rimVal) * step(0.0, NdotL);
                half3 rim = _RimColor.rgb * rimMask;

                half3 lighting = _AmbientColor.rgb + diffuse + specularColor + rim;
                half3 color = albedo.rgb * lighting;

                color = MixFog(color, input.fogCoord);
                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}