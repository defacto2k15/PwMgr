Shader "Custom/Sandbox/HybridApparentRidgesObject" {
    Properties {
			_DebugScalar("DebugScalar", Range(0,10000)) = 1
			_TauFactor("TauFactor", Range(-100,100)) = 2.5
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
						
			struct PrincipalCurvatureInfo {
				float3 direction1;
				float value1;
				float3 direction2;
				float value2;
				float4 derivative;
			};

			struct HybridApparentRidgesStage1Output {
				float q1;
				float3 w1;
			};

			struct HybridApparentRidgesStage1Input{
				float3 vertexWorldPos;
				float3 objectNrm;
				PrincipalCurvatureInfo curvatureInfo;
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
#ifdef VERT_CALCULATIONS
				HybridApparentRidgesStage1Output stage1Result : ANY_STAGE_1;
#else
				HybridApparentRidgesStage1Input stage1Input : ANY_STAGE_1_INPUT;
#endif
			};

			StructuredBuffer<float3> _InterpolatedNormalsBuffer;
			StructuredBuffer<PrincipalCurvatureInfo> _PrincipalCurvatureBuffer;

			float _DebugScalar;
			float _TauFactor;

			HybridApparentRidgesStage1Output CalculateHybridApparentRidges(HybridApparentRidgesStage1Input input) {
				float3 vertexWorldPos = input.vertexWorldPos;
				float3 objectNrm = input.objectNrm;

				float3 worldNrm = normalize(mul((float3x3)unity_ObjectToWorld, normalize(objectNrm)));
				float3 viewDir =normalize( vertexWorldPos -  UNITY_MATRIX_IT_MV[2].xyz);
				float ndotv = dot(worldNrm,viewDir);

				float u = dot(viewDir, (input.curvatureInfo.direction1));
				float v = dot(viewDir, (input.curvatureInfo.direction2));
				float uv = u*v;
				float u2 = u * u;
				float v2 = v*v;
				float csc2theta = 1 / (u2 + v2);
				float secthetaminus1 = 1 /abs(ndotv) - 1;

				float2x2 P_1;
				P_1[0][0] = (1.0 + secthetaminus1*csc2theta*u2);
				P_1[0][1] = (secthetaminus1*csc2theta*uv);
				P_1[1][0] = (secthetaminus1*csc2theta*uv);
				P_1[1][1] = (1.0 + secthetaminus1*csc2theta*v2);

				float2x2 S;
				S[0][0] = input.curvatureInfo.value1;
				S[0][1] = 0;
				S[1][0] = 0;
				S[1][1] = input.curvatureInfo.value2;

				float2x2 Q = mul(S, P_1);

				float2x2 QTQ = mul(transpose(Q), Q);

				float disc = sqrt(pow(QTQ[0][0] * QTQ[1][1], 2) + 4 * pow(QTQ[0][1], 2)) / 2;

				float lambda1 = (QTQ[0][0] + QTQ[1][1]) / 2 + disc;
				float lambda2 = (QTQ[0][0] + QTQ[1][1]) / 2 - disc;
				float q1;
				if (abs(lambda1) >= abs(lambda2)) {
					q1 = lambda1;
				}
				else {
					q1 = lambda2;
				}

				float2 s1 = normalize(float2(Q[1][1] - q1, -Q[0][1]));
				float3 w1 = s1[0] * input.curvatureInfo.direction1 + s1[1] * input.curvatureInfo.direction2;

				HybridApparentRidgesStage1Output stage1Result;
				
				stage1Result.w1 = w1;
				stage1Result.q1 = q1;

				return stage1Result;
			}

			v2f vert(appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				float3 vertexWorldPos = mul(unity_ObjectToWorld, in_v.vertex);
				float3 objectNrm = _InterpolatedNormalsBuffer[vid];

				PrincipalCurvatureInfo info = _PrincipalCurvatureBuffer[vid];

				HybridApparentRidgesStage1Input stage1Input;
				stage1Input.vertexWorldPos = vertexWorldPos;
				stage1Input.objectNrm = objectNrm;
				stage1Input.curvatureInfo = info;


#ifdef VERT_CALCULATIONS
				HybridApparentRidgesStage1Output stage1Output = CalculateHybridApparentRidges(stage1Input);
				o.stage1Result = stage1Output;
#else
				o.stage1Input = stage1Input;
#endif
				return o;
			}

			float3 PackStage1Result(HybridApparentRidgesStage1Output stage1Result) {
				float2 t1;
				if (stage1Result.w1.z >= 0) {
					t1 = normalize(-stage1Result.w1.xy);
				}
				else {
					t1 = normalize(stage1Result.w1.xy);
				}
				t1 = (t1 + 1) / 2;
				return float3(pow(2, _TauFactor)*stage1Result.q1, t1);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 color = 0;
#ifdef VERT_CALCULATIONS
				color.rgb = PackStage1Result(CalculateHybridApparentRidges(i.stage1Result));
#else
				color.rgb = PackStage1Result(CalculateHybridApparentRidges(i.stage1Input));
#endif
				return color;
			}
			ENDCG
		}
	}
}
