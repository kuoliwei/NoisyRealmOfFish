Shader "StarFX/StarRipple_SineAlpha"
{
    Properties
    {
        _Color ("Ripple Color", Color) = (1,1,1,1)
        _Alpha ("Overall Alpha", Range(0,1)) = 1.0
        _Frequency ("Wave Frequency", Range(1,100)) = 20.0
        _Phase ("Wave Phase", Range(0,6.28)) = 0.0
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
            float _Phase;

            fixed4 frag(v2f i) : SV_Target
            {
                // 中心距離（0 ~ 約 0.707）
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);

                // 將 sin() 從 [-1,1] 映射到 [0,1]
                float wave = 0.5 + 0.5 * sin(dist * _Frequency - _Phase);

                // wave 現在是亮度/透明度值
                fixed4 col = _Color;
                col.a = wave * _Alpha;

                return col;
            }
            ENDCG
        }
    }
}
