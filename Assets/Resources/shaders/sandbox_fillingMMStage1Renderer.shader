Shader "Custom/Sandbox/Filling/MMStage1Renderer" {
	Properties{
		_StrokeSeedGridMap("StrokeSeedGridMap", 2DArray) = "blue"{}
		_MainTex ("MainTex", any) = "" {}
		_StrokeTex("StrokeTex", 2D) = "blue"{}
		_DebugScalar("DebugScalar", Range(0,1)) = 0
		_ArcCurvatureMargin("ArcCurvatureMargin", Range(0,1)) = 0.1
		_HatchLength("HatchLength", Range(0,128)) = 10
		_DistanceLinkingMargin("DistanceLinkingMargin", Range(0,10)) = 5
		_RotationSliceIndex("RotationSliceIndex",Range(0,16)) = 0
		_ScreenCellHeightMultiplier("ScreenCellHeightMultiplier", Range(0,12)) = 6
		_Stage1RenderingSize("Stage1RenderingSize", Vector) = (0.0, 0.0, 0.0, 0.0)

		_RotationSlicesCount("RotationSlicesCount", Int) = 16
		_BlockSize("BlockSize", Vector) = (32.0,32.0,0,0)

		_BlockCount("BlockCount", Vector) = (32.0,32.0,0,0)

		_WorldPositionTex("WorldPositionTex", 2D) = "blue"{}
		_VectorsTex("VectorsTex", 2D) = "blue"{}
		_SeedPositionTex3D("SeedPositionTex3D", 3D) = "" {}

		_MaximumPointToSeedDistance("MaximumPointToSeedDistance", Range(0,10)) = 1.0
		_SeedDensityF("SeedDensityF", Range(0,10)) = 1.0
		_MZoomPowFactor("MZoomPowFactor", Range(0,3)) = 0.7

		_SeedSamplingMultiplier("SeedSamplingMultiplier", Range(0,10)) = 1.0

		_DistanceToLineTreshold("DistanceToLineTreshold", Range(0,5)) = 2.0

		_TierCount("TierCount", Range(1,5)) = 1.0

		_VectorQuantCount("VectorQuantCount", Range(1,100)) = 8.0
		_LightHatchErasureMode("LightHatchErasureMode", Range(0,4)) = 0.0
		_LightHatchErasureMode3Factor("LightHatchErasureMode3Factor", Range(0,5)) = 1.0

		_ZoomHatchErasureMode("ZoomHatchErasureMode", Range(0,4)) = 0.0
		_Stage1BlockCoordsFilterPrimaryMargin("_Stage1BlockCoordsFilterPrimaryMargin",Range(0,5))=1

		[MaterialToggle] _FilterBySliceIndex("FilterBySliceIndex", Float) = 1
		[MaterialToggle] _FilterByBlockCoords("FilterByBlockCoords", Float) = 1
		[MaterialToggle] _FilterBySamplingCellSize("FilterBySamplingCellSize", Float) = 1
		[MaterialToggle] _FilterBySeedVectors("FilterBySeedVectors", Float) = 1
		_SamplingCellSizeLength("SamplingCellSizeLength", Range(0,30)) = 15

	}

		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			
			#pragma multi_compile __ MEASUREMENT
			#pragma multi_compile __ LIGHT_SHADING_ON
			#pragma multi_compile __ DIRECTION_PER_LIGHT
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;

			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : ANY_UV;
			};

#include "filling_variables.txt"

			RWTexture2DArray<float4> _StrokeSeedGridMap;
			float _FilterBySliceIndex;
			float _FilterByBlockCoords;
			float _FilterBySamplingCellSize;
			float _FilterBySeedVectors;
			float _SamplingCellSizeLength;

					//bool filterBySliceIndex,
					//bool filterByBlockCoords,
					//bool filterBySamplingCellSize

#include "mm_common.txt"
#define CUSTOM_MATRIX_VP _MyUnityMatrixVP
#include "filling_common.txt"
#include "filling_erasure.txt"
#include "filling_calculateSgmColor2.txt"
#include "filling_calculateSgmFromTextures.txt"

			AppendStructuredBuffer<int4> _GeometryRenderingFragmentBuffer;


			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				return o;

			}

			float2 angleTo2DVector(float angle) {
				return float2(sin(angle), cos(angle)).yx;
			}

			float ComputeLightAtHatchSamplePoint(inout SSGMPixel pixel, ConstantParametersPack paramPack) {
				float distanceToSamplePointFactor = 0; //ONCE 1 was here
				float2 thisPixelCoords = pixel.screenCoords;
				float2 coordsAtSamplePoint = thisPixelCoords + angleTo2DVector(pixel.strokeAngle) * paramPack.hatchLength*distanceToSamplePointFactor;

				float2 lightSamplePointUv = intScreenCoords_to_sampleuv(coordsAtSamplePoint);
				LightIntensityAngleOccupancy liao = unpackLightIntensityAngleOccupancy(  tex2D(_WorldPositionTex, lightSamplePointUv).a );
				float lightAtSample = liao.lightIntensity;
				if (!liao.occupancy) {
					return -1;
				}
				return lightAtSample;
			}

			float2 ComputeLightAtHatchSamplePointX(inout SSGMPixel pixel, ConstantParametersPack paramPack) {
				float distanceToSamplePointFactor = _DebugScalar;
				float2 thisPixelCoords = pixel.screenCoords;
				float2 coordsAtSamplePoint = thisPixelCoords + -angleTo2DVector(pixel.strokeAngle) * paramPack.hatchLength*distanceToSamplePointFactor;
				return coordsAtSamplePoint;
			}


			bool ShouldLightEraseHatchMode3(inout SSGMPixel pixel, ConstantParametersPack paramPack) {
				float lightAtSample =  ComputeLightAtHatchSamplePoint(pixel, paramPack);
				float f = generateLightHatchErasureBound(pixel.id);
				if ( lightAtSample<0 || pow(f, _LightHatchErasureMode3Factor) < lightAtSample) {
					return true;
				}
				else {
					return false; 
				}
			}

			float lightHatchErasureAlphaMultiplier(SSGMPixel pixel, float mFactor, ConstantParametersPack paramPack) {
				if (_LightHatchErasureMode == 3) {
					if (ShouldLightEraseHatchMode3(pixel, paramPack)) {
						return 0;
					}
					else {
						return 1;
					}
				}
				else {
					return 1;
				}
			}

			float generateSParamForId(int id) {
				return sin(id * 143.653184f) / 2.0 + 0.5;
			}

			float zoomHatchErasureAlphaMultiplier(int id, bool seedIsInSparseLevelToo, float mFactor, ConstantParametersPack paramPack) {
				float sParam = generateSParamForId(id);
				sParam = paramPack.sFilterMargins.x + sParam * (paramPack.sFilterMargins.y - paramPack.sFilterMargins.x);
				if (seedIsInSparseLevelToo) {
					sParam = 1;
				}

				if (_ZoomHatchErasureMode == 2) {
					if (sParam > mFactor) {
						return 1;
					}
					else {
						return 0;
					}
				}
				else if (_ZoomHatchErasureMode == 1) { //progressive
					// niskie sParam latwo znikaja
					float startOfMerge = sParam;
					float zoomHatchErasureMode1MergeDistance = 0.3; //TODO
					float stopOfMerge = min(sParam+zoomHatchErasureMode1MergeDistance, 1);
					return 1- (mFactor - startOfMerge) / (stopOfMerge - startOfMerge);
				}
				else {
					return 1;
				}
			}

			float computeAlphaMultiplier(SSGMPixel pixel, float mFactor, ConstantParametersPack paramPack) {
				return min(zoomHatchErasureAlphaMultiplier(pixel.id, pixel.seedIsInSparseLevelToo, mFactor, paramPack), lightHatchErasureAlphaMultiplier(pixel, mFactor, paramPack));
			}

			bool toBool(float f) {
				return f > 0.5;
			}

			float4 perTierStage1( int tierIndex, int2 screenCoords, float3 worldSpacePos, VectorsSetPair vPair, float samplePointStrokeAngle, ConstantParametersPack paramPack ) {
				float4 debugOut=0;

				float2 seedSpaceMultipliers = calculateTwoSeedSpaceMultipliers(tierIndex, screenCoords, worldSpacePos, vPair.genuine.alignmentVector, vPair.genuine.crossAlignmentVector, paramPack);
				float seedSpaceMultiplier = seedSpaceMultipliers.x;
				float mFactor = seedSpaceMultipliers.y;
				//seedSpaceMultiplier = 1;

				SSGMPixel sgmColorX =  calculateSgmPixel(tierIndex, worldSpacePos, vPair, seedSpaceMultiplier, samplePointStrokeAngle, paramPack); // TODO REMOVE

				if (length(sgmColorX.screenCoords)!=0) {

					SSGMFilterState filterState = make_SSGMFilterState(
						toBool(_FilterBySliceIndex)
						, toBool(_FilterByBlockCoords)
						, toBool(_FilterBySamplingCellSize)
						, toBool(_FilterBySeedVectors));

					SSGMUpdateInstanceSpec updateSpec = GenerateSSGMUpdateData(tierIndex, worldSpacePos, vPair, samplePointStrokeAngle, screenCoords, sgmColorX, filterState, paramPack);
					SSGMPixel pixel = updateSpec.pixel;
					pixel = sgmColorX;
					pixel.id = pixel.id | (asuint(tierIndex) << 30);

					float alphaMultiplier = computeAlphaMultiplier(pixel, mFactor, paramPack);

					if (alphaMultiplier > 0.01 ){
						if (!filterState.filterBySamplingCellSize || (length(pixel.screenCoords - screenCoords) < _SamplingCellSizeLength)) {
							float4 sgmColor = float4(
								asfloat(PackScreenCoordsToUint(pixel.screenCoords)),
								asfloat(pixel.id),
								pixel.strokeAngle,
								alphaMultiplier);
							if (updateSpec.slice1.shouldUpdate) {
								debugOut=1;

								uint3 tierAwareCoords = calculateTierAwareSSGMCoords(tierIndex, updateSpec.slice1.ssgmCoords, paramPack);

								_StrokeSeedGridMap[tierAwareCoords] = sgmColor;

								_GeometryRenderingFragmentBuffer.Append(int4(tierAwareCoords, 0));
								_GeometryRenderingFragmentBuffer.Append(int4(screenCoords, 0, 1));

								if (updateSpec.slice2.shouldUpdate ) {
									uint3 tierAwareCoords = calculateTierAwareSSGMCoords(tierIndex, updateSpec.slice2.ssgmCoords, paramPack);
									_StrokeSeedGridMap[tierAwareCoords] = sgmColor;
								}
							}

						}
					}
				}
				return debugOut;
			}

			float2 intScreenCoords_to_sampleuvX(int2 coords) {
				return float2((0.5+coords.x) / _ScreenParams.x, (0.5+coords.y) / _ScreenParams.y);
			}

			float4 frag(v2f input) : SV_Target
			{
				uint2 blockSize = uint2( round(_BlockSize.x), round(_BlockSize.y) );
				ConstantParametersPack paramPack = createFromProperties_ConstantParametersPack();

				float2 uv = float2Multiply(input.pos.xy, 1 / paramPack.stage1RenderingSize);
				int2 screenCoords = float2Multiply(uv, _ScreenParams.xy);

				float3 worldSpacePos = tex2D(_WorldPositionTex, uv).xyz;

				VectorsSet genuineVectorsSet = RetriveVectorsFromTextures(uv);
				int quantCount = _VectorQuantCount;
				float2 quantizationOffset = 0;
				QuantizationResult quantizationResult = ProcessAndQuantisizeVectors(genuineVectorsSet, quantizationOffset, quantCount);
				VectorsSet quantisizedVectorsSet = quantizationResult.vectors;
				VectorsSetPair vPair = make_VectorsSetPair(genuineVectorsSet, quantisizedVectorsSet);

				float samplePointStrokeAngle = PI * 2 * 24 / 32.0;
				float4 worldPositionTexPixel = tex2D(_WorldPositionTex, uv);
				LightIntensityAngleOccupancy liao = unpackLightIntensityAngleOccupancy(worldPositionTexPixel.a);
				samplePointStrokeAngle = liao.angle;

				float4 lodStrength = 0;
				for (int i = 0; i < _TierCount; i++) {
					lodStrength[i] = perTierStage1( i, screenCoords, worldSpacePos, vPair, samplePointStrokeAngle, paramPack);
				}

				return lodStrength;
			}
			ENDCG
		}
	}
}
