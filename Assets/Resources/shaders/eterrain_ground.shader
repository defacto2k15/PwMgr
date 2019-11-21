Shader "Custom/ETerrain/Ground"
{
	Properties
	{
		_HeightMap0("_HeightMap0", 2D) = "pink"{}
		_HeightMap1("_HeightMap1", 2D) = "pink"{}
		_HeightMap2("_HeightMap2", 2D) = "pink"{}

		_SurfaceTexture0("_SurfaceTexture0", 2D) = "pink"{}
		_SurfaceTexture1("_SurfaceTexture1", 2D) = "pink"{}
		_SurfaceTexture2("_SurfaceTexture2", 2D) = "pink"{}

		_MainPyramidLevelWorldSize( "MainPyramidLevelWorldSize", Float) = 1.0

		_SegmentCoords("_SegmentCoords", Vector) = (0.0, 0.0, 1.0, 1.0)
		_HighQualityMipMap("_HighQualityMipMap", Range(0,5)) = 0

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

			sampler2D _HeightMap0;
			sampler2D _HeightMap1;
			sampler2D _HeightMap2;

			sampler2D _SurfaceTexture0;
			sampler2D _SurfaceTexture1;
			sampler2D _SurfaceTexture2;

			float _HighQualityMipMap;

			float2 _MainPyramidCenterWorldSpace;
			float _MainPyramidLevelWorldSize;

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
				float shouldDiscardMarker;
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

			void vert( inout appdata_full v, out Input v2f_o) {
				float2 uv = v.texcoord.xy;
				float2 inSegmentSpaceUv = calculateInSegmentSpaceUv(uv);

				int levelIndex = _ThisLevelIndex;

				ELevelAndRingIndexes levelAndRingIndexes = make_ELevelAndRingIndexes(levelIndex, round(_HighQualityMipMap));//TODO
				ETerrainParameters terrainParameters = init_ETerrainParametersFromUniforms();
				EPerRingParameters perRingParameters = init_EPerRingParametersFromBuffers(levelAndRingIndexes, terrainParameters);
				ETerrainHeightCalculationOut terrainOut = calculateETerrainHeight2(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters, perRingParameters);

				v.vertex.y = - levelAndRingIndexes.levelIndex*0.00002f + terrainOut.finalHeight;

				v2f_o.inSegmentSpaceUv = inSegmentSpaceUv;
				v2f_o.uv = uv;
				v2f_o.usedMipMapLevel = _HighQualityMipMap + terrainOut.terrainMergingLerpParam;
				v2f_o.terrainMergingLerpParam = terrainOut.terrainMergingLerpParam;
				v2f_o.shouldDiscardMarker = terrainOut.shouldBeDiscarded ? 1 : 0;
				v2f_o.worldSpaceLocation = mainGlobalLevelUvSpaceToWorldSpace(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters);
			}

			float3 seedColorFrom(float value) {
				int seed = round(value);
				if (seed < 0.5) {
					return float3(1, 0, 0);
				}
				else if (seed < 1.5) {
					return float3(1, 1, 0);
				}
				else if (seed < 2.5) {
					return float3(0, 1, 0);
				}
				else if (seed < 3.5) {
					return float3(0, 1, 1);
				}
				else {
					return float3(0, 0, 1);
				}
			}

			float4 sampleSurfaceTexture(int level, float4 uv) { //TODO VERY UNOPTIMAL
				if (level == 0) {
					return tex2Dlod(_SurfaceTexture0, uv);
				}
				else if (level == 1) {
					return tex2Dlod(_SurfaceTexture1, uv);
				}
				else if (level == 2) {
					return tex2Dlod(_SurfaceTexture2, uv);
				}
				else {
					return 1000;
				}
			}

			float4 calculateESurfaceColor(float2 inSegmentSpaceUv, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters terrainParameters, float lod ) {
				int mainHeightTextureResolution = terrainParameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureResolution;
				float2 textureSamplingUv = frac(inSegmentSpaceUv + MainPyramidCenterUv(levelAndRingIndexes, terrainParameters));

				float2 sampleCenteredHighQualityUv = textureSamplingUv + 1.0/ (mainHeightTextureResolution * 2.0); //This is to align UV to sample center of heightmap pixels
				float4 currentColor = sampleSurfaceTexture(levelAndRingIndexes.levelIndex, float4(sampleCenteredHighQualityUv,0,lod));

				float4 secondChanceColor =sampleSurfaceTexture(1, float4(sampleCenteredHighQualityUv,0,lod));
				float secondChanceMarker = saturate(0.01+currentColor.a / 0.025);
				return lerp(secondChanceColor, currentColor, secondChanceMarker) ;
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

			float3 calculateNormal(float3 vertices[3]) {
				float3 u = vertices[1] - vertices[0];
				float3 v = vertices[2] - vertices[0];
				return normalize(cross(u, v));
			}

			struct TriangulatedSurfaceInfo {
				float3 worldNormal;
				InTriangleGridPosition gridPosition;
				float2 downLeftVerticleInSegmentSpaceUv;
			};

			TriangulatedSurfaceInfo make_TriangulatedSurfaceInfo(float3 worldNormal, InTriangleGridPosition gridPosition, float2 downLeftVerticleInSegmentSpaceUv) {
				TriangulatedSurfaceInfo o;
				o.worldNormal = worldNormal;
				o.gridPosition = gridPosition;
				o.downLeftVerticleInSegmentSpaceUv = downLeftVerticleInSegmentSpaceUv;
				return o;
			}

			TriangulatedSurfaceInfo CalculateTriangulatedSurfaceInfo(float2 inSegmentSpaceUv, ELevelAndRingIndexes levelAndRingIndexes, EPerRingParameters perRingParameters, ETerrainParameters terrainParameters) {
				float subRingMultiplier = pow(2, perRingParameters.highQualityMipMap);
				int gridResolution = terrainParameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureResolution / subRingMultiplier;
				float levelWorldSize = terrainParameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].levelWorldSize;
				float worldSpaceGridCellsLength = levelWorldSize / gridResolution;
				InTriangleGridPosition gridPosition = calculateInTriangleGridPosition(inSegmentSpaceUv, gridResolution);
				int2  squareIndex= gridPosition.squareIndex;

				float2 downLeftVerticleInSegmentSpaceUv = squareIndexToDownLeftVerticlePlateUv(squareIndex, gridResolution);

				float2 segmentSpaceUvOffsets[4] = { float2(0,0), float2(1.0f / gridResolution,0), float2(0,1.0f / gridResolution), float2(1.0f/gridResolution, 1.0/gridResolution) };
				float2 worldSpaceOffsetsDelta[4] = { float2(0,0), float2(worldSpaceGridCellsLength,0), float2(0, worldSpaceGridCellsLength), float2(worldSpaceGridCellsLength, worldSpaceGridCellsLength) };
				int triangleIndexes[3];
				if (gridPosition.isLowerTriangle) {
					triangleIndexes[0] = 0;
					triangleIndexes[1] = 1;
					triangleIndexes[2] = 3;
				}
				else {
					triangleIndexes[0] = 0;
					triangleIndexes[1] = 3;
					triangleIndexes[2] = 2;
				}
				
				float sampledHeights[3];
				float3 sampledPositions[3];
				for (int j = 0; j < 3; j++) {
					int triangleIndex = triangleIndexes[j];
					float2 sampleSegmentSpaceUv = (downLeftVerticleInSegmentSpaceUv+segmentSpaceUvOffsets[triangleIndex]);
					ETerrainHeightCalculationOut terrainOut = calculateETerrainHeight2(sampleSegmentSpaceUv, levelAndRingIndexes, terrainParameters, perRingParameters);
					float height = terrainOut.finalHeight * 2385; //TODO 2385 should got to same parameter
					sampledHeights[j] = height;
					sampledPositions[j] = float3(worldSpaceOffsetsDelta[triangleIndex].x,height, worldSpaceOffsetsDelta[triangleIndex].y);
				}
				float3 worldNormal = -calculateNormal(sampledPositions);

				return make_TriangulatedSurfaceInfo(worldNormal, gridPosition, downLeftVerticleInSegmentSpaceUv);
			}


			//Our Fragment Shader
			void surf(in Input i, inout SurfaceOutput o) {	//TODO add normals coloring
				if (i.shouldDiscardMarker > 0.5) {
					discard;
				}

				float4 finalColor;
				ETerrainParameters terrainParameters = init_ETerrainParametersFromUniforms();
				ELevelAndRingIndexes levelAndRingIndexes = FindLevelAndIndexFromWorldSpacePosition(i.worldSpaceLocation,terrainParameters);
				EPerRingParameters perRingParameters = init_EPerRingParametersFromBuffers(levelAndRingIndexes, terrainParameters);
				int levelIndex = levelAndRingIndexes.levelIndex;
				int ringIndex = levelAndRingIndexes.ringIndex;

				TriangulatedSurfaceInfo surfaceInfo = CalculateTriangulatedSurfaceInfo(i.inSegmentSpaceUv, levelAndRingIndexes, perRingParameters, terrainParameters);

				InTriangleGridPosition gridPosition = surfaceInfo.gridPosition;
				int2 squareIndex = gridPosition.squareIndex;
				if (gridPosition.isLowerTriangle) {
					squareIndex.x += 100000;
				}

				float surfaceColorLod = levelAndRingIndexes.ringIndex + i.terrainMergingLerpParam ;
				if (levelAndRingIndexes.levelIndex == 2) {
					surfaceColorLod += 1;
				}
				finalColor = calculateESurfaceColor(surfaceInfo.downLeftVerticleInSegmentSpaceUv, levelAndRingIndexes, terrainParameters, surfaceColorLod);

				float3 worldNormal = surfaceInfo.worldNormal;
				o.Albedo = finalColor;
				o.Normal = float3(worldNormal.x, -worldNormal.z, worldNormal.y);// mul((float3x3)unity_WorldToObject, float3(worldNormal)).zyx;


				//finalColor = 0;
				//if (levelIndex == 0) {
				//	finalColor = float4(1, 0, 0, 1);
				//}
				//else if (levelIndex == 1) {
				//	finalColor = float4(0, 1, 0, 1);
				//}
				//else {
				//	finalColor = float4(0, 0, 1, 1);
				//}

				//finalColor /= (ringIndex + 1.0f);
				//if (i.terrainMergingLerpParam > 0.02 && i.terrainMergingLerpParam < 0.98) {
				//	finalColor = float4(1, 1, 1, 1) *i.terrainMergingLerpParam;
				//}
				//o.Albedo = finalColor;
			} 

			ENDCG
		
	}
	FallBack "Diffuse"
}
