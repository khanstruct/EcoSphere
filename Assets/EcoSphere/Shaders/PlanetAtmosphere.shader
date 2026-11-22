Shader "EcoSphere/Planet Atmosphere"
{
    Properties
    {
        _AtmosphereColor ("Color", Color) = (1,.25,.035,1)
        _Strength ("Strength", Range(0,1)) = 0
        _Width ("Width", Range(.01,.12)) = .055
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Back ZWrite Off ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _AtmosphereColor;
                float _Strength, _Width;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;};
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float facing=abs(dot(normalize(input.normalWS),GetWorldSpaceNormalizeViewDir(input.positionWS)));
                float radius=sqrt(saturate(1-facing*facing));
                float surfaceRadius=rcp(1+_Width);
                // A faint veil across the face rises toward the limb, then softens outward.
                float rim=pow(saturate(radius/surfaceRadius),10);
                float outwardFade=1-smoothstep(surfaceRadius,1,radius);
                float opacity=lerp(.018,.8,rim)*outwardFade*saturate(_Strength);
                return half4(_AtmosphereColor.rgb,opacity);
            }
            ENDHLSL
        }
    }
}
