Shader "Custom/Measurements/Filling/Szecsi" {
	Properties{
		_UniformCycle("UniformCycle", Int) = 0
		_UniformCycleX("UniformCycleX", Int) = 0
		_UniformCycleY("UniformCycleY", Int) = 0
		_FSeedDensity("FSeedDensity",Range(0,1000)) = 1
		_MZoomPowFactor("MZoomPowFactor",Range(0,10)) = 1.0
		_DebugScalar("DebugScalar", Range(0,1)) = 1

		_ScaleChangeRate("ScaleChangeRate", Range(0,10)) = 1
		_DetailFadeRate("DetailFadeRate", Range(0,10)) = 1
		_StrokeSize("StrokeSize", Range(0,10)) = 1
		_Rotation("Rotation", Range(0,10)) = 0

		_HatchTex("HatchTex", 2D) = "pink"{}

    }

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ MEASUREMENT
			#pragma multi_compile __ LIGHT_SHADING_ON
			#pragma multi_compile __ DIRECTION_PER_LIGHT
			#include "UnityCG.cginc"
#include "filling_common.txt"
#include "mf_common.txt"
						
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
				float3 tangent : TANGENT;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 objectSpacePos : ANY_OBJECT_SPACE_POS;
				float3 worldSpacePos : ANY_WORLD_SPACE_POS;
				float2 uv : ANY_UV;
				float3 tangent : ANY_TANGENT;
				float3 normal : ANY_NORMAL;
			};

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.objectSpacePos = in_v.vertex.xyz;
				o.uv = in_v.uv;
				o.worldSpacePos = mul(unity_ObjectToWorld, in_v.vertex); 

				o.normal = UnityObjectToWorldNormal(in_v.norm);
				o.tangent= UnityObjectToWorldNormal(in_v.tangent);

				return o;
			}

			float _FSeedDensity;
			float _MZoomPowFactor;
			uint _UniformCycle;
			uint _UniformCycleX;
			uint _UniformCycleY;
			float _DebugScalar;
			float _ScaleChangeRate;
			float _DetailFadeRate;
			float _StrokeSize;
			float _Rotation;

			sampler2D _HatchTex;

			Buffer<int> _UniformCyclesBuf;

			float2x2 createRotationMatrix(float fi) {
				float sinX = sin (fi);
				float cosX = cos (fi);
				float2x2 rotationMatrix = float2x2( cosX, -sinX, sinX, cosX);
				return rotationMatrix;
			}

			float2 waveNotation(float2 a) {
				//return float2(frac(a.x + 0.5) - 0.5, frac(a.y + 0.5) - 0.5);
				return float2(frac(a.x + 0.5) , frac(a.y + 0.5) );
				//return a;
			}

			float2 elementwiseDivision(float2 a, float2 b) {
				return float2(a.x / b.x, a.y / b.y);
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


			float _GlobalMMultiplier;
			float calculateMZoom(uint2 screenCoords, float3 worldSpacePos, float3 testVector1, float3 testVector2, float seedDensityF, float mZoomPowFactor) {
				float mp = 0.0001;
				float a1 = length(worldSpacePosToScreenCoords(worldSpacePos) - worldSpacePosToScreenCoords(worldSpacePos + testVector1*mp));
				float a2 = length(worldSpacePosToScreenCoords(worldSpacePos) - worldSpacePosToScreenCoords(worldSpacePos + testVector2*mp));
				float a3 = length(worldSpacePosToScreenCoords(worldSpacePos) - worldSpacePosToScreenCoords(worldSpacePos + (testVector1+testVector2)*mp));
				float G = min(min(a1,a2),a3) / mp;
				G /= 100;

				float T = 1;
				float F = seedDensityF;
				float M = -log2(G*T*F);
				float rt = M - (-4.5); // For M = -4.5 rt = 0  For M = -2.5 rt = 2
				if (rt > 0) { // M > -4.5
					rt = pow(rt,mZoomPowFactor);
				}
				M = rt - 4.5;
				return M;
			}

			float3 projectVectorOntoPlane(float3 planeN, float3 u) {
				return u - (dot(u, planeN) / pow(length(planeN), 2)) * planeN;
			}

			uint generateSeedFromUv(float2 uv) {
				return abs(round(uv.x*543.12) + round(uv.y*7291.231)) % 65536;
			}

			uint generateIdFromGlobalUv(float2 globalUv, uint twoBitsIdSuffix) {
				globalUv = frac(globalUv);
				uint xShort = round(globalUv.x * 32767.0);
				uint yShort = round(globalUv.y * 32767.0);

				return (xShort + (yShort << 15))  | twoBitsIdSuffix << 30;
			}

			mf_retrivedHatchPixel szecsiSample(float M, float m, float2 uv, float lightIntensity, float rotation, float2 lightTresholdBounds, uint twoBitsIdSuffix) {
				//uint cycle = asuint(_UniformCycle);
				uint2 cycles = uint2 (asuint(_UniformCycleX), asuint(_UniformCycleY));
				//uint2 cycles = uint2 (asuint(_UniformCyclesBuf[1]), asuint(_UniformCyclesBuf[2]));
				uint topMask = 4294901760; // 0x‭FFFF0000‬;
				int cycleLength = 32;
				float maximum = pow(2, 32);

				mf_retrivedHatchPixel maxPixel = make_empty_mf_retrivedHatchPixel();

				for (int i = 0; i < 16; i++) { // for each seed
					float2 s_uv = uv * pow(2, -floor(M)); //seed space
					float2 selectedSeedPosBlockCoords = floor(s_uv);

					float2 seedPos = 0;

					seedPos.x = cycles.x / maximum;
					seedPos.y = cycles.y / maximum;

					float2 seedDelta = frac(s_uv) - seedPos;
					if (seedDelta.x > 0.5) {
						selectedSeedPosBlockCoords.x += 1;
					}
					if (seedDelta.x < -0.5) {
						selectedSeedPosBlockCoords.x -= 1;
					}
					if (seedDelta.y > 0.5) {
						selectedSeedPosBlockCoords.y += 1;
					}
					if (seedDelta.y < -0.5) {
						selectedSeedPosBlockCoords.y -= 1;
					}
					float2 seedPosInGlobalUv =  selectedSeedPosBlockCoords +seedPos;
					seedPosInGlobalUv /=  pow(2, -floor(M)); //seed space


					int2 w = floor( ((s_uv) - seedPos) + float2(0.5, 0.5));

					bool seedIsInSparselevelToo = ((w.x % 2) == cycles.x % 2) && (cycles.y % 2 == (w.y % 2));

					float alpha = 1;
					if (!seedIsInSparselevelToo) {
						alpha = saturate((1 - m)*_DetailFadeRate);
					}


					float thisStrokeRotation = rotation;
					float cosAngle = cos(thisStrokeRotation);
	                float sinAngle = sin(thisStrokeRotation);
	                float2x2 rot = float2x2(cosAngle, -sinAngle, sinAngle, cosAngle);
 
					s_uv = frac(mul( rot, s_uv));

					float2 texUv = frac(s_uv - seedPos);
					float hatchStrokeScale = pow(2,1-m)/(_StrokeSize);
					texUv = texUv *  hatchStrokeScale + 0.5;
					texUv = fmod(texUv + hatchStrokeScale, hatchStrokeScale);

					float4 texColor = tex2Dlod(_HatchTex, float4(texUv,0,0));
					alpha = alpha * texColor.a;

					uint hatchId = generateIdFromGlobalUv(seedPosInGlobalUv, twoBitsIdSuffix);
					int seed = generateSeedFromUv(seedPos);

					float tParam =  texColor.b;

					float lightTreshold = (seed % 64) / 63.0;
					lightTreshold *= (lightTresholdBounds.y - lightTresholdBounds.x);
					lightTreshold += lightTresholdBounds.x;

					if (lightIntensity < lightTreshold) {
						if (alpha > maxPixel.blendingHatchStrength) {
							maxPixel = make_mf_retrivedHatchPixel(hatchId, tParam, true, alpha, 0);
						}
					}

					cycles.x = ((cycles.x << 1) | (cycles.x >> (cycleLength - 1)));
					cycles.y = ((cycles.y << 1) | (cycles.y >> (cycleLength - 1)));
				}
				return maxPixel;
			}


			mf_MRTFragmentOutput frag (v2f input) : SV_Target
			{
				float2 uv = input.uv;
				float2 screenCoords = input.pos.xy;
				float3 worldSpacePos = input.worldSpacePos;
				float3 tangent = normalize(input.tangent);
				float3 normal = normalize(input.normal);
				float3 crossTangent = cross(tangent, normal);

				float lightIntensity =  computeLightIntensity(worldSpacePos, normal);

				float M = calculateMZoom(screenCoords, worldSpacePos, tangent, crossTangent, _FSeedDensity, _MZoomPowFactor);
				float m = M - floor(M); //m == 0 w oddali od granicy. m == 1 przy granicy między poziomami
				float2 M2 = M - 0.5;
				float m2 = M2 - floor(M2);

				mf_retrivedHatchPixel p0 = szecsiSample(M, m, uv, lightIntensity, _Rotation, float2(0, 0.4), 0);
				mf_retrivedHatchPixel p1 = szecsiSample(M2, m2, frac(uv+ float2(0.33,0.125)), lightIntensity, _Rotation+(3.14/2), float2(0.5,1), 1);

				mf_retrivedHatchPixel maxPixel = p0;
				if (p1.blendingHatchStrength > maxPixel.blendingHatchStrength) {
					maxPixel = p1;
				}

				mf_MRTFragmentOutput output = retrivedPixelToFragmentOutput(maxPixel, worldSpacePos, lightIntensity);

				return output;
			}
			ENDCG
		}
	}
}
