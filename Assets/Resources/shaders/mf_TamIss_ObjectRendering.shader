Shader "Custom/Measurements/Filling/TamIssObjectRendering" {
	Properties{
		_DebugScalar("DebugScalar",Range(0,1000)) = 0.0
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

		float _DebugScalar;

			struct TamIss_ObjectRendering_MRTFragmentOutput
			{
				float4 mfDest0 : SV_Target0;
				float4 dest1 : SV_Target1;
				float4 dest2 : SV_Target2;
				float4 mfDest1 : SV_Target3;
				float4 mfDest2 : SV_Target4;
				float4 mfDest3 : SV_Target5;
				float4 mfDest4 : SV_Target6;
			};

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

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.projPos = ComputeScreenPos (o.pos);

				float3 vertexWorldPos = mul(unity_ObjectToWorld , in_v.vertex);
				o.vertexWorldPos = vertexWorldPos;

				float3 objectNrm = in_v.norm;
				o.worldNrm = UnityObjectToWorldNormal(normalize(objectNrm));

				o.uv = in_v.uv;

				return o;
			}

			TamIss_ObjectRendering_MRTFragmentOutput frag (v2f i) : SV_Target
			{
				float lightIntensity = computeLightIntensity(i.vertexWorldPos, i.worldNrm);
			//lightIntensity = 1;

				TamIss_ObjectRendering_MRTFragmentOutput fo;
				//fo.dest0.r = 1;
				//fo.dest0.g = lightIntensity;

				fo.dest1.r = lightIntensity;
				fo.dest1.g = frac(i.uv.x);
				fo.dest1.b = frac(i.uv.y);
				fo.dest1.a = 1;

				if (i.pos.x < _DebugScalar) {
					fo.dest1.a = 0;
				}

				fo.dest2.r = i.vertexWorldPos.x;
				fo.dest2.g = i.vertexWorldPos.y;
				fo.dest2.b = i.vertexWorldPos.z;


				mf_MRTFragmentOutput mfOutput = retrivedPixelToFragmentOutput(make_mf_retrivedHatchPixel(0, 0, false, 0, 0), i.vertexWorldPos, lightIntensity);
				fo.mfDest0 = mfOutput.dest0;

#ifdef  MEASUREMENT
				fo.mfDest1 = mfOutput.dest1;
				fo.mfDest2 = mfOutput.dest2;
				fo.mfDest3 = mfOutput.dest3;
				fo.mfDest4 = mfOutput.dest4;
#endif

				return fo;
			}

			ENDCG
		}
	}
}
