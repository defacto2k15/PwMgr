Shader "Custom/Sandbox/Filling/MMStrokeSeedGridMapDebugRenderer" {
	Properties{
		_StrokeSeedGridMap("StrokeSeedGridMap", 2DArray) = "blue"{}
		_MainTex ("MainTex", any) = "" {}
		_StrokeTex("StrokeTex", 2D) = "blue"{}
		_DebugScalar("DebugScalar", Range(0,1)) = 0
		_SelectSgmRetrivalMethod("SelectSgmRetrivalMethod", Range(0,1)) = 0

		_ArcCurvatureMargin("ArcCurvatureMargin", Range(0,1)) = 0.1
		_HatchLength("HatchLength", Range(0,128)) = 10
		_DistanceLinkingMargin("DistanceLinkingMargin", Range(0,10)) = 5
		_RotationSliceIndex("RotationSliceIndex",Range(0,16)) = 0

		_RotationSlicesCount("RotationSlicesCount", Int) = 16
		_BlockSize("BlockSize", Vector) = (32.0,32.0,0,0)

		_BlockCount("BlockCount", Vector) = (32.0,32.0,0,0)

		_ArtisticMainTex("ArtisticMainTex", 2D) = "blue"{}
		_WorldPositionTex("WorldPositionTex", 2D) = "blue"{}
		_VectorsTex("VectorsTex", 2D) = "blue"{}
		_SeedPositionTex3D("SeedPositionTex3D", 3D) = "" {}
		_DummyStage1Texture("DummyStage1Texture", 2D) = "blue"{}

		_MaximumPointToSeedDistance("MaximumPointToSeedDistance", Range(0,10)) = 1.0
		_SeedDensityF("SeedDensityF", Range(0,10)) = 1.0
		_MZoomPowFactor("MZoomPowFactor", Range(0,3)) = 0.7

		_SeedSamplingMultiplier("SeedSamplingMultiplier", Range(0,10)) = 1.0

		_DistanceToLineTreshold("DistanceToLineTreshold", Range(0,50)) = 2.0

		_TierCount("TierCount", Range(1,5)) = 1.0
		_VectorQuantCount("VectorQuantCount", Range(1,100)) = 8.0
		_LightHatchErasureMode("LightHatchErasureMode", Range(0,4)) = 0.0
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
			#pragma multi_compile __ MEASUREMENT
			#pragma multi_compile __ LIGHT_SHADING_ON
			#pragma multi_compile __ DIRECTION_PER_LIGHT
			#include "UnityCG.cginc"
			#include "text_printing.hlsl"

			sampler2D _ArtisticMainTex;

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : ANY_UV;
			};

#include "filling_variables.txt"
			Texture2DArray<float4> _StrokeSeedGridMap;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				return o;
			}



			/////////// SEEED CALCULATION LOL
			////////////////////////////////////////////////////////////////////////////////
			////////////////////////////////////////////////////////////////////////////////
			////////////////////////////////////////////////////////////////////////////////


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


#define CUSTOM_MATRIX_VP _MyUnityMatrixVP

#include "mm_common.txt"
#include "filling_common.txt"
#include "filling_calculateSgmColor2.txt"
#include "filling_calculateSgmFromTextures.txt"
#include "filling_erasure.txt"
#include "mf_common.txt"

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

			curveDistanceResult distanceToUnlinkedLine(float2 inScreenCoords, float2 p1, float2 p1Vec, float hatchLength) {
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
					return make_curveDistanceResult(9999, 9999, 9999);
				}

				float currentDistanceFromStart = length(rr - p1);
				if (currentDistanceFromStart > hatchLength) {
					return make_curveDistanceResult(9999, 9999, 9999);
				}

				float distanceToLine = length(inScreenCoords - rr);
				float distanceFromStart = length(rr - p1);

				//float s = sign( dot(normalize(dvA), p1Vec));
				//float2 origin = p1;
				//float2 A = normalize(inScreenCoords - origin);

				//p1 = normalize(p1);
				//float s = p1.x*A.y - p1.y*A.x;// cross(A, p1);
				//if (s < -0.5) {
				//	s = -1;
				//}
				//else {
				//	s = 1;
				//}

				return make_curveDistanceResult( distanceToLine, distanceFromStart,  hatchLength);
			}

			///////////////////////////////
			struct mf_retrivedHatchPixelWithDarkness{
				mf_retrivedHatchPixel pixel;
				float darkness;
			};

			mf_retrivedHatchPixelWithDarkness make_mf_retrivedHatchPixelWithDarkness 
					( mf_retrivedHatchPixel pixel, float4 darkness )
			{
				mf_retrivedHatchPixelWithDarkness m;
				m.pixel = pixel;
				m.darkness = darkness;
				return m;
			};

			mf_retrivedHatchPixelWithDarkness sampleAndCreate_mf_retrivedHatchPixelWithDarkness
				( mf_retrivedHatchPixel pixel, float vCoord) {
				float2 hatchTexCoords = float2(1-pixel.tParam, vCoord/(1/_DebugScalar) + 0.5);
				float4 hatchTexColor = tex2D(_StrokeTex, hatchTexCoords);
				float bhs = pixel.blendingHatchStrength;
				return make_mf_retrivedHatchPixelWithDarkness(pixel, hatchTexColor.a*bhs);
			}


			struct SeedSpec {
				float2 screenCoords;
				float angle;
				bool isActive;
				uint id;
				float alphaMultiplier;
			};

			SeedSpec make_SeedSpec(float2 screenCoords, float angle, bool isActive, uint id, float alphaMultiplier) {
				SeedSpec s;
				s.screenCoords = screenCoords;
				s.angle = angle;
				s.isActive = isActive;
				s.id = id;
				s.alphaMultiplier = alphaMultiplier;
				return s;
			}

			SeedSpec createRASeedSpec(SeedSpec old, float sliceRotationAngle) {
				float2 RAScreenCoords = rotateAroundPivot(old.screenCoords, sliceRotationAngle, _ScreenParams.xy / 2);
				float angle = fmod(old.angle + sliceRotationAngle, PI * 2);
				return make_SeedSpec(RAScreenCoords, angle, old.isActive, old.id, old.alphaMultiplier);
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

			SeedSpec retriveSgm_fromMap(int tierIndex, int2 blockCoords, uint rotationSliceIndex, ConstantParametersPack paramPack) {
				uint3 nonTierAwareCoords = uint3(blockCoords, rotationSliceIndex);
				uint3 tierAwareCoords = calculateTierAwareSSGMCoords(tierIndex, nonTierAwareCoords, paramPack);
				float4 retrived = _StrokeSeedGridMap[tierAwareCoords];

				float2 screenCoords = UnpackScreenCoordsFromUint(asuint(retrived.r));
				float angle = retrived.z;
				float alphaMultiplier = retrived.a;
				uint id = asuint(retrived.g);

				if (alphaMultiplier > 0) {
					return make_SeedSpec(screenCoords,angle, true, id, alphaMultiplier); 
				}
				else {
					return make_SeedSpec(0, 0, false, 0, 0);
				}

				//uint ssgbIndex = ComputeSSGBIndex(uint4(nonTierAwareCoords, tierIndex), paramPack);
				//return make_SeedSpec(fm.screenCoords, fm.strokeAngle, true, fm.id, fm.alphaMultiplier);
			}


			SeedSpec retriveSgm_inPlaceCalculation(int tierIndex, int2 RAblockCoords, uint rotationSliceIndex, ConstantParametersPack paramPack) {
				return make_SeedSpec(0, 0, false, 0, 0); //TODO
				//SSGMPixel pixel = calculateSgmColorFromTextures(tierIndex, RAblockCoords, rotationSliceIndex, paramPack);
				//if (pixel.sParam > 0.5 ) {
				//	return make_SeedSpec(pixel.screenCoords, pixel.strokeAngle, true, pixel.id);
				//}
				//else {
				//	return make_SeedSpec(pixel.screenCoords, pixel.strokeAngle, true, pixel.id);
				//}
			}

			SeedSpec retriveSgm(int tierIndex, int2 RAblockCoords, uint rotationSliceIndex, ConstantParametersPack paramPack) {
				if (_SelectSgmRetrivalMethod > 0.5) {
					return retriveSgm_inPlaceCalculation(tierIndex, RAblockCoords, rotationSliceIndex, paramPack);
				}
				else {
					return retriveSgm_fromMap(tierIndex, RAblockCoords, rotationSliceIndex, paramPack);
				}
			}

////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////

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

			float2 calculateUvInFrame(float4 frame, float2 p) {
				float2 size = float2(frame[2] - frame[0], frame[3] - frame[1]);
				return float2(
					(p.x - frame[0]) / size[0],
					(p.y - frame[1]) / size[1]);
			}

			float4 drawHaloAroundThisBlockSeed(int2 blockCoords, uint rotationSliceIndex , int2 inScreenCoords, int tierIndex, ConstantParametersPack paramPack) {
				float4 color = 0;
				SeedSpec centerSgm = retriveSgm(tierIndex, blockCoords, rotationSliceIndex, paramPack);
				if (length(centerSgm.screenCoords - inScreenCoords) < 8) {
					//color += centerSgm.angle;
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

			SeedSpec findSouthSeedRA(SeedSpec RAMainSeed, int2 seedBlockCoords, uint rotationSliceIndex, float2 RAInScreenCoords, uint2 blockSize, float sliceRotationAngle,
					int tierIndex, ConstantParametersPack paramPack) {
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
					seeds[i] = retriveSgm(tierIndex, seedCoords[i], rotationSliceIndex, paramPack);
					RASeeds[i] = createRASeedSpec(seeds[i], sliceRotationAngle);
				}

				// there should be no two linkabe seeds in line
				for (int i = 0; i < 4; i++) {
					if (canLink(RASeeds[i], RAMainSeed, blockSize)) {
						return seeds[i];
					}
				}
				return make_SeedSpec(0, 0, false,0,0);
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

				float distanceFromStart;
				float wholeCurveLength;

			float3 drawUnlinkedOneHatchInOrientation(int2 blockCoords, uint rotationSliceIndex, float2 inScreenCoords, SeedSpec seed, ConstantParametersPack paramPack) {
				float distanceToLine = 999999;
				float tParam = 0;

				if (seed.isActive) {
					curveDistanceResult  nDistanceResult = distanceToUnlinkedLine(inScreenCoords, seed.screenCoords, toDirectionVec(seed.angle), _HatchLength);
					distanceToLine = nDistanceResult.distanceToLine;
					tParam = nDistanceResult.distanceFromStart / nDistanceResult.wholeCurveLength;
				}
				if (abs(distanceToLine) < paramPack.hatchWidth) {
					return float3(seed.alphaMultiplier, tParam, distanceToLine/ paramPack.hatchWidth);
				}
				return 0;
			}

			float4 applyLightHatchErasure(float4 currentColor, SeedSpec spec, float lightIntensity) {
				if (_LightHatchErasureMode == 1) {
					return float4(currentColor.xyz, currentColor.a * (1 - lightIntensity));
				}
				else if (_LightHatchErasureMode == 2) {
					float f = generateLightHatchErasureBound(spec.id);
					return float4(currentColor.xyz, currentColor.a * step(  lightIntensity, f));
				}
				else {
					return currentColor;
				}
			}

			bool isInActiveColumn(SeedSpec seed, uint2 blockSize, float2 inScreenCoords) {
				return seed.isActive && abs(seed.screenCoords.x - inScreenCoords.x) < blockSize.x / 2.0; ///TODO
			}

			uint2 loopNegativeBlockCoords(int2 blockCoords, uint2 blockCount) {
				return (blockCoords + blockCount * 2) % blockCount;
			}

			mf_retrivedHatchPixelWithDarkness drawHatchesInOrientation(int2 RABlockCoords, uint2 blockSize, uint rotationSliceIndex, float2 inScreenCoords,
				float2 RAInScreenCoords, float2 RAThisPixelInBlockUv, float sliceRotationAngle, uint2 blockCount, int tierIndex, float lightIntensity, ConstantParametersPack paramPack) {
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

						seedsColumn[y][x] = retriveSgm(tierIndex, blockCoords, rotationSliceIndex, paramPack);
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

				mf_retrivedHatchPixelWithDarkness outPixel = 
					make_mf_retrivedHatchPixelWithDarkness( make_empty_mf_retrivedHatchPixel(),0);
				for (int y = 1; y < 4; y++) {
					if (rowStatus[y] < 2) {
						SeedSpec mainSeed = seedsColumn[y][ rowStatus[y]];
						SeedSpec RAMainSeed = RASeedsColumn[y][rowStatus[y]];
						SeedSpec southSeed = findSouthSeedRA(RAMainSeed,RABlockCoords +int2(leftBlockOffset+rowStatus[y], y-1), rotationSliceIndex, RAInScreenCoords, blockSize, sliceRotationAngle, tierIndex, paramPack);

						//color += drawOneHatchInOrientation(RABlockCoords, rotationSliceIndex, inScreenCoords, mainSeed, southSeed);
						float3 foundInfo = drawUnlinkedOneHatchInOrientation(RABlockCoords, rotationSliceIndex, inScreenCoords, mainSeed, paramPack);
						//newColor = applyLightHatchErasure(newColor, mainSeed, lightIntensity);
						mf_retrivedHatchPixelWithDarkness newPixel
							= sampleAndCreate_mf_retrivedHatchPixelWithDarkness(make_mf_retrivedHatchPixel(mainSeed.id, foundInfo.y, true, foundInfo.x, 0), foundInfo.z);
						if (newPixel.darkness > outPixel.darkness){
							outPixel = newPixel;
						}
					}
				}
				return outPixel;
			}

			float4 tierAwareDrawDebugGraphics(int tierIndex, uint2 screenCoords, float strokeAngle, ConstantParametersPack paramPack) {
				int rotationSliceIndex = calculateRotationSliceIndex(tierIndex, strokeAngle, paramPack);
				float sliceRotationAngle = computeSliceRotationAngle(tierIndex, rotationSliceIndex, paramPack);

				float2 RAInScreenCoords = rotateAroundPivot(screenCoords, sliceRotationAngle, _ScreenParams.xy / 2);
				int2 RABlockCoords = int2(floor(RAInScreenCoords.x / (float)paramPack.blockSize.x), floor(RAInScreenCoords.y / (float)paramPack.blockSize.y));
				// looping negative coords
				float2 RAThisPixelInBlockUv = toInBlockUv(RAInScreenCoords, int2Multiply(RABlockCoords,paramPack.blockSize), paramPack.blockSize);

				//////// COLOR UPDATING
				float4 color = 0;
				color += drawHaloAroundThisBlockSeed(RABlockCoords, rotationSliceIndex, screenCoords, tierIndex, paramPack);
				float2 RAInBlockCoords = abs(RAInScreenCoords- int2Multiply(RABlockCoords,paramPack.blockSize));
				color += drawBlockGrid( paramPack.blockSize, RAInBlockCoords);
				return color;
			}


			mf_retrivedHatchPixelWithDarkness tierAwareRenderer(int tierIndex, uint2 screenCoords, float strokeAngle, float lightIntensity, ConstantParametersPack paramPack) {
				int rotationSliceIndex = calculateRotationSliceIndex(tierIndex, strokeAngle, paramPack);
				float sliceRotationAngle = computeSliceRotationAngle(tierIndex, rotationSliceIndex, paramPack);

				float2 RAInScreenCoords = rotateAroundPivot(screenCoords, sliceRotationAngle, _ScreenParams.xy / 2);
				int2 RABlockCoords = int2(floor(RAInScreenCoords.x / (float)paramPack.blockSize.x), floor(RAInScreenCoords.y / (float)paramPack.blockSize.y));
				// looping negative coords
				float2 RAThisPixelInBlockUv = toInBlockUv(RAInScreenCoords, int2Multiply(RABlockCoords,paramPack.blockSize), paramPack.blockSize);
				
				///////// Colors from hatches
				return drawHatchesInOrientation(RABlockCoords, paramPack.blockSize, rotationSliceIndex,
					screenCoords, RAInScreenCoords, RAThisPixelInBlockUv, sliceRotationAngle, paramPack.blockCount, tierIndex, lightIntensity, paramPack); //TODO not pass things that are arleady in paramPack
			}

			bool hasFoundSgmInBlock(int tierIndex, uint2 screenCoords, float strokeAngle, float lightIntensity, ConstantParametersPack paramPack) {
				int rotationSliceIndex = calculateRotationSliceIndex(tierIndex, strokeAngle, paramPack);
				float sliceRotationAngle = computeSliceRotationAngle(tierIndex, rotationSliceIndex, paramPack);

				float2 RAInScreenCoords = rotateAroundPivot(screenCoords, sliceRotationAngle, _ScreenParams.xy / 2);
				int2 RABlockCoords = int2(floor(RAInScreenCoords.x / (float)paramPack.blockSize.x), floor(RAInScreenCoords.y / (float)paramPack.blockSize.y));
				
				///////// Colors from hatches
				SeedSpec sp = retriveSgm(tierIndex, RABlockCoords, rotationSliceIndex, paramPack);
				return sp.isActive;

			}

		mf_MRTFragmentOutput retrivedPixelToFragmentOutputX(mf_retrivedHatchPixel p, float3 worldSpacePos, float lightIntensity) { // TODO remove ?
			float4 artisticColor = 0;
			artisticColor = p.blendingHatchStrength;
			artisticColor.r = lightIntensity;

			float4 hatchMainColor = 0;
			hatchMainColor.r = round(p.blendingHatchStrength);
			hatchMainColor.g = lightIntensity;
			hatchMainColor.b = p.tParam;
			hatchMainColor.a = 1; // signal that this is hatched element

			float4 idColor = uintTo4Bytes(p.id);

			float4 positions1Color = 0;
			float4 positions2Color = 0;

			float max = 10;
			positions1Color.rg = to2ByteValue(worldSpacePos.x, max);
			positions1Color.ba = to2ByteValue(worldSpacePos.y, max);
			positions2Color.rg = to2ByteValue(worldSpacePos.z, max);

			return make_mf_MRTFragmentOutput(artisticColor, hatchMainColor, idColor, positions1Color, positions2Color);
		}


			mf_MRTFragmentOutput frag(v2f input) : SV_Target
			{
				float maxRotation = 2 * PI;

				float2 uv =  input.projPos.xy;
				float2 orgUv =  input.projPos.xy;
				uint2 screenCoords = input.pos.xy;


				///////// ANGLE RELATED
				ConstantParametersPack paramPack = createFromProperties_ConstantParametersPack();
				float4 worldPositionTexPixel = tex2D(_WorldPositionTex, uv);
				float3 worldSpacePos = worldPositionTexPixel.xyz;
				LightIntensityAngleOccupancy liao = unpackLightIntensityAngleOccupancy(worldPositionTexPixel.a);
				float lightIntensity = liao.lightIntensity;
				float strokeAngle = liao.angle;

				float4 debugGraphicsColor = tierAwareDrawDebugGraphics(0, screenCoords, strokeAngle, paramPack);

				mf_retrivedHatchPixelWithDarkness maxPixel =make_mf_retrivedHatchPixelWithDarkness( make_empty_mf_retrivedHatchPixel(),0);
				if (liao.occupancy) {
					[unroll(4)]
					for (int i = 0; i < _TierCount; i++) {
						mf_retrivedHatchPixelWithDarkness newPixel = tierAwareRenderer(i, screenCoords, strokeAngle, lightIntensity, paramPack);
						if (newPixel.darkness > maxPixel.darkness ) {
							maxPixel = newPixel;
						}
					}
				}

				float4 mainTexColor = tex2D(_ArtisticMainTex, orgUv);
				float4 artisticColor = lerp(mainTexColor, 0, maxPixel.darkness);
				artisticColor.a = 1;

				mf_MRTFragmentOutput fo =   retrivedPixelToFragmentOutputArtistic(maxPixel.pixel, worldSpacePos, lightIntensity);

#ifdef MEASUREMENT
				if (liao.occupancy) {
					artisticColor = lerp(float4(0.6, 0.6, 1, 1),0, maxPixel.darkness);
				}
				else {
					artisticColor = 1;
				}
#endif
				fo.dest0 = artisticColor;

				return fo;
			}
			 ENDCG

		}

	}
}
