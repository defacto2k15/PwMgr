Shader "Custom/Presentation/MMOnePointRenderer" {
	Properties{
		_StrokeSeedGridMap("StrokeSeedGridMap", 2DArray) = "blue"{}
		_MainTex ("", any) = "" {}
		_StrokeTex("StrokeTex", 2D) = "blue"{}
		_DebugScalar("DebugScalar", Range(0,1)) = 0
		_ArcCurvatureMargin("ArcCurvatureMargin", Range(0,1)) = 0.1
		_HatchLength("HatchLength", Range(0,128)) = 10
		_DistanceLinkingMargin("DistanceLinkingMargin", Range(0,10)) = 5
		_RotationSliceIndex("RotationSliceIndex",Range(0,16)) = 0

		_RotationSlicesCount("RotationSlicesCount", Int) = 16
		_BlockSize("BlockSize", Vector) = (32.0,32.0,0,0)
		_ScreenCellHeightMultiplier("ScreenCellHeightMultiplier", Range(0,16)) = 1

		_BlockCount("BlockCount", Vector) = (32.0,32.0,0,0)

		_WorldPositionTex("WorldPositionTex", 2D) = "blue"{}
		_NormalTex("NormalTex", 2D) = "blue"{}
		_AngleTex("AngleTex", 2D) = "blue"{}
		_SeedPositionTex3D("SeedPositionTex3D", 3D) = "" {}

		_MaximumPointToSeedDistance("MaximumPointToSeedDistance", Range(0,10)) = 1.0
		_SeedDensityF("SeedDensityF", Range(0,10)) = 1.0
		_MZoomPowFactor("MZoomPowFactor", Range(0,3)) = 0.7

		_SeedSamplingMultiplier("SeedSamplingMultiplier", Range(0,10)) = 1.0

		_DistanceToLineTreshold("DistanceToLineTreshold", Range(0,5)) = 2.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
		// THIS SHADER TAKES AGGREGATE TEXTURE AND SOLVES THE COEFFICIENTS
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#include "UnityCG.cginc"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : ANY_UV;
			};

			//sampler2D _StrokeSeedGridMap;
			Texture2DArray _StrokeSeedGridMap;
			sampler2D _MainTex;
			sampler2D _StrokeTex;
			float _DebugScalar;
			float _ArcCurvatureMargin;
			float _HatchLength;
			float  _DistanceLinkingMargin;
			int _RotationSliceIndex;

			int _RotationSlicesCount;
			float2 _BlockSize;
			float2 _BlockCount;
			float _ScreenCellHeightMultiplier;

			sampler2D _WorldPositionTex;
			sampler2D _NormalTex;
			sampler2D _AngleTex;
			Texture3D<float4> _SeedPositionTex3D;
			float _MaximumPointToSeedDistance;
			float4x4 _MyUnityMatrixVP;
			float _SeedSamplingMultiplier;
			float _MZoomPowFactor;
			float _SeedDensityF;

			float _DistanceToLineTreshold;

			static float PI = 3.14159;

#define TWO_SEEDS 1
#define USE_LINKING 1

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				return o;
			}


			uint2 uv_to_intScreenCoords(float2 uv) {
				return uint2(floor(uv.x * _ScreenParams.x), floor(uv.y * _ScreenParams.y));
			}

			float2 intScreenCoords_to_uv(int2 coords) {
				return float2(coords.x / _ScreenParams.x, coords.y / _ScreenParams.y);
			}

			float2 intScreenCoords_to_sampleuv(int2 coords) {
				return float2((0.5+coords.x) / _ScreenParams.x, (0.5+coords.y) / _ScreenParams.y);
			}

			uint2 uint2Multiply(uint2 a, uint2 b) {
				return uint2(a.x*b.x, a.y*b.y);
			}

			int2 int2Multiply(int2 a, int2 b) {
				return int2(a.x*b.x, a.y*b.y);
			}

			/////////// SEEED CALCULATION LOL
			////////////////////////////////////////////////////////////////////////////////
			////////////////////////////////////////////////////////////////////////////////
			////////////////////////////////////////////////////////////////////////////////

			float myMod1_f(float x) {
				float a = frac(x);
				if (a < 0) {
					return -1;
				}
				else {
					return a;
				}
			}

			float2 myMod1_f2(float2 x) {
				return float2(myMod1_f(x.x), myMod1_f(x.y));
			}

			float3 myMod1_f3(float3 x) {
				return float3(myMod1_f(x.x), myMod1_f(x.y), myMod1_f(x.z));
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


			struct ClosestSeedSpecification {
				float3 position;
				bool seedIsInSparseLevelToo;
				bool isActive;
			};

			ClosestSeedSpecification make_ClosestSeedSpecification( float3 position, bool seedIsInSparseLevelToo, bool isActive ){
				ClosestSeedSpecification s;
				s.position = position;
				s.seedIsInSparseLevelToo = seedIsInSparseLevelToo;
				s.isActive = isActive;
				return s;
			}

			ClosestSeedSpecification retriveClosestSeed(float3 s_uv) { // s_uv - nonRepeatable, 0-1 in 4x4
				int3 bigBlockCoords = floor(s_uv); //  nonRepeatable, 0-1 in 4x4
				float3 positive_s_uv = myMod1_f3(s_uv);

				float3 inRepBlockUv = myMod1_f3(s_uv / 2); // repeatable 0-1 in 8x8
				uint3 cellCoords = floor(positive_s_uv * 4) % 4; // repetable, 0-3 in 4x4
				float3 inCellUv = frac(positive_s_uv * 4); // repeatable, 0-1 in 1x1

				int3 downBottomLeftCellOffset = floor( inCellUv - 0.5);
				//downBottomLeftCellOffset = 0;

				ClosestSeedSpecification closestSeed = make_ClosestSeedSpecification(0,0,false);
				for (int x = 0; x < 2; x++) {
					for (int y = 0; y < 2; y++) {
						for (int z = 0; z < 1; z++) { // HERE was 2 once
							int3 baseBlockCoords = cellCoords + downBottomLeftCellOffset + uint3(x, y, z);

							uint3 seed2BlockCoords = (baseBlockCoords+4) % 4;
							float4 seedPosSample = _SeedPositionTex3D[seed2BlockCoords];

							float3 seed2Offset = seedPosSample.xyz;
							float3 seed2Position = seed2Offset / 4.0 +  baseBlockCoords / 4.0 + bigBlockCoords;

							float cycleFloat = seedPosSample.w;
							int cycleInt = round(cycleFloat *  7.0);
							uint3 cycleLastBits = 0;
							cycleLastBits[2] = floor( (cycleInt%8)/4.0);
							cycleLastBits[1] = floor( (cycleInt%4)/2.0);
							cycleLastBits[0] = floor( (cycleInt%2)/1.0);

							float3 positive_seed2Position = myMod1_f3(seed2Position/2)*2; // repeatable 0-2 values in 8x8
							int3 w2 = ( floor(positive_seed2Position)) % 2;
							bool seedIsInSparseLevelToo =
									(w2.x == cycleLastBits.x % 2) && (w2.y == cycleLastBits.y % 2) && (w2.z == cycleLastBits.z % 2);

							if ( !closestSeed.isActive || ( length(s_uv - seed2Position) < length(s_uv - closestSeed.position) )) {
								closestSeed = make_ClosestSeedSpecification(seed2Position, seedIsInSparseLevelToo, true);
							}
						}
					}
				}
				return closestSeed;
			}

			float2 worldSpacePosToScreenCoords(float3 worldSpacePos) {
				float4 clipPos = mul(_MyUnityMatrixVP, float4(worldSpacePos, 1.0));
				float2 clipPosNorm= (clipPos / clipPos.w).xy;
				clipPosNorm = (clipPosNorm + 1) / 2;
				clipPosNorm.y = 1 - clipPosNorm.y;
				return float2(clipPosNorm.x * _ScreenParams.x, clipPosNorm.y * _ScreenParams.y);
			}

			float3 projectPointOnPlane(float3 origin, float3 normal, float3 p) {
				float3 v = p - origin;
				float dist = dot(v, normal);
				return p - dist * normal;
			}



			float2 rotateAroundPivot(float2 p, float angle, float2 pivot) {
				angle = -angle;
				float sinX = sin (angle);
				float cosX = cos (angle);
				float2x2 rotationMatrix = float2x2( cosX, -sinX, sinX, cosX);

				return mul(rotationMatrix, p-pivot) + pivot;
			}

			float computeSliceRotationAngle(int rotationSliceIndex, float rotationSliceLength, float rotationSlicesCount) {
				return ((rotationSliceIndex + (rotationSlicesCount/4)) % rotationSlicesCount) * rotationSliceLength;
			}

			struct RAScreenInfo {
				int2 RABlockCoords;
				float2 RAScreenCoords;
				uint2 RAInBlockCoords;
			};

			RAScreenInfo make_RAScreenInfo(int2 RABlockCoords, float2 RAScreenCoords, uint2 RAInBlockCoords) {
				RAScreenInfo p;
				p.RABlockCoords = RABlockCoords;
				p.RAScreenCoords = RAScreenCoords;
				p.RAInBlockCoords = RAInBlockCoords;
				return p;
			}

			RAScreenInfo calculateRAScreenInfo(uint2 screenCoords, uint2 blockCount, uint2 blockSize, int rotationSliceIndex, float rotationSliceLength,
					int rotationSlicesCount) {
				float sliceRotationAngle = computeSliceRotationAngle(rotationSliceIndex, rotationSliceLength, rotationSlicesCount);
				// looping negative coords
				float2 RAScreenCoords = rotateAroundPivot(screenCoords, sliceRotationAngle, _ScreenParams.xy / 2);
				int2 RABlockCoords = int2(floor(RAScreenCoords.x / (float)blockSize.x), floor(RAScreenCoords.y / (float)blockSize.y));
				RABlockCoords = (RABlockCoords + blockCount * 2) % blockCount;
				RAScreenCoords = float2(
					fmod(RAScreenCoords.x + blockCount.x*blockSize.x, blockCount.x*blockSize.x),
					fmod(RAScreenCoords.y + blockCount.y*blockSize.y, blockCount.y*blockSize.y));

				int2 RAInBlockCoords =  RAScreenCoords - int2(RABlockCoords.x*blockSize.x, RABlockCoords.y*blockSize.y);

				return make_RAScreenInfo(RABlockCoords, RAScreenCoords, RAInBlockCoords);
			}

			bool shouldUpdateSSGM(RAScreenInfo info, uint2 blockCount, uint2 blockSize, float2 seedPosition, float sliceRotationAngle) {
				if (true /*|| abs(info.RAInBlockCoords.x - blockSize.x/2) < 2 && abs(info.RAInBlockCoords.y - blockSize.y/2) < 2*/) { // HERE IS MODIFICATION

					float2 RASeedPosition = rotateAroundPivot(seedPosition, sliceRotationAngle, _ScreenParams.xy / 2);

					// looping negative coords
					RASeedPosition = float2(
						fmod(RASeedPosition.x + blockCount.x*blockSize.x, blockCount.x*blockSize.x),
						fmod(RASeedPosition.y + blockCount.y*blockSize.y, blockCount.y*blockSize.y));

					int2 seedBlockCoords =
						int2(floor(RASeedPosition.x / (float)blockSize.x), floor(RASeedPosition.y / (float)blockSize.y));

					if (seedBlockCoords.x == info.RABlockCoords.x && seedBlockCoords.y == info.RABlockCoords.y) {
						return true;
					}
				}
				return false;
			}

			struct ConstantParametersPack {
				float rotationSliceLength;
				int rotationSlicesCount;
				uint2 blockSize;
				uint2 blockCount;
				float maxRotation;
				float3 gridSpaceMultiplier;
				float seedDensityF;
				float mZoomPowFactor;
				float maximumPointToSeedDistance;
				float seedSamplingMultiplier;
			};

			ConstantParametersPack createFromProperties_ConstantParametersPack() {
				ConstantParametersPack p;
				p.maxRotation = 2 * PI;
				p.rotationSlicesCount = _RotationSlicesCount;
				p.rotationSliceLength = (p.maxRotation) / p.rotationSlicesCount;
				p.blockSize = uint2( round(_BlockSize.x), round(_BlockSize.y) );
				p.blockCount = uint2(round(_BlockCount.x), round(_BlockCount.y));
				p.gridSpaceMultiplier = float3(1, 1, _ScreenCellHeightMultiplier);
				p.seedDensityF = _SeedDensityF;
				p.mZoomPowFactor = _MZoomPowFactor;
				p.maximumPointToSeedDistance = _MaximumPointToSeedDistance;
				p.seedSamplingMultiplier = _SeedSamplingMultiplier;
				return p;
			}

			float4 calculateSgmColor2(float3 worldSpacePos, float3 worldSpaceSeedPosition, float3 normal, float strokeAngle, ConstantParametersPack paramPack, uint2 screenCoords) {
				float3 worldSpaceSeedPositionOnSurface = projectPointOnPlane(worldSpacePos, normal, worldSpaceSeedPosition);
				if (length(worldSpaceSeedPosition - worldSpaceSeedPositionOnSurface) < paramPack.maximumPointToSeedDistance) {
					float2 seedCoords = worldSpacePosToScreenCoords(worldSpaceSeedPositionOnSurface);

					int rotationSliceIndex = floor(paramPack.rotationSlicesCount * strokeAngle / paramPack.maxRotation);
					RAScreenInfo RAinfo = calculateRAScreenInfo(screenCoords, paramPack.blockCount, paramPack.blockSize,
						rotationSliceIndex, paramPack.rotationSliceLength, paramPack.rotationSlicesCount);
					float sliceRotationAngle = computeSliceRotationAngle(rotationSliceIndex, paramPack.rotationSliceLength,
						paramPack.rotationSlicesCount);
					bool shouldUpdate = shouldUpdateSSGM(RAinfo, paramPack.blockCount, paramPack.blockSize, seedCoords,
						sliceRotationAngle);

					bool pixelSet = false;
					float4 pColor = 0;
					if (shouldUpdate) {
						pixelSet = true;
						pColor = float4(seedCoords, strokeAngle, 1);
					}

					float inSliceAngle = fmod(strokeAngle, paramPack.rotationSliceLength);
					float inSlicePercent = (inSliceAngle / paramPack.rotationSliceLength);
					int extraRotationSliceIndex = 0;
					if (inSlicePercent < 0.5) {
						extraRotationSliceIndex = (rotationSliceIndex + paramPack.rotationSlicesCount - 1)
							% paramPack.rotationSlicesCount;
					}
					else {
						extraRotationSliceIndex = (rotationSliceIndex + 1) % paramPack.rotationSlicesCount;
					}

					RAinfo = calculateRAScreenInfo(screenCoords, paramPack.blockCount, paramPack.blockSize, extraRotationSliceIndex,
						paramPack.rotationSliceLength, paramPack.rotationSlicesCount);
					sliceRotationAngle = computeSliceRotationAngle(extraRotationSliceIndex, paramPack.rotationSliceLength,
						paramPack.rotationSlicesCount);
					shouldUpdate = shouldUpdateSSGM(RAinfo, paramPack.blockCount, paramPack.blockSize, seedCoords,
						sliceRotationAngle);
					if (shouldUpdate) {
						pixelSet = true;
						pColor = float4(seedCoords, strokeAngle, 1);
					}

					if (pixelSet) {
						return pColor;
					}
					else {
						return float4(seedCoords, 0, 0);
					}
				}
				return 0;
			}

			float3 memberwiseMultiplyF3(float3 a, float3 b) {
				return float3(a.x*b.x, a.y*b.y, a.z*b.z);
			}

			float calculateMZoom(uint2 screenCoords, float3 worldSpacePos, float3 testVector1, float3 testVector2, ConstantParametersPack paramPack) {
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
				return M;
			}

			float3 transportToGridSpace(float3 newStraight, float3 newUp, float3 p) {
				float3 X1 = float3(1, 0, 0); // straight
				float3 X2 = float3(0, 1, 0); // up 
				float3 X3 = float3(0, 0, 1); //right
				if (dot(X1, newStraight) > 0.999) {
					return p;
				}

				// These vectors are the local X,Y,Z of the rotated object
				float3 X1Prime = newStraight; // newStraight
				float3 X2Prime = newUp;
				float3 X3Prime = normalize(cross(X1Prime, X2Prime)); // right;

				// This matrix will transform points from the world back to the rotated axis
				float3x3 WorldToLocalTransform = 
				{
					  dot(X1Prime, X1),
					  dot(X1Prime, X2),
					  dot(X1Prime, X3),
					  dot(X2Prime, X1),
					  dot(X2Prime, X2),
					  dot(X2Prime, X3),
					  dot(X3Prime, X1),
					  dot(X3Prime, X2),
					  dot(X3Prime, X3),
				};
				return mul(WorldToLocalTransform, p);
			}

			float3 transportFromGridSpace(float3 newStraight, float3 newUp, float3 p) {
				float3 X1 = float3(1, 0, 0); // straight
				float3 X2 = float3(0, 1, 0); // up 
				float3 X3 = float3(0, 0, 1); //right
				if (dot(X1, newStraight) > 0.999) {
					return p;
				}

				// These vectors are the local X,Y,Z of the rotated object
				float3 X1Prime = newStraight; // newStraight
				float3 X2Prime = newUp;
				float3 X3Prime = normalize(cross(X1Prime, X2Prime)); // right;

				// This matrix will transform points from the rotated axis to the world
				float3x3 LocalToWorldTransform = 
				{
					  dot(X1, X1Prime),
					  dot(X1, X2Prime),
					   dot(X1, X3Prime),
					  dot(X2, X1Prime),
					  dot(X2, X2Prime),
					  dot(X2, X3Prime),
					  dot(X3, X1Prime),
					  dot(X3, X2Prime),
					  dot(X3, X3Prime),
				};
				return mul(LocalToWorldTransform, p);
			}


			float4 calculateSgmColor(float3 worldSpacePos, float3 normal, float3 alignmentVector, float3 perpAlignmentVector, float strokeAngle, ConstantParametersPack paramPack, uint2 screenCoords) {
				float M = calculateMZoom(screenCoords, worldSpacePos, alignmentVector, perpAlignmentVector, paramPack);
				float seedSpaceMultiplier = pow(2, -floor(M)) * paramPack.seedSamplingMultiplier;
				float m = M - floor(M);
				seedSpaceMultiplier = 1;

				worldSpacePos *= seedSpaceMultiplier;
				ClosestSeedSpecification spec = retriveClosestSeed(worldSpacePos);
#if TWO_SEEDS
				if (worldSpacePos.z > 0.1) {
					spec.position = float3(0.05,0.0,0.4);
				}
				else {
					spec.position = 0;
				}
#else
				spec.position = 0;

#endif
				float3 worldSpaceSeedPosition = spec.position;// transportFromGridSpace(normal, alignmentVector, spec.position / seedSpaceMultiplier);
				if (spec.isActive) {
					return calculateSgmColor2(worldSpacePos, worldSpaceSeedPosition, normal, strokeAngle, paramPack, screenCoords);
				}
				else {
					return 0;
				}
			}



			////////////////////////////////////////////////////////////////////////////////
			////////////////////////////////////////////////////////////////////////////////
			////////////////////////////////////////////////////////////////////////////////














			// na podstawie https://www.shadertoy.com/view/lsV3Wd
			// F(x,y)
			float F ( in float2 coords, float4 coef )
			{
				float A = coef[0];
				float B = coef[1];
				float C = coef[2];
				float D = coef[3];

				float T = coords.x;
				return
					(A * (1.0 - T) * (1.0 - T) * (1.0 - T)) +
					(B * 3.0 * (1.0 - T) * (1.0 - T) * T) +
					(C * 3.0 * (1.0 - T) * T * T) +
					(D * T * T * T) - coords.y;
			}

			//////////////////////////////// BEZIERS
			float2 grad( in float2 coords, float4 coef )
			{
				float A = coef[0];
				float B = coef[1];
				float C = coef[2];
				float D = coef[3];

				float T = coords.x;
				float T2 = T * T;
				float S = (1.0 - T);
				float S2 = S * S;
				
				float DF =
					(B-A) * S2 +
					(C-B) * 2.0 * S * T +
					(D-C) * T2;
				
				return float2 (
					3.0*DF,
					-1.0
				);
			}

			float sdfBezier(float2 coords, float4 coef) {
				float v = F(coords, coef);
				float2 g = grad(coords, coef); // to jest tylko do kontroli grubości linii
				return abs(v) / length(g);
			}

			float3x3 RSTMatrix(float2 t, float angle, float scale) {
				scale = 1 / scale;
				float3x3 tMat = {
					1, 0, t.x,
					0, 1, t.y,
					0, 0, 1
				};

				float3x3 rMat = {
					cos(angle), sin(angle), 0,
					-sin(angle), cos(angle), 0,
					0, 0, 1
				};

				float3x3 sMat = {
					scale, 0, 0,
					0, scale, 0,
					0,0,1
				};

				return mul(mul(rMat, sMat), tMat);
				//return mul(mul(tMat, rMat), sMat);
			}

			float distanceToLine(float3 lineCoeffs, float2 p) {
				return abs(lineCoeffs[0] * p.x + lineCoeffs[1] * p.y + lineCoeffs[2])
					/ sqrt(pow(lineCoeffs[0], 2) + pow(lineCoeffs[1], 2));
			}

			struct curveDistanceResult {
				float distanceToLine;
				float distanceFromStart;
				float wholeCurveLength;
			};

			curveDistanceResult make_curveDistanceResult( float distanceToLine, float distanceFromStart, float wholeCurveLength ) {
				curveDistanceResult o;
				o.distanceToLine = distanceToLine;
				o.distanceFromStart = distanceFromStart;
				o.wholeCurveLength = wholeCurveLength;
				return o;
			}

			float2 rayBoxIntersectionPoint(float2 p, float angle) {
				// from -PI , PI to 0; 2PI
				if (angle < 0) {
					angle = PI +  -1*angle;
				}
				// we will check intersection with two lines - one horizontal and one vertical
				float xToCheck = 0;
				float yToCheck = 0;

				if (angle < PI / 2) {
					xToCheck = 1;
					yToCheck = 1;
				}else if (angle < PI ) {
					xToCheck = 0;
					yToCheck = 1;
				}else if (angle < PI * 1.5 ) {
					xToCheck = 0;
					yToCheck = 0;
				}else {
					xToCheck = 1;
					yToCheck = 0;
				}

				float alpha = tan(angle);
				// as in y = alpha*x + c
				float c = p.y - p.x*alpha;

				float2 horizontalPoint;
				float2 verticalPoint;

				if (xToCheck == 0) {
					verticalPoint = float2(0, c);
				}
				else {
					verticalPoint = float2(1, alpha + c);
				}

				if (yToCheck == 0) {
					horizontalPoint = float2(-c / alpha, 0);
				}
				else {
					horizontalPoint = float2( (1-c) / alpha, 1);
				}

				if (length(p - verticalPoint) < length(p - horizontalPoint)) {
					return verticalPoint;
				}
				else {
					return horizontalPoint;
				}
			}

			float myBezier1DValue(float4 coefs, float x) {
				float B = coefs[1];
				float C = coefs[2];

				return  3 * B*pow(1 - x, 2)*x + 3 * C*(1 - x)*pow(x, 2);
			}

			float2 calculateBezierExtremes(float4 coefs) {
				float B = coefs[1];
				float C = coefs[2];
				float root1 = ((12 * B - 6 * C) + sqrt(pow(6 * C - 12 * B, 2) - 12 * B*(9 * B - 9 * C))) / (2 * (9 * B - 9 * C));
				float root2 = ((12 * B - 6 * C) - sqrt(pow(6 * C - 12 * B, 2) - 12 * B*(9 * B - 9 * C))) / (2 * (9 * B - 9 * C));

				float extremum1 = myBezier1DValue(coefs, root1);
				float extremum2 = myBezier1DValue(coefs, root2);

				return float2(
					min(min(0, extremum1), extremum2),
					max(max(0, extremum1), extremum2));
			}

			// arc is the bezier curve
			// tail is the straight line after main seed point
			float2 calculateDistancesToTail(float2 inScreenCoords, float2 p2, float2 p2Vec, float2 iPoint) {
				// tail is a ray, not line. We have to take it into account
				//p2 is start of tail ray
				float2 dvA = inScreenCoords - p2;
				// rzutujemy 
				float2 dr = dot(dvA, normalize(p2Vec))*normalize(p2Vec);
				// inScreenCoords zrzutowane na prostą utworzoną z tail
				float2 rr = p2 + dr;

				float cosAngleP2 = dot(p2Vec, normalize(dvA));
				if (cosAngleP2 < 0) { // we are 'behind' start of ray
					rr = p2;
				}
				float cosAngleIP = dot(p2Vec, normalize(inScreenCoords - iPoint));
				if (cosAngleIP > 0) {
					rr = iPoint;
				}

				float distanceToLine = length(inScreenCoords - rr);
				float distanceToEndOfLine = length(rr - iPoint); 

				return float2(distanceToLine, distanceToEndOfLine);
			}


			curveDistanceResult distanceToLinkedLine(float2 inScreenCoords, float2 p1, float2 p1Vec, float2 p2, float2 p2Vec, float unLinkedHatchLength, float linkingMultiplier, float distanceLinkingMargin, bool p2IsPresent) {
				if (!p2IsPresent) {
					p2 = p1 + p1Vec*1000;
					p2Vec = p1Vec;
				}
				float arcLength = length(p1 - p2); //very crude approximation

				// we have to compute distances to four 

				float2 delta = (p2 - p1);
				float2 nDelta = normalize(delta);
				float angle = atan2(nDelta.y, nDelta.x);
				float scale = length(delta);

				float3x3 rstMat = RSTMatrix(-p1, angle, scale);
				// inScreenCoords in bezier-space
				float2 ninScreenCoords = mul( rstMat, float3(inScreenCoords,1) ).xy;
				// rotated direction vectors
				float2 cp1Vec = mul( (float2x2)rstMat,(p1Vec));
				float2 cp2Vec = mul( (float2x2)rstMat,(p2Vec));
				
				//calculate coefficients
				float yB = (1 / 3.0)* cp1Vec.y / cp1Vec.x;
				float yC = (-1 / 3.0)* (cp2Vec.y / cp2Vec.x);
				float noLink_yC = (2 / 3.0)* (cp1Vec.y / cp1Vec.x);
				float yD = 0;
				float noLink_yD = (cp1Vec.y / cp1Vec.x);

				float4 coefs = float4(0, yB, yC, yD);

				//// We check if curvature is small enough to use full bezier
				float2 extremes = calculateBezierExtremes(coefs);
				float currentCurveChange = (extremes[1] - extremes[0]) / arcLength;

				float linkingWeight = 1;

				if (currentCurveChange < _DebugScalar) {
					linkingWeight = 1;
				}
				else if (currentCurveChange < _DebugScalar + _ArcCurvatureMargin) {
					linkingWeight = 1 - (currentCurveChange - _DebugScalar) / _ArcCurvatureMargin;
				}
				else {
					linkingWeight = 0;
				}

				float weightFromDistance = 0;
				if (arcLength < unLinkedHatchLength) {
					weightFromDistance = 1;
				}
				else if (arcLength < unLinkedHatchLength + distanceLinkingMargin) {
					weightFromDistance = 1 -(arcLength - unLinkedHatchLength) / distanceLinkingMargin;
				}

				linkingWeight = min(linkingWeight, weightFromDistance);

				yC = lerp(noLink_yC, yC, linkingWeight);
				yD = lerp(noLink_yD, yD, linkingWeight);
				coefs = float4(0, yB, yC, yD);

				float2 unLinkedEndPoint = p1 + p1Vec * unLinkedHatchLength;
				float2 unLinkedEndPointInNinScreenCoordsSpace = mul(rstMat, float3(unLinkedEndPoint, 1)).xy;

				float distanceToLinkedLine = sdfBezier(ninScreenCoords, coefs) * arcLength;
				float linkedDistanceFromStart = arcLength * ninScreenCoords.x;
				linkingWeight *= linkingMultiplier;
				float maxNinScreenCoords = lerp(unLinkedEndPointInNinScreenCoordsSpace.x, 1, sqrt(linkingWeight) );

				if (ninScreenCoords.x > maxNinScreenCoords  ) {
					distanceToLinkedLine = length(inScreenCoords - p2);
					linkedDistanceFromStart = arcLength;
				}
				if (ninScreenCoords.x < 0) {
					distanceToLinkedLine = length(inScreenCoords - p1);
					linkedDistanceFromStart = 0;
				}

				return make_curveDistanceResult( distanceToLinkedLine, linkedDistanceFromStart , arcLength );
			}

			curveDistanceResult distanceToUnlinedLine(float2 inScreenCoords, float2 p1, float2 p1Vec, float hatchLength) {
				float a = atan2(p1Vec.y, p1Vec.x);
				// y = a*x + b
				float b = p1.y - a * p1.x;

				// vector from start to currentPoint
				float2 dvA = inScreenCoords - p1;

				// zrzutowany wektor na linie ( gdzie 0,0 to p1)
				float2 dr = dot(dvA, normalize(p1Vec))*normalize(p1Vec);

				// powrót do zwyczajnej coords space
				float2 rr = p1 + dr;

				float cosAngle = dot(p1Vec, normalize(dvA));
				if (cosAngle < 0) { // we are 'behind' start of ray
					rr = p1;
				}

				float currentDistanceFromStart = length(rr - p1);
				if (currentDistanceFromStart > hatchLength) {
					rr = p1 + p1Vec * hatchLength;
				}

				float distanceToLine = length(inScreenCoords - rr);
				float distanceFromStart = length(rr - p1);

				return make_curveDistanceResult( distanceToLine, distanceFromStart,  hatchLength);
			}

			////////////////////////////////


			float2 toInBlockUv(float2 screenPosCoords, float2 blockScreenPosCoords, uint2 blockSize) {
				return  float2(
					(screenPosCoords.x - blockScreenPosCoords.x) / ((float)blockSize.x),
					(screenPosCoords.y - blockScreenPosCoords.y) / ((float)blockSize.y) );
			}

			float2 computeIntersectionWithNorthBorder(float2 northSeed, float northAngle) {
				// y =ax+b
				float b = northSeed.y - tan(northAngle)*northSeed.x;

				float x = (1 - b) / tan(northAngle);
				return float2(x, 1);
			}

			float2 toDirectionVec(float angle) {
				return float2(cos(angle), sin(angle));
			}

			struct SeedSpec {
				float2 screenCoords;
				float angle;
				bool isActive;
			};

			SeedSpec make_SeedSpec(float2 screenCoords, float angle, bool isActive) {
				SeedSpec s;
				s.screenCoords = screenCoords;
				s.angle = angle;
				s.isActive = isActive;
				return s;
			}

			SeedSpec createRASeedSpec(SeedSpec old, float sliceRotationAngle) {
				float2 RAScreenCoords = rotateAroundPivot(old.screenCoords, sliceRotationAngle, _ScreenParams.xy / 2);
				float angle = fmod(old.angle + sliceRotationAngle, PI * 2);
				return make_SeedSpec(RAScreenCoords, angle, old.isActive);
			}

			struct SeedSpecWithBlockInfo {
				SeedSpec seedSpec;
				int2 seedBlockCoords;
				int2 seedBlockToThisSeedOffset;
			};

			SeedSpecWithBlockInfo make_SeedSpecWithBlockInfo(SeedSpec seedSpec, int2 seedBlockCoords, int2 seedBlockToThisSeedOffset) {
				SeedSpecWithBlockInfo s;
				s.seedSpec = seedSpec;
				s.seedBlockCoords = seedBlockCoords;
				s.seedBlockToThisSeedOffset = seedBlockToThisSeedOffset;
				return s;
			}

			SeedSpec retriveSgm3(int2 blockCoords, uint rotationSliceIndex) {
				float4 retrived = _StrokeSeedGridMap[uint3(blockCoords, rotationSliceIndex)];
				if (retrived.a > 0) {
					return make_SeedSpec(retrived.xy, retrived.z, true);
				}
				else {
					return make_SeedSpec(0, 0, false);
				}
			}

			SeedSpec retriveSgm2(int2 RAblockCoords, uint rotationSliceIndex) {
				ConstantParametersPack paramPack = createFromProperties_ConstantParametersPack();

				float maxRotation = 2 * PI; // TODO take this from constant params
				float rotationSlicesCount = _RotationSlicesCount;
				float rotationSliceLength = (maxRotation) / rotationSlicesCount;

				float sliceRotationAngle = ( /*rotationSlicesCount -*/  (rotationSliceIndex + (rotationSlicesCount/4)) % rotationSlicesCount) * rotationSliceLength;
				float strokeInSliceRotationAngle = rotationSliceIndex * rotationSliceLength;

				int2 RAsampleCoord = int2(RAblockCoords.x * paramPack.blockSize.x, RAblockCoords.y * paramPack.blockSize.y) +  paramPack.blockSize/ 2.0;

				int2 sampleCoord = round(rotateAroundPivot(RAsampleCoord, -sliceRotationAngle, _ScreenParams.xy / 2));

				float2 sampleUv = intScreenCoords_to_uv(sampleCoord);

				float4 worldSpacePos = tex2D(_WorldPositionTex, sampleUv);
				float4 normal = tex2D(_NormalTex, sampleUv);
				float3 alignmentVector = tex2D(_AngleTex, sampleUv).xyz;
				float strokeAngle = PI * 2 * 24 / 32.0;
				strokeAngle = tex2D(_MainTex, sampleUv).b;

				float4 sgmColor = calculateSgmColor(worldSpacePos, normal, alignmentVector, cross(normal,alignmentVector), strokeAngle, paramPack, sampleCoord);
				if (sgmColor.a > 0.5 && abs(strokeAngle - strokeInSliceRotationAngle) <= rotationSliceLength*2  ) { //TODO what if rotation from 2 deg to 357 deg
					return make_SeedSpec(sgmColor.xy, sgmColor.z, true);
				}
				else {
					return make_SeedSpec(sgmColor.xy, sgmColor.z, false);
				}
			}

			float2 calculateUvInFrame(float4 frame, float2 p) {
				float2 size = float2(frame[2] - frame[0], frame[3] - frame[1]);
				return float2(
					(p.x - frame[0]) / size[0],
					(p.y - frame[1]) / size[1]);
			}

			float4 drawHaloAroundThisBlockSeed(int2 blockCoords, uint rotationSliceIndex , int2 inScreenCoords) {
				float4 color = 0;
				SeedSpec centerSgm = retriveSgm2(blockCoords, rotationSliceIndex);
				if (length(centerSgm.screenCoords - inScreenCoords) < 8) {
					if (centerSgm.isActive) {
						color += float4(0, 1, 0, 1);
					}
					else {
						color += float4(0, 1, 0, 1);
					}
				}
				return color;
			}

			float4 drawBlockGrid( uint2 blockSize, float2 inBlockCoords) {
				float4 color = 0;
				if (inBlockCoords.x <= 1 || inBlockCoords.y <= 1) {
					color += float4(0, 0, 1, 1);
				}
				if ( round(inBlockCoords.x) ==  blockSize.x/2.0 || round(inBlockCoords.y) == blockSize.y/2.0) {
					color += float4(1, 0, 1, 1)/2;
				}
				return color;
			}

			bool canLink(SeedSpec upperSeed, SeedSpec lowerSeed, uint2 blockSize) {
				if (!(lowerSeed.isActive && upperSeed.isActive)) {
					return false;
				}
				float minimalYDistance = blockSize.y ;
				float maximalXDistance = blockSize.x / 2.0;

				float2 delta = abs(upperSeed.screenCoords - lowerSeed.screenCoords);
				return (delta.x < maximalXDistance && delta.y > minimalYDistance);
			}


			SeedSpec findSouthSeedRA(SeedSpec RAMainSeed, int2 seedBlockCoords, uint rotationSliceIndex, float2 RAInScreenCoords, uint2 blockSize, float sliceRotationAngle) {
				float2 RAMainSeedInBlockUv = toInBlockUv( RAInScreenCoords,  int2Multiply(seedBlockCoords,blockSize), blockSize);

				int leftBlockOffset = -1;
				if (RAMainSeedInBlockUv.x > 0.5) {
					leftBlockOffset = 0;
				}

				//first, we search for top neighbour
				
				//we retrive info about four potential north seeds
				// top-left top-right bottom-right bototm-left
				int2 blockCoords = seedBlockCoords;
				int2 seedCoords[4];
				seedCoords[0] = blockCoords +  int2(leftBlockOffset + 0, -1);
				seedCoords[1] = blockCoords +  int2(leftBlockOffset + 1, -1);
				seedCoords[2] = blockCoords +  int2(leftBlockOffset + 1, -2);
				seedCoords[3] = blockCoords +  int2(leftBlockOffset + 0, -2);

				SeedSpec seeds[4];
				SeedSpec RASeeds[4];
				for (int i = 0; i < 4; i++) {
					seeds[i] = retriveSgm2(seedCoords[i], rotationSliceIndex);
					RASeeds[i] = createRASeedSpec(seeds[i], sliceRotationAngle);
				}

				// there should be no two linkabe seeds in line
				for (int i = 0; i < 4; i++) {
					if (canLink(RASeeds[i], RAMainSeed, blockSize)) {
						return seeds[i];
					}
				}
				return make_SeedSpec(0, 0, false);
			}

			float4 drawOneHatchInOrientation(int2 blockCoords, uint rotationSliceIndex, float2 inScreenCoords,  SeedSpec mainSeed, SeedSpec southSeed) {
				float distanceToLine = 999999;

				if (mainSeed.isActive) {
					float linkingWeight = 0;
					bool p2IsPresent;
					if (southSeed.isActive) {
						linkingWeight = 1;
						p2IsPresent = true;
					}
					else {
						p2IsPresent = false;
					}
					curveDistanceResult  nDistanceResult = distanceToLinkedLine(inScreenCoords, mainSeed.screenCoords, toDirectionVec(mainSeed.angle),
						southSeed.screenCoords, toDirectionVec(southSeed.angle), _HatchLength, linkingWeight, _DistanceLinkingMargin, p2IsPresent);
					distanceToLine = nDistanceResult.distanceToLine;
				}
				float4 color = 0;
				if (distanceToLine < _DistanceToLineTreshold) {
					color = float4(4, 0, 0, 1);
				}
				return color;
			}

			float4 drawUnlinkedOneHatchInOrientation(int2 blockCoords, uint rotationSliceIndex, float2 inScreenCoords, SeedSpec seed) {
				float distanceToLine = 999999;

				if (seed.isActive) {
					curveDistanceResult  nDistanceResult = distanceToUnlinedLine(inScreenCoords, seed.screenCoords, toDirectionVec(seed.angle), _HatchLength);
					distanceToLine = nDistanceResult.distanceToLine;
				}
				float4 color = 0;
				if (distanceToLine < _DistanceToLineTreshold) {
					color = float4(4, 0, 0, 1);
				}
				return color;

			}


			bool isInActiveColumn(SeedSpec seed, uint2 blockSize, float2 inScreenCoords) {
				return seed.isActive && abs(seed.screenCoords.x - inScreenCoords.x) < blockSize.x / 2.0; ///TODO
			}

			uint2 loopNegativeBlockCoords(int2 blockCoords, uint2 blockCount) {
				return (blockCoords + blockCount * 2) % blockCount;
			}

			float4 drawHatchesInOrientation(int2 RABlockCoords, uint2 blockSize, uint rotationSliceIndex, float2 inScreenCoords,
				float2 RAInScreenCoords, float2 RAThisPixelInBlockUv, float sliceRotationAngle, uint2 blockCount) {
				int leftBlockOffset = -1;
				if (RAThisPixelInBlockUv.x > 0.5) {
					leftBlockOffset = 0;
				}

				SeedSpec seedsColumn[4][2];
				SeedSpec RASeedsColumn[4][2];

				// there are 8 cells in 4 rows. 2 Columns there is.
				// Our pixel is in row 2. (When 1 is lowest row)
				// When pixel is over seed in row 2 We do not check for linked seeds that start in row 1. (As there is not enough vertical space for seed in row 2 and linked seed in row 1 that would have impact in our pixel)
				// When there is no seed oin our row then we take into account the linked line ending below 
				// Every row should have at most one seed in active column

				// looking for interesting seeds
				for (int x = 0; x < 2; x++) {
					for (int y = 0; y < 4; y++) {
						int2 blockCoords = RABlockCoords + int2(leftBlockOffset + x, y - 1);
						blockCoords = loopNegativeBlockCoords(blockCoords, blockCount);

						seedsColumn[y][x] = retriveSgm2(blockCoords, rotationSliceIndex);
						RASeedsColumn[y][x] =  createRASeedSpec(seedsColumn[y][x], sliceRotationAngle);
					}
				}

				// 0 - seed in active column is in left column. 1 - in right column  2 - no active pixel 
				int rowStatus[4];
				for (int y = 0; y < 4; y++) {
					float2 dist[2];
					if (isInActiveColumn(RASeedsColumn[y][0], blockSize, RAInScreenCoords)) {
						rowStatus[y] = 0;
					}
					else if (isInActiveColumn(RASeedsColumn[y][1], blockSize, RAInScreenCoords)) {
						rowStatus[y] = 1;
					}
					else {
						rowStatus[y] = 2;
					}
				}

				// W takim razie W OGÓLE NIE MUSIMY ROBIĆ LINE FITU PUNKTOW CO leżą pod nami. (Z dokładnością do grubości linii)
				// NIE MUSIMY SIE ZAJMOWAC NAJNIZSZYM ROWEM (TODO usuń seedsColumn[0]

				float4 color = 0;
				for (int y = 1; y < 4; y++) {
					if (rowStatus[y] < 2) {
						SeedSpec mainSeed = seedsColumn[y][ rowStatus[y]];
						SeedSpec RAMainSeed = RASeedsColumn[y][rowStatus[y]];
						SeedSpec southSeed = findSouthSeedRA(RAMainSeed,RABlockCoords +int2(leftBlockOffset+rowStatus[y], y-1), rotationSliceIndex, RAInScreenCoords, blockSize, sliceRotationAngle);
						//color += 1;

#if USE_LINKING
						color += drawOneHatchInOrientation(RABlockCoords, rotationSliceIndex, inScreenCoords, mainSeed, southSeed);
#else
						color += drawUnlinkedOneHatchInOrientation(RABlockCoords, rotationSliceIndex, inScreenCoords, mainSeed);
#endif
					}
					
				}
				
				return color;
			}

			float4 frag(v2f input) : SV_Target
			{
				float maxRotation = 2 * PI;
				float rotationSlicesCount = _RotationSlicesCount;
				float rotationSliceLength = (maxRotation) / rotationSlicesCount;
				uint2 blockSize = uint2( round(_BlockSize.x), round(_BlockSize.y) );

				float4 color = 0;
				float2 uv =  input.projPos.xy;
				float2 orgUv =  input.projPos.xy;
				uint2 screenCoords = input.pos.xy;

				uint2 inScreenCoords = uint2(floor(uv.x * _ScreenParams.x), floor(uv.y*_ScreenParams.y));

				///////// ANGLE RELATED
				float strokeAngle = _RotationSliceIndex * (2 * 3.14159 / 16.0);
				strokeAngle = tex2D(_MainTex, uv).z;
				//strokeAngle = PI * 2 * 24 / 32.0;
				//strokeAngle = PI * 1.5;
				//strokeAngle = PI * 22 / 16.0;
				//strokeAngle = PI * 2 * 24 / 32.0;

				int rotationSliceIndex = floor(rotationSlicesCount * strokeAngle / maxRotation) ;
				float sliceRotationAngle = ( /*rotationSlicesCount -*/  (rotationSliceIndex + (rotationSlicesCount/4)) % rotationSlicesCount) * rotationSliceLength;

				float2 RAInScreenCoords = rotateAroundPivot(screenCoords, sliceRotationAngle, _ScreenParams.xy / 2);

				int2 RABlockCoords = int2(floor(RAInScreenCoords.x / (float)blockSize.x), floor(RAInScreenCoords.y / (float)blockSize.y));

				// looping negative coords
				uint2 blockCount = uint2(round(_BlockCount.x), round(_BlockCount.y));

				float2 RAThisPixelInBlockUv = toInBlockUv(RAInScreenCoords, int2Multiply(RABlockCoords,blockSize), blockSize);

				//////// COLOR UPDATING
				color += drawHaloAroundThisBlockSeed(RABlockCoords, rotationSliceIndex, inScreenCoords);
				float2 RAInBlockCoords = abs(RAInScreenCoords- int2Multiply(RABlockCoords,blockSize));
				color += drawBlockGrid( blockSize, RAInBlockCoords);
				
				///////// Colors from hatches
				float4 mainSliceColor = drawHatchesInOrientation(RABlockCoords, blockSize, rotationSliceIndex,
					inScreenCoords, RAInScreenCoords, RAThisPixelInBlockUv, sliceRotationAngle, blockCount);
				color += mainSliceColor;




				//color += -float4(RABlockCoords.x / 10.0, RABlockCoords.y / 10.0, 0, 1);
				//color.xy += RAThisPixelInBlockUv;
				//color.xy += RAInScreenCoords.xy / 100.0;
				//color.xy += RABlockCoords.xy/32.0;

				//return tex2D(_MainTex, orgUv);
				return saturate(tex2D(_MainTex, orgUv).ggra)/2.0 +color / 4;
			}

			 ENDCG

		}

	}
}
