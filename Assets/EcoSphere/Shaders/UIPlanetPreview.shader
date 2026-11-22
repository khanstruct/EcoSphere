Shader "EcoSphere/UI/Planet Preview"
{
    Properties
    {
        [PerRendererData] _MainTex ("Preview",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
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
        Blend SrcAlpha OneMinusSrcAlpha
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
            sampler2D _MainTex;float4 _Color,_ClipRect;
            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;
                o.uv=v.uv;o.localPosition=v.vertex.xy;return o;
            }
            half4 frag(v2f i):SV_Target
            {
                half4 sample=tex2D(_MainTex,i.uv);
                // Camera blending stores premultiplied RGB. Undo it before the standard UI blend,
                // otherwise low-opacity atmosphere is multiplied by alpha a second time.
                sample.rgb=sample.a>.0001 ? sample.rgb/sample.a : 0;
                sample*=i.color;
                #ifdef UNITY_UI_CLIP_RECT
                sample.a*=UnityGet2DClipping(i.localPosition,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(sample.a-.001);
                #endif
                return sample;
            }
            ENDCG
        }
    }
}
