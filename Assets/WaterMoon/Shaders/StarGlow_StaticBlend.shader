Shader "Custom/StarGlow_StaticBlend"
{
    Properties
    {
        _MainTex ("Glow Texture", 2D) = "white" {}
        _CrossTex ("Cross Flare Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _CrossColor ("Cross Color", Color) = (1,1,1,1)
        _CrossIntensity ("Cross Intensity", Range(0,2)) = 0
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
            float _CrossIntensity;

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
                // 圓形光暈
                fixed4 glow = tex2D(_MainTex, i.uv) * _BaseColor;

                // 十字閃光，強度由外部 C# 控制
                fixed4 cross = tex2D(_CrossTex, i.uv) * _CrossColor * _CrossIntensity;

                // 混合
                fixed4 col = glow + cross;
                col.a = saturate(glow.a + cross.a * _CrossIntensity);

                return col;
            }
            ENDCG
        }
    }
}
