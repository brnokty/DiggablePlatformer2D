Shader "Hidden/DiggableTerrains2D/SplatMapCircleBrush"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
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
                float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Circle_PS;
			float _BrushSoftness = 1;
			float4 _BrushColor;
			float _BrushOpacity;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float2 uv = i.uv;
			    fixed4 col = tex2D(_MainTex, uv);
				float dx = (_Circle_PS.x - uv.x) / _Circle_PS.z;
				float dy = (_Circle_PS.y - uv.y) / _Circle_PS.w;
				float dis = sqrt(dx * dx  + dy * dy);
				float a = 1 - dis;
				a = clamp(a,0,1);
				a = smoothstep(0, _BrushSoftness, a);
				col = lerp(col, _BrushColor, a * _BrushOpacity);
				return col;
            }
            ENDCG
        }
    }
}