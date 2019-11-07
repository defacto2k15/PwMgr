Shader "Custom/ESurface/SegmentPlacer"
{
	Properties
	{
		_SegmentSurfaceTexture("_SegmentSurfaceTexture", 2D) = "pink" {}
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

			sampler2D _SegmentSurfaceTexture;
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
				// THIS IS SURFACE SURFACE SURFACE!!!!! NOT TERRAIN
				float2 uv = i.uv;
				uv.y = 1 - uv.y; 
				return tex2D(_SegmentSurfaceTexture, uv);
			} 

			ENDCG
		}
	}
	FallBack "Diffuse"
}
