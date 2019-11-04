Shader "Custom/Sandbox/Filling/MMDebug" {
	Properties{
		_TamIdTex("TamIdTex", 2D) = "black"{}
		_FragmentTexWidth("FragmentTexWidth", Range(0,512)) = 0
		_StrokeSeedGridMap("StrokeSeedGridMap", 2DArray) = "" {}
		_RotationSlicesCount("RotationSlicesCount", Int) = 16
		_BlockSize("BlockSize", Vector) = (32.0,32.0,0,0)

		_BlockCount("BlockCount", Vector) = (32.0,32.0,0,0)

		_DebugScalar("DebugScalar", Range(0,8)) = 1
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

			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 worldSpacePos : ANY_WORLD_SPACE_POS;
				float2 uv : ANY_UV;
			};

			RWTexture2DArray<float4> StrokeSeedGridMap;
			int _RotationSlicesCount;
			float2 _BlockSize;
			float2 _BlockCount;
			float _DebugScalar;

			static float PI = 3.14159;

			v2f vert(appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.worldSpacePos = mul(unity_ObjectToWorld, in_v.vertex);
				o.uv = in_v.uv;  

				return o; 
			}

			bool pointInBlock(uint2 blockSize, uint2 RABlockCoords, uint2 pointCoord) {
				return
					pointCoord.x < blockSize.x * RABlockCoords.x &&
					pointCoord.x > blockSize.x * (RABlockCoords.x + 1) &&
					pointCoord.y < blockSize.y * RABlockCoords.y &&
					pointCoord.y > blockSize.y * (RABlockCoords.y + 1);
			}


			float calculateScreenStrokeAngle(float maxRotation, float3 ourWorldSpacePos){
				float3 worldSpaceStrokeDirection = normalize(float3(0, 0, 1));
				float4 debPoint1 = UnityObjectToClipPos(ourWorldSpacePos);
				debPoint1 /= debPoint1.w;
				float4 debPoint2 = UnityObjectToClipPos((ourWorldSpacePos) + worldSpaceStrokeDirection);
				debPoint2 /= debPoint2.w;
				float2 strokeDirection =(debPoint2.xy - debPoint1.xy); //to rozwiązanie (a nie stare strokeDirection) daje troszkę lepsze wyniki

				// ClipSpace is normalized to <0,1> But screen is not rectangle. Have to take this into account;
				strokeDirection.x *= _ScreenParams.x;
				strokeDirection.y *= _ScreenParams.y;

				return  fmod(-(atan2(strokeDirection.y, strokeDirection.x)) + maxRotation, maxRotation);

				//float2 v = normalize(mul((float3x3)UNITY_MATRIX_V, float3(1, 0, 0)).xy); -- TODO alternatywa - opisz. Wszystkie stroke w jedną strone na obiekcie, ale nie ustawiają sie w linie
				//return atan2(v.y, v.x);
			}

			float2 rotatePoint( float2 pos, float rotation){
				float sinX = sin (rotation);
				float cosX = cos (rotation);
				float sinY = sin (rotation);
				float2x2 rotationMatrix = float2x2( cosX, -sinX, sinY, cosX);
				return mul(pos, rotationMatrix);
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

			bool UpdateSSGMap(int rotationSliceIndex, float rotationSliceLength, float rotationSlicesCount, float strokeAngle, uint2 screenCoords, uint2 blockSize, float2 delta, float2 dg, float2 dh, float ssgmStrokeAngle) {
				float sliceRotationAngle = computeSliceRotationAngle(rotationSliceIndex, rotationSliceLength, rotationSlicesCount);

				// looping negative coords
				uint2 blockCount = uint2(round(_BlockCount.x), round(_BlockCount.y));

				float2 RAScreenCoords = rotateAroundPivot(screenCoords, sliceRotationAngle, _ScreenParams.xy / 2);
				int2 RABlockCoords = int2(floor(RAScreenCoords.x / (float)blockSize.x), floor(RAScreenCoords.y / (float)blockSize.y));
				RABlockCoords = (RABlockCoords + blockCount * 2) % blockCount;
				RAScreenCoords = float2(
					fmod(RAScreenCoords.x + blockCount.x*blockSize.x, blockCount.x*blockSize.x),
					fmod(RAScreenCoords.y + blockCount.y*blockSize.y, blockCount.y*blockSize.y));

				uint2 RAInBlockCoords =  RAScreenCoords - int2(RABlockCoords.x*blockSize.x, RABlockCoords.y*blockSize.y);

				if ( abs(RAInBlockCoords.x - blockSize.x/2) < 2 && abs(RAInBlockCoords.y - blockSize.y/2) < 2) {
					float2 ourPos = screenCoords;

					// basycly it is
					// a*dg + b*dh = delta
					// we have to compute dx and dy
					// solve a*{g1;g2} + b*{h1;h2} = {c1;c2} for a,b
					float a = (delta.y*dh.x - delta.x*dh.y) / (dg.y*dh.x - dg.x*dh.y);
					float b = (delta.y*dg.x - delta.x*dg.y) / (dg.x*dh.y - dg.y*dh.x);
					float2 predictedOffset = float2(a, b);

					float2 seedPosition = screenCoords + predictedOffset;
					float2 RASeedPosition = rotateAroundPivot(seedPosition, sliceRotationAngle, _ScreenParams.xy / 2);

					// looping negative coords
					RASeedPosition = float2(
						fmod(RASeedPosition.x + blockCount.x*blockSize.x, blockCount.x*blockSize.x),
						fmod(RASeedPosition.y + blockCount.y*blockSize.y, blockCount.y*blockSize.y));

					uint2 seedBlockCoords = 
						uint2(floor(RASeedPosition.x / (float)blockSize.x), floor(RASeedPosition.y / (float)blockSize.y));

					if (seedBlockCoords.x == RABlockCoords.x && seedBlockCoords.y == RABlockCoords.y) {
					//if (length(seedBlockCoords -RABlockCoords) <=1){ //overdraw possibility
						//float ssgmStrokeAngle = fmod(2 * PI - strokeAngle, 2 * PI);
						float4 pColor = float4(seedPosition, ssgmStrokeAngle, 1);

						StrokeSeedGridMap[ uint3(RABlockCoords,rotationSliceIndex) ] = pColor;

						return true;
					}
				}
				return false;
			}


			float4 frag(v2f input) : SV_Target
			{
				float maxRotation = 2 * PI;
				float rotationSlicesCount = _RotationSlicesCount;
				float rotationSliceLength = (maxRotation) / rotationSlicesCount;
				uint2 blockSize = uint2( round(_BlockSize.x), round(_BlockSize.y) );

				uint2 screenCoords = input.pos.xy;
				float3 worldSpacePos = input.worldSpacePos;
				float2 uv = input.uv;
				float2 screenUv = float2(screenCoords.x / _ScreenParams.x, screenCoords.y / _ScreenParams.y);

				float2 dg = ddx(uv);
				float2 dh = ddy(uv);

				float2 uvSeed = float2(0.4, 0.38);
				if (uv.y > 0.45) {
					uvSeed = float2(0.42, 0.5);
				}
				uvSeed = round(uv * 10) / 10.0  ;

				float2 delta =  uvSeed - uv;

				float strokeAngle = calculateScreenStrokeAngle(maxRotation, worldSpacePos);
				float sAng =  strokeAngle - (PI*1.5);
				int ss = sign(sAng);

				sAng = pow(abs(sAng), _DebugScalar);
				strokeAngle = PI * 1.5 + sAng * ss;
				
				//if (strokeAngle < _DebugScalar*1.01 && strokeAngle > _DebugScalar*0.99) {
				//	return 1;
				//}

				float oldStrokeAngle = strokeAngle;

				//strokeAngle = fmod(2 * PI - strokeAngle, 2 * PI);
				//strokeAngle = PI * 1.5;
				//strokeAngle = PI * 2 * 25 / 32.0;
				int rotationSliceIndex = floor(rotationSlicesCount * strokeAngle / maxRotation);

				float inSliceAngle = fmod(strokeAngle, rotationSliceLength);
				float inSlicePercent = (inSliceAngle / rotationSliceLength);

				bool pixelSet = UpdateSSGMap(rotationSliceIndex, rotationSliceLength, rotationSlicesCount, strokeAngle, screenCoords, blockSize, delta, dg, dh, oldStrokeAngle);

				int extraRotationSliceIndex = 0;
				if (inSlicePercent < 0.5) {
					extraRotationSliceIndex = (rotationSliceIndex + rotationSlicesCount - 1) % rotationSlicesCount;
				}
				else {
					extraRotationSliceIndex = (rotationSliceIndex + 1) % rotationSlicesCount;
				}


				pixelSet = pixelSet | UpdateSSGMap( extraRotationSliceIndex, rotationSliceLength, rotationSlicesCount, strokeAngle, screenCoords, blockSize, delta, dg, dh, oldStrokeAngle);
				if (pixelSet) {
					return float4(1, 1, strokeAngle, 0);
				}

				if (length(uvSeed - uv) < 0.002) {
					return float4(1,0, strokeAngle, 1);
				}

				return float4( 0.0001*dg + 0.0001*dh,strokeAngle,1);
			}
			ENDCG
		}
	}
}
