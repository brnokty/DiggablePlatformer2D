Shader "Hidden/DiggableTerrains2D/Fill"
{
	SubShader
	{
		//Tags {"Queue"="Transparent-1" "DisableBatching"="True"}
		Tags {"Queue"="Transparent"}
		ZWrite Off
		Pass
		{
			CGPROGRAM

			#include "UnityCG.cginc"


			#pragma multi_compile_local _LAYERS_N1 _LAYERS_N2 _LAYERS_N3 _LAYERS_N4

			#pragma vertex vert
			#pragma fragment frag
            
			matrix _Matrix;

			#if _LAYERS_N1
				sampler2D _Tex0;
				float4 _Tex0_ST;
				float4 _Col0;
			#elif _LAYERS_N2
				sampler2D _TexS;
				sampler2D _Tex0;
				sampler2D _Tex1;
				float4 _TexS_ST;
				float4 _Tex0_ST;
				float4 _Tex1_ST;
				float4 _Col0;
				float4 _Col1;
			#elif _LAYERS_N3
				sampler2D _TexS;
				sampler2D _Tex0;
				sampler2D _Tex1;
				sampler2D _Tex2;
				float4 _TexS_ST;
				float4 _Tex0_ST;
				float4 _Tex1_ST;
				float4 _Tex2_ST;
				float4 _Col0;
				float4 _Col1;
				float4 _Col2;
			#elif _LAYERS_N4
				sampler2D _TexS;
				sampler2D _Tex0;
				sampler2D _Tex1;
				sampler2D _Tex2;
				sampler2D _Tex3;
				float4 _TexS_ST;
				float4 _Tex0_ST;
				float4 _Tex1_ST;
				float4 _Tex2_ST;
				float4 _Tex3_ST;
				float4 _Col0;
				float4 _Col1;
				float4 _Col2;
				float4 _Col3;
			#endif

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;

				#if _LAYERS_N1
					float2 uv0 : TEXCOORD0;
				#elif _LAYERS_N2
					float2 uv0 : TEXCOORD0;
					float2 uv1 : TEXCOORD1;
					float2 uvS : TEXCOORD4;
				#elif _LAYERS_N3
					float2 uv0 : TEXCOORD0;
					float2 uv1 : TEXCOORD1;
					float2 uv2 : TEXCOORD2;
					float2 uvS : TEXCOORD4;
				#elif _LAYERS_N4
					float2 uv0 : TEXCOORD0;
					float2 uv1 : TEXCOORD1;
					float2 uv2 : TEXCOORD2;
					float2 uv3 : TEXCOORD3;
					float2 uvS : TEXCOORD4;
				#endif
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);				

				float2 uv = mul(_Matrix, v.vertex).xy;
	
				#if _LAYERS_N1
					o.uv0 = TRANSFORM_TEX(uv, _Tex0);
				#elif _LAYERS_N2
					o.uv0 = TRANSFORM_TEX(uv, _Tex0);
					o.uv1 = TRANSFORM_TEX(uv, _Tex1);
					o.uvS = TRANSFORM_TEX(uv, _TexS);
				#elif _LAYERS_N3
					o.uv0 = TRANSFORM_TEX(uv, _Tex0);
					o.uv1 = TRANSFORM_TEX(uv, _Tex1);
					o.uv2 = TRANSFORM_TEX(uv, _Tex2);
					o.uvS = TRANSFORM_TEX(uv, _TexS);
				#elif _LAYERS_N4
					o.uv0 = TRANSFORM_TEX(uv, _Tex0);
					o.uv1 = TRANSFORM_TEX(uv, _Tex1);
					o.uv2 = TRANSFORM_TEX(uv, _Tex2);
					o.uv3 = TRANSFORM_TEX(uv, _Tex3);
					o.uvS = TRANSFORM_TEX(uv, _TexS);
				#endif

				return o;
			}


			fixed4 frag(v2f i) : SV_TARGET
			{
				#if _LAYERS_N1
					return tex2D(_Tex0, i.uv0) * _Col0;
				#elif _LAYERS_N2
					float4 c0 = tex2D(_Tex0, i.uv0) * _Col0;
					float4 c1 = tex2D(_Tex1, i.uv1) * _Col1;
					float4 s = tex2D(_TexS, i.uvS);
					return c0 * s.x + c1 * s.y;
				#elif _LAYERS_N3
					float4 c0 = tex2D(_Tex0, i.uv0) * _Col0;
					float4 c1 = tex2D(_Tex1, i.uv1) * _Col1;
					float4 c2 = tex2D(_Tex2, i.uv2) * _Col2;
					float4 s = tex2D(_TexS, i.uvS);
					return c0 * s.x + c1 * s.y + c2 * s.z;
				#elif _LAYERS_N4
					float4 c0 = tex2D(_Tex0, i.uv0) * _Col0;
					float4 c1 = tex2D(_Tex1, i.uv1) * _Col1;
					float4 c2 = tex2D(_Tex2, i.uv2) * _Col2;
					float4 c3 = tex2D(_Tex3, i.uv3) * _Col3;
					float4 s = tex2D(_TexS, i.uvS);
					return c0 * s.x + c1 * s.y + c2 * s.z + c3 * s.w;
				#endif
			}
			ENDCG
		}
	}
}