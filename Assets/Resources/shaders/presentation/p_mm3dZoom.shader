Shader "Custom/Presentation/MM3DZoom" {
	Properties{
		_TamIdTex("TamIdTex", 2D) = "black"{}
		_FragmentTexWidth("FragmentTexWidth", Range(0,512)) = 0
		_StrokeSeedGridMap("StrokeSeedGridMap", 2DArray) = "" {}
		_RotationSlicesCount("RotationSlicesCount", Int) = 16
		_BlockSize("BlockSize", Vector) = (32.0,32.0,0,0)

		_BlockCount("BlockCount", Vector) = (32.0,32.0,0,0)

		_DebugScalar("DebugScalar", Range(0,1)) = 1

		_SeedPositionTex3D("SeedPositionTex3D", 3D) = "" {}

		_MaximumPointToSeedDistance("MaximumPointToSeedDistance", Range(0,10)) = 1.0
		_SeedDensityF("SeedDensityF", Range(0,10)) = 1.0
		_MZoomPowFactor("MZoomPowFactor", Range(0,3)) = 0.7
		_SeedSamplingMultiplier("SeedSamplingMultiplier", Range(0,10)) = 1.0
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
			
			#include "UnityCG.cginc"

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
			};

			struct MRTFragmentOutput
			{
				float4 dest0 : SV_Target0;
				float4 dest1 : SV_Target1;
				float4 dest2 : SV_Target2;
				float4 dest3 : SV_Target3;
			};

			RWTexture2DArray<float4> StrokeSeedGridMap;
			int _RotationSlicesCount;
			float2 _BlockSize;
			float2 _BlockCount;
			float _DebugScalar;
			float _MaximumPointToSeedDistance;
			float _SeedDensityF;
			float _MZoomPowFactor;
			float _SeedSamplingMultiplier;

			Texture3D<float4> _SeedPositionTex3D;

			static float PI = 3.14159;

			v2f vert(appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.worldSpacePos = mul(unity_ObjectToWorld, in_v.vertex);
				o.uv = in_v.uv;  
				o.norm = UnityObjectToWorldNormal(in_v.norm);
				o.direction = UnityObjectToWorldNormal(cross( in_v.tangent, in_v.norm));

				return o; 
			}


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
						for (int z = 0; z < 1; z++) {
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




			float2 worldSpacePosToScreenUv(float3 worldSpacePos) {
				float4 clipPos = mul(UNITY_MATRIX_VP, float4(worldSpacePos, 1.0));
				float2 clipPosNorm= (clipPos / clipPos.w).xy;
				clipPosNorm = (clipPosNorm + 1) / 2;
				clipPosNorm.y = 1 - clipPosNorm.y;
				return clipPosNorm;
			}

			float2 worldSpacePosToScreenCoords(float3 worldSpacePos) {
				float2 clipPosNorm = worldSpacePosToScreenUv(worldSpacePos);
				return float2(clipPosNorm.x * _ScreenParams.x, clipPosNorm.y * _ScreenParams.y);
			}

			float3 projectPointOnPlane(float3 origin, float3 normal, float3 p) {
				float3 v = p - origin;
				float dist = dot(v, normal);
				return p - dist * normal;
			}


			float calculateScreenStrokeAngle(float maxRotation, float3 ourWorldSpacePos, float3 worldSpaceStrokeDirection){
				//worldSpaceStrokeDirection = normalize(float3(0, 1, 0));
				//float3 worldSpaceStrokeDirection = normalize(float3(0, -1, 0));
				float2 debPoint1 = worldSpacePosToScreenCoords(ourWorldSpacePos);
				float2 debPoint2 = worldSpacePosToScreenCoords((ourWorldSpacePos) + worldSpaceStrokeDirection);
				float2 strokeDirection =(debPoint2.xy - debPoint1.xy); //to rozwiązanie (a nie stare strokeDirection) daje troszkę lepsze wyniki

				strokeDirection = normalize(strokeDirection);

				return  fmod((atan2(strokeDirection.y, strokeDirection.x)) + maxRotation + PI, maxRotation);

				float2 v = normalize(mul((float3x3)UNITY_MATRIX_V, float3(0, -1, 0)).xy);// -- TODO alternatywa - opisz. Wszystkie stroke w jedną strone na obiekcie, ale nie ustawiają sie w linie
				return (-atan2(v.y, v.x))+PI;
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

				uint2 RAInBlockCoords =  RAScreenCoords - int2(RABlockCoords.x*blockSize.x, RABlockCoords.y*blockSize.y);

				return make_RAScreenInfo(RABlockCoords, RAScreenCoords, RAInBlockCoords);
			}

			bool shouldUpdateSSGM(RAScreenInfo info, uint2 blockCount, uint2 blockSize, float2 seedPosition, float sliceRotationAngle) {
				if ( abs(info.RAInBlockCoords.x - blockSize.x/2) < 2 && abs(info.RAInBlockCoords.y - blockSize.y/2) < 2) {

					float2 RASeedPosition = rotateAroundPivot(seedPosition, sliceRotationAngle, _ScreenParams.xy / 2);

					// looping negative coords
					RASeedPosition = float2(
						fmod(RASeedPosition.x + blockCount.x*blockSize.x, blockCount.x*blockSize.x),
						fmod(RASeedPosition.y + blockCount.y*blockSize.y, blockCount.y*blockSize.y));

					uint2 seedBlockCoords =
						uint2(floor(RASeedPosition.x / (float)blockSize.x), floor(RASeedPosition.y / (float)blockSize.y));

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
				p.gridSpaceMultiplier = float3(1, 1, 6);
				p.seedDensityF = _SeedDensityF;
				p.mZoomPowFactor = _MZoomPowFactor;
				p.maximumPointToSeedDistance = _MaximumPointToSeedDistance;
				p.seedSamplingMultiplier = _SeedSamplingMultiplier;
				return p;
			}

			float3 transportToGridSpace(float3 newStraight, float3 newUp, float3 p) {
				//p.xyz = p.zyx;
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
				
				float3 r = mul(LocalToWorldTransform, p);
				//r.xyz = r.zyx;
				return r;
			}

			float4 calculateSgmColor2(float3 worldSpacePos, float3 worldSpaceSeedPosition, float3 normal, float strokeAngle, ConstantParametersPack paramPack, uint2 screenCoords) {

				float3 worldSpaceSeedPositionOnSurface = projectPointOnPlane(worldSpacePos, normal, worldSpaceSeedPosition);
				if ( true || length(worldSpaceSeedPosition - worldSpaceSeedPositionOnSurface) < paramPack.maximumPointToSeedDistance) { //i do  not use 3D seeds TODO
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
						StrokeSeedGridMap[uint3(RAinfo.RABlockCoords, rotationSliceIndex)] = pColor;
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
						StrokeSeedGridMap[uint3(RAinfo.RABlockCoords, rotationSliceIndex)] = pColor;
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

			float3 quantisizeNormalizedVector2(float3 v, int quantCount) {
				return normalize(round(v* quantCount) / ((float)quantCount));
			}


			float2 getYawAndPitch(float3 v) {
				//return float2(v.x / -v.y, sqrt(v.x *v.x + v.y*v.y) / v.z);
				float at2 = atan2(v.x, v.z);
				if (isnan(at2)) {
					at2 = 0;
				}

				return float2(at2, asin(-v.y));
			}

			float3 yawAndPitchToVector(float2 yawAndPitch) {
				float alpha = yawAndPitch.x;
				float beta = yawAndPitch.y;
				float3 v = float3(
					sin(alpha)*cos(beta), 
					-sin(beta),
					cos(alpha) * cos(beta)
				);
				return v;
			}

			float3 quantisizeNormalizedVector(float3 v, int quantCount) {
				float2 yAndP = getYawAndPitch(v);
				yAndP = round((yAndP / PI)*quantCount) * PI / quantCount;
				return normalize(yawAndPitchToVector(yAndP));
			}

			float3 quantisizeNormalizedVector4(float3 v, int quantCount) {
				return v;
			}

			MRTFragmentOutput	frag(v2f input) : SV_Target
			{
				ConstantParametersPack paramPack = createFromProperties_ConstantParametersPack();
				int quantCount = 8;

				uint2 screenCoords = input.pos.xy;
				float3 worldSpacePos = input.worldSpacePos;

				float3 alignmentVector = quantisizeNormalizedVector(normalize(input.direction), quantCount);
				float3 normalVector = quantisizeNormalizedVector(normalize(input.norm), quantCount);
				float3 crossAlignmentVector =(cross(alignmentVector, normalVector));

				float2 uv = input.uv;
				float2 screenUv = float2(screenCoords.x / _ScreenParams.x, screenCoords.y / _ScreenParams.y);

				float strokeAngle = calculateScreenStrokeAngle(paramPack.maxRotation, worldSpacePos, alignmentVector);

				float sAng =  strokeAngle - (PI*1.5);
				int ss = sign(sAng);
				sAng = pow(abs(sAng), 1);
				strokeAngle = PI * 1.5 + sAng * ss;
				float oldStrokeAngle = strokeAngle;
				//strokeAngle = PI * 2 * 24 / 32.0;

				float M = calculateMZoom(screenCoords, worldSpacePos, alignmentVector, crossAlignmentVector , paramPack);
				float z = abs(M);


				float r = (floor(z) % 2) / 2.0;
				float g = (2 + floor(z) % 4) / 4.0;
				float b = (4 + floor(z) % 8) / 8.0;
				float4 color0 = float4(r, g, b, 0);

				MRTFragmentOutput output;
				output.dest0 = color0;
				output.dest1 = float4(worldSpacePos,0);
				output.dest2 = float4(normalVector,0);
				output.dest3 = float4(alignmentVector,0);
				return output;
			}
			ENDCG
		}
	}
}
