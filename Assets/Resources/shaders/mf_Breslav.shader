Shader "Custom/Measurements/Filling/Breslav" {
	Properties{
		_DebugScalar("DebugScalar", Range(0,1)) = 0.0

		_BreslavU("BreslavU", Vector) = (0.0, 0.0, 0.0, 0.0)
		_BreslavO("BreslavO", Vector) = (0.0, 0.0, 0.0, 0.0)
		_BreslavV("BreslavV", Vector) = (0.0, 0.0, 0.0, 0.0)
		_BreslavSt("BreslavSt", Range(0,1)) = 0
		_LodScale("LodScale", Range(0,4)) = 1.0

		_UseBreslavScalling("useBreslavScalling", Range(0,1)) = 1.0

		_TamIdTexScale("TamIdTextureScale", Vector) = (1.0, 1.0, 0.0, 0.0)
		_TamIdTex("TamIdTex", 2DArray) = "blue" {}
		_TamIdTexBias("TamIdTexBias", Range(-5,5)) = 0.0
		_TamIdTexTonesCount("TamIdTexTonesCount", Range(0,10)) = 1.0
		_TamIdTexLayersCount("TamIdTexLayersCount", Range(0,10)) = 1.0
    }
	SubShader
	{
		Tags { "RenderType"="Opaque" }
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
				float4 projPos : ANY_PROJ_POS;
				float3 worldNrm : ANY_WORLD_NRM;
				float3 vertexWorldPos : ANY_VERTEX_WORLD_POS;
			};

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.projPos = ComputeScreenPos (o.pos);

				float3 vertexWorldPos = mul(unity_ObjectToWorld , in_v.vertex);
				o.vertexWorldPos = vertexWorldPos;

				o.worldNrm = UnityObjectToWorldNormal(normalize(in_v.norm));

				return o;
			}

			float2 _BreslavU;
			float2 _BreslavO;
			float2 _BreslavV;
			float _BreslavSt;
			float _LodScale;
			float _DebugScalar;
			float _UseBreslavScalling;

			fixed4 colorFromUv(fixed2 uv) {
				fixed4 c = 0;
				c.rg = frac(uv * 10);
				return c;
			}


			mf_MRTFragmentOutput frag (v2f i) : SV_Target
			{
				fixed4 color = 0;
				float2 uv = i.projPos.xy / i.projPos.w;
				uv -= _BreslavO;
				uv = float2(
					dot(uv, _BreslavU) / (dot(_BreslavU, _BreslavU)),
					dot(uv, _BreslavV) / (dot(_BreslavV, _BreslavV)));

				fixed4 h_lo = colorFromUv(uv);
				fixed4 h_hi = colorFromUv(uv*2);

				if (_BreslavSt <= 0) { 
					color = h_lo;
				}
				else if (_BreslavSt >= 1){
					color = h_hi;
				}
				else {
					color = lerp(h_lo, h_hi, _BreslavSt);
				}
				color = h_lo;

				float lightIntensity = computeLightIntensity(i.vertexWorldPos, i.worldNrm);
				int loLevelEvenBit = (floor(log2(_LodScale))+64)%2;
				int hiLevelEvenBit = (loLevelEvenBit + 1) % 2;

				mf_weightedRetrivedData retrivedDatas[MAX_HATCH_BLEND_COUNT];
				retrivedDatas[2] = make_empty_mf_weightedRetrivedData();
				retrivedDatas[3] = make_empty_mf_weightedRetrivedData();

				int customLodLevel;
				if (_UseBreslavScalling > 0.5) {
					retrivedDatas[0].weight = 1-_BreslavSt;
					retrivedDatas[1].weight = _BreslavSt;
					customLodLevel = 0 + _TamIdTexBias;
				}
				else {
					retrivedDatas[0].weight = 1;
					retrivedDatas[1].weight = 0;
					customLodLevel = -1;
				}

				sampleTamIdTexture(retrivedDatas[0].pixel, uv, lightIntensity,customLodLevel, loLevelEvenBit);
				sampleTamIdTexture(retrivedDatas[1].pixel, uv*2, lightIntensity,customLodLevel, hiLevelEvenBit);
				mf_retrivedHatchPixel maxPixel = findMaximumActivePixel(retrivedDatas);
				mf_MRTFragmentOutput output = retrivedPixelToFragmentOutput(maxPixel, i.vertexWorldPos, lightIntensity);
				return output;
			}
			ENDCG
		}
	}
}
