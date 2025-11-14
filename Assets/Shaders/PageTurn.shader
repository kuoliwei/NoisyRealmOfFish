Shader "Custom/PageTurn"
{
    Properties
    {
        _FrontTex ("Front Texture", 2D) = "white" {}
        _BackTex  ("Back Texture", 2D) = "white" {}

        // 翻頁進度 0 到 1
        _Turn ("Turn Progress", Range(0,1)) = 0

        // 彎曲半徑
        _Radius ("Bend Radius", Float) = 0.3

        // 背面變暗程度
        _BackDarkness ("Backside Darkening", Range(0,1)) = 0.4

        // 背面的透光效果
        _Translucency ("Translucency", Range(0,1)) = 0.2
    }

    SubShader
    {
        // 使用透明隊列，才能顯示 PNG 透明
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        // 翻頁需要雙面顯示
        Cull Off

        // 透明混合模式（最標準的）
        Blend SrcAlpha OneMinusSrcAlpha

        // 半透明不能寫 Z，避免排序錯誤
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _FrontTex;
            sampler2D _BackTex;

            float _Turn;
            float _Radius;
            float _BackDarkness;
            float _Translucency;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float facing : TEXCOORD3;
            };

            // 頂點著色器，負責彎曲紙張
            v2f vert (appdata v)
            {
                v2f o;

                float3 p = v.vertex.xyz;

                // 將 x 從 -0.5..0.5 轉成 0..1
                float x01 = p.x + 0.5;

                // 翻頁角度 0..PI
                float theta = _Turn * 3.14159;
                float localAngle = theta * x01;

                float s = sin(localAngle);
                float c = cos(localAngle);
                float r = _Radius;

                // 彎曲後位置
                float3 bentPos;
                bentPos.x = p.x + r * s;
                bentPos.y = p.y;
                bentPos.z = r * (1.0 - c);

                // 彎曲後法線
                float3 n = v.normal;
                float3 bentN;
                bentN.x = n.x * c + n.z * s;
                bentN.z = -n.x * s + n.z * c;
                bentN.y = n.y;

                o.pos = UnityObjectToClipPos(float4(bentPos,1));
                float4 wp = mul(unity_ObjectToWorld, float4(bentPos,1));
                o.worldPos = wp.xyz;

                o.normal = UnityObjectToWorldNormal(bentN);

                float3 viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                o.facing = dot(o.normal, viewDir);

                o.uv = v.uv;
                return o;
            }

            // 使用 PNG 的透明度
            fixed4 frag (v2f i) : SV_Target
            {
                float isFront = step(0.0, i.facing);
                float isBack  = 1.0 - isFront;

                fixed4 frontCol = tex2D(_FrontTex, i.uv);
                fixed4 backCol  = tex2D(_BackTex, float2(1.0 - i.uv.x, i.uv.y));

                // 顏色混合
                float3 baseColor =
                    frontCol.rgb * isFront +
                    backCol.rgb  * isBack;

                // 背面暗角
                baseColor *= (1.0 - isBack * _BackDarkness);

                // 透光
                baseColor += backCol.rgb * isBack * _Translucency;

                // 使用圖片本身的透明度（最重要）
                float alpha =
                    frontCol.a * isFront +
                    backCol.a  * isBack;

                // 避免整張透明
                alpha = max(alpha, 0.01);

                return fixed4(baseColor, alpha);
            }

            ENDCG
        }
    }

    FallBack Off
}
