Shader "EcoSphere/Barren Planet"
{
    Properties
    {
        _BaseMap ("Barren terrain", 2D) = "white" {}
        _HeightMap ("Elevation (linear)", 2D) = "gray" {}
        _SeaLevel ("Sea level", Range(-0.02, 1.02)) = -0.02
        _WaterColor ("Deep water", Color) = (0.025, 0.16, 0.32, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_HeightMap); SAMPLER(sampler_HeightMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _WaterColor;
                float _SeaLevel;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; float3 positionWS : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half elevation = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, input.uv).r;
                half3 rock = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half water = 1 - smoothstep(_SeaLevel - .008, _SeaLevel + .008, elevation);
                half depth = saturate((_SeaLevel - elevation) * 9);
                half3 ocean = lerp(_WaterColor.rgb * half3(1.6, 1.8, 1.65), _WaterColor.rgb, depth);
                half3 albedo = lerp(rock, ocean, water);
                half3 normal = normalize(input.normalWS);
                Light light = GetMainLight();
                half diffuse = saturate(dot(normal, light.direction));
                half3 illumination = max(SampleSH(normal), half3(.18, .18, .18)) + light.color * diffuse * .65;
                half3 halfDirection = normalize(light.direction + GetWorldSpaceNormalizeViewDir(input.positionWS));
                half glint = pow(saturate(dot(normal, halfDirection)), 64) * water * diffuse;
                return half4(albedo * illumination + light.color * glint * .45, 1);
            }
            ENDHLSL
        }
    }
}
