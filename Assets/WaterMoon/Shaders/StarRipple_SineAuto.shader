Shader "StarFX/StarRipple_SineAuto"
{
    Properties
    {
        _Color ("Ripple Color", Color) = (1,1,1,1)
        _Alpha ("Overall Alpha", Range(0,1)) = 1.0
        _Frequency ("Wave Frequency", Range(1,100)) = 20.0
        _WaveSpeed ("Wave Speed", Range(0,100)) = 2.0
        _InvertDirection ("Invert Direction (0=Out,1=In)", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 _Color;
            float _Alpha;
            float _Frequency;
            float _WaveSpeed;
            float _InvertDirection; // 0 = 由內而外, 1 = 由外而內

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);

                // 時間推進相位
                float phase = _Time.y * _WaveSpeed;

                // 根據方向切換正負
                float dir = (_InvertDirection > 0.5) ? 1.0 : -1.0;

                // 將 sin 映射到 0~1
                float wave = 0.5 + 0.5 * sin(dist * _Frequency + dir * phase);

                // 直接以波控制透明度
                fixed4 col = _Color;
                col.a = wave * _Alpha;

                return col;
            }
            ENDCG
        }
    }
}
