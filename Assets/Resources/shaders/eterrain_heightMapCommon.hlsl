#ifndef ETERRAIN_HEIGHTMAPCOMMON_HLSL
#define ETERRAIN_HEIGHTMAPCOMMON_HLSL

#define MAX_RINGS_PER_LEVEL_COUNT (3)
#define MAX_LEVELS_COUNT (3)

			struct ERingConfiguration {
				float2 uvRange;
				float2 mergeRange;
			};

			struct ELevelConfiguration {
				ERingConfiguration ringsConfiguration[MAX_RINGS_PER_LEVEL_COUNT];
				float levelWorldSize;
				float ceilTextureResolution;
			};

			struct EPyramidConfiguration {
				ELevelConfiguration levelsConfiguration[MAX_LEVELS_COUNT];
			};

			struct ELevelPerFrameConfiguration {
				float2 levelCenterWorldSpace;
			};

			struct EPyramidPerFrameConfiguration {
				ELevelPerFrameConfiguration levelConfiguration[MAX_LEVELS_COUNT];
			};

			struct ELevelAndRingIndexes {
				int levelIndex;
				int ringIndex;
			};

			ELevelAndRingIndexes make_ELevelAndRingIndexes (
				int levelIndex,
				int ringIndex
			){
				ELevelAndRingIndexes i;
				i.levelIndex = levelIndex;
				i.ringIndex = ringIndex;
				return i;
			};

			struct ETerrainParameters {
				EPyramidConfiguration pyramidConfiguration;
				EPyramidPerFrameConfiguration perFrameConfiguration;
				float2 travellerPositionWorldSpace;
				int ringsPerLevelCount;
				int levelsCount;
			};


			struct EPerRingParameters {
				bool auxHeightMapMode;
				int highQualityMipMap;
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

			struct  EPerRingParameters init_EPerRingParametersFromBuffers(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters terrainParameters){
				EPerRingParameters o;
				o.auxHeightMapMode = calculateAuxHeightMapMode(levelAndRingIndexes, terrainParameters);
				o.highQualityMipMap = levelAndRingIndexes.ringIndex;
				o.higherLevelAreaCutting = calculateHigherLevelAreaCutting(levelAndRingIndexes);
				return o;
			}

			float2 worldSpaceToGlobalLevelUvSpace(float2 worldSpace, int levelIndex, ETerrainParameters parameters) {
				return (worldSpace - parameters.perFrameConfiguration.levelConfiguration[levelIndex].levelCenterWorldSpace)
					/ parameters.pyramidConfiguration.levelsConfiguration[levelIndex].levelWorldSize+ 0.5;
			}

			bool isInRectangle(float2 pos, float4 rect) {
				return pos.x >= rect.x && pos.y >= rect.y && pos.x <= (rect.x + rect.z) && pos.y <= (rect.y + rect.w);
			}

			ELevelAndRingIndexes FindLevelAndIndexFromWorldSpacePosition(float2 worldSpacePosition, ETerrainParameters parameters) {
				float4 ringsUvs[3] = { //TODO make configurable
					float4(0.5 - 1 / 12.0, 0.5 - 1 / 12.0, 1 / 6.0, 1 / 6.0),
					float4(0.5 - 2 / 12.0, 0.5 - 2 / 12.0, 2 / 6.0, 2 / 6.0),
					float4(0.5 - 4 / 12.0, 0.5 - 4 / 12.0, 4 / 6.0, 4 / 6.0)
				};

				[unroll(MAX_LEVELS_COUNT)]
				for (int levelIndex = 0; levelIndex < parameters.levelsCount; levelIndex++) {
					float2 levelUv = worldSpaceToGlobalLevelUvSpace(worldSpacePosition, levelIndex, parameters);
					[unroll(MAX_RINGS_PER_LEVEL_COUNT)]
					for (int ringIndex = 0; ringIndex < parameters.ringsPerLevelCount; ringIndex++) {
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

			float2 MainPyramidCenterUv(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				float2  levelWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].levelWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex].levelCenterWorldSpace;
				return (pyramidCenterWorldSize/ levelWorldSize) + 0.5;
			}

			float2 MainTravellerPositionUv(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				float2  levelWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].levelWorldSize;
				return (parameters.travellerPositionWorldSpace / levelWorldSize) + 0.5;
			}

			float2 AuxTravellerPositionUv(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				int auxLevelOffset = AuxLevelOffset(levelAndRingIndexes, parameters);
				float2  levelWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].levelWorldSize;
				return (parameters.travellerPositionWorldSpace / levelWorldSize) + 0.5;
			}

			float2 AuxPyramidCenterUv(ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				int auxLevelOffset = AuxLevelOffset(levelAndRingIndexes, parameters);
				float2 levelWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].levelWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].levelCenterWorldSpace;
				return (pyramidCenterWorldSize/ levelWorldSize) + 0.5;
			}

			float2 auxGlobalLevelUvSpaceToWorldSpace(float2 auxGlobalLevelUv, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				int auxLevelOffset = AuxLevelOffset(levelAndRingIndexes, parameters);
				float levelWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].levelWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].levelCenterWorldSpace;
				float2 offset = (auxGlobalLevelUv-0.5) * levelWorldSize;
				return (pyramidCenterWorldSize+offset);
			}

			float2 mainGlobalLevelUvSpaceToWorldSpace(float2 auxGlobalLevelUv, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				float2 levelWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].levelWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex].levelCenterWorldSpace;
				float2 offset = (auxGlobalLevelUv-0.5) * levelWorldSize;
				return (pyramidCenterWorldSize+offset);
			}

			float2 worldSpaceToMainGlobalLevelUvSpace(float2 worldSpace, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				float2 levelWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].levelWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex].levelCenterWorldSpace;
				return (worldSpace - pyramidCenterWorldSize) / (levelWorldSize) + 0.5;
			}

			float2 worldSpaceToAuxGlobalLevelUvSpace(float2 worldSpace, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters parameters) {
				int auxLevelOffset = AuxLevelOffset(levelAndRingIndexes, parameters);
				float2 levelWorldSize = parameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].levelWorldSize;
				float2 pyramidCenterWorldSize = parameters.perFrameConfiguration.levelConfiguration[levelAndRingIndexes.levelIndex+auxLevelOffset].levelCenterWorldSpace;
				return (worldSpace - pyramidCenterWorldSize) / (levelWorldSize) + 0.5;
			}

			struct ETerrainHeightCalculationOut {
				float finalHeight;
				float terrainMergingLerpParam;
				bool shouldBeDiscarded;
			};

			ETerrainHeightCalculationOut make_ETerrainHeightCalculationOut(
				float finalHeight,
				float terrainMergingLerpParam,
				bool shouldBeDiscarded
			) {
				ETerrainHeightCalculationOut o;
				o.finalHeight = finalHeight;
				o.terrainMergingLerpParam = terrainMergingLerpParam;
				o.shouldBeDiscarded = shouldBeDiscarded;
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
				if (level == 0) {
					return tex2Dlod(_HeightMap0, uv).r;
				}
				else if (level == 1) {
					return tex2Dlod(_HeightMap1, uv).r;
				}
				else if (level == 2) {
					return tex2Dlod(_HeightMap2, uv).r;
				}
				else {
					return 1000;
				}
			}

			ETerrainHeightCalculationOut calculateETerrainHeight2(float2 inSegmentSpaceUv, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters terrainParameters, EPerRingParameters perRingParameters) {
				int mainHeightTextureResolution = terrainParameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureResolution;
				float2 textureSamplingUv = frac(inSegmentSpaceUv + MainPyramidCenterUv(levelAndRingIndexes, terrainParameters));

				float2 sampleCenteredHighQualityUv = textureSamplingUv + pow(2, perRingParameters.highQualityMipMap) / (mainHeightTextureResolution * 2.0); //This is to align UV to sample center of heightmap pixels
				float highQualityHeight = sampleHeightMap(levelAndRingIndexes.levelIndex, float4(sampleCenteredHighQualityUv, 0, perRingParameters.highQualityMipMap));

				float lowQualityHeight = -100;

				bool areWeInLastRing = perRingParameters.auxHeightMapMode; 
				if (!areWeInLastRing ) {
					int auxHeightTextureResolution = terrainParameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureResolution;
					float2 sampleCenteredLowQualityUv = textureSamplingUv + pow(2,  perRingParameters.highQualityMipMap + 1) / (auxHeightTextureResolution * 2.0);
					lowQualityHeight = sampleHeightMap(levelAndRingIndexes.levelIndex, float4(sampleCenteredLowQualityUv, 0,  perRingParameters.highQualityMipMap+ 1));
				}
				else{ // we are in biggest (LAST) ring
					bool areWeInLastLevel = (levelAndRingIndexes.levelIndex +1) == terrainParameters.levelsCount;
					if (areWeInLastLevel) {
						lowQualityHeight = highQualityHeight;
					}
					else {
						float2 globalWorldSpace = mainGlobalLevelUvSpaceToWorldSpace(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters);
						float2 auxGlobalLevelUvSpace = worldSpaceToAuxGlobalLevelUvSpace(globalWorldSpace, levelAndRingIndexes, terrainParameters);
						float2 auxLevelUvSpace = frac(auxGlobalLevelUvSpace + AuxPyramidCenterUv(levelAndRingIndexes, terrainParameters));
						float2 samplingCenteredSamplingUv = auxLevelUvSpace + 1.0 / (mainHeightTextureResolution * 2.0); //This is to align UV to sample center of heightmap pixels
						lowQualityHeight = sampleHeightMap(levelAndRingIndexes.levelIndex + AuxLevelOffset(levelAndRingIndexes, terrainParameters), float4(samplingCenteredSamplingUv, 0, 0));
					}
				}

				float2 pyramidLevelSpaceUv = inSegmentSpaceUv + (MainPyramidCenterUv( levelAndRingIndexes, terrainParameters) - 0.5);
				float2 transitionRange = terrainParameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ringsConfiguration[levelAndRingIndexes.ringIndex].mergeRange;
				float fromCenterDistance = max(abs(pyramidLevelSpaceUv.x - MainTravellerPositionUv( levelAndRingIndexes, terrainParameters).x), abs(pyramidLevelSpaceUv.y - MainTravellerPositionUv( levelAndRingIndexes, terrainParameters).y));
				fromCenterDistance *= 2; // to make fromCenterDistance seem like uv is from -1 to 1
				float lerpParam = invLerp(transitionRange.x, transitionRange.y, fromCenterDistance);
				lerpParam = 0;
				float finalHeight =lerp(highQualityHeight, lowQualityHeight, lerpParam);

				bool shouldBeDiscarded = false;
				if (perRingParameters.higherLevelAreaCutting) {
					float addition = 1/240.0;
					float2 highLevelMinCornerInWS = auxGlobalLevelUvSpaceToWorldSpace(float2(1.0 / 6.0, 1.0 / 6.0) + addition, levelAndRingIndexes, terrainParameters);
					float2 highLevelMaxCornerInWS = auxGlobalLevelUvSpaceToWorldSpace(1 - float2(1.0 / 6.0, 1.0 / 6.0), levelAndRingIndexes, terrainParameters);
					float2 thisLevelPositionInWS = mainGlobalLevelUvSpaceToWorldSpace(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters);

					if (thisLevelPositionInWS.x > highLevelMinCornerInWS.x && thisLevelPositionInWS.x < highLevelMaxCornerInWS.x) {
						if (thisLevelPositionInWS.y > highLevelMinCornerInWS.y && thisLevelPositionInWS.y < highLevelMaxCornerInWS.y) {
							finalHeight = highQualityHeight;
							shouldBeDiscarded =true;
						}
					}
				}
				//finalHeight = highQualityHeight;
				return  make_ETerrainHeightCalculationOut(finalHeight, lerpParam, shouldBeDiscarded);
			}

#endif
