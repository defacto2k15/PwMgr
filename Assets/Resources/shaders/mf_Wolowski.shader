Shader "Custom/Measurements/Filling/Wolowski" {
	Properties{
		_RotationQuant("RotationQuant", Range(0,10)) = 1
		_DebugScalar("DebugScalar", Range(-10,10)) = 0
		_MarginSize("MarginSize", Range(0,1)) = 0.2


		_TamIdTexScale("TextureScale", Vector) = (1.0, 1.0, 0.0, 0.0)
		_TamIdTex("TamIdTex", 2DArray) = "blue" {}
		_TamIdTexBias("TamIdTexBias", Range(-5,5)) = 0.0
		_TamIdTexTonesCount("TamIdTexTonesCount", Range(0,10)) = 1.0
		_TamIdTexLayersCount("TamIdTexLayersCount", Range(0,10)) = 1.0

	}

		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100

			Pass
			{
				Tags{ "LightMode" = "ForwardBase" }

				CGPROGRAM
#pragma target 5.0
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile __ MEASUREMENT
			#pragma multi_compile __ LIGHT_SHADING_ON
			#pragma multi_compile __ DIRECTION_PER_LIGHT
				#include "UnityCG.cginc"
#include "common.txt"
#include "filling_common.txt"
#include "mf_common.txt"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldNrm : ANY_WORLD_NRM;
				float3 vertexWorldPos : ANY_VERTEX_WORLD_POS;
				float4 projPos : ANY_PROJ_POS;
				float2 uv : ANY_UV;
			};

			StructuredBuffer<float3> _InterpolatedNormalsBuffer;
			float _RotationQuant;
			float _DebugScalar;
			float _MarginSize;

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.projPos = ComputeScreenPos (o.pos);

				float3 vertexWorldPos = mul(unity_ObjectToWorld , in_v.vertex);
				o.vertexWorldPos = vertexWorldPos;

				float3 objectNrm = _InterpolatedNormalsBuffer[vid];
				o.worldNrm = UnityObjectToWorldNormal(normalize(objectNrm));

				o.uv = in_v.uv;
				return o;
			}

			half2 rotateUv( half2 pos, half rotation){
				float sinX = sin (rotation);
				float cosX = cos (rotation);
				float sinY = sin (rotation);
				float2x2 rotationMatrix = float2x2( cosX, -sinX, sinY, cosX);
				return mul(pos, rotationMatrix);
			}

			uint addQuantIdPrefix(uint oldId, int quantIndex) {
				return ((oldId << 5) >> 5) | ( ((uint)quantIndex) << (32 - 5));
			}


			mf_MRTFragmentOutput frag (v2f i) : SV_Target
			{
				float3 lightDir = mf_getDirectionVector(i.vertexWorldPos);

				float lightIntensity = computeLightIntensity(i.vertexWorldPos, i.worldNrm);
				float3 alignmentVector = lightDir;
				float2 uv = (i.projPos.xy / i.projPos.w);

				float3 worldSpaceDifferenceVector = normalize(normalize(i.worldNrm) - alignmentVector);
				float2 imageSpaceDifferenceVector = normalize(mul((float3x3)UNITY_MATRIX_VP, worldSpaceDifferenceVector)).xy;

				float alpha = acos(dot(imageSpaceDifferenceVector, float2(0, 1))); // in Wolowski was that this should be Y axis vector (0,1)

				float quantIndex[2];
				quantIndex[0] = floor(alpha*_RotationQuant);
				quantIndex[1] = ceil(alpha*_RotationQuant);

				int customLodLevel = _TamIdTex.CalculateLevelOfDetail(sampler_TamIdTex, uv) + _TamIdTexBias;

				mf_weightedRetrivedData retrivedDatas[MAX_HATCH_BLEND_COUNT];
				retrivedDatas[2] = make_empty_mf_weightedRetrivedData();
				retrivedDatas[3] = make_empty_mf_weightedRetrivedData();

				for (int k = 0; k < 2; k++) {
					float qAlpha = quantIndex[k] / _RotationQuant;
					float weight = 1-abs(qAlpha - alpha)*_RotationQuant;

					float2 qUv = rotateUv(uv, qAlpha);
					sampleTamIdTexture(retrivedDatas[k].pixel, qUv, lightIntensity,customLodLevel,0);
					retrivedDatas[k].weight = weight;
					for (int i1 = 0; i1 < MAX_TAMID_LAYER_COUNT; i1++) {
						retrivedDatas[k].pixel[i1].id = addQuantIdPrefix(retrivedDatas[k].pixel[i1].id, quantIndex[k]);
					}
				}

				int biggerWeightIndex = 0;
				if (retrivedDatas[1].weight > retrivedDatas[0].weight) {
					biggerWeightIndex = 1;
				}
				retrivedDatas[biggerWeightIndex].weight = 1;
				float w2 = retrivedDatas[(biggerWeightIndex + 1) % 2].weight;
				w2 = (0.5 + (0.5 - w2)/-_MarginSize)*2;
				retrivedDatas[(biggerWeightIndex + 1) % 2].weight = w2;

				mf_retrivedHatchPixel maxPixel = findMaximumActivePixel(retrivedDatas);
				mf_MRTFragmentOutput o = retrivedPixelToFragmentOutput(maxPixel, i.vertexWorldPos, lightIntensity);
				return o;
			}					

			ENDCG
		}
	}
}
