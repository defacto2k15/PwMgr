Shader "Custom/Hp/MM3DSeedsDebugShadows" {
	Properties{
		_TamIdTex("TamIdTex", 2D) = "black"{}
		_FragmentTexWidth("FragmentTexWidth", Range(0,512)) = 0
		_StrokeSeedGridMap("StrokeSeedGridMap", 2DArray) = "" {}
		_RotationSlicesCount("RotationSlicesCount", Int) = 16
		_BlockSize("BlockSize", Vector) = (32.0,32.0,0,0)

		_BlockCount("BlockCount", Vector) = (32.0,32.0,0,0)

		_DebugScalar("DebugScalar", Range(0,1)) = 1
		_DebugScalar2("DebugScalar2", Range(0,1)) = 0
		_ScreenCellHeightMultiplier("ScreenCellHeightMultiplier", Range(0,12)) = 6

		_SeedPositionTex3D("SeedPositionTex3D", 3D) = "" {}

		_MaximumPointToSeedDistance("MaximumPointToSeedDistance", Range(0,10)) = 1.0
		_SeedDensityF("SeedDensityF", Range(0,10)) = 1.0
		_MZoomPowFactor("MZoomPowFactor", Range(0,3)) = 0.7
		_SeedSamplingMultiplier("SeedSamplingMultiplier", Range(0,10)) = 1.0
		_VectorQuantCount("VectorQuantCount", Range(1,100)) = 8.0

		_DebugMParam("DebugMParam", Range(-10,5)) = 0.0
		_HatchLength("HatchLength", Range(0,128)) = 10
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
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma target 5.0
			#pragma multi_compile __ DEBUG_DRAW_AREAS DEBUG_DRAW_ANGLES DEBUG_DRAW_LIGHTING
			#pragma multi_compile __ MEASUREMENT
			#pragma multi_compile __ LIGHT_SHADING_ON
			#pragma multi_compile __ DIRECTION_PER_LIGHT
			
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "text_printing.hlsl"

#include "mm_common.txt"
#include "filling_variables.txt"

			#include "UnityLightingCommon.cginc"
#define CUSTOM_MATRIX_VP UNITY_MATRIX_VP

#include "filling_common.txt"
#include "filling_calculateSgmColor2.txt"
#include "mf_common.txt"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
				float4 tangent : TANGENT;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 worldSpacePos : ANY_WORLD_SPACE_POS;
				float2 uv : ANY_UV;
				float3 norm : ANY_NORM;
				float3 direction : ANY_DIRECTION;
				float4 _ShadowCoord : ANY_SHADOW_COORD;
			};

			struct MRTFragmentOutput
			{
				float4 dest0 : SV_Target0;	//artistic
				float4 dest1 : SV_Target1;	// float4(worldSpacePos, strokeAngle);
				float4 dest2 : SV_Target2;	// Vectors
#ifdef MEASUREMENT
				float4 dest3 : SV_Target3;	// HatchMain
				float4 dest4 : SV_Target4;	// HatchId
				float4 dest5 : SV_Target5;	// WorldPos1
				float4 dest6 : SV_Target6;	// WorldPos2
#endif
			};

			float _DebugScalar2;

			v2f vert(appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.worldSpacePos = mul(unity_ObjectToWorld, in_v.vertex);
				o.uv = in_v.uv;  
				o.norm = UnityObjectToWorldNormal(in_v.norm);
				o.direction = UnityObjectToWorldNormal(cross( in_v.tangent, in_v.norm));
				o._ShadowCoord = ComputeScreenPos(o.pos);

				return o; 
			}

			float4 debugSurfaceColor3d(float3 s_uv) {
				s_uv += floor(s_uv)*-1;
				float4 color = 0;

				if (min( fmod(s_uv.z/2,1), min(fmod(s_uv.x/2, 1), fmod(s_uv.y/2, 1))) < 0.01) {
					color -= float4(1, 1, 0, 1);
				}
				if (min( min(fmod(s_uv.x, 1), fmod(s_uv.y, 1)), fmod(s_uv.z, 1)) < 0.01) {
					color -= float4(0, 1, 0, 1);
				}

				int3 cUv = floor(s_uv * 4)%4;
				if ((cUv.x + cUv.y + cUv.z) % 2 == 0) {
					color += 1;
				}
				else {
					color += 0.5;
				}
				return color;
			}

			float3 generateRandomColor(int seed) {
				seed = seed + 10000000;;
				float3 colorsArray[64];
				for (int r = 0; r < 4; r++) {
					for (int g = 0; g < 4; g++) {
						for (int b = 0; b < 4; b++) {
							colorsArray[r + 4 * g + 16 * b] = float3(r / 3.0, g / 3.0, b / 3.0);
						}
					}
				}
				return colorsArray[seed %64 ];
			}

			float2 yAndPTo01(float2 yAndP) {
				float y1 = (yAndP.x / (2 * PI)) + 0.5;;
				float y2 = yAndP.y/2 + 0.5;
				return float2(y1, y2);
			}

			float calculateScreenStrokeAngle(float maxRotation, float3 ourWorldSpacePos, float3 worldSpaceStrokeDirection){
				float2 debPoint1 = worldSpacePosToScreenCoords(ourWorldSpacePos);
				float2 debPoint2 = worldSpacePosToScreenCoords((ourWorldSpacePos) + worldSpaceStrokeDirection);
				float2 strokeDirection =(debPoint2.xy - debPoint1.xy); //to rozwiązanie (a nie stare strokeDirection) daje troszkę lepsze wyniki

				strokeDirection = normalize(strokeDirection);

				return  fmod((atan2(strokeDirection.y, strokeDirection.x)) + maxRotation + PI, maxRotation);

				float2 v = normalize(mul((float3x3)UNITY_MATRIX_V, float3(0, -1, 0)).xy);// -- TODO alternatywa - opisz. Wszystkie stroke w jedną strone na obiekcie, ale nie ustawiają sie w linie
				return (-atan2(v.y, v.x))+PI;
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

			float calculateMZoomX(uint2 screenCoords, float3 worldSpacePos, float3 testVector1, float3 testVector2, ConstantParametersPack paramPack) {
				float mp = 0.0001;
				float a1 = length(worldSpacePosToScreenCoords(worldSpacePos) - worldSpacePosToScreenCoords(worldSpacePos + testVector1*mp));
				float a2 = length(worldSpacePosToScreenCoords(worldSpacePos) - worldSpacePosToScreenCoords(worldSpacePos + testVector2*mp));
				float a3 = length(worldSpacePosToScreenCoords(worldSpacePos) - worldSpacePosToScreenCoords(worldSpacePos + (testVector1+testVector2)*mp));
				float G = min(min(a1,a2),a3) / mp;
				G /= 100;

				float T = paramPack.gridSpaceMultiplier.z;
				T = 1;
				float F = paramPack.seedDensityF;
				float M = -log2( pow(G*T*F, paramPack.mZoomPowFactor));
				return M * _GlobalMMultiplier;
			}

			float2 calculateTwoSeedSpaceMultipliersX(uint2 screenCoords, float3 worldSpacePos, float3 alignmentVector, float3 crossAlignmentVector, ConstantParametersPack paramPack) {
				float M = calculateMZoomX(screenCoords, worldSpacePos, alignmentVector, crossAlignmentVector , paramPack);
				float rt = M - (-4.5); // FOr M = -4.5 rt = 0  For M = -2.5 rt = 2
				if (rt > 0) { // M > -4.5
					rt = pow(rt, _DebugScalar2);
				}
				M = rt - 4.5;

				float seedSpaceMultiplier = pow(2, -floor(M))*paramPack.seedSamplingMultiplier;
				float m = M - floor(M);

				return float2(seedSpaceMultiplier, m);
			}


			float2 calculateColor0PerTier(
				int tierIndex, float3 worldSpacePos, VectorsSetPair vPair, float strokeAngle, uint2 screenCoords, ConstantParametersPack paramPack) {
				float2 outColor0 = 0;

				float2 seedSpaceMultipliers = calculateTwoSeedSpaceMultipliers(tierIndex, screenCoords, worldSpacePos, vPair.genuine.alignmentVector, vPair.genuine.crossAlignmentVector, paramPack);
				float seedSpaceMultiplier = seedSpaceMultipliers.x;
				float3 GSSpacePos = moveToGSSpace(worldSpacePos, vPair.quantisized.normalVector, vPair.quantisized.alignmentVector, seedSpaceMultiplier, 0, paramPack);

				outColor0.xy += debugSurfaceColor3d(GSSpacePos) / 3.0;
				SSGMPixel pixel = calculateSgmPixel(0, worldSpacePos, vPair, seedSpaceMultiplier, strokeAngle, paramPack);

				float m = seedSpaceMultipliers.y;
				float ll = length(int2(pixel.screenCoords.xy) - int2(screenCoords));
				if (ll < 3) {
					float alpha = zoomHatchErasureAlphaMultiplier(pixel.id, pixel.seedIsInSparseLevelToo, m, paramPack);
					outColor0.xy = float2(0.7, 0.7);
				}


				return outColor0;
			}

			float3 projectVectorOntoPlane(float3 planeN, float3 u) {
				return u - (dot(u, planeN) / pow(length(planeN), 2)) * planeN;
			}

			MRTFragmentOutput	frag(v2f input) : SV_Target
			{
				ConstantParametersPack paramPack = createFromProperties_ConstantParametersPack();

				uint2 screenCoords = input.pos.xy;
				float3 worldSpacePos = input.worldSpacePos;

				float3 genuineAlignmentVector = (normalize(input.direction));
#if DIRECTION_PER_LIGHT
				float3 nDir = mf_getDirectionVector(input.worldSpacePos);
				genuineAlignmentVector = projectVectorOntoPlane(input.norm, nDir);
				genuineAlignmentVector = cross(input.norm, genuineAlignmentVector);
#endif

				//genuineAlignmentVector = mf_getDirectionVector(worldSpacePos);
				float3 genuineNormalVector = (normalize(input.norm));
				float3 genuineCrossAlignmentVector = normalize(cross(genuineAlignmentVector, genuineNormalVector));
				VectorsSet genuineVectorsSet = make_VectorsSet(genuineNormalVector, genuineAlignmentVector, genuineCrossAlignmentVector);

				int quantCount = paramPack.vectorQuantCount;
				float2 quantizationOffset = _DebugScalar;
				QuantizationResult quantizationResult = ProcessAndQuantisizeVectors(genuineVectorsSet, quantizationOffset, quantCount);
				VectorsSet quantisizedVectorsSet = quantizationResult.vectors;

				float2 screenUv = float2(screenCoords.x / _ScreenParams.x, screenCoords.y / _ScreenParams.y);

				float strokeAngle = calculateScreenStrokeAngle(paramPack.maxRotation, worldSpacePos, genuineVectorsSet.alignmentVector);
				//strokeAngle = calculateScreenStrokeAngle(paramPack.maxRotation, worldSpacePos, quantisizedVectorsSet.alignmentVector);

				float sAng =  strokeAngle - (PI*1.5);
				int ss = sign(sAng);
				sAng = pow(abs(sAng), 1);
				strokeAngle = PI * 1.5 + sAng * ss;

				float4 color0 = 0;
				float lightIntensity = SHADOW_ATTENUATION(input);

#ifdef  DEBUG_DRAW_AREAS
				color0.rg = calculateColor0PerTier( 0, worldSpacePos, make_VectorsSetPair(genuineVectorsSet, quantisizedVectorsSet), strokeAngle, screenCoords, paramPack);
				//color0.b = strokeAngle;
				//color0.g = lightIntensity;
#elif defined(DEBUG_DRAW_ANGLES)
				float tStrokeAngle = strokeAngle;
				tStrokeAngle /= (3.14 * 2);
				tStrokeAngle *= paramPack.rotationSlicesCount;
				int iStrokeAngle = round(tStrokeAngle);
				if (iStrokeAngle%2 == 0) {
					color0.r = 1;
				}
				else {
					color0.r = 0;
				}
#elif defined(DEBUG_DRAW_LIGHTING)
				color0.g = lightIntensity;
#else
				color0 = float4(0.6,0.6,1,1);
#endif

			//#pragma multi_compile __ DEBUG_DRAW_AREAS DEBUG_DRAW_ANGLES DEBUG_DRAW_LIGHTING


				MRTFragmentOutput output;
				output.dest0 =  color0;
				output.dest1 = float4(worldSpacePos, packLightIntensityAngleOccupancy( lightIntensity, strokeAngle, true));
				output.dest2.xy = PackNormalToTwoChannels(genuineVectorsSet.normalVector);
				output.dest2.zw = PackNormalToTwoChannels(genuineVectorsSet.alignmentVector);

				mf_retrivedHatchPixel dummyPixel = make_mf_retrivedHatchPixel(0, 0, false, 0, 0);
				mf_MRTFragmentOutput dummyFragmentOutput = retrivedPixelToFragmentOutput(dummyPixel, input.worldSpacePos, lightIntensity);
#ifdef MEASUREMENT
				output.dest3 = dummyFragmentOutput.dest1;
				output.dest4 = dummyFragmentOutput.dest2;
				output.dest5 = dummyFragmentOutput.dest3;  
				output.dest6 = dummyFragmentOutput.dest4;
#endif

				return output;
			}
			ENDCG
		}

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
