Shader "Custom/Tool/ExtractMipmap"
{
	Properties
	{
		_InputTexture("_InputTexture", 2D) = ""{}
		_MipmapLevelToExtract("_MipmapLevelToExtract", Float) = 0.0
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

			sampler2D _InputTexture;
			float _MipmapLevelToExtract;

			struct v2f {
				float4 pos : POSITION;
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
				return tex2Dlod(_InputTexture, float4(inPos, 0,_MipmapLevelToExtract));
			} 
			ENDCG
		}
	}
	FallBack "Diffuse"
}
