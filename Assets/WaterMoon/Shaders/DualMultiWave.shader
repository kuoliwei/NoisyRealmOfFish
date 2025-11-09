Shader "WaterMoon/DualMultiWave_GaussianOffset_Alpha"
{
    Properties
    {
        _ReflectionTex ("Reflection Texture", 2D) = "white" {}
        _WaterTint ("Water Tint", Color) = (0.7, 0.8, 1, 1)
        _ReflectionIntensity ("Reflection Intensity", Range(0, 1)) = 0.6
        _Alpha ("Alpha", Range(0,1)) = 1     // 新增透明度控制
        _WaveCountX ("Wave Count X", Range(1, 8)) = 3
        _WaveCountY ("Wave Count Y", Range(1, 8)) = 3
        _WaveAnimate ("Animate Waves", Float) = 1

        _CenterBrightnessMultiplier ("Center Brightness Multiplier", Range(1,10)) = 1
        _BrightnessRange ("Brightness Range", Range(0.05,0.5)) = 0.3
        _BrightnessOffset ("Brightness Center Offset", Range(-0.5,0.5)) = 0
        _BrightnessFalloff ("Brightness Falloff Ratio", Range(0.05,0.9)) = 0.5
    }

    SubShader
    {
        // 若是底層可用 Transparent
        // 若要當上層：改成 Transparent+1 或 Transparent+10
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _ReflectionTex;
            float _ReflectionIntensity;
            fixed4 _WaterTint;
            float _Alpha;

            int _WaveCountX;
            int _WaveCountY;
            float _WaveAnimate;

            float _WaveStrengthsX[8];
            float _WaveFrequenciesX[8];
            float _WaveSpeedsX[8];

            float _WaveStrengthsY[8];
            float _WaveFrequenciesY[8];
            float _WaveSpeedsY[8];

            float _CenterBrightnessMultiplier;
            float _BrightnessRange;
            float _BrightnessOffset;
            float _BrightnessFalloff;

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

            fixed4 frag(v2f i) : SV_Target
            {
                float waveX = 0;
                float waveY = 0;
                float t = (_WaveAnimate > 0.5) ? _Time.y : 0.0;

                for (int j = 0; j < _WaveCountY; j++)
                    waveX += sin(i.uv.y * _WaveFrequenciesY[j] + t * _WaveSpeedsY[j]) * _WaveStrengthsY[j];

                for (int k = 0; k < _WaveCountX; k++)
                    waveY += cos(i.uv.x * _WaveFrequenciesX[k] + t * _WaveSpeedsX[k]) * _WaveStrengthsX[k];

                float2 reflectUV = float2(i.uv.x + waveX, 1.0 - (i.uv.y + waveY));
                fixed4 reflection = tex2D(_ReflectionTex, reflectUV);

                fixed4 color = reflection * _WaterTint * _ReflectionIntensity;

                float center = 0.5 + _BrightnessOffset;
                float dx = i.uv.x - center;
                float sigma = _BrightnessRange / sqrt(2.0 * log(1.0 / _BrightnessFalloff));
                float gauss = exp(-0.5 * (dx * dx) / (sigma * sigma));
                float brightnessFactor = gauss * _CenterBrightnessMultiplier;
                color.rgb *= brightnessFactor;

                // 透明度調整
                color.a = _Alpha;

                return color;
            }
            ENDCG
        }
    }
}
