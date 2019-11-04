Shader "Custom/Measurements/Filling/ShowerDoor" {
	Properties{
		_DebugScalar("DebugScalar", Range(0,1)) = 0.0

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
				float2 uv : ANY_UV;
			};

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.projPos = ComputeScreenPos (o.pos);

				float3 vertexWorldPos = mul(unity_ObjectToWorld , in_v.vertex);
				o.vertexWorldPos = vertexWorldPos;

				o.worldNrm = UnityObjectToWorldNormal(normalize(in_v.norm));
				o.uv = in_v.uv;

				return o;
			}

			float _DebugScalar;


			mf_MRTFragmentOutput frag (v2f i) : SV_Target
			{
				fixed4 color = 0;
				float2 uv = i.projPos.xy / i.projPos.w;

				float lightIntensity = computeLightIntensity(i.vertexWorldPos, i.worldNrm);

				mf_weightedRetrivedData retrivedDatas[MAX_HATCH_BLEND_COUNT];
				retrivedDatas[1] = make_empty_mf_weightedRetrivedData();
				retrivedDatas[2] = make_empty_mf_weightedRetrivedData();
				retrivedDatas[3] = make_empty_mf_weightedRetrivedData();

				sampleTamIdTexture(retrivedDatas[0].pixel, uv, lightIntensity, -1, 0);
				retrivedDatas[0].weight = 1;

				mf_retrivedHatchPixel maxPixel = findMaximumActivePixel(retrivedDatas);
				return retrivedPixelToFragmentOutput(maxPixel, i.vertexWorldPos, lightIntensity);
			}
			ENDCG
		}
	}
}
