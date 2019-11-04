Shader "Custom/Tool/DrawHeightTextureMipMaps"
{
	Properties
	{
		_HeightTexture("HeightTexture", 2D) = "" {}
		_HeightTextureMipLevel("HeightTextureMipLevel", Range(0,10)) = 2 
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

			sampler2D _HeightTexture;
			int _HeightTextureMipLevel;

			float2 _GlobalTravellerPosition;

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
				float height = tex2Dlod(_HeightTexture, float4(inPos, _HeightTextureMipLevel,_HeightTextureMipLevel));
				float4 outColor = float4(height, height, height, 1);
				if (length(inPos - frac(_GlobalTravellerPosition + 0.5)) < 0.04) {
					outColor = float4(1, 0, 0, 1);
				}

				return outColor;
			} 
			ENDCG
		}
	}
	FallBack "Diffuse"
}
