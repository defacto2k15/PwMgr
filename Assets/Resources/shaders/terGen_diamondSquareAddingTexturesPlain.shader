Shader "Custom/TerrainCreation/DiamondSquareTextureAddingPlain"
{
	Properties
	{
		_Texture1("Texture1", 2D) = "white"{}
		_Texture2("Texture2", 2D) = "white"{}
		_Texture2Weight("Texture2Weight", Range(0,1)) = 0.0
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
			#include "UnityCG.cginc" 

			sampler2D _Texture1;
			sampler2D _Texture2;
			float _Texture2Weight;
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
			#include "noise.hlsl"
			#include "HeightColorTransform.hlsl"

			//Our Fragment Shader
			fixed4 frag(v2f i) : Color{
				float2 inPos = i.uv;

				inPos.x *= _Coords[2];
				inPos.y *= _Coords[3];

				float2 pos = inPos + float2(_Coords[0], _Coords[1]);

				float height1 =(tex2D(_Texture1, pos));
				float height2 =(tex2D(_Texture2, pos));
				height2 -= 0.5;

				float4 finalValue =(height1 + height2* _Texture2Weight);

				return finalValue;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
