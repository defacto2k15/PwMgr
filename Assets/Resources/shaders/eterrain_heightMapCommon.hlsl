#ifndef ETERRAIN_HEIGHTMAPCOMMON_HLSL
#define ETERRAIN_HEIGHTMAPCOMMON_HLSL

#define MAX_RINGS_PER_LEVEL_COUNT (3)
#define MAX_LEVELS_COUNT (3)

#define MAX_CEIL_SLICES_COUNT (3)

			struct ERingConfiguration {
				float2 uvRange;
				float2 mergeRange;
			};

			ERingConfiguration null_ERingConfiguration( ){
				ERingConfiguration c;
				c.uvRange = 0;
				c.mergeRange = 0;
				return c;
			}

			struct ELevelConfiguration {
				ERingConfiguration ringsConfiguration[MAX_RINGS_PER_LEVEL_COUNT];
				float ceilTextureWorldSize;
				float ceilTextureResolution;
			};

			ELevelConfiguration null_ELevelConfiguration() {
				ELevelConfiguration c;
				for (int i = 0; i < MAX_RINGS_PER_LEVEL_COUNT; i++) {
					c.ringsConfiguration[i] = null_ERingConfiguration();
				}
				c.ceilTextureWorldSize = 0;
				c.ceilTextureResolution = 0;
				return c;
			}

			//struct ECeilSliceConfiguration {
			//	int2 segmentCount;
			//	float sliceWorldSize;
			//};


			struct EPyramidConfiguration {
				ELevelConfiguration levelsConfiguration[MAX_LEVELS_COUNT];
			};

			EPyramidConfiguration null_EPyramidConfiguration() {
				EPyramidConfiguration c;
				for (int i = 0; i < MAX_LEVELS_COUNT; i++) {
					c.levelsConfiguration[i] = null_ELevelConfiguration();
				}
				return c;
			}

			struct ELevelPerFrameConfiguration {
				float2 levelCenterWorldSpace;
			};

			ELevelPerFrameConfiguration null_ELevelPerFrameConfiguration()  {
				ELevelPerFrameConfiguration c;
				c.levelCenterWorldSpace = 0;
				return c;
			}

			struct EPyramidPerFrameConfiguration {
				ELevelPerFrameConfiguration levelConfiguration[MAX_LEVELS_COUNT];
			};

			EPyramidPerFrameConfiguration  null_EPyramidPerFrameConfiguration() {
				EPyramidPerFrameConfiguration c;
				for (int i = 0; i < MAX_LEVELS_COUNT; i++) {
					c.levelConfiguration[i] = null_ELevelPerFrameConfiguration();
				}
				return c;
			}

			struct ELevelAndRingIndexes {
				int levelIndex;
				int ringIndex;
			};

			ELevelAndRingIndexes make_ELevelAndRingIndexes ( int levelIndex, int ringIndex ){
				ELevelAndRingIndexes i;
				i.levelIndex = levelIndex;
				i.ringIndex = ringIndex;
				return i;
			}

			struct ETerrainParameters {
				EPyramidConfiguration pyramidConfiguration;
				EPyramidPerFrameConfiguration perFrameConfiguration;
				float2 travellerPositionWorldSpace;
				int ringsPerLevelCount;
				int levelsCount;
			};


			struct EPerRingParameters {
				int ringIndex; //todo remove
				bool auxHeightMapMode;
				bool higherLevelAreaCutting;
			};


			bool calculateAuxHeightMapMode(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters terrainParameters) {
				if (levelAndRingIndexes.levelIndex >= terrainParameters.levelsCount-1) {
					return false;
				}
				return levelAndRingIndexes.ringIndex == terrainParameters.ringsPerLevelCount - 1;
			}

			bool  calculateHigherLevelAreaCutting(ELevelAndRingIndexes levelAndRingIndexes) {
				if (levelAndRingIndexes.levelIndex == 0) {
					return false;
				}
				return levelAndRingIndexes.ringIndex == 0;
			}

			EPerRingParameters init_EPerRingParametersFromBuffers(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters terrainParameters){
				EPerRingParameters o;
				o.auxHeightMapMode = calculateAuxHeightMapMode(levelAndRingIndexes, terrainParameters);
				o.ringIndex = levelAndRingIndexes.ringIndex;
				o.higherLevelAreaCutting = calculateHigherLevelAreaCutting(levelAndRingIndexes);
				return o;
			}

			float2 worldSpaceToGlobalLevelUvSpace(float2 worldSpace, int levelIndex, ETerrainParameters parameters) {
				return (worldSpace - parameters.perFrameConfiguration.levelConfiguration[levelIndex].levelCenterWorldSpace)
					/ parameters.pyramidConfiguration.levelsConfiguration[levelIndex].ceilTextureWorldSize+ 0.5;
			}

			bool isInRectangle(float2 pos, float4 rect) {
				return pos.x >= rect.x && pos.y >= rect.y && pos.x <= (rect.x + rect.z) && pos.y <= (rect.y + rect.w);
			}

			ELevelAndRingIndexes FindLevelAndIndexFromWorldSpacePosition(float2 worldSpacePosition, ETerrainParameters parameters) {
				//parameters.levelsCount
				float4 ringsUvs[3] = { //TODO make configurable
					float4(0.5 - 1 / 12.0, 0.5 - 1 / 12.0, 1 / 6.0, 1 / 6.0),
					float4(0.5 - 2 / 12.0, 0.5 - 2 / 12.0, 2 / 6.0, 2 / 6.0),
					float4(0.5 - 4 / 12.0, 0.5 - 4 / 12.0, 4 / 6.0, 4 / 6.0)
				};

				for (int levelIndex = 0; levelIndex < min(parameters.levelsCount, MAX_LEVELS_COUNT); levelIndex++) {
					float2 levelUv = worldSpaceToGlobalLevelUvSpace(worldSpacePosition, levelIndex, parameters);
					for (int ringIndex = 0; ringIndex < min(parameters.ringsPerLevelCount, MAX_RINGS_PER_LEVEL_COUNT); ringIndex++) {
						if (isInRectangle(levelUv, ringsUvs[ringIndex])) {
							return make_ELevelAndRingIndexes(levelIndex, ringIndex);
						}
					}
				}
				return make_ELevelAndRingIndexes(99, 99);
			}

			int AuxLevelOffset(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				if (levelAndRingIndexes.ringIndex == 0) {
					return -1;
				}
				else if (levelAndRingIndexes.ringIndex == (parameters.ringsPerLevelCount- 1)) {
					return 1;
				}
				return 1000;
			}

			float2 MainPyramidCenterUv(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) { //todo delete
				float2  ceilTextureWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex].levelCenterWorldSpace;
				return (pyramidCenterWorldSize/ ceilTextureWorldSize) + 0.5;
			}

			float2 MainTravellerPositionUv(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				float2  ceilTextureWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureWorldSize;
				return (parameters.travellerPositionWorldSpace / ceilTextureWorldSize) + 0.5;
			}

			float2 AuxTravellerPositionUv(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				int auxLevelOffset = AuxLevelOffset(levelAndRingIndexes, parameters);
				float2  ceilTextureWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].ceilTextureWorldSize;
				return (parameters.travellerPositionWorldSpace / ceilTextureWorldSize) + 0.5;
			}

			float2 AuxPyramidCenterUv(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				int auxLevelOffset = AuxLevelOffset(levelAndRingIndexes, parameters);
				float2 ceilTextureWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].ceilTextureWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].levelCenterWorldSpace;
				return (pyramidCenterWorldSize/ ceilTextureWorldSize) + 0.5;
			}

			float2 auxGlobalLevelUvSpaceToWorldSpace(float2 auxGlobalLevelUv, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				int auxLevelOffset = AuxLevelOffset(levelAndRingIndexes, parameters);
				float ceilTextureWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].ceilTextureWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].levelCenterWorldSpace;
				float2 offset = (auxGlobalLevelUv-0.5) * ceilTextureWorldSize;
				return (pyramidCenterWorldSize+offset);
			}

			float2 mainGlobalLevelUvSpaceToWorldSpace(float2 auxGlobalLevelUv, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				float2 ceilTextureWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex].levelCenterWorldSpace;
				float2 offset = (auxGlobalLevelUv-0.5) * ceilTextureWorldSize;
				return (pyramidCenterWorldSize+offset);
			}

			float2 worldSpaceToMainGlobalLevelUvSpace(float2 worldSpace, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				float2 ceilTextureWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex].levelCenterWorldSpace;
				return (worldSpace - pyramidCenterWorldSize) / (ceilTextureWorldSize) + 0.5;
			}

			float2 worldSpaceToAuxGlobalLevelUvSpace(float2 worldSpace, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				int auxLevelOffset = AuxLevelOffset(levelAndRingIndexes, parameters);
				float2 ceilTextureWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].ceilTextureWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].levelCenterWorldSpace;
				return (worldSpace - pyramidCenterWorldSize) / (ceilTextureWorldSize) + 0.5;
			}

			struct ETerrainHeightCalculationOut {
				float finalHeight;
				float terrainMergingLerpParam;
				float shouldBeDiscardedMarker;
			};

			ETerrainHeightCalculationOut make_ETerrainHeightCalculationOut(
				float finalHeight,
				float terrainMergingLerpParam,
				float shouldBeDiscardedMarker
			) {
				ETerrainHeightCalculationOut o;
				o.finalHeight = finalHeight;
				o.terrainMergingLerpParam = terrainMergingLerpParam;
				o.shouldBeDiscardedMarker = shouldBeDiscardedMarker;
				return o;
			}

			float2 remapToMinusOneOne(float2 input) {
				return input * 2 - 1;
			}

			float2 remapToZeroOne(float2 input) {
				return (input +1) /2.0;
			}

			bool isValueInRange(float value, float2 range) {
				return value >= range.x && value <= range.y;
			}

			bool isAtLeastOneValueInRange(float2 values, float2 range) {
				return isValueInRange(values.x, range) || isValueInRange(values.y, range);
			}
			
			float sampleHeightMap(int level, float4 uv) { //TODO VERY UNOPTIMAL
#ifdef SHADER_API_D3D11  
				return _HeightMap.SampleLevel(sampler_HeightMap, float3(uv.xy, level), uv.w);
#else
				return UNITY_SAMPLE_TEX2DARRAY_LOD(_HeightMap, float3(uv.xy, level), uv.w);
#endif
			}

			float computeShouldVertexBeDiscardedMarker(float2 inSegmentSpaceUv, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters terrainParameters, EPerRingParameters perRingParameters) {
				if (perRingParameters.higherLevelAreaCutting) {
					float2 highLevelMinCornerInWS = auxGlobalLevelUvSpaceToWorldSpace(float2(1.0 / 6.0, 1.0 / 6.0) , levelAndRingIndexes, terrainParameters);
					float2 highLevelMaxCornerInWS = auxGlobalLevelUvSpaceToWorldSpace(1 - float2(1.0 / 6.0, 1.0 / 6.0), levelAndRingIndexes, terrainParameters);
					float2 thisLevelPositionInWS = mainGlobalLevelUvSpaceToWorldSpace(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters);

					float xMarker1 =   (thisLevelPositionInWS.x - highLevelMinCornerInWS.x);
					float xMarker2 = - (thisLevelPositionInWS.x - highLevelMaxCornerInWS.x);
					float yMarker1 =   (thisLevelPositionInWS.y - highLevelMinCornerInWS.y );
					float yMarker2 = - (thisLevelPositionInWS.y - highLevelMaxCornerInWS.y);

					return min(min(xMarker1, xMarker2), min(yMarker1, yMarker2));
				}
				return 0;
			}

			struct CeilSliceAndLerpParam {
				int ceilSlice;
				float lerpParam;
			};

			CeilSliceAndLerpParam make_CeilSliceAndLerpParam  ( int ceilSlice, float lerpParam ){
				CeilSliceAndLerpParam p;
				p.ceilSlice = ceilSlice;
				p.lerpParam = lerpParam;
				return p;
			}

			struct CeilSliceAndMipmap {
				int slice;
				int mipmap;
			};

			CeilSliceAndMipmap make_CeilSliceAndMipmap( int slice, int mipmap){
				CeilSliceAndMipmap c;
				c.slice = slice;
				c.mipmap = mipmap;
				return c;
			}

			struct CeilTextureSamplingQuery {
				CeilSliceAndMipmap ceilSliceAndMipmap;
				float2 uv;
			};

			CeilTextureSamplingQuery make_CeilTextureSamplingQuery( CeilSliceAndMipmap ceilSliceAndMipmap, float2 uv){
				CeilTextureSamplingQuery q;
				q.ceilSliceAndMipmap = ceilSliceAndMipmap;
				q.uv = uv;
				return q;
			}

			float sampleHeightTextureWithQuery(CeilTextureSamplingQuery query) {
				return sampleHeightMap(query.ceilSliceAndMipmap.slice, float4(query.uv, 0, query.ceilSliceAndMipmap.mipmap));
			}

			float2 alignUvToPixelCenter(float2 textureSamplingUv, int levelIndex, int ringIndex, ETerrainParameters terrainParameters, EPerRingParameters perRingParameters) {
				int textureResolution = terrainParameters.pyramidConfiguration.levelsConfiguration[levelIndex].ceilTextureResolution;
				return textureSamplingUv + pow(2, ringIndex) / (textureResolution * 2.0); 
			}

			float2 MainPyramidCenterUv2(int sliceIndex, ETerrainParameters parameters) {
				float2  ceilTextureWorldSize = parameters.pyramidConfiguration.levelsConfiguration[sliceIndex].ceilTextureWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[sliceIndex].levelCenterWorldSpace;
				return (pyramidCenterWorldSize/ ceilTextureWorldSize) + 0.5;
			}

			float sampleHeightTextureComplex(float2 inSegmentSpaceUv, CeilSliceAndMipmap ceilSliceAndMipmap,  ETerrainParameters terrainParameters, EPerRingParameters perRingParameters) {
				float2 textureSamplingUv = frac(inSegmentSpaceUv + MainPyramidCenterUv2(ceilSliceAndMipmap.slice, terrainParameters));
				CeilTextureSamplingQuery  query = make_CeilTextureSamplingQuery(
					ceilSliceAndMipmap,
					alignUvToPixelCenter(textureSamplingUv, ceilSliceAndMipmap.slice,  ceilSliceAndMipmap.mipmap, terrainParameters, perRingParameters)
				);
				return sampleHeightTextureWithQuery(query);
			}

			float ComputeLerpParam(float2 inSegmentSpaceUv, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters terrainParameters, EPerRingParameters perRingParameters) {
				float2 pyramidLevelSpaceUv = inSegmentSpaceUv + (MainPyramidCenterUv( levelAndRingIndexes, terrainParameters) - 0.5);
				float2 transitionRange = terrainParameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ringsConfiguration[levelAndRingIndexes.ringIndex].mergeRange;
				float fromCenterDistance = max(abs(pyramidLevelSpaceUv.x - MainTravellerPositionUv( levelAndRingIndexes, terrainParameters).x), abs(pyramidLevelSpaceUv.y - MainTravellerPositionUv( levelAndRingIndexes, terrainParameters).y));
				fromCenterDistance *= 2; // to make fromCenterDistance seem like uv is from -1 to 1
				float lerpParam = invLerp(transitionRange.x, transitionRange.y, fromCenterDistance);

				return lerpParam;
			}

			ETerrainHeightCalculationOut calculateETerrainHeight2(float2 inSegmentSpaceUv, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters terrainParameters, EPerRingParameters perRingParameters) {
				float lerpParam = ComputeLerpParam(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters, perRingParameters);

				float highQualityHeight = sampleHeightTextureComplex(inSegmentSpaceUv,
					make_CeilSliceAndMipmap(levelAndRingIndexes.levelIndex, perRingParameters.ringIndex), terrainParameters, perRingParameters);

				float lowQualityHeight = -100;

				bool areWeInLastRing = perRingParameters.auxHeightMapMode; 
				if (!areWeInLastRing ) {
					lowQualityHeight = sampleHeightTextureComplex(inSegmentSpaceUv, make_CeilSliceAndMipmap(levelAndRingIndexes.levelIndex, perRingParameters.ringIndex+1), terrainParameters, perRingParameters);
				}
				else{ // we are in biggest (LAST) ring
					bool areWeInLastLevel = (levelAndRingIndexes.levelIndex +1) == terrainParameters.levelsCount;
					if (areWeInLastLevel) {
						lerpParam = 0;
					}
					else {
						float2 globalWorldSpace = mainGlobalLevelUvSpaceToWorldSpace(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters);
						float2 auxGlobalLevelUvSpace = worldSpaceToAuxGlobalLevelUvSpace(globalWorldSpace, levelAndRingIndexes, terrainParameters);
						lowQualityHeight = sampleHeightTextureComplex(auxGlobalLevelUvSpace, make_CeilSliceAndMipmap(levelAndRingIndexes.levelIndex+1, 0), terrainParameters, perRingParameters);
					}
				}

				float finalHeight =lerp(highQualityHeight, lowQualityHeight, lerpParam);

				float shouldBeDiscardedMarker = computeShouldVertexBeDiscardedMarker(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters, perRingParameters);
				if (shouldBeDiscardedMarker > 0) {
					finalHeight = highQualityHeight;
				}
				return  make_ETerrainHeightCalculationOut(finalHeight, lerpParam, shouldBeDiscardedMarker);
			}

#endif
