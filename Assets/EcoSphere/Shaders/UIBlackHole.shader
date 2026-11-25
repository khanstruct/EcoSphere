Shader "EcoSphere/UI/Black Hole"
{
    // Stylized black hole for a UI Image: a tilted accretion disk built from the face-on disk mask, streaming
    // turbulence along the orbit, Doppler brightening, the far side of the disk lensed into an arc over the top,
    // a thin photon ring, and an opaque event horizon the near side of the disk passes in front of.
    // Colors and speeds come from the sun type's SunAppearance.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (unused)",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        _DiskTex ("Disk mask (face-on, hole in the center)",2D)="black" {}
        _TurbulenceTex ("Turbulence mask (streaks along u, tiles in v)",2D)="gray" {}
        _DiskMaskInner ("Disk mask hole radius (0-1 of image)",Range(.05,.6))=.235
        _CoreColor ("Disk inner (hot)",Color)=(1,.96,.85,1)
        _SurfaceColor ("Disk middle",Color)=(1,.6,.2,1)
        _SpotColor ("Disk outer (cool)",Color)=(.45,.08,.02,1)
        _CoronaColor ("Photon ring and glow",Color)=(1,.85,.6,1)
        _HorizonRadius ("Event horizon radius",Range(.05,.5))=.2
        _DiskInner ("Disk inner radius",Range(.1,.9))=.3
        _DiskOuter ("Disk outer radius",Range(.3,1))=.92
        _Tilt ("Disk tilt (vertical squash)",Range(.1,1))=.19
        _SurfaceSpeed ("Disk rotation speed",Float)=.025
        _SurfaceContrast ("Disk contrast",Float)=1.6
        _CoronaSpeed ("Turbulence flow speed",Float)=.032
        _CoronaStrength ("Lensed arc and ring strength",Float)=1.2
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
            sampler2D _DiskTex,_TurbulenceTex;
            float4 _Color,_ClipRect,_CoreColor,_SurfaceColor,_SpotColor,_CoronaColor;
            float _DiskMaskInner,_HorizonRadius,_DiskInner,_DiskOuter,_Tilt,_SurfaceSpeed,_SurfaceContrast,_CoronaSpeed,_CoronaStrength,_GlowStrength;
            static const float TAU=6.2831853;

            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;
                o.uv=v.uv;o.localPosition=v.vertex.xy;return o;
            }
            float2 Rotate(float2 p,float a){float s=sin(a),c=cos(a);return float2(c*p.x-s*p.y,s*p.x+c*p.y);}

            // The face-on disk mask at a disk-plane position. Two rigid copies turn at inner and outer speeds and are
            // blended by radius: the inner disk visibly spins faster, without the spiral winding up over time.
            float DiskMask(float2 d,float t)
            {
                float scale=_DiskMaskInner/_DiskInner;          // align the mask's hole with the disk's inner edge
                float rd=length(d);
                float inner=tex2D(_DiskTex,Rotate(d,-t*_SurfaceSpeed*9)*scale*.5+.5).r;
                float outer=tex2D(_DiskTex,Rotate(d,-t*_SurfaceSpeed*3.5)*scale*.5+.5).r;
                return lerp(inner,outer,smoothstep(_DiskInner*1.2,_DiskOuter*.7,rd));
            }
            // Turbulence streaks flowing along the orbit. Polar u wraps once per repeat; the mask's u edges don't quite
            // match, so two samples half a repeat apart are blended to hide the seam. Explicit LOD avoids a mip seam.
            float Flow(float rd,float angle,float t)
            {
                float u=(angle/TAU+t*_CoronaSpeed*.9/pow(max(rd,.15),1.5))*3;
                float v=rd*1.4-t*_CoronaSpeed*.15;
                float a=tex2Dlod(_TurbulenceTex,float4(frac(u),v,0,1.2)).r;
                float b=tex2Dlod(_TurbulenceTex,float4(frac(u+.5),v+.31,0,1.2)).r;
                return lerp(a,b,abs(frac(u)*2-1));
            }
            float3 Heat(float rd)
            {
                float heat=saturate(1-(rd-_DiskInner)/(_DiskOuter-_DiskInner));
                return heat<.5 ? lerp(_SpotColor.rgb,_SurfaceColor.rgb,heat*2) : lerp(_SurfaceColor.rgb,_CoreColor.rgb,heat*2-1);
            }
            float Disk(float2 d,float t)
            {
                float rd=length(d);
                float shape=DiskMask(d,t);
                float flow=saturate((Flow(rd,atan2(d.y,d.x),t)-.5)*_SurfaceContrast+.5);
                float edges=smoothstep(_DiskInner*.92,_DiskInner*1.02,rd)*(1-smoothstep(_DiskOuter*.8,_DiskOuter,rd));
                // The mask gives the disk's structure and falloff; the flowing streaks modulate it.
                return shape*(.45+.9*flow)*edges*1.5;
            }

            half4 frag(v2f i):SV_Target
            {
                float t=_Time.y;
                float2 p=i.uv*2-1;
                float r=length(p);
                float h=_HorizonRadius;

                // Accretion disk seen from slightly above: unsquash to disk-plane coordinates.
                float2 d=float2(p.x,p.y/_Tilt);
                float rd=length(d);
                float beam=1-.45*(rd>.0001 ? d.x/rd : 0);          // approaching (left) side brighter
                float3 diskRgb=Heat(rd)*Disk(d,t)*beam;
                bool farSide=d.y>0;                                // far half sits behind the horizon

                // Lensing: the far side of the disk wraps into an arc hugging the horizon, strongest overhead.
                // It samples the same disk mask along the screen direction, so the arc's swirls match the disk.
                float lensBand=smoothstep(h*1.03,h*1.15,r)*(1-smoothstep(h*1.3,h*2.1,r));
                float lensRadius=lerp(_DiskInner*1.02,_DiskOuter*.7,saturate((r-h*1.1)/(h*1.1)));
                float2 dir=r>.0001 ? p/r : float2(0,1);
                float overhead=.25+.75*pow(saturate(dir.y*.5+.5),1.5);
                float lens=lensBand*overhead*Disk(dir*lensRadius,t+11.7)*_CoronaStrength;
                float3 lensRgb=Heat(lensRadius)*lens;

                // Thin photon ring right at the horizon, and a soft glow around everything.
                float ring=exp(-pow((r-h*1.04)/(h*.05),2))*_CoronaStrength*.9;
                float glow=exp(-max(r-h,0)*9)*_GlowStrength*.35;
                float3 halo=_CoronaColor.rgb*(ring+glow);

                float edgeFade=1-smoothstep(.8,.99,r);              // radial, so no square edge shows
                float hole=1-smoothstep(h-.004,h+.004,r);
                float3 behind=(lensRgb+halo+(farSide ? diskRgb : 0))*(1-hole);
                float3 front=farSide ? 0 : diskRgb;
                float3 rgb=(behind+front)*edgeFade;
                // Only the horizon occludes the starfield; everything else adds light.
                half4 color=half4(rgb*i.color.rgb,hole)*i.color.a;
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
