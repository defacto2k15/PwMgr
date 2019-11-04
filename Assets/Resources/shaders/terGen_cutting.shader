Shader "Custom/TerGen/Cutting"
{
	Properties
	{
		_SourceTexture("SourceTexture", 2D) = "white"{}
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

			sampler2D _SourceTexture;
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

			//Our Fragment Shader
			fixed4 frag(v2f i) : Color{
				float2 inPos = i.uv;
				inPos.x *= _Coords[2];
				inPos.y *= _Coords[3];

				float2 pos = inPos + float2(_Coords[0], _Coords[1]);
				float3 val = tex2D(_SourceTexture, pos);
				return fixed4(val, 1.0);
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
