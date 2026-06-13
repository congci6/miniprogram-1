Shader "PocketCity/AnimatedWater"
{
    Properties
    {
        _WaterColor ("Water Color", Color) = (0.4, 0.87, 0.96, 0.8)
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveScale ("Wave Scale", Float) = 0.1
        _WaveFrequency ("Wave Frequency", Float) = 2.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _WaterColor;
            float _WaveSpeed;
            float _WaveScale;
            float _WaveFrequency;

            v2f vert (appdata v)
            {
                v2f o;

                // 顶点波动
                float wave = sin(v.vertex.x * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveScale;
                wave += sin(v.vertex.z * _WaveFrequency * 0.7 + _Time.y * _WaveSpeed * 1.3) * _WaveScale * 0.5;
                v.vertex.y += wave;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // UV波动
                float2 uv = i.uv;
                uv.x += sin(uv.y * _WaveFrequency + _Time.y * _WaveSpeed) * 0.02;
                uv.y += cos(uv.x * _WaveFrequency + _Time.y * _WaveSpeed * 0.8) * 0.02;

                // 深浅变化
                float depth = sin(uv.x * 10 + _Time.y) * 0.1 + 0.9;

                fixed4 color = _WaterColor;
                color.rgb *= depth;
                return color;
            }
            ENDCG
        }
    }
}
