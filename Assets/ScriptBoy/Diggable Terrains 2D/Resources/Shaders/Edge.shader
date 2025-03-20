Shader "Hidden/DiggableTerrains2D/Edge"
{
	SubShader
	{
		Tags {"Queue"="Transparent"}

		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off
		ZTest Off

		Pass
		{
			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma multi_compile_local _LAYERS_N1 _LAYERS_N2 _LAYERS_N3 _LAYERS_N4
			#pragma multi_compile_local _UV_MAPPING_X _UV_MAPPING_Y _UV_MAPPING_XY
			#pragma vertex vert
			#pragma fragment frag
            
			matrix _Matrix;
			float _Height;

			#if _LAYERS_N1
				sampler2D _Tex0;
				float4 _Tex0_ST;
				float4 _Col0;
			#elif _LAYERS_N2
				sampler2D _Tex0;
				sampler2D _Tex1;
				sampler2D _TexS;
				float4 _Tex0_ST;
				float4 _Tex1_ST;
				float4 _TexS_ST;
				float4 _Col0;
				float4 _Col1;
			#elif _LAYERS_N3
				sampler2D _Tex0;
				sampler2D _Tex1;
				sampler2D _Tex2;
				sampler2D _TexS;
				float4 _Tex0_ST;
				float4 _Tex1_ST;
				float4 _Tex2_ST;
				float4 _TexS_ST;
				float4 _Col0;
				float4 _Col1;
				float4 _Col2;
			#elif _LAYERS_N4
				sampler2D _Tex0;
				sampler2D _Tex1;
				sampler2D _Tex2;
				sampler2D _Tex3;
				sampler2D _TexS;
				float4 _Tex0_ST;
				float4 _Tex1_ST;
				float4 _Tex2_ST;
				float4 _Tex3_ST;
				float4 _TexS_ST;
				float4 _Col0;
				float4 _Col1;
				float4 _Col2;
				float4 _Col3;
			#endif

			struct appdata
			{
				float4 vertex : POSITION;
				#if _UV_MAPPING_XY
					float3 normal : NORMAL;
				#endif
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;

				#if _UV_MAPPING_X || _UV_MAPPING_Y
					#if _LAYERS_N1
						float2 uv0 : TEXCOORD0;
					#elif _LAYERS_N2
						float2 uvS : TEXCOORD4;
						float2 uv0 : TEXCOORD0;
						float2 uv1 : TEXCOORD1;
					#elif _LAYERS_N3
						float2 uvS : TEXCOORD4;
						float2 uv0 : TEXCOORD0;
						float2 uv1 : TEXCOORD1;
						float2 uv2 : TEXCOORD2;
					#elif _LAYERS_N4
						float2 uvS : TEXCOORD4;
						float2 uv0 : TEXCOORD0;
						float2 uv1 : TEXCOORD1;
						float2 uv2 : TEXCOORD2;
						float2 uv3 : TEXCOORD3;
					#endif
				#elif _UV_MAPPING_XY
					float3 normal : NORMAL;

					#if _LAYERS_N1
						float3 uv0 : TEXCOORD0;
					#elif _LAYERS_N2
						float3 uv0 : TEXCOORD0;
						float3 uv1 : TEXCOORD1;
						float2 uvS : TEXCOORD4;
					#elif _LAYERS_N3
						float3 uv0 : TEXCOORD0;
						float3 uv1 : TEXCOORD1;
						float3 uv2 : TEXCOORD2;
						float2 uvS : TEXCOORD4;
					#elif _LAYERS_N4
						float3 uv0 : TEXCOORD0;
						float3 uv1 : TEXCOORD1;
						float3 uv2 : TEXCOORD2;
						float3 uv3 : TEXCOORD3;
						float2 uvS : TEXCOORD4;
					#endif
				#endif
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);				
				v.vertex.z /= 0.01f;

				#if _UV_MAPPING_X || _UV_MAPPING_Y	
					#if _UV_MAPPING_X
						float2 uv = v.vertex.xz;
						uv.x = mul(_Matrix, v.vertex).x;
					#elif _UV_MAPPING_Y
						float2 uv = v.vertex.yz;
						uv.x = mul(_Matrix, v.vertex).y;
					#endif



					#if _LAYERS_N1
						o.uv0 = TRANSFORM_TEX(uv, _Tex0);
					#elif _LAYERS_N2
						o.uv0 = TRANSFORM_TEX(uv, _Tex0);
						o.uv1 = TRANSFORM_TEX(uv, _Tex1);
						o.uvS = TRANSFORM_TEX(v.vertex.xy, _TexS);
					#elif _LAYERS_N3
						o.uv0 = TRANSFORM_TEX(uv, _Tex0);
						o.uv1 = TRANSFORM_TEX(uv, _Tex1);
						o.uv2 = TRANSFORM_TEX(uv, _Tex2);
						o.uvS = TRANSFORM_TEX(v.vertex.xy, _TexS);
					#elif _LAYERS_N4
						o.uv0 = TRANSFORM_TEX(uv, _Tex0);
						o.uv1 = TRANSFORM_TEX(uv, _Tex1);
						o.uv2 = TRANSFORM_TEX(uv, _Tex2);
						o.uv3 = TRANSFORM_TEX(uv, _Tex3);
						o.uvS = TRANSFORM_TEX(v.vertex.xy, _TexS);
					#endif
				#elif _UV_MAPPING_XY
					o.normal = v.normal;

					float3 _uv = v.vertex.xyz;
					_uv.xy = mul(_Matrix, v.vertex).xy;


					float3 uv = _uv;
					uv.xy += v.normal * (uv.z == 0 ? _Height : -_Height);
					

					#if _LAYERS_N1
						o.uv0.xz = TRANSFORM_TEX(uv.xz, _Tex0);
						o.uv0.yz = TRANSFORM_TEX(uv.yz, _Tex0);
					#elif _LAYERS_N2
						o.uv0.xz = TRANSFORM_TEX(uv.xz, _Tex0);
						o.uv0.yz = TRANSFORM_TEX(uv.yz, _Tex0);
						o.uv1.xz = TRANSFORM_TEX(uv.xz, _Tex1);
						o.uv1.yz = TRANSFORM_TEX(uv.yz, _Tex1);
						o.uvS = TRANSFORM_TEX(_uv.xy, _TexS);
					#elif _LAYERS_N3
						o.uv0.xz = TRANSFORM_TEX(uv.xz, _Tex0);
						o.uv0.yz = TRANSFORM_TEX(uv.yz, _Tex0);
						o.uv1.xz = TRANSFORM_TEX(uv.xz, _Tex1);
						o.uv1.yz = TRANSFORM_TEX(uv.yz, _Tex1);
						o.uv2.xz = TRANSFORM_TEX(uv.xz, _Tex2);
						o.uv2.yz = TRANSFORM_TEX(uv.yz, _Tex2);
						o.uvS = TRANSFORM_TEX(_uv.xy, _TexS);
					#elif _LAYERS_N4
						o.uv0.xz = TRANSFORM_TEX(uv.xz, _Tex0);
						o.uv0.yz = TRANSFORM_TEX(uv.yz, _Tex0);
						o.uv1.xz = TRANSFORM_TEX(uv.xz, _Tex1);
						o.uv1.yz = TRANSFORM_TEX(uv.yz, _Tex1);
						o.uv2.xz = TRANSFORM_TEX(uv.xz, _Tex2);
						o.uv2.yz = TRANSFORM_TEX(uv.yz, _Tex2);
						o.uv3.xz = TRANSFORM_TEX(uv.xz, _Tex3);
						o.uv3.yz = TRANSFORM_TEX(uv.yz, _Tex3);
						o.uvS = TRANSFORM_TEX(_uv.xy, _TexS);
					#endif
				#endif

				return o;
			}

			fixed4 frag(v2f i) : SV_TARGET
			{
				#if _UV_MAPPING_X || _UV_MAPPING_Y
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
						float4 c2 = tex2D(_Tex2, i.uv2) * _Col1;
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
				#elif _UV_MAPPING_XY
					const float PI = 3.14159265;
					const float Rad2Deg = 360 / (PI * 2);
					float angle = abs(atan2(i.normal.y, i.normal.x)) * Rad2Deg ;
					if (angle > 90) angle = abs( 180 - angle);
					float t = 1 - angle/90;
					float a = 0.45;
					float b = 1 - 0.45;
					if (t < a) t = 0;
					else if (t > b) t = 1;
					else t = smoothstep(a, b, t);

					#if _LAYERS_N1
						float4 x0 = tex2D(_Tex0, i.uv0.xz) * _Col0;
						float4 y0 = tex2D(_Tex0, i.uv0.yz) * _Col0;
						return lerp(x0, y0, t);
					#elif _LAYERS_N2
						float4 x0 = tex2D(_Tex0, i.uv0.xz) * _Col0;
						float4 y0 = tex2D(_Tex0, i.uv0.yz) * _Col0;
						float4 x1 = tex2D(_Tex1, i.uv1.xz) * _Col1;
						float4 y1 = tex2D(_Tex1, i.uv1.yz) * _Col1;
						float4 s = tex2D(_TexS, i.uvS);
						float4 x = x0 * s.x + x1 * s.y;
						float4 y = y0 * s.x + y1 * s.y;
						return lerp(x, y, t);
					#elif _LAYERS_N3
						float4 x0 = tex2D(_Tex0, i.uv0.xz) * _Col0;
						float4 y0 = tex2D(_Tex0, i.uv0.yz) * _Col0;
						float4 x1 = tex2D(_Tex1, i.uv1.xz) * _Col1;
						float4 y1 = tex2D(_Tex1, i.uv1.yz) * _Col1;
						float4 x2 = tex2D(_Tex2, i.uv2.xz) * _Col2;
						float4 y2 = tex2D(_Tex2, i.uv2.yz) * _Col2;
						float4 s = tex2D(_TexS, i.uvS);
						float4 x = x0 * s.x + x1 * s.y + x2 * s.z;
						float4 y = y0 * s.x + y1 * s.y + y2 * s.z;
						return lerp(x, y, t);
					#elif _LAYERS_N4
						float4 x0 = tex2D(_Tex0, i.uv0.xz) * _Col0;
						float4 y0 = tex2D(_Tex0, i.uv0.yz) * _Col0;
						float4 x1 = tex2D(_Tex1, i.uv1.xz) * _Col1;
						float4 y1 = tex2D(_Tex1, i.uv1.yz) * _Col1;
						float4 x2 = tex2D(_Tex2, i.uv2.xz) * _Col2;
						float4 y2 = tex2D(_Tex2, i.uv2.yz) * _Col2;
						float4 x3 = tex2D(_Tex3, i.uv3.xz) * _Col3;
						float4 y3 = tex2D(_Tex3, i.uv3.yz) * _Col3;
						float4 s = tex2D(_TexS, i.uvS);
						float4 x = x0 * s.x + x1 * s.y + x2 * s.z + x3 * s.w;
						float4 y = y0 * s.x + y1 * s.y + y2 * s.z + y3 * s.w;
						return lerp(x, y, t);
					#endif
				#endif
			}
			ENDCG
		}
	}
}