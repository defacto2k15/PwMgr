Shader "Custom/Sandbox/PrincipalHighlights" {
	Properties{
		_Test6Cutoff("Test6Cutoff", Range(-1,1)) = 0.1 //Torus=0.1
		_TestDerivativeCutoff("TestDerivativeCutoff", Range(0, 100)) =3 //T=4.2
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
						
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
			};

			struct PrincipalCurvatureInfo {
				float3 direction1;
				float value1;
				float3 direction2;
				float value2;
				float4 derivative;
			};

			struct contoursComputationInput {
				float ndotv;
				float dwtg;
				float test6;
			};

			contoursComputationInput make_contoursComputationInput(float ndotv, float dwtg, float test6) {
				contoursComputationInput input;
				input.ndotv = ndotv;
				input.dwtg = dwtg;
				input.test6 = test6;
				return input;
			}

			struct v2f
			{
				float4 pos : SV_POSITION;

				contoursComputationInput contoursInput : ANY_CONTOURS_INPUT;
			};

#include "common.txt"

			StructuredBuffer<PrincipalCurvatureInfo> _PrincipalCurvatureBuffer;
			StructuredBuffer<float3> _InterpolatedNormalsBuffer;

			contoursComputationInput workOutContoursComputationInput(float3 viewDir, float3 objectNrm, PrincipalCurvatureInfo info) {
				float3 worldNrm = normalize(mul((float3x3)unity_ObjectToWorld, normalize(objectNrm))); //n(p) z (1)
				float ndotv = dot(worldNrm,viewDir); //cos(alpha)

				float3 w = normalize(viewDir - worldNrm * dot(viewDir, worldNrm));
				float u = dot(w,(info.direction1)); //cos(fi) Fi więc jest kątem między w a direction1
				float v = dot(w,(info.direction2)); //sin(fi) direction1 i direction2 są zawsze ortogonalne
				float u2 = u * u; //cos(fi)2
				float v2 = v*v; // sin(fi)2 
				float tg =  (info.value2 - info.value1)*u*v;

				float u_v = u*v;
				// obliczanie pochodnych. 
				float dwII = (u2 *u *info.derivative.x) + (3 * u * u_v *info.derivative.y) + (3 * u_v * v * info.derivative.z) + (v*v2*info.derivative.w);
				float dwtg = dwII + 2 * info.value1 * info.value2 * ndotv / sqrt(1 - pow(ndotv, 2));

				float test6 = (pow(info.value1, 2) - pow(info.value2, 2))*u;
				return make_contoursComputationInput(ndotv, dwtg, test6);
			}

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);

				float3 vertexWorldPos = mul(unity_ObjectToWorld , in_v.vertex);
				float3 viewDir = normalize(_WorldSpaceCameraPos - vertexWorldPos); //camera to vertex v(p) z (1)

				float3 objectNrm = _InterpolatedNormalsBuffer[vid];

				PrincipalCurvatureInfo info = _PrincipalCurvatureBuffer[vid];
				o.contoursInput = workOutContoursComputationInput(viewDir, objectNrm, info);
				return o;
			}

			float _Test6Cutoff;
			float _TestDerivativeCutoff;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = 0.5;

				if (abs(i.contoursInput.dwtg) > _TestDerivativeCutoff) {
					float test6 = abs(i.contoursInput.test6);
					float test6_2 = (test6 - test6*pow(i.contoursInput.ndotv, 2));

					test6_2 /= fwidth(test6_2)*10;

					float mm = _Test6Cutoff*0.5;
					float max = _Test6Cutoff*1.5;

					color = invLerp(_Test6Cutoff*0.9, _Test6Cutoff*1.1, test6_2);

				}

				//if (abs(i.contoursInput.dwtg) > _TestDerivativeCutoff) { // ładnie pokazać tym można
				//	color.r = 1;
				//}
				//if (abs(i.contoursInput.test6) > _Test6Cutoff) {
				//	color.g = 1;
				//}

				return color;
			}
			ENDCG
		}
	}
}
