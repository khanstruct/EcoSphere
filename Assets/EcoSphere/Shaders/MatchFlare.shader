Shader "EcoSphere/Match Flare"
{
    Properties { _Strength("Strength",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;};
            CBUFFER_START(UnityPerMaterial)
                float _Strength;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half rim=1-saturate(dot(normalize(input.normalWS),GetWorldSpaceNormalizeViewDir(input.positionWS)));
                half brightness=_Strength*(.7+.3*rim*rim);
                return half4(brightness,brightness,brightness,1);
            }
            ENDHLSL
        }
    }
}
