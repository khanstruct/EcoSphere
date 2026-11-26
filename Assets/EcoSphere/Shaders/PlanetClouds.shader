Shader "EcoSphere/Planet Clouds"
{
    Properties
    {
        _CloudMap("Cloud mask",2D)="black"{}
        _Coverage("Coverage",Range(0,1))=0
        _Density("Density",Range(0,1))=0
        _Darkness("Darkness",Range(0,1))=0
        _Drift("Independent drift",Float)=0
        _LightningCenter("Future flash direction and radius",Vector)=(0,1,0,.15)
        _LightningStrength("Future localized flash",Range(0,4))=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
        Pass
        {
            Cull Back ZWrite Off ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_CloudMap);SAMPLER(sampler_CloudMap);
            CBUFFER_START(UnityPerMaterial)
                float _Coverage,_Density,_Darkness,_Drift,_LightningStrength;
                float4 _LightningCenter;
            CBUFFER_END
            float _PlanetNightAmbientDim;
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 normalOS:TEXCOORD1;float2 uv:TEXCOORD2;float3 positionWS:TEXCOORD3;};
            Varyings Vert(Attributes input)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.normalOS=input.normalOS;o.uv=input.uv;return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 uv=i.uv+float2(_Drift,0);
                float broad=SAMPLE_TEXTURE2D(_CloudMap,sampler_CloudMap,uv).r;
                float detail=SAMPLE_TEXTURE2D(_CloudMap,sampler_CloudMap,i.uv*2+float2(-_Drift*.43,_Drift*.12)).r;
                float mask=lerp(broad,broad*.75+detail*.25,.45);
                float threshold=lerp(.94,.02,_Coverage);
                float cloud=lerp(smoothstep(threshold-.18,threshold+.18,mask),1,smoothstep(.85,1,_Coverage))*saturate(_Coverage*20);
                float alpha=cloud*lerp(.25,.92,_Density);
                float facing=saturate(dot(normalize(i.normalWS),GetWorldSpaceNormalizeViewDir(i.positionWS)));
                alpha*=smoothstep(0,.16,facing);
                Light light=GetMainLight();float diffuse=saturate(dot(normalize(i.normalWS),light.direction));
                float3 color=lerp(float3(.97,.98,1),float3(.18,.19,.22),_Darkness);
                // _PlanetNightAmbientDim (global, 0 by default) dims the fixed ambient term in the day/night view.
                color*=(float3(.23,.25,.3)*(1-_PlanetNightAmbientDim)+light.color*diffuse*.77)*lerp(.8,1,mask);
                float distanceFromFlash=1-dot(normalize(i.normalOS),normalize(_LightningCenter.xyz));
                float flash=exp(-distanceFromFlash/max(.001,_LightningCenter.w))*_LightningStrength;
                color+=float3(.72,.82,1)*flash*cloud;
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
