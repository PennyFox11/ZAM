Shader "UI/Glitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // Set by the UIGlitch script.
        _Intensity ("Glitch Intensity", Range(0, 3)) = 0
        _ShiftAmount ("Shift Amount", Range(0, 1)) = 1
        _ColorShift ("Color Shift (0 or 1)", Float) = 1
        _GlitchTint ("Glitch Tint", Color) = (0.5, 0.5, 0, 1)
        _SliceHeight ("Slice Height (pixels)", Float) = 20
        _GlitchTime ("Glitch Time", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _Intensity;
            float _ShiftAmount;
            float _ColorShift;
            fixed4 _GlitchTint;
            float _SliceHeight;
            float _GlitchTime;

            float hash(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Slices are based on the canvas position, so the panel and the buttons glitch in line.
                float step24 = floor(_GlitchTime * 24.0);
                float slice = floor(IN.worldPosition.y / max(_SliceHeight, 1.0));
                float active = step(0.6, hash(slice + step24 * 13.7)) * step(0.001, _Intensity);

                // UV shifting: some horizontal slices slide sideways.
                float offset = (hash(slice * 3.1 + step24 * 7.3) - 0.5) * 0.2 * _ShiftAmount * _Intensity * active;

                // Colour shifting: red and blue separate from green.
                float split = 0.01 * _Intensity * _ColorShift * (0.5 + active);

                half4 cr = tex2D(_MainTex, IN.texcoord + float2(offset + split, 0)) + _TextureSampleAdd;
                half4 cg = tex2D(_MainTex, IN.texcoord + float2(offset, 0)) + _TextureSampleAdd;
                half4 cb = tex2D(_MainTex, IN.texcoord + float2(offset - split, 0)) + _TextureSampleAdd;

                half4 color;
                color.r = cr.r;
                color.g = cg.g;
                color.b = cb.b;
                color.a = max(cr.a, max(cg.a, cb.a));

                // Tint on the glitching slices, plus faint scanlines.
                float i = saturate(_Intensity);
                color.rgb = lerp(color.rgb, color.rgb * _GlitchTint.rgb * 2.0, i * 0.6 * active);
                float scan = 0.5 + 0.5 * sin(IN.worldPosition.y * 1.5 + step24);
                color.rgb *= 1.0 - 0.15 * i * scan;

                color *= IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
