Shader "Custom/TerrainCreation/SurfaceFuseNonTexArray"
{
	Properties
	{
		_Texture0("Texture0", 2D) = "black" {}
		_Texture1("Texture1", 2D) = "black" {}
		_Texture2("Texture2", 2D) = "black" {}
		_Texture3("Texture3", 2D) = "black" {}
		_Texture4("Texture4", 2D) = "black" {}

		_TexturesCount("TexturesCount", Range(0,10)) = 0
		_Coords("Coords", Vector) = (0.0,0.0,0.0,0.0)
	}

		SubShader
		{
			Pass
			{
			ZTest Always Cull Off ZWrite Off Fog { Mode Off } //Rendering settings
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.0

			#include "UnityCG.cginc"

			sampler2D _Texture0;
			sampler2D _Texture1;
			sampler2D _Texture2;
			sampler2D _Texture3;
			sampler2D _Texture4;

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

			float4 getMoreOpaqueColor(float4 c1, float4 c2) {
				if (c1.a > c2.a) {
					return c1;
				}
				else {
					return c2;
				}
			}

			//Our Fragment Shader
			fixed4 frag(v2f i) : Color{
				float2 inPos = i.uv;
				inPos.x *= _Coords[2];
				inPos.y *= _Coords[3];

				float2 pos = inPos + float2(_Coords[0], _Coords[1]);

				float4 pixel = float4(0, 0, 0, 0);
				for (int i = 0; i < MAX_ARRAY_ELEMENTS; i++) {
					if (i < _TexturesCount) {
						if (i == 0) {
							pixel = getMoreOpaqueColor(pixel, tex2D(_Texture0, float2(pos.x, pos.y)));
						}
						else if (i == 1) {
							pixel = getMoreOpaqueColor(pixel, tex2D(_Texture1, float2(pos.x, pos.y)));
						}
						else if (i == 2) {
							pixel = getMoreOpaqueColor(pixel, tex2D(_Texture2, float2(pos.x, pos.y)));
						}
						else if (i == 3) {
							pixel = getMoreOpaqueColor(pixel, tex2D(_Texture3, float2(pos.x, pos.y)));
						}
						else if (i == 4) {
							pixel = getMoreOpaqueColor(pixel, tex2D(_Texture4, float2(pos.x, pos.y)));
						}
						//if (pixel.a > 0) {
						//	return pixel;
						//}
					}
				}

				float t0 = tex2D(_Texture0, float2(pos.x, pos.y)).a;
				float t1 = tex2D(_Texture1, float2(pos.x, pos.y)).a;
				//return abs(t0 - t1)*100;
				//return tex2D(_Texture0, float2(pos.x, pos.y));
				//return float4(t0, t1, 0, 1);

				//float4 texArray[MAX_ARRAY_ELEMENTS];
				//for (int i = 0; i < MAX_ARRAY_ELEMENTS; i++) {
				//	if (i == 0) {
				//		texArray[i] = tex2D(_Texture0, float2(pos.x, pos.y));
				//	}
				//	else if (i == 1) {
				//		texArray[i] = tex2D(_Texture1, float2(pos.x, pos.y));
				//	}
				//	else if (i == 2) {
				//		texArray[i] = tex2D(_Texture2, float2(pos.x, pos.y));
				//	}
				//	else if (i == 3) {
				//		texArray[i] = tex2D(_Texture3, float2(pos.x, pos.y));
				//	}
				//	else if (i == 4) {
				//		texArray[i] = tex2D(_Texture4, float2(pos.x, pos.y));
				//	}
				//}

				//float4 outColor = 0;
				//for (int i = 0; i < min(4,_TexturesCount); i++) {
				//	outColor[i] = texArray[i][i];
				//}
				//return outColor;




				//if (pixel.a <= 0) {
				//	pixel = 0;
				//}
				return pixel;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
