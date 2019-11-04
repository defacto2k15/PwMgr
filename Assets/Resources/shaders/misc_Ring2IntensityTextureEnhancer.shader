Shader "Custom/Misc/Ring2IntensityTextureEnhancer"
{
	Properties
	{
		_OriginalControlTex("OriginalControlTex", 2D) = "white" {}
		_Coords("Coords", Vector) = (0.0, 0.0, 1.0, 1.0)
	}
		SubShader
		{

		Pass
		{
		ZTest Always Cull Off ZWrite Off Fog { Mode Off } //Rendering settings
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc" 
			#include "billboardGrassGenerator.hlsl"
			#include "noise.hlsl"

			sampler2D _OriginalControlTex;
			float4 _Coords;

			//Our Fragment Shader
			fixed4 frag(v2f_img i) : Color{
				half2 pos = float2(
					_Coords[2] * i.uv.x + _Coords[0],
					_Coords[3] * i.uv.y + _Coords[1]);

				pos *= 0.2f;
				float4 pixel = tex2D(_OriginalControlTex, i.uv);
				
				for (int i = 0; i < 4; i++) {
					float randomFactor = remap(fractal_improvedValueNoise2D_3(float2(pos.x + 54.213*i, pos.y + 1.332*i))+1);
					pixel[i] *= lerp(0.7, 1.3, randomFactor);

				}
				return pixel;
			}
			ENDCG
		}
		}
			FallBack "Diffuse"
}