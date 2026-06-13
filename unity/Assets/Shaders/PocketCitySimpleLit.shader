Shader "PocketCity/SimpleLit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                fixed3 lighting : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;

                // 简单漫反射光照
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0, dot(worldNormal, lightDir));

                // 环境光 + 漫反射
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb;
                fixed3 diffuse = _LightColor0.rgb * NdotL;
                o.lighting = ambient + diffuse;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                fixed4 finalColor = texColor * i.color * _Color;
                finalColor.rgb *= i.lighting;
                return finalColor;
            }
            ENDCG
        }
    }

    Fallback "VertexLit"
}
