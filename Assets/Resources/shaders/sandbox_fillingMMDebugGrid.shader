Shader "Custom/Sandbox/Filling/MMDebugGrid" {
	Properties{
		_StrokeSeedGridMap("StrokeSeedGridMap", 2DArray) = "blue"{}
		_MainTex ("MainTex", any) = "" {}
		_BaseMainTex ("BaseMainTex", any) = "" {}
		_StrokeTex("StrokeTex", 2D) = "blue"{}
		_DummyStage1Texture("DummyStage1Texture", 2D) = "blue"{}
		_DebugScalar("DebugScalar", Range(0,4)) = 0
		_SelectSgmRetrivalMethod("SelectSgmRetrivalMethod", Range(0,1)) = 0

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
			#pragma fragment frag
			#pragma target 5.0
			#include "UnityCG.cginc"
			
#include "filling_variables.txt"
#include "mm_common.txt"

			RWTexture2DArray<float4> _StrokeSeedGridMap;

			#define CUSTOM_MATRIX_VP _MyUnityMatrixVP
			#include "filling_common.txt"
			#include "filling_calculateSgmColor2.txt"
			#include "filling_calculateSgmFromTextures.txt"
			#include "filling_erasure.txt"
			#include "text_printing.hlsl"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : ANY_UV;
			};

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				return o;
			}
			float4 drawBlockGrid( uint2 blockSize, float2 inBlockCoords) {
				float4 color = 0;
				if (inBlockCoords.x <= 1 || inBlockCoords.y <= 1) {
					color = float4(0, 1, 0, 1);
				}
				if ( round(inBlockCoords.x) ==  blockSize.x/2.0 || round(inBlockCoords.y) == blockSize.y/2.0) {
					color = float4(1, 0, 0, 0.7);
				}
				return color;
			}


			float4 tierAwareDrawDebugGraphics(int tierIndex, uint2 screenCoords, float strokeAngle, ConstantParametersPack paramPack) {
				int rotationSliceIndex = calculateRotationSliceIndex(tierIndex, strokeAngle, paramPack);
				float sliceRotationAngle = computeSliceRotationAngle(tierIndex, rotationSliceIndex, paramPack);

				float2 RAInScreenCoords = rotateAroundPivot(screenCoords, sliceRotationAngle, _ScreenParams.xy / 2);
				int2 RABlockCoords = int2(floor(RAInScreenCoords.x / (float)paramPack.blockSize.x), floor(RAInScreenCoords.y / (float)paramPack.blockSize.y));
				// looping negative coords

				//////// COLOR UPDATING
				float4 color = 0;
				float2 RAInBlockCoords = abs(RAInScreenCoords- int2Multiply(RABlockCoords,paramPack.blockSize));
				color += drawBlockGrid( paramPack.blockSize, RAInBlockCoords);
				color.a = saturate(color.a);
				return color;
			}

			bool coordsAreInAxis(uint2 screenCoords, uint2 expected) {
				bool x = screenCoords.x == expected.x;
				bool y =  screenCoords.y == expected.y;

				return (x || y) && !(x&&y);
			}

			float4 frag(v2f input) : SV_Target
			{
				float2 uv =  input.projPos.xy;
				float2 orgUv =  input.projPos.xy;
				uint2 screenCoords = input.pos.xy;

				///////// ANGLE RELATED
				float strokeAngle = _RotationSliceIndex * (2 * 3.14159 / 16.0);

				float4 worldPositionTexPixel = tex2D(_WorldPositionTex, uv);
				LightIntensityAngleOccupancy liao = unpackLightIntensityAngleOccupancy(worldPositionTexPixel.a);
				strokeAngle = liao.angle;

				ConstantParametersPack paramPack = createFromProperties_ConstantParametersPack();

				float4 dGraphics = tierAwareDrawDebugGraphics(0, screenCoords, strokeAngle, paramPack);
				return lerp(tex2D(_MainTex, uv), dGraphics, saturate(dGraphics.a-0.5));
			}

			 ENDCG

		}

	}
}

