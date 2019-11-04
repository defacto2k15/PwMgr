Shader "Custom/TerrainCreation/NoiseAddingPlain"
{
	Properties
	{
		_SourceTexture("SourceTexture", 2D) = "white"{}
		_Coords("Coords", Vector) = (0.0,0.0,0.0,0.0)
		_QuantingResolution("QuantingResolution", Range(1,1024)) = 1
		_InputGlobalCoords("InputGlobalCoords", Vector) = (0.0, 0.0, 0.0, 0.0)
		_DebugScalar("DebugScalar", Range(0,2)) = 1
		_DetailResolutionMultiplier("DetailResolutionMultiplier", Range(0,2)) = 1
		_NoiseStrengthMultiplier("NoiseStrengthMultiplier", Range(0,10)) = 1
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
			float _QuantingResolution;
			float4 _InputGlobalCoords;
			float _DebugScalar;
			float _DetailResolutionMultiplier;
			float _NoiseStrengthMultiplier;

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
			#include "noise.hlsl"

			GEN_fractalNoise( tc_na_fractal_improvedPerlinNoise2D_3, 3, snoise2D, 0.3, 0.55)

			float2 quantToResolution(float2 inPos, float resolution) {
				float2 outVal = float2(0, 0);
				outVal = (round(inPos * resolution)) / resolution;
				return outVal;
			}

			//Our Fragment Shader
			fixed4 frag(v2f i) : Color{
				i.uv = quantToResolution(i.uv, _QuantingResolution);
				float2 inPos = i.uv;

				inPos.x *= _Coords[2];
				inPos.y *= _Coords[3];

				float2 pos = inPos + float2(_Coords[0], _Coords[1]);
				float heightmapValue =( tex2D(_SourceTexture, float4(pos,0,0)));

				float2 globalPos = float2(
					_InputGlobalCoords[0] + inPos.x *_InputGlobalCoords[2],
					_InputGlobalCoords[1] + inPos.y *_InputGlobalCoords[3]
					);

				float2 noisePos = globalPos + float2(421.22, 984.1);
				float noise = tc_na_fractal_improvedPerlinNoise2D_3(_DetailResolutionMultiplier * noisePos/400);
				noise *= 0.001 * 0.25;
				noise += 0.0005;
				noise *= _NoiseStrengthMultiplier;
				
				heightmapValue += noise;
				return (heightmapValue);
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
