Shader "Custom/FishChromaKey"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _KeyColor ("Key Color", Color) = (0,0,0,1)
        _Threshold ("Threshold", Range(0,1)) = 0.2
        _Smooth ("Smoothness", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
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
            float4 _KeyColor;
            float _Threshold;
            float _Smooth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // 計算與 KeyColor 的差異
                float3 diff = abs(col.rgb - _KeyColor.rgb);
                float delta = dot(diff, float3(0.333, 0.333, 0.333));

                // 將接近 KeyColor 的地方設為透明
                float alpha = smoothstep(_Threshold, _Threshold + _Smooth, delta);
                col.a = alpha;

                return col;
            }
            ENDCG
        }
    }
}
