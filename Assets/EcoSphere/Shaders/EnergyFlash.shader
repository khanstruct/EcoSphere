Shader "EcoSphere/Energy Flash"
{
    Properties
    {
        _TintColor("Tint",Color)=(1,1,1,1)
        _Strength("Strength",Float)=1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Depth test",Float)=4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            Cull Off
            ZWrite Off
            ZTest [_ZTest]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;float _Strength;
            CBUFFER_END
            Varyings Vert(Attributes input){Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.uv=input.uv;return o;}
            half4 Frag(Varyings input):SV_Target
            {
                half radius=length(input.uv*2-1);
                half glow=exp(-radius*radius*5)*(1-smoothstep(.65,1,radius));
                half core=1-smoothstep(0,.3,radius);
                return half4(lerp(_TintColor.rgb,half3(1,1,1),core)*glow*_Strength,1);
            }
            ENDHLSL
        }
    }
}
