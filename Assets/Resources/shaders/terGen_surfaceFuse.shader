Shader "Custom/TerrainCreation/SurfaceFuse"
{
	Properties
	{
		_TexturesArray("TexturesArray", 2DArray) = "" {}
		_NormalsArray("NormalsArray", 2DArray) = "" {}
		_TexturesCount("TexturesCount", Range(0,10) ) = 0
		_Coords("Coords", Vector) = (0.0,0.0,0.0,0.0)
	}

		SubShader
		{
			Pass
			{
			ZTest Always Cull Off ZWrite Off Fog { Mode Off } //Rendering settings
			CGPROGRAM
			#pragma multi_compile OUT_COLOR OUT_NORMAL
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.0

			#include "UnityCG.cginc"
			 
			UNITY_DECLARE_TEX2DARRAY(_TexturesArray);
			UNITY_DECLARE_TEX2DARRAY(_NormalsArray); 
			float _TexturesCount;
			float4 _Coords;

			struct v2f {
				float4 pos : POSITION; // niezbedna wartosc by dzialal shader
				half2 uv : TEXCOORD0;
			};

			v2f vert(appdata_img v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex); //niezbedna linijka by dzialal shader
				o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				return o;
			}

			#include "common.txt"

#define MAX_ARRAY_ELEMENTS (5)

			//Our Fragment Shader
			fixed4 frag(v2f i) : Color{
				float2 inPos = i.uv;
				inPos.x *= _Coords[2];
				inPos.y *= _Coords[3];

				float2 pos = inPos + float2(_Coords[0], _Coords[1]);
				float3 lastColor = float3(0, 0, 0);

				for (int i = 0; i < MAX_ARRAY_ELEMENTS; i++) {
					if (i < _TexturesCount) {
						float4 col = UNITY_SAMPLE_TEX2DARRAY(_TexturesArray, float3(pos.x, pos.y, i));
						if (col.a > 0.0001) {
#ifdef OUT_COLOR
							lastColor = col.rgb;
							return col;
#endif
#ifdef OUT_NORMAL
							float4 normal = UNITY_SAMPLE_TEX2DARRAY(_NormalsArray, float3(pos.x, pos.y, i));
							lastColor = normal.rgb;
							return normal;
#endif
						}
					}
				}

				return fixed4(lastColor.r, lastColor.g, lastColor.b,0);
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
