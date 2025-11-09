Shader "StarFX/StarRipple_SingleWave"
{
    Properties
    {
        _Color ("Ripple Color", Color) = (1,1,1,1)
        _Alpha ("Overall Alpha", Range(0,1)) = 1.0
        _Radius ("Base Radius", Range(0,5)) = 0.0
        _Frequency ("Wave Frequency", Range(1,100)) = 20.0
        _Amplitude ("Wave Amplitude", Range(0,1)) = 0.05
        _Phase ("Wave Phase", Range(0,6.28)) = 0.0
        _Softness ("Edge Softness", Range(0.001,0.5)) = 0.1
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
            float _Radius;
            float _Frequency;
            float _Amplitude;
            float _Phase;
            float _Softness;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);

                // 產生波動半徑（含振幅與相位）
                float wave = sin(dist * _Frequency + _Phase) * _Amplitude;
                float effectiveRadius = _Radius + wave;

                // 距離差轉成透明度
                float ringDist = abs(dist - effectiveRadius);
                float alpha = smoothstep(_Softness, 0.0, ringDist);

                fixed4 col = _Color;
                col.a = alpha * _Alpha;

                return col;
            }
            ENDCG
        }
    }
}
