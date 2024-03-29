﻿Shader "Custom/TerGen/MirrorHeightTexture"
{
	Properties
	{
		_SourceTexture("SourceTexture", 2D) = "white"{}
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
            #include "HeightColorTransform.hlsl"
			#include "common.txt"

			sampler2D _SourceTexture;

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
				float2 mirroredPos = float2(1-i.uv.x, 1-i.uv.y);
				mirroredPos = mirroredPos.yx;
				float4 sourceHeight = tex2D(_SourceTexture, mirroredPos);
				return sourceHeight;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
