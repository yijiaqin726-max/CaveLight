Shader "UI/DarknessMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0, 0, 0, 1)
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 0.25
        _Softness ("Softness", Float) = 0.08
        _Opacity ("Opacity", Float) = 1.0
        _Aspect ("Aspect", Float) = 1.777
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Softness;
            float _Opacity;
            float _Aspect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 delta = i.uv - _Center.xy;
                delta.x *= _Aspect;

                float distanceFromCenter = length(delta);
                float edge = max(_Softness, 0.0001);
                float alpha = smoothstep(_Radius, _Radius + edge, distanceFromCenter) * saturate(_Opacity);

                return fixed4(0, 0, 0, alpha);
            }
            ENDCG
        }
    }
}
