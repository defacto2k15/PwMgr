Shader "Custom/Tool/FillHeightTextureWithRandomValues"
{
	Properties
	{
		_HeightTexture("HeightTexture", 2D) = "" {}
		_PositionMultiplier("_PositionMultiplier", Range(1,20) ) = 1
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
			#include "common.txt"
			#include "noise.hlsl"

			sampler2D _HeightTexture;
			float _PositionMultiplier;

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

			//Our Fragment Shader
			fixed4 frag(v2f i) : Color{
				float2 inPos = i.uv;
				float height = (fractal_improvedPerlinNoise2D_3(inPos*8 * _PositionMultiplier)+5)/5;
				return float4(height, height, height, 1);
			} 
			ENDCG
		}
	}
	FallBack "Diffuse"
}
