Shader "Hidden/DiggableTerrains2D/SplatMapTextureBrush"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BrushTex ("Brush Texture", 2D) = "white" {}
		_BrushColor ("Brush Color", COLOR) = (1, 1, 1, 1)
		_BrushOpacity ("Brush Color", float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
			sampler2D _BrushTex;
			float4 _MainTex_ST;
			float4 _BrushTex_ST;
			float4 _BrushColor;
			float _BrushOpacity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv0 = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv1 = TRANSFORM_TEX(v.uv, _BrushTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
			    fixed4 a = tex2D(_MainTex, i.uv0);
				fixed4 b = tex2D(_BrushTex, i.uv1);
				a = lerp(a, _BrushColor, b.a * _BrushOpacity);
				return a;
            }
            ENDCG
        }
    }
}