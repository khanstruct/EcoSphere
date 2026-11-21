Shader "EcoSphere/UI/Energy Bar"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
        _EnergyTex ("Grayscale Energy", 2D) = "gray" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ScrollSpeed ("Upward Flow Speed", Range(0,1)) = .12
        _TextureScale ("Texture Tiling", Vector) = (1,2,0,0)
        _Intensity ("Surface Brightness", Range(0,3)) = 1.25
        _Contrast ("Texture Contrast", Range(.1,4)) = 1.6
        _EdgeLight ("Edge Light", Range(0,3)) = 1.2
        _Glow ("Glow Strength", Range(0,2)) = .65
        _FlareSpeed ("Flare Cycles Per Second", Range(.02,1)) = .22
        _FlareBrightness ("Flare Brightness", Range(0,4)) = 2
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 animation : TEXCOORD1; // width, height, gameplay time, phase
                float4 state : TEXCOORD2; // leader, self-combination
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 animation : TEXCOORD1;
                float4 state : TEXCOORD2;
                float4 mask : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _EnergyTex;
            float4 _Color, _TextureScale, _ClipRect;
            float _ScrollSpeed, _Intensity, _Contrast, _EdgeLight, _Glow, _FlareSpeed, _FlareBrightness;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.color=v.color*_Color; o.uv=v.uv; o.animation=v.animation; o.state=v.state;
                float2 pixelSize=o.vertex.w / abs(mul((float2x2)UNITY_MATRIX_P,_ScreenParams.xy));
                float4 clipRect=clamp(_ClipRect,-2e10,2e10);
                o.mask=float4(v.vertex.xy*2-clipRect.xy-clipRect.zw,
                    .25/(.25*float2(_UIMaskSoftnessX,_UIMaskSoftnessY)+abs(pixelSize)));
                return o;
            }
            float trail(float position,float head,float length)
            {
                float distance=frac(head-position+1);
                return pow(saturate(1-distance/length),2);
            }
            float4 frag(v2f i) : SV_Target
            {
                float2 size=max(i.animation.xy,1);
                float2 p=(i.uv-.5)*size;
                float2 q=abs(p)-size*.5;
                float outside=length(max(q,0));
                float distance=outside+min(max(q.x,q.y),0);
                float aa=max(fwidth(distance),.2);
                float fill=1-smoothstep(-aa,aa,distance);
                float time=i.animation.z, phase=i.animation.w;
                float2 uv=i.uv*_TextureScale.xy;
                uv.y-=time*_ScrollSpeed+phase;
                float energy=tex2D(_EnergyTex,uv).r;
                float detail=tex2D(_EnergyTex,uv*float2(1.7,2.3)+float2(.23,time*.035)).r;
                energy=saturate((energy*.8+detail*.2-.5)*_Contrast+.5);
                float edge=exp(-abs(distance)/1.1);
                float shine=pow(saturate(energy-.3),2)*1.1;
                float3 tint=i.color.rgb;
                float3 surface=tint*(.23+energy*_Intensity)+lerp(tint,1,.45)*shine;
                surface+=lerp(tint,1,.65)*edge*_EdgeLight;

                // Parameterize the nearest point on the actual filled rectangle's outline.
                float2 clamped=clamp(p,-size*.5,size*.5);
                float2 toEdge=size*.5-abs(clamped);
                float along;
                if (toEdge.x<toEdge.y)
                    along=clamped.x>0 ? size.x+(size.y*.5-clamped.y) : 2*size.x+size.y+(clamped.y+size.y*.5);
                else
                    along=clamped.y>0 ? clamped.x+size.x*.5 : size.x+size.y+(size.x*.5-clamped.x);
                float position=along/(2*(size.x+size.y));
                float self=saturate(i.state.y), leader=saturate(i.state.x);
                float head=frac(time*_FlareSpeed+phase);
                float flare=max(trail(position,head,lerp(.23,.36,self)),self*trail(position,frac(head+.5),.36))*leader;
                float3 flareTint=lerp(tint,float3(1,.64,.12),self);
                float halo=exp(-outside/3)*_Glow;
                float flareHalo=exp(-abs(distance)/lerp(2.5,4,self))*flare*_FlareBrightness;
                float3 rgb=lerp(tint*.7,surface,fill)+lerp(flareTint,1,.45)*flareHalo;
                float alpha=max(fill,halo*.28)+flareHalo*.45;
                alpha=saturate(alpha)*i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                float2 mask=saturate((_ClipRect.zw-_ClipRect.xy-abs(i.mask.xy))*i.mask.zw);
                alpha*=mask.x*mask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha-.001);
                #endif
                return float4(rgb,alpha);
            }
            ENDCG
        }
    }
}
