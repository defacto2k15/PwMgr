Shader "Custom/ETerrain/Ground"
{
	Properties
	{
		_HeightMap("_HeightMap", 2DArray) = "pink"{}
		_SurfaceTexture("_SurfaceTexture", 2DArray) = "pink"{}
		_NormalTexture("_NormalTexture", 2DArray) = "pink"{}

		_SegmentCoords("_SegmentCoords", Vector) = (0.0, 0.0, 1.0, 1.0)
		_SegmentLevelCoords("_SegmentLevelCoords", Vector) = (0.0, 0.0, 1.0, 1.0)

		_TravellerPositionWorldSpace("TravellerPositionWorldSpace", Vector) = (0.0, 0.0, 0.0, 0.0)

		_RingsPerLevelCount("RingsPerLevelCount", Int) = 3
		_LevelsCount("LevelsCount", Int) = 3
		_ThisLevelIndex("ThisLevelIndex",Int) = 0
		_Debug("Debug", Range(0,1)) = 0
	}

	SubShader
	{
			CGPROGRAM
			#pragma surface surf Lambert addshadow vertex:vert 
			#pragma target 5.0
#pragma exclude_renderers d3d11_9x
#pragma exclude_renderers d3d9
			#include "UnityCG.cginc" 
			#include "common.txt"

			float4 _SegmentCoords;
			float4 _SegmentLevelCoords;
			UNITY_DECLARE_TEX2DARRAY(_HeightMap);
			UNITY_DECLARE_TEX2DARRAY(_SurfaceTexture);
			UNITY_DECLARE_TEX2DARRAY(_NormalTexture);

			float2 _MainPyramidCenterWorldSpace;

			float2 _TravellerPositionWorldSpace;

			int _RingsPerLevelCount;
			int _LevelsCount;
			int _ThisLevelIndex;
			float _Debug;

			struct Input {
				half2 inSegmentSpaceUv;
				half2 uv;
				float usedMipMapLevel;
				float terrainMergingLerpParam;
				float2 worldSpaceLocation;
				float shouldBeDiscardedMarker;
			};


#include "eterrain_heightMapCommon.hlsl"
#ifdef SHADER_API_D3D11
			StructuredBuffer<EPyramidConfiguration> _EPyramidConfigurationBuffer;
			StructuredBuffer<EPyramidPerFrameConfiguration> _EPyramidPerFrameConfigurationBuffer;
#endif

			ETerrainParameters init_ETerrainParametersFromUniforms() {
				ETerrainParameters p;
#ifdef SHADER_API_D3D11
				p.pyramidConfiguration = _EPyramidConfigurationBuffer[0];
				p.perFrameConfiguration = _EPyramidPerFrameConfigurationBuffer[0];
#else
				p.pyramidConfiguration = null_EPyramidConfiguration();
				p.perFrameConfiguration = null_EPyramidPerFrameConfiguration();
#endif
				p.travellerPositionWorldSpace = _TravellerPositionWorldSpace;
				p.ringsPerLevelCount = _RingsPerLevelCount;
				p.levelsCount = _LevelsCount;
				return p;
			}

				// UV IN RECTANGLE [{-L/2; -L/2} - {L/2; L/2}] where L - length of ceilTexture in worldSpace
			float2 calculateInSegmentSpaceUv(float2 uv) {
				return _SegmentCoords.xy + float2(uv.x * _SegmentCoords.z, uv.y * _SegmentCoords.w);
			}

			float2 calculateInLevelSpaceUv(float2 uv) {
				return _SegmentLevelCoords.xy + float2(uv.x * _SegmentLevelCoords.z, uv.y * _SegmentLevelCoords.w);
			}

			int findRingIndex(ETerrainParameters terrainParameters, float2 inLevelSpaceUv) {
				float4 ringsUvs[3] = { //TODO make configurable
					float4(0.5 - 1 / 8.0, 0.5 - 1 / 8.0, 1 / 4.0, 1 / 4.0),
					float4(0.5 - 2 / 8.0, 0.5 - 2 / 8.0, 2 / 4.0, 2 / 4.0),
					float4(0.5 - 4 / 8.0, 0.5 - 4 / 8.0, 4 / 4.0, 4 / 4.0)
				};
				for (int ringIndex = 0; ringIndex < min(terrainParameters.ringsPerLevelCount, MAX_RINGS_PER_LEVEL_COUNT); ringIndex++) {
					if (isInRectangle(inLevelSpaceUv, ringsUvs[ringIndex])) {
						return ringIndex;
					}
				}
				return -1;
			}

			void vert( inout appdata_full v, out Input v2f_o) {
				float2 uv = v.texcoord.xy;
				float2 inSegmentSpaceUv = calculateInSegmentSpaceUv(uv);
				float2 inLevelSpaceUv = calculateInLevelSpaceUv(uv);

				ETerrainParameters terrainParameters = init_ETerrainParametersFromUniforms();

				int levelIndex = _ThisLevelIndex;
				int ringIndex = findRingIndex(terrainParameters, inLevelSpaceUv);

				ELevelAndRingIndexes levelAndRingIndexes = make_ELevelAndRingIndexes(levelIndex, ringIndex);
				EPerRingParameters perRingParameters = init_EPerRingParametersFromBuffers(levelAndRingIndexes, terrainParameters);
				ETerrainHeightCalculationOut terrainOut = calculateETerrainHeight2(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters, perRingParameters);

				v.vertex.y = 0;// -levelAndRingIndexes.levelIndex*0.00002f * 0 + terrainOut.finalHeight;

				v2f_o.inSegmentSpaceUv = inSegmentSpaceUv;
				v2f_o.uv = uv;
				v2f_o.usedMipMapLevel =  ringIndex+ terrainOut.terrainMergingLerpParam;
				v2f_o.terrainMergingLerpParam = terrainOut.terrainMergingLerpParam;
				v2f_o.shouldBeDiscardedMarker = terrainOut.shouldBeDiscardedMarker;
				v2f_o.worldSpaceLocation = mainGlobalLevelUvSpaceToWorldSpace(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters);
			}

			float4 sampleSurfaceTexture(int level, float4 uv) { //TODO VERY UNOPTIMAL
#ifdef SHADER_API_D3D11  
				return _SurfaceTexture.SampleLevel(sampler_SurfaceTexture, float3(uv.xy, level), uv.w);
#else
				return UNITY_SAMPLE_TEX2DARRAY_LOD(_SurfaceTexture, float3(uv.xy, level), uv.w);
#endif
			}

			float4 sampleNormalTexture(int level, float4 uv) { //TODO VERY UNOPTIMAL
#ifdef SHADER_API_D3D11  
				return _NormalTexture.SampleLevel(sampler_NormalTexture, float3(uv.xy, level), uv.w);
#else
				return UNITY_SAMPLE_TEX2DARRAY_LOD(_NormalTexture, float3(uv.xy, level), uv.w);
#endif
			}

			struct ESurfaceInfo {
				float3 color;
				float3 normal;
			};

			ESurfaceInfo  make_ESurfaceInfo(float3 color, float3 normal) {
				ESurfaceInfo i;
				i.color = color;
				i.normal = normal;
				return i;
			}

			ESurfaceInfo calculateESurfaceInfo(float2 inSegmentSpaceUv, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters terrainParameters, float lod ) {
				int mainHeightTextureResolution = terrainParameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureResolution;
				float2 textureSamplingUv = frac(inSegmentSpaceUv + MainPyramidCenterUv(levelAndRingIndexes, terrainParameters));

				float2 sampleCenteredHighQualityUv = textureSamplingUv + 1.0/ (mainHeightTextureResolution * 2.0); //This is to align UV to sample center of heightmap pixels
				float4 currentColor = sampleSurfaceTexture(levelAndRingIndexes.levelIndex, float4(sampleCenteredHighQualityUv,0,lod));
				float4 secondChanceColor = sampleSurfaceTexture(1, float4(sampleCenteredHighQualityUv,0,lod));

				float4 currentNormal = sampleNormalTexture(levelAndRingIndexes.levelIndex, float4(sampleCenteredHighQualityUv,0,lod));
				float4 secondChanceNormal = sampleNormalTexture(1, float4(sampleCenteredHighQualityUv,0,lod));

				float secondChanceMarker = saturate(0.01+currentColor.a / 0.025);
				return make_ESurfaceInfo(lerp(secondChanceColor.xyz, currentColor.xyz, secondChanceMarker), currentNormal);// normalize(lerp(secondChanceNormal.xyz, currentNormal.xyz, secondChanceMarker)));
			}


#include "text_printing.hlsl"
#include "noise.hlsl"

			float3 randomColor(float2 input) {
				return float3(
					rand2(input),
					rand2(input.yx + float2(1231.131123, 431.231)),
					rand2(input + float2(-32.5312, 31.922))
					);
			}

			struct InTriangleGridPosition {
				int2 squareIndex;
				bool isLowerTriangle;
			};

			InTriangleGridPosition make_InTriangleGridPosition( int2 squareIndex, bool isLowerTriangle ){
				InTriangleGridPosition p;
				p.squareIndex = squareIndex;
				p.isLowerTriangle = isLowerTriangle;
				return p;
			}

			InTriangleGridPosition calculateInTriangleGridPosition(float2 uv, int gridResolution) {
				float2 perVerticleUv = uv * gridResolution;
				int2 verticleIndex = floor(perVerticleUv);
				float2 inVerticleUv = perVerticleUv - verticleIndex;

				bool isLowerTriangle = false;
				if (inVerticleUv.x < 1-inVerticleUv.y) {
					isLowerTriangle = true;
				}
				return make_InTriangleGridPosition(verticleIndex, isLowerTriangle);
			}

			float2 squareIndexToDownLeftVerticlePlateUv(int2 squareIndex, int gridResolution) {
				return squareIndex / ((float)gridResolution);
			}

			float2 CalculateDownLeftVerticleInSegmentSpaceUv(float2 inSegmentSpaceUv, ELevelAndRingIndexes levelAndRingIndexes, EPerRingParameters perRingParameters, ETerrainParameters terrainParameters) {
				float subRingMultiplier = pow(2, perRingParameters.ringIndex);
				int gridResolution = terrainParameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureResolution / subRingMultiplier;
				float ceilTextureWorldSize = terrainParameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureWorldSize;
				float worldSpaceGridCellsLength = ceilTextureWorldSize / gridResolution;
				InTriangleGridPosition gridPosition = calculateInTriangleGridPosition(inSegmentSpaceUv, gridResolution);
				int2  squareIndex = gridPosition.squareIndex;

				float2 downLeftVerticleInSegmentSpaceUv = squareIndexToDownLeftVerticlePlateUv(squareIndex, gridResolution);
				return downLeftVerticleInSegmentSpaceUv;
			}

			float4 GenerateDebugColorFromIndexes(ELevelAndRingIndexes levelAndRingIndexes, float terrainMergingLerpParam) {
				float4 finalColor = 0;
				int levelIndex = levelAndRingIndexes.levelIndex;
				int ringIndex = levelAndRingIndexes.ringIndex;

				if (levelIndex == 0) {
					finalColor = float4(1, 0, 0, 1);
				}
				else if (levelIndex == 1) {
					finalColor = float4(0, 1, 0, 1);
				}
				else {
					finalColor = float4(0, 0, 1, 1);
				}
				finalColor /= (ringIndex + 1.0f);
				if (terrainMergingLerpParam > 0.02 && terrainMergingLerpParam < 0.98) {
					finalColor = float4(1, 1, 1, 1) * terrainMergingLerpParam;
				}
				return finalColor;
			}

#include "common.txt"

			//Our Fragment Shader
			void surf(in Input i, inout SurfaceOutput o) {	//TODO add normals coloring
				if (i.shouldBeDiscardedMarker> 0) {
					discard;
				}

				float4 finalColor;
				ETerrainParameters terrainParameters = init_ETerrainParametersFromUniforms();
				ELevelAndRingIndexes levelAndRingIndexes = FindLevelAndIndexFromWorldSpacePosition(i.worldSpaceLocation,terrainParameters);
				EPerRingParameters perRingParameters = init_EPerRingParametersFromBuffers(levelAndRingIndexes, terrainParameters);
				int levelIndex = levelAndRingIndexes.levelIndex;
				int ringIndex = levelAndRingIndexes.ringIndex;

				float surfaceColorLod = levelAndRingIndexes.ringIndex + i.terrainMergingLerpParam ;
				if (levelAndRingIndexes.levelIndex == 2) {
					surfaceColorLod += 1;
				}

				float2 downLeftVerticleInSegmentSpaceUv = CalculateDownLeftVerticleInSegmentSpaceUv(i.inSegmentSpaceUv, levelAndRingIndexes, perRingParameters, terrainParameters);
				ESurfaceInfo surfaceInfo = calculateESurfaceInfo(downLeftVerticleInSegmentSpaceUv, levelAndRingIndexes, terrainParameters, surfaceColorLod);
				finalColor = float4(surfaceInfo.color,1);

				float3 worldNormal = surfaceInfo.normal;
				o.Albedo =  finalColor;
				o.Normal = decodeNormal(worldNormal);

				o.Albedo = GenerateDebugColorFromIndexes(levelAndRingIndexes, i.terrainMergingLerpParam);
			} 

			ENDCG
		
	}
	FallBack "Diffuse"
}

