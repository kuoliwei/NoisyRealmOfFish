Shader "Custom/StarGlow_WithCross_SeparateIntensity"
{
    Properties
    {
        _MainTex ("Glow Texture", 2D) = "white" {}
        _CrossTex ("Cross Flare Texture", 2D) = "white" {}

        _BaseColor ("Glow Color", Color) = (1,1,1,1)
        _CrossColor ("Cross Color", Color) = (1,1,1,1)

        _GlowIntensity ("Glow Intensity", Range(0,5)) = 1
        _CrossIntensity ("Cross Intensity", Range(0,5)) = 1

        _GlowScaleX ("Glow Scale X", Range(0.1, 3)) = 1
        _GlowScaleY ("Glow Scale Y", Range(0.1, 3)) = 1

        _CrossScaleX ("Cross Scale X", Range(0.1, 3)) = 1
        _CrossScaleY ("Cross Scale Y", Range(0.1, 3)) = 1
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

            sampler2D _MainTex;
            sampler2D _CrossTex;

            float4 _BaseColor;
            float4 _CrossColor;

            float _GlowIntensity;
            float _CrossIntensity;

            float _GlowScaleX;
            float _GlowScaleY;
            float _CrossScaleX;
            float _CrossScaleY;

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
                // --- Glow ¼h ---
                float2 glowUV = (i.uv - 0.5) / float2(_GlowScaleX, _GlowScaleY) + 0.5;
                fixed4 glow = tex2D(_MainTex, glowUV) * _BaseColor * _GlowIntensity;

                // --- Cross ¼h ---
                float2 crossUV = (i.uv - 0.5) / float2(_CrossScaleX, _CrossScaleY) + 0.5;
                fixed4 cross = tex2D(_CrossTex, crossUV) * _CrossColor * _CrossIntensity;

                // --- ²V¦X ---
                fixed4 col = glow + cross;
                col.a = saturate(glow.a + cross.a);

                return col;
            }
            ENDCG
        }
    }
}
