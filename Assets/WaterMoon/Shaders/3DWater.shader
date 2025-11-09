Shader "WaterMoon/SimpleWater"
{
    Properties
    {
        _ReflectionTex ("Reflection Texture", 2D) = "white" {}
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1
        _WaveStrength ("Wave Strength", Range(0, 1.0)) = 0.02
        _WaveFrequencyX ("Wave Frequency X", Range(1, 100)) = 15
        _WaveFrequencyY ("Wave Frequency Y", Range(1, 100)) = 20
        _WaterTint ("Water Tint", Color) = (0.7, 0.8, 1, 1)
        _ReflectionIntensity ("Reflection Intensity", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _ReflectionTex;
            float _WaveSpeed;
            float _WaveStrength;
            float _WaveFrequencyX;
            float _WaveFrequencyY;
            fixed4 _WaterTint;
            float _ReflectionIntensity;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 加入可調波頻率
                float waveX = sin(i.uv.y * _WaveFrequencyY + _Time.y * _WaveSpeed);
                float waveY = cos(i.uv.x * _WaveFrequencyX + _Time.y * (_WaveSpeed * 1.3));

                i.uv.x += waveX * _WaveStrength;
                i.uv.y += waveY * _WaveStrength * 0.5;

                float2 reflectUV = float2(i.uv.x, 1.0 - i.uv.y);
                fixed4 reflection = tex2D(_ReflectionTex, reflectUV);

                reflection.rgb *= _WaterTint.rgb;
                reflection.rgb *= _ReflectionIntensity;
                reflection.a = 0.9;

                return reflection;
            }
            ENDCG
        }
    }
}
