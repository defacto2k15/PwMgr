Shader "Custom/ETerrain/SegmentPlacer"
{
	Properties
	{
		_SegmentHeightTexture("_SegmentHeightTexture", 2D) = "pink" {}
		_SegmentCoords("_SegmentCoords", Vector) = (0.0, 0.0, 1.0, 1.0)
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

			sampler2D _SegmentHeightTexture;
			float4 _SegmentCoords;

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
				uv.y = 1 - uv.y;
				float4 height = tex2D(_SegmentHeightTexture, uv);
				return height;
			} 

			ENDCG
		}
	}
	FallBack "Diffuse"
}
