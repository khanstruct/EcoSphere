Shader "EcoSphere/UI/Animated Sun"
{
    // Animated star for a UI Image: churning plasma disc, a slowly turning corona and a soft glow.
    // Colors and motion come from SunAppearance (per sun type); textures are shared grayscale masks.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (unused)",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        _PlasmaTex ("Plasma mask (tiling)",2D)="gray" {}
        _CoronaTex ("Corona mask",2D)="black" {}
        _CoreColor ("Hot core",Color)=(1,.97,.8,1)
        _SurfaceColor ("Surface",Color)=(1,.72,.2,1)
        _SpotColor ("Cool spots",Color)=(.6,.18,.02,1)
        _CoronaColor ("Corona",Color)=(1,.78,.35,1)
        _DiscRadius ("Disc radius (0-1 of image)",Range(.2,.8))=.475
        _SurfaceSpeed ("Surface speed",Float)=.015
        _SurfaceContrast ("Surface contrast",Float)=1.6
        _CoronaSpeed ("Corona turn speed",Float)=.02
        _CoronaStrength ("Corona strength",Float)=1.2
        _GlowStrength ("Glow strength",Float)=.8
        _StencilComp ("Stencil Comparison",Float)=8
        _Stencil ("Stencil ID",Float)=0
        _StencilOp ("Stencil Operation",Float)=0
        _StencilWriteMask ("Stencil Write Mask",Float)=255
        _StencilReadMask ("Stencil Read Mask",Float)=255
        _ColorMask ("Color Mask",Float)=15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        // Premultiplied output: the glow adds light over the starfield without a dark fringe.
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float2 localPosition:TEXCOORD1;};
            sampler2D _PlasmaTex,_CoronaTex;
            float4 _Color,_ClipRect,_CoreColor,_SurfaceColor,_SpotColor,_CoronaColor;
            float _DiscRadius,_SurfaceSpeed,_SurfaceContrast,_CoronaSpeed,_CoronaStrength,_GlowStrength;

            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;
                o.uv=v.uv;o.localPosition=v.vertex.xy;return o;
            }
            float2 Rotate(float2 p,float a){float s=sin(a),c=cos(a);return float2(c*p.x-s*p.y,s*p.x+c*p.y);}

            half4 frag(v2f i):SV_Target
            {
                float t=_Time.y;
                float2 p=i.uv*2-1;
                float r=length(p);

                // Surface: project onto a sphere so features bulge toward the middle and compress at the limb.
                float2 s=p/_DiscRadius;
                float z=sqrt(saturate(1-dot(s,s)));
                float2 sphereUV=s/(1+z);
                // A slow warp field distorts two drifting plasma layers, so bright regions form and fade.
                float warp=tex2D(_PlasmaTex,sphereUV*.55+float2(-t*_SurfaceSpeed*.6,t*_SurfaceSpeed*.25)).r-.5;
                float a=tex2D(_PlasmaTex,sphereUV*.9+float2(t*_SurfaceSpeed,t*_SurfaceSpeed*.37)+warp*.22).r;
                float b=tex2D(_PlasmaTex,sphereUV*1.35-float2(t*_SurfaceSpeed*.8,t*_SurfaceSpeed*.5)-warp*.3).r;
                float plasma=saturate((a*.6+b*.4-.5)*_SurfaceContrast+.5);
                float3 surface=plasma<.5 ? lerp(_SpotColor.rgb,_SurfaceColor.rgb,plasma*2) : lerp(_SurfaceColor.rgb,_CoreColor.rgb,plasma*2-1);
                surface*=lerp(.55,1.1,pow(z,.45));              // limb darkening
                surface+=_CoronaColor.rgb*pow(1-z,4)*.5;         // thin bright rim
                float disc=1-smoothstep(_DiscRadius-.006,_DiscRadius+.004,r);

                // Corona: two copies of the mask turning opposite ways, with wisps flickering outward.
                float c1=tex2D(_CoronaTex,Rotate(p,t*_CoronaSpeed)*.5+.5).r;
                float c2=tex2D(_CoronaTex,Rotate(p,-t*_CoronaSpeed*.7)*.5*1.02+.5).r;
                float2 dir=r>.0001 ? p/r : float2(0,1);
                float flicker=tex2D(_PlasmaTex,p*.7-dir*t*_CoronaSpeed*3).r;
                float corona=(c1*.6+c2*.4)*lerp(.55,1.45,flicker);
                float outside=smoothstep(_DiscRadius-.012,_DiscRadius+.006,r);
                float glow=exp(-max(r-_DiscRadius,0)*9)*_GlowStrength;
                float edgeFade=1-smoothstep(.62,.98,r);           // fade well inside the image so no circular edge shows
                float3 halo=_CoronaColor.rgb*(corona*_CoronaStrength+glow)*outside*edgeFade;

                float3 rgb=surface*disc+halo;
                // Only the disc occludes; corona and glow add light, so stars stay visible through them.
                float alpha=disc;
                half4 color=half4(rgb*i.color.rgb,alpha)*i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                color*=UnityGet2DClipping(i.localPosition,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
