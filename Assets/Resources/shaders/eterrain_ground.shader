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

		_MainHeightMap("_MainHeightMap", 2D) = "pink" {}

		_MainPyramidCenterWorldSpace("MainPyramidCenterWorldSpace", Vector) = (0.0, 0.0, 0.0, 0.0)
		_MainPyramidLevelWorldSize( "MainPyramidLevelWorldSize", Float) = 1.0
		_AuxPyramidLevelWorldSize( "AuxPyramidLevelWorldSize", Float) = 1.0

		_SegmentCoords("_SegmentCoords", Vector) = (0.0, 0.0, 1.0, 1.0)
		_HeightMergeRange("_HeightMergeRange", Vector) = (0.0, 1.0, 0.0, 0.0)
		_HighQualityMipMap("_HighQualityMipMap", Range(0,5)) = 0

		_AuxHeightMap("_AuxHeightMap", 2D) = "pink" {}
		_AuxHeightMapMode("_AuxHeightMapMode", Int) = 0 // 0 - no aux; 1- is lower heightmap 2- is higher heightmap
		_HigherLevelAreaCutting("_HigherLevelAreaCutting", Int) = 0 // Cutting space in center of centerObjects when there is shape from higher object
		_AuxPyramidCenterWorldSpace("AuxPyramidCenterWorldSpace", Vector) = (0.0, 0.0, 0.0, 0.0)
		 _LastRingSegmentUvRange("LastRingSegmentUvRange", Vector) = (0.0, 0.0, 0.0, 0.0)

		_TravellerPositionWorldSpace("TravellerPositionWorldSpace", Vector) = (0.0, 0.0, 0.0, 0.0)

		_RingsPerLevelCount("RingsPerLevelCount", Int) = 3
		_LevelsCount("LevelsCount", Int) = 3
		_Debug("Debug", Range(0,1)) = 0
	}

	SubShader
	{
		Pass
		{
		Cull Off ZWrite On Fog { Mode Off } //Rendering settings
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc" 
			#include "common.txt"

			float4 _SegmentCoords;

			sampler2D _HeightMap0;
			sampler2D _HeightMap1;
			sampler2D _HeightMap2;

			sampler2D _SurfaceTexture0;
			sampler2D _SurfaceTexture1;
			sampler2D _SurfaceTexture2;

			sampler2D _MainHeightMap;
			float2 _HeightMergeRange;
			float _HighQualityMipMap;

			float2 _MainPyramidCenterWorldSpace;
			float _MainPyramidLevelWorldSize;

			sampler2D _AuxHeightMap;
			int _AuxHeightMapMode;
			float2 _AuxPyramidCenterWorldSpace;
			float _AuxPyramidLevelWorldSize;
			float2 _TravellerPositionWorldSpace;
			int _HigherLevelAreaCutting;

			float2 _LastRingSegmentUvRange;

			int _RingsPerLevelCount;
			int _LevelsCount;
			float _Debug;

			struct v2f {
				float4 pos : POSITION; // niezbedna wartosc by dzialal shader
				half2 inSegmentSpaceUv : TEXCOORD0;
				half2 uv : ANY_UV;
				float usedMipMapLevel : TEXCOORD1;
				float terrainMergingLerpParam : ANY_TERRAIN_MERGING_LERP_PARAM;
				float2 worldSpaceLocation : ANY_WSL;
				float2 debug : ANY_DEBUG;
				bool shouldDiscard : ANY_SHOULD_DISCARD;
			};

#include "eterrain_heightMapCommon.hlsl"
			StructuredBuffer<EPyramidConfiguration> _EPyramidConfigurationBuffer;
			StructuredBuffer<EPyramidPerFrameConfiguration> _EPyramidPerFrameConfigurationBuffer;

			ETerrainParameters init_ETerrainParametersFromUniforms() {
				ETerrainParameters p;
				p.pyramidConfiguration = _EPyramidConfigurationBuffer[0];
				p.perFrameConfiguration = _EPyramidPerFrameConfigurationBuffer[0];
				p.travellerPositionWorldSpace = _TravellerPositionWorldSpace;
				p.ringsPerLevelCount = _RingsPerLevelCount;
				p.levelsCount = _LevelsCount;
				return p;
			};

			EPerRingParameters init_EPerRingParametersFromUniforms() {
				EPerRingParameters o;
				o.auxHeightMapMode = (_AuxHeightMapMode > 0);//TODO
				o.highQualityMipMap = _HighQualityMipMap;
				o.higherLevelAreaCutting = _HigherLevelAreaCutting;
				return o;
			}

			v2f vert(appdata_img v) {
				v2f o;

				float2 uv = v.texcoord.xy;
				// UV IN RECTANGLE [{-L/2; -L/2} - {L/2; L/2}] where L - length of ceilTexture in worldSpace
				float2 inSegmentSpaceUv = _SegmentCoords.xy + float2(uv.x * _SegmentCoords.z, uv.y * _SegmentCoords.w);

				//TODO
				int levelIndex;
				if (_MainPyramidLevelWorldSize <= 541) {
					levelIndex = 0;
				}
				else if (_MainPyramidLevelWorldSize < 34550){
					levelIndex = 1;
				}
				else {
					levelIndex = 2;
				}

				ELevelAndRingIndexes levelAndRingIndexes = make_ELevelAndRingIndexes(levelIndex, round(_HighQualityMipMap));//TODO
				ETerrainParameters terrainParameters = init_ETerrainParametersFromUniforms();
				//EPerRingParameters perRingParameters = init_EPerRingParametersFromUniforms();
				EPerRingParameters perRingParameters = init_EPerRingParametersFromBuffers(levelAndRingIndexes, terrainParameters);
				ETerrainHeightCalculationOut terrainOut = calculateETerrainHeight2(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters, perRingParameters);

				v.vertex.y += terrainOut.finalHeight * 2385;
				o.pos = UnityObjectToClipPos(v.vertex); //niezbedna linijka by dzialal shader
				//terrainOut.finalHeight = 0;// sampleHeightMap(_Debug, float4(uv, 0, 0));
				//float4 vertexWorldPos =  mul(unity_ObjectToWorld , v.vertex);
				//vertexWorldPos.y = terrainOut.finalHeight*1000;
				//o.pos =  mul(UNITY_MATRIX_VP, float4(vertexWorldPos.xyz, 1.0));

				o.inSegmentSpaceUv = inSegmentSpaceUv;
				o.uv = uv;
				o.usedMipMapLevel = _HighQualityMipMap + terrainOut.terrainMergingLerpParam;

				o.terrainMergingLerpParam = terrainOut.terrainMergingLerpParam;
				o.shouldDiscard = terrainOut.shouldBeDiscarded;

				float2 worldSpaceLocation = mainGlobalLevelUvSpaceToWorldSpace(inSegmentSpaceUv, levelAndRingIndexes, terrainParameters);
				o.worldSpaceLocation = worldSpaceLocation;
				o.debug = uv;

				return o;
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

			float4 sampleSurfaceTexture(int level, float2 uv) { //TODO VERY UNOPTIMAL
				if (level == 0) {
					return tex2D(_SurfaceTexture0, uv);
				}
				else if (level == 1) {
					return tex2D(_SurfaceTexture1, uv).r;
				}
				else if (level == 2) {
					return tex2D(_SurfaceTexture2, uv).r;
				}
				else {
					return 1000;
				}
			}

			float4 calculateESurfaceColor(float2 inSegmentSpaceUv, ELevelAndRingIndexes levelAndRingIndexes, ETerrainParameters terrainParameters ) {
				int mainHeightTextureResolution = terrainParameters.pyramidConfiguration.levelsConfiguration[levelAndRingIndexes.levelIndex].ceilTextureResolution;
				float2 textureSamplingUv = frac(inSegmentSpaceUv + MainPyramidCenterUv(levelAndRingIndexes, terrainParameters));

				float2 sampleCenteredHighQualityUv = textureSamplingUv + 1.0/ (mainHeightTextureResolution * 2.0); //This is to align UV to sample center of heightmap pixels
				return sampleSurfaceTexture(levelAndRingIndexes.levelIndex, sampleCenteredHighQualityUv);
			}


#include "text_printing.hlsl"
			//Our Fragment Shader
			fixed4 frag(v2f i) : Color{
				if (i.shouldDiscard) {
					discard;
				}

				float3 lowColor = seedColorFrom(floor(i.usedMipMapLevel.x));
				float3 highColor = seedColorFrom(ceil(i.usedMipMapLevel.x));

				float3 finalColor;

				float2 localCircleCenter = (round(float2(i.inSegmentSpaceUv.x, i.inSegmentSpaceUv.y)*300))/300.0;
				float2 distanceTo = distance(localCircleCenter, i.inSegmentSpaceUv);

				float cutOffValue = distanceTo * 400;
				if (i.usedMipMapLevel.x < 0.01) {
					finalColor = lowColor;
				}else if (cutOffValue < frac(i.usedMipMapLevel.x)) {
					finalColor = highColor; 
				}
				else {
					finalColor = lowColor;
				}

				if (min(i.uv.x, i.uv.y) < 0.01) {
					finalColor = 1;
				}
				if (max(i.uv.x, i.uv.y) > 0.99) {
					finalColor = 1;
				}
				if (frac(i.usedMipMapLevel) > 0.0001 && frac(i.usedMipMapLevel) < 0.9999) {
					finalColor /= 4.0;
				}
				if (i.terrainMergingLerpParam.x > 0) {
					finalColor = float4(1, 0, 1, 1);
				}
				ETerrainParameters parameters = init_ETerrainParametersFromUniforms();
				ELevelAndRingIndexes levelAndRingIndexes = FindLevelAndIndexFromWorldSpacePosition(i.worldSpaceLocation, parameters);
				int levelIndex = levelAndRingIndexes.levelIndex;
				int ringIndex = levelAndRingIndexes.ringIndex;
				levelIndex = ringIndex;
				if (levelIndex == 0) {
					finalColor = float4(1, 0, 0, 1);
				}
				else if (levelIndex == 1) {
					finalColor = float4(0, 1, 0, 1);
				} else if (levelIndex == 2) {
					finalColor = float4(0, 0, 1, 1);
				}
				else {
					finalColor = float4(1, 1, 1, 1);
				}

				//if (i.terrainMergingLerpParam.x > 0.00001 && i.terrainMergingLerpParam.x < 0.99999) {
				//	finalColor = 1;
				//}
				finalColor = (sampleHeightMap(0, float4(frac(0.5 + i.uv), 0, 0)) - 0.23) / 0.07;

				return fixed4(finalColor,1);
			} 

			ENDCG
		}
	}
	FallBack "Diffuse"
}
