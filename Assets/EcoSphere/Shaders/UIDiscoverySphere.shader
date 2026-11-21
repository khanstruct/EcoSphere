Shader "EcoSphere/UI/Discovery Sphere"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Texture",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        _StencilComp ("Stencil Comparison",Float)=8
        _Stencil ("Stencil ID",Float)=0
        _StencilOp ("Stencil Operation",Float)=0
        _StencilWriteMask ("Stencil Write Mask",Float)=255
        _StencilReadMask ("Stencil Read Mask",Float)=255
        _ColorMask ("Color Mask",Float)=15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip",Float)=0
        _Whiten ("White Charge",Range(0,1))=0
        _Glow ("Glow",Range(0,4))=0
        _OrbFallback ("Procedural Element Orb",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
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
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 mask:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            float4 _Color,_ClipRect;
            float _UIMaskSoftnessX,_UIMaskSoftnessY;
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color*_Color; o.uv=v.uv;
                float2 pixelSize=o.vertex.w/abs(mul((float2x2)UNITY_MATRIX_P,_ScreenParams.xy));
                float4 clipRect=clamp(_ClipRect,-2e10,2e10);
                o.mask=float4(v.vertex.xy*2-clipRect.xy-clipRect.zw,.25/(.25*float2(_UIMaskSoftnessX,_UIMaskSoftnessY)+abs(pixelSize)));
                return o;
            }
            float ClipAlpha(float alpha,v2f i)
            {
                #ifdef UNITY_UI_CLIP_RECT
                float2 m=saturate((_ClipRect.zw-_ClipRect.xy-abs(i.mask.xy))*i.mask.zw); alpha*=m.x*m.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha-.001);
                #endif
                return alpha;
            }
            sampler2D _MainTex;
            float _Whiten,_Glow,_OrbFallback;
            float4 frag(v2f i):SV_Target
            {
                float4 sample=tex2D(_MainTex,i.uv);
                float neighbor=0;
                float2 offsets[8]={float2(1,0),float2(-1,0),float2(0,1),float2(0,-1),float2(.7,.7),float2(-.7,.7),float2(.7,-.7),float2(-.7,-.7)};
                for(int n=0;n<8;n++)neighbor+=tex2D(_MainTex,i.uv+offsets[n]*.024).a*.125;
                if(_OrbFallback>.5)
                {
                    float r=length((i.uv-.5)*2);
                    sample=float4(i.color.rgb*(.45+.55*sqrt(saturate(1-r*r))),1-smoothstep(.81,.84,r));
                    neighbor=1-smoothstep(.82,.9,r);
                }
                else sample.rgb*=i.color.rgb;
                float halo=max(0,neighbor-sample.a)*_Glow*.65;
                float alpha=saturate(sample.a+halo)*i.color.a;
                float3 rgb=lerp(sample.rgb,1,_Whiten)+halo;
                return float4(rgb,ClipAlpha(alpha,i));
            }
            ENDCG
        }
    }
}
