Shader "EcoSphere/Alien Overlay"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white" {}
        _Color("Tint",Color)=(1,1,1,1)
        _Brightness("Shell brightness",Range(1,3))=1
        _OutlineColor("Infestation highlight",Color)=(.45,1,.75,1)
        _OutlineWidth("Outline width (texels)",Range(0,12))=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" "DisableBatching"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                half4 _Color,_OutlineColor;
                float _Brightness,_OutlineWidth;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            Varyings Vert(Attributes input){Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.uv=input.uv;o.color=input.color*_Color;return o;}
            half SurroundingAlpha(float2 uv,float2 offset)
            {
                half a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+float2(offset.x,0)).a;
                a=max(a,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv-float2(offset.x,0)).a);
                a=max(a,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+float2(0,offset.y)).a);
                a=max(a,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv-float2(0,offset.y)).a);
                a=max(a,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+offset*.7071).a);
                a=max(a,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv-offset*.7071).a);
                a=max(a,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+float2(offset.x,-offset.y)*.7071).a);
                return max(a,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+float2(-offset.x,offset.y)*.7071).a);
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 shell=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv);
                if(_OutlineWidth<=0)return half4(saturate(shell.rgb*_Brightness),shell.a)*input.color;
                float2 offset=_MainTex_TexelSize.xy*_OutlineWidth;
                half edge=SurroundingAlpha(input.uv,offset);
                half outline=saturate(edge-shell.a);
                half halo=SurroundingAlpha(input.uv,offset*2)*.22*(1-edge);
                half alpha=saturate(shell.a+outline+halo);
                half3 brightShell=saturate(shell.rgb*_Brightness+.065);
                half3 color=(brightShell*shell.a+_OutlineColor.rgb*(outline+halo))/max(alpha,.001);
                return half4(color*input.color.rgb,alpha*input.color.a);
            }
            ENDHLSL
        }
    }
}
