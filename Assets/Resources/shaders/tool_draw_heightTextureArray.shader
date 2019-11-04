Shader "Custom/Tool/DrawHeightTextureArray"
{
	Properties
	{
		_HeightTextureArray("HeightTextureArray", 2DArray) = "" {}
		_HeightTextureSelectedLevel("HeightTextureSelectedLevel", Range(0,10)) = 2 
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

			UNITY_DECLARE_TEX2DARRAY(_HeightTextureArray);  
			int _HeightTextureSelectedLevel;

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
				float textureArrayIndex = (float)_HeightTextureSelectedLevel;

				float height = UNITY_SAMPLE_TEX2DARRAY(_HeightTextureArray, float3(inPos, textureArrayIndex)).g;

				return float4(height, height, height, 1);
			} 
			ENDCG
		}
	}
	FallBack "Diffuse"
}
