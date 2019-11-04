Shader "Custom/TerrainCreation/SurfaceFuseNonTexArray"
{
	Properties
	{
		_Texture0("Texture0", 2D) = "white" {}
		_Texture1("Texture1", 2D) = "white" {}
		_Texture2("Texture2", 2D) = "white" {}
		_Texture3("Texture3", 2D) = "white" {}
		_Texture4("Texture4", 2D) = "white" {}

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

			//Our Fragment Shader
			fixed4 frag(v2f i) : Color{
				float2 inPos = i.uv;
				inPos.x *= _Coords[2];
				inPos.y *= _Coords[3];

				float2 pos = inPos + float2(_Coords[0], _Coords[1]);

				for (int i = 0; i < MAX_ARRAY_ELEMENTS; i++) {
					if (i < _TexturesCount) {
						float4 pixel = float4(0, 0, 0, 0);
						if (i == 0) {
							pixel = tex2D(_Texture0, float2(pos.x, pos.y));
						}
						else if (i == 1) {
							pixel = tex2D(_Texture1, float2(pos.x, pos.y));
						}
						else if (i == 2) {
							pixel = tex2D(_Texture2, float2(pos.x, pos.y));
						}
						else if (i == 3) {
							pixel = tex2D(_Texture3, float2(pos.x, pos.y));
						}
						else if (i == 4) {
							pixel = tex2D(_Texture4, float2(pos.x, pos.y));
						}

						if (pixel.a > 0.01) {
							return pixel;
						}
					}
				}

				return fixed4(1,0.25,1,1);
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
