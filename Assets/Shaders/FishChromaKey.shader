Shader "Custom/FishChromaKey"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _BlackThreshold("Black Cutoff Threshold", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _BlackThreshold;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // 計算亮度（加權平均法）
                float brightness = dot(col.rgb, float3(0.299, 0.587, 0.114));

                // 若亮度低於閾值，將 alpha 設為 0
                if (brightness < _BlackThreshold)
                {
                    col.a = 0;
                }

                return col;
            }
            ENDCG
        }
    }
}
