Shader "Custom/Measurements/Filling/TamIssFragmentGathering" {
	Properties{
		_ColorTex("ColorTex", 2D) = "blue"{}
		_FragmentsTex("FragmentsTex", 2D) = "blue"{}
		_WorldPositionTex("WorldPositionTex", 2D) = "blue"{}

		_TamIdTexScale("TamIdTextureScale", Vector) = (1.0, 1.0, 0.0, 0.0)
		_TamIdTex("TamIdTex", 2DArray) = "blue" {}
		_TamIdTexBias("TamIdTexBias", Range(-5,5)) = 0.0
		_TamIdTexTonesCount("TamIdTexTonesCount", Range(0,10)) = 1.0
		_TamIdTexLayersCount("TamIdTexLayersCount", Range(0,10)) = 1.0

		_DebugScalar("DebugScalar", Range(0,15)) = 0.0
	}

		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
#pragma target 5.0
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile __ MEASUREMENT
			#pragma multi_compile __ LIGHT_SHADING_ON
			#pragma multi_compile __ DIRECTION_PER_LIGHT
				#include "UnityCG.cginc"
#include "common.txt"
#include "filling_common.txt"
#include "mf_common.txt"
#include "tamIss_common.txt" 

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : ANY_UV;
			};

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				return o;
			}


		int sampleTamIdTextureX(out mf_retrivedHatchPixel outArray[MAX_TAMID_LAYER_COUNT], float2 uv, float lightIntensity, int lodLevel, uint idPrefixBits) {
			if (lodLevel < 0) {
				lodLevel = _TamIdTex.CalculateLevelOfDetail(sampler_TamIdTex, uv) + _TamIdTexBias;
			}

			uv = float2Multiply(uv, _TamIdTexScale.xy);

			for (int i = 0; i < MAX_TAMID_LAYER_COUNT; i++) {
				outArray[i] = make_empty_mf_retrivedHatchPixel();
			}

			int2 uvCellsIndex = (floor(uv) + 64) % 2; //TODO change 1
			int2 offsetDirections = round(frac(uv)) * 2 - 1; // Values -1 and 1

			int toneIndex = round(( _TamIdTexTonesCount)  * (1 - lightIntensity));

			if (toneIndex != 0) {
				for (int layerIdx = 0; layerIdx <  _TamIdTexLayersCount; layerIdx++) {
					int arrayElementSelector = (toneIndex - 1) * _TamIdTexLayersCount + layerIdx;
					//lodLevel = 1;
					float4 c = UNITY_SAMPLE_TEX2DARRAY_LOD(_TamIdTex, float3(uv, arrayElementSelector), lodLevel);

					uint2 idBytes;
					idBytes[0] = round(c.r * 255);
					idBytes[1] = round(c.g * 255);

					uint offsetXBit = saturate(idBytes[1] & (1 << 6));
					uint offsetYBit = saturate(idBytes[1] & (1 << 7));

					int2 newUvCellsIndex = (uvCellsIndex + int2(offsetXBit*offsetDirections[0], offsetYBit*offsetDirections[1]) + 256)  % 2; //TODO CHANGE %2 + 256
					uint cellIdPrefix = (newUvCellsIndex[0] << 12) + (newUvCellsIndex[1] << 14); // TOOD CHANGE
					uint idPrefix = cellIdPrefix | (idPrefixBits << 30);

					uint idBytesConcat = idBytes[0] + (idBytes[1] & 15)*256;

					uint id = idBytesConcat +idPrefix;

					bool isThereHatch = false;
					if (c.a > 0.5) {
						isThereHatch = true;
					}

					outArray[layerIdx] = make_mf_retrivedHatchPixel(id, c.b, isThereHatch, c.a, arrayElementSelector);
				}
			}
			return 0;
		}

			AppendStructuredBuffer<TamIdFragment> _FragmentsBuffer;
			Texture2D<float4> _ColorTex;
			Texture2D<float4> _FragmentsTex;
			Texture2D<float4> _WorldPositionTex; // NOT USED
			int _DebugScalar;


			float4 frag(v2f i) : SV_Target
			{
				float2 screenUv = float2Multiply(i.pos.xy, 1 / _ScreenParams.xy);
				int2 screenCoords = float2Multiply(screenUv, _ScreenParams.xy);

				//TamIdFragment f1 = make_TamIdFragment(screenUv.x, screenUv.y, 1, 1);
				//					_FragmentsBuffer.Append(f1);
				//					return 0;

				float4 fragmentsPixel = _FragmentsTex[screenCoords];
				if (fragmentsPixel.a > 0) {
					float2 objectUv = fragmentsPixel.gb;
					float lightIntensity = _FragmentsTex[screenCoords].r;

					mf_retrivedHatchPixel retrivedPixels[MAX_TAMID_LAYER_COUNT];
					sampleTamIdTextureX(retrivedPixels, objectUv, lightIntensity, -1, 0);

					bool atLeastOneFragment = false;
					float maxStrength = 0;
					float4 c = 0;
							for (int i = 0; i < MAX_TAMID_LAYER_COUNT; i++) {
								mf_retrivedHatchPixel pixel = retrivedPixels[i];
								if (pixel.blendingHatchStrength > 0) {
									TamIdFragment f = make_TamIdFragment(screenUv.x, screenUv.y, pixel.tParam, pixel.id);

									_FragmentsBuffer.Append(f);
									if (pixel.blendingHatchStrength > maxStrength) {
										atLeastOneFragment = true;
										maxStrength = pixel.blendingHatchStrength;
										c = debugIntToColor(pixel.id % (6 * 6 * 6)).xyzz;
									}
								}
							}

					if (atLeastOneFragment) {
						return c;
					}
					return 0.1;
				}
				return 1;
			}

			ENDCG
		}
	}
}
