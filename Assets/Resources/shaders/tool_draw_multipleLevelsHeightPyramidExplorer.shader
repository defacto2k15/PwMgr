Shader "Custom/Tool/MultipleLevelsHeightPyramidExplorer"
{
	Properties
	{
		_SegmentsStateFontmap("SegmentsStateFontmap", 2D) = "pink" {}
		_FloorTexturesArray("HeightTexture", 2DArray) = "" {}
		_SelectedLevelIndex("HeightTextureMipLevel", Int) = 0
		_SlotMapSize("SlotMapSize", Vector) = (2.0,2.0,0.0,0.0)
		_RingsUvRange("RingsUvRange", Vector) = (0.0,0.0,0.0,0.0)
		_PerLevelFloorTextureWorldSpaceSizes("_PerLevelFloorTextureWorldSpaceSizes", Vector) = (0.0,0.0,0.0,0.0)
		_TravellerPosition("_TravellerPosition", Vector) = (0.0, 0.0, 0.0, 0.0)
		_Pyramid0WorldSpaceCenter("_Pyramid0WorldSpaceCenter", Vector) = (0.0, 0.0, 0.0, 0.0)
		_Pyramid1WorldSpaceCenter("_Pyramid1WorldSpaceCenter", Vector) = (0.0, 0.0, 0.0, 0.0)
		_Pyramid2WorldSpaceCenter("_Pyramid2WorldSpaceCenter", Vector) = (0.0, 0.0, 0.0, 0.0)
		_Debug("Debug", Range(-1000,1000)) = 0
		_Debug2("Debug2", Range(-1000,1000)) = 0
	}

	SubShader
	{
		Pass
		{
		ZTest Always Cull Off ZWrite Off Fog { Mode Off } //Rendering settings
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#include "UnityCG.cginc" 

			sampler2D _SegmentsStateFontmap;
			UNITY_DECLARE_TEX2DARRAY(_FloorTexturesArray);
			int _SelectedLevelIndex;
			float4 _SlotMapSize;
			float4 _RingsUvRange;
			float4 _PerLevelFloorTextureWorldSpaceSizes;

			float4 _TravellerPosition;
			float4 _Pyramid0WorldSpaceCenter;
			float4 _Pyramid1WorldSpaceCenter;
			float4 _Pyramid2WorldSpaceCenter;

			float _Debug;
			float _Debug2;

			struct GpuSingleSegmentState
			{
				int IsProcessOnGoing;
				int RequiredSituationIndex;
				int CurrentSituationIndex;
				int FillingIsNecessary;
			};

			StructuredBuffer<GpuSingleSegmentState> _FillingStateBuffer;

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

			float2 RetrivePyramidWorldSpaceCenter(int pyramidIndex) {
				if (pyramidIndex == 0) {
					return _Pyramid0WorldSpaceCenter.xy;
				}else if (pyramidIndex == 1) {
					return _Pyramid1WorldSpaceCenter.xy;
				}
				else {
					return _Pyramid2WorldSpaceCenter.xy;
				}
			}

			float maxComponent(float2 i) {
				return max(i.x, i.y);
			}

			bool sampleStateFontmap(float2 uv, int characterIndex) {
				float intensity = tex2D(_SegmentsStateFontmap, float2(uv.x / 3.0 + characterIndex * 1 / 3.0, uv.y)).r;
				return intensity < 0.5;
			}

			fixed4 frag(v2f i) : Color{
				float2 uv = i.uv;
				float height = UNITY_SAMPLE_TEX2DARRAY_LOD(_FloorTexturesArray, float3(uv, _SelectedLevelIndex), 0);

				float4 outColor = float4(height, height, height, 1);

				float floorTextureWorldSpaceSize = _PerLevelFloorTextureWorldSpaceSizes[_SelectedLevelIndex];
				float4 worldSpaceRingRanges = _RingsUvRange * floorTextureWorldSpaceSize;

				float2 pyramidCenterWorldSpace = RetrivePyramidWorldSpaceCenter(_SelectedLevelIndex);// float2(_Debug, _Debug2);

				int currentRingIndex = -1;
				bool weAreInRingBorder = false;
				for (int ringIndex = 0; ringIndex < 3; ringIndex++) {
					float thisRingRange = _RingsUvRange[ringIndex];
					float2 pyramidCenterUv = frac(pyramidCenterWorldSpace / floorTextureWorldSpaceSize);
					float2 loopedPyramidCenterUv = pyramidCenterUv;

					int2 uvQuart = round(uv);
					int2 centerQuart = round(pyramidCenterUv);
					loopedPyramidCenterUv = pyramidCenterUv + uvQuart - centerQuart;

					float mDistanceToCenterUv = 99999999;
					for (int x = -1; x <= 1; x++) {
						for (int y = -1; y <= 1; y++) {
							mDistanceToCenterUv = min(mDistanceToCenterUv,
								maxComponent(abs(pyramidCenterUv + int2(x, y) - uv)));
						}
					}

					if (currentRingIndex == -1 && mDistanceToCenterUv <= thisRingRange) {
						currentRingIndex = ringIndex;
					}

					if (abs(mDistanceToCenterUv - thisRingRange) / thisRingRange < 0.05 / (ringIndex + 1)) {
						weAreInRingBorder = true;
					}
				}

				if (currentRingIndex != -1) {
					float ringColorIntensity = 0.4 * 1.0 / (currentRingIndex + 1);
					if (_SelectedLevelIndex == 0) {
						outColor.r += ringColorIntensity;
					}
					else if (_SelectedLevelIndex == 1) {
						outColor.g += ringColorIntensity;
					}
					else {
						outColor.b += ringColorIntensity;
					}
				}

				int2 segmentCoords = floor(float2(uv.x * _SlotMapSize.x, uv.y * _SlotMapSize.y));
				float2 segmentUv = frac(float2(uv.x * _SlotMapSize.x, uv.y * _SlotMapSize.y));


				int segmentStateIndex = segmentCoords.x + segmentCoords.y * _SlotMapSize.x;
				GpuSingleSegmentState segmentState = _FillingStateBuffer[segmentStateIndex];
				if (segmentState.IsProcessOnGoing > 0) {
					if (abs(frac((uv.y - uv.x) * 32)) < 0.2) {
						return float4(1, 0, 1, 1);
					}
				}
					int requiredSituationIndex = segmentState.RequiredSituationIndex;
					int currentSituationIndex = segmentState.CurrentSituationIndex;
					if (requiredSituationIndex != 0) {
						bool requirementFufilled = false;
						bool requirementNeeded = true;
						if (sampleStateFontmap(segmentUv, requiredSituationIndex - 1)) {
							if (requiredSituationIndex == 1) { //created
								requirementFufilled = currentSituationIndex >= 3; // created
							}
							else if (requiredSituationIndex == 2) { //filled
								if (segmentState.FillingIsNecessary == 0) {
									requirementNeeded = false;
								}
								requirementFufilled = currentSituationIndex >= 5;
							}

							if (requirementFufilled) {
								return float4(0, 1, 0, 1);
							}
							else {
								if (requirementNeeded) {
									return float4(1, 0, 0, 1);
								} else {
									return float4(0, 0, 1, 1);
								}
							}
						}
					}

				if (weAreInRingBorder) {
					outColor = float4(1, 0, 1, 1);
				}
				else {
					int2 segmentCount = round(_SlotMapSize.xy);
					float distanceToGrid = min(
						abs(uv.x * segmentCount.x - round(uv.x * segmentCount.x)) / segmentCount.x,
						abs(uv.y * segmentCount.y - round(uv.y * segmentCount.y)) / segmentCount.y
					);
					if (distanceToGrid < 0.005) {
						outColor = 1;
					}
				}

				float2 travellerPositionCeilSpace = frac(_TravellerPosition.xy / floorTextureWorldSpaceSize);
				if (length(travellerPositionCeilSpace - uv) < 0.01) {
					outColor = float4(1, 1, 0, 1);
				}

				outColor.a = 1;
				return outColor;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
