Shader "Custom/ETerrain/CornerPlacer"
{
	Properties
	{
		_ModifiedCornerBuffer("_ModifiedCornerBuffer", 2D) = "pink" {}
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

			sampler2D _ModifiedCornerBuffer;
			float4 _WeldingAreaCoords;
			float _MarginSize;
			float4 _CornerToWeld;
			float _PixelSizeInUv;

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
				float2 uv = i.uv;
				float4 outHeight = tex2D(_ModifiedCornerBuffer, uv);
				return float4(outHeight.rgb, 1);
			} 

			ENDCG
		}
	}
	FallBack "Diffuse"
}
