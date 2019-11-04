Shader "Custom/Sandbox/Filling/MMGeometryRenderer" {
	Properties{
		_StrokeSeedGridMap("StrokeSeedGridMap", 2DArray) = "blue"{}
		_MainTex ("MainTex", any) = "" {}
		_StrokeTex("StrokeTex", 2D) = "blue"{}
		_DebugScalar("DebugScalar", Range(0,4)) = 0
		_SelectSgmRetrivalMethod("SelectSgmRetrivalMethod", Range(0,1)) = 0
		_DummyStage1Texture("DummyStage1Texture", 2D) = "blue"{}

		_ArcCurvatureMargin("ArcCurvatureMargin", Range(0,1)) = 0.1
		_HatchLength("HatchLength", Range(0,128)) = 10
		_DistanceLinkingMargin("DistanceLinkingMargin", Range(0,10)) = 5
		_RotationSliceIndex("RotationSliceIndex",Range(0,16)) = 0

		_RotationSlicesCount("RotationSlicesCount", Int) = 16
		_BlockSize("BlockSize", Vector) = (32.0,32.0,0,0)

		_BlockCount("BlockCount", Vector) = (32.0,32.0,0,0)

		_WorldPositionTex("WorldPositionTex", 2D) = "blue"{}
		_VectorsTex("VectorsTex", 2D) = "blue"{}
		_SeedPositionTex3D("SeedPositionTex3D", 3D) = "" {}

		_MaximumPointToSeedDistance("MaximumPointToSeedDistance", Range(0,10)) = 1.0
		_SeedDensityF("SeedDensityF", Range(0,10)) = 1.0
		_MZoomPowFactor("MZoomPowFactor", Range(0,3)) = 0.7

		_SeedSamplingMultiplier("SeedSamplingMultiplier", Range(0,10)) = 1.0

		_DistanceToLineTreshold("DistanceToLineTreshold", Range(0,5)) = 2.0

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
		// THIS SHADER TAKES FRAGMENT BUFFER AND RENDERS IT ON MIN MAX TEXTURE
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma target 5.0
			#pragma multi_compile __ MEASUREMENT
			#pragma multi_compile __ LIGHT_SHADING_ON
			#pragma multi_compile __ DIRECTION_PER_LIGHT
			#include "UnityCG.cginc"
			
			Texture2DArray<float4> _StrokeSeedGridMap;

#include "mm_common.txt"
#include "filling_variables.txt"

			#define CUSTOM_MATRIX_VP _MyUnityMatrixVP
			#include "filling_common.txt"
			#include "filling_calculateSgmColor2.txt"
			#include "filling_calculateSgmFromTextures.txt"
			#include "filling_erasure.txt"
			StructuredBuffer<int4> _GeometryRenderingFragmentBuffer;

#include "mf_common.txt"

			struct v2g {
				float4 pos : SV_POSITION;
			};

			struct g2f {
				float4 pos : SV_POSITION;
				float4 token : ANY_TOKEN;
				uint id : ANY_ID;
				float tParam : ANY_TPARAM;
			};

			g2f make_g2f(float4 pos, float4 token, uint id, float tParam) {
				g2f g;
				g.pos = pos;
				g.token = token;
				g.id = id;
				g.tParam = tParam;   
				return g;
			}

			v2g vert(uint id : SV_VertexID) 
			{
				v2g o;
				o.pos = 0.5;
				return o;
			}

#define SEGMENT_COUNT 2

			[maxvertexcount(SEGMENT_COUNT*3*3)]
			void geom(point v2g input[1], uint pid : SV_PrimitiveID, inout TriangleStream<g2f> outStream)
			{
				ConstantParametersPack paramPack = createFromProperties_ConstantParametersPack();

				int4 ssc = _GeometryRenderingFragmentBuffer[pid];
				int3 ssgmCoords = ssc.xyz;

				float2 hatchScreenUv = 0;
				float hatchAngle = 0;

				float fFac = sqrt(pow(_ScreenParams.x, 2)+ pow(_ScreenParams.y, 2));
				float segmentWidth = paramPack.hatchWidth*2.5 / fFac;
				float segmentLength =  paramPack.hatchLength *10 / fFac;

				float4 token = 0;
					float4  retrived = _StrokeSeedGridMap[ssgmCoords];

					float2 seedScreenCoords = UnpackScreenCoordsFromUint(asuint(retrived.r));
					float seedAngle = retrived.z;
					float seedAlphaMultiplier = retrived.a;
					uint seedId = asuint(retrived.g);
					uint tier = seedId >> 30;

					hatchScreenUv = seedScreenCoords;
					hatchScreenUv.x /= _ScreenParams.x;
					hatchScreenUv.y /= _ScreenParams.y;

					float2 screenStrokeStart = hatchScreenUv * 2 - 1;
					screenStrokeStart.y *= -1;
					screenStrokeStart = 0.2;
					screenStrokeStart.x = 0.4;

					hatchAngle = seedAngle;

					float lodStrength = tex2Dlod(_DummyStage1Texture, float4(hatchScreenUv,0,2))[tier];
					if (lodStrength > 0.1) {
						token.a = 1;
					}else{
						token.a = 0;
					}

					float2 centerPoints[SEGMENT_COUNT];

					for (int i = 0; i < SEGMENT_COUNT; i++) {
						float t = (((float)i) / (SEGMENT_COUNT - 1));
						float2 strokeD = (float2(sin(hatchAngle), cos(hatchAngle))).yx*  t / SEGMENT_COUNT  * segmentLength;
						float2 pos = hatchScreenUv + strokeD;// rotateAroundPivot(strokeD, hatchAngle, 0);
						pos.xy = (pos.xy * 2) - 1;
						pos.y *= -1;
						//pos.xy /= 10;

						centerPoints[i] = pos;
					}

					float2 segVectors[SEGMENT_COUNT - 1];
					for (int i = 0; i < SEGMENT_COUNT - 1; i++) {
						float2 delta = centerPoints[i + 1] - centerPoints[i];
						segVectors[i] = normalize(delta);
					}

					float2 perpVectors[SEGMENT_COUNT];
					for (int i = 0; i < SEGMENT_COUNT; i++) {
						float2 currentSegVec = segVectors[min(max(0, i), SEGMENT_COUNT - 2)];
						float2 prevSegVec = segVectors[min(max(0, i - 1), SEGMENT_COUNT - 2)];
						float2 averageVec = normalize(currentSegVec + prevSegVec);
						float2 perpVec = float2(averageVec.y, -averageVec.x);

						perpVectors[i] = perpVec;
					}

					float2 newPoints[SEGMENT_COUNT * 2];
					for (int i = 0; i < SEGMENT_COUNT; i++) {
						newPoints[i * 2 + 0] =  centerPoints[i] + perpVectors[i] * segmentWidth;
						newPoints[i * 2 + 1] =  centerPoints[i] - perpVectors[i] * segmentWidth;
					}


					for (int i = 0; i < SEGMENT_COUNT; i++) {
						float t = ((float)i) / (SEGMENT_COUNT - 1);
						outStream.Append(make_g2f(float4(newPoints[i * 2 + 1], 1, 1), token, seedId, t));
						outStream.Append(make_g2f(float4(newPoints[i * 2 + 0], 1, 1), token, seedId, t));
					}
					outStream.RestartStrip();


			}

		    mf_MRTFragmentOutput frag(g2f input) : SV_Target
			{
				float4 artisticColor = 0;
				if (input.token.a > 0.5) {
					artisticColor = float4(0.5, 1, 0, 1);
				}
				else {
					artisticColor = 1;
					discard;
				}

				float4 wspPixel = tex2Dlod(_WorldPositionTex, float4( intScreenCoords_to_uv( input.pos.xy), 0,0)) ;
				float3 worldSpacePos = wspPixel.xyz;
				LightIntensityAngleOccupancy liao = unpackLightIntensityAngleOccupancy(wspPixel.a);
				float lightIntensity = liao.lightIntensity;

				if (!liao.occupancy) {
					discard;
				}

				float blendingHatchStrength = 1;
				mf_retrivedHatchPixel pixel = make_mf_retrivedHatchPixel(input.id, input.tParam, true, blendingHatchStrength, 0);

				mf_MRTFragmentOutput output = retrivedPixelToFragmentOutput(pixel, worldSpacePos, lightIntensity);
				output.dest0 = artisticColor;
				return output;
			}

			 ENDCG

		}

	}
}

