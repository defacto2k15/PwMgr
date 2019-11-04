Shader "Custom/Sandbox/SuggestiveHighlights" {
    Properties {
		_FeatureSize("FeatureSize", Range(0,10)) = 0.31 //dla venus 1.3
		_SuggestiveContourLimit("SuggestiveContourLimit", Range(-1,1)) = -0.62 //V -0.3
		_DwKrLimit("DwKrLimit", Range(-0.1,0.1)) = -0.0053 //V-0.002
		_JeroenMethod("JeroenMethod", Int) = 1 
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
				float kr;
				float dwkr;
			};

			contoursComputationInput make_contoursComputationInput(float ndotv, float kr, float dwkr) {
				contoursComputationInput input;
				input.ndotv = ndotv;
				input.kr = kr;
				input.dwkr = dwkr;
				return input;
			}

			struct v2f
			{
				float4 pos : SV_POSITION;

				contoursComputationInput contoursInput : ANY_CONTOURS_INPUT;
			};

			StructuredBuffer<PrincipalCurvatureInfo> _PrincipalCurvatureBuffer;
			StructuredBuffer<float3> _InterpolatedNormalsBuffer;


			contoursComputationInput workOutContoursComputationInput(float3 viewDir, float3 objectNrm, PrincipalCurvatureInfo info) {
				float3 worldNrm = normalize(mul((float3x3)unity_ObjectToWorld, normalize(objectNrm))); //n(p) z (1)
				float ndotv = dot(worldNrm,viewDir); //cos(alpha)

				// w to powinno być - w defined as the (unnormalized) projection of the view vector v onto the tangent plane at p
				// i rzeczywiście, to jest taki niepełny wzór na rzucowanie, ale zauważcie że potem robimy normalizacje
				float3 w = normalize(viewDir - worldNrm * dot(viewDir, worldNrm));
				float u = dot(w,(info.direction1)); //cos(fi) Fi więc jest kątem między w a direction1
				float v = dot(w,(info.direction2)); //sin(fi) direction1 i direction2 są zawsze ortogonalne
				float u2 = u * u; //cos(fi)2
				float v2 = v*v; // sin(fi)2 
				float kr = (info.value1*u2) + (info.value2*v2); //wzór (6)

				float u_v = u*v;
				// obliczanie pochodnych. 
				// Opis obliczenia pochodnych, bardzo ładne ale po holendersku, znaleźć można w pracy pana Jeroena Baerta, strona 30, plik masterproef.pdf
				float dwII = (u2 *u *info.derivative.x) + (3 * u * u_v *info.derivative.y) + (3 * u_v * v * info.derivative.z) + (v*v2*info.derivative.w);
				float dwkr = dwII + 2 * info.value1 * info.value2 * ndotv / sqrt(1 - pow(ndotv, 2));
				// wzór 4.11, masterproef.pdf  
				// warto zauważyć, że zgodnie z 4.10i4.11 cos(fi) = sqrt(1-pow(ndotv, 2))
				//DwKr ze wzoru (2)
				return make_contoursComputationInput(ndotv, kr, dwkr);
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

			float _DwKrLimit;
			float _SuggestiveContourLimit;
			float _FeatureSize; //FeatureSize
			int _JeroenMethod;


			fixed4 generateContourColor(fixed4 color, contoursComputationInput contoursInput) {
				float fz = _FeatureSize;
				float sc_limit = _SuggestiveContourLimit;
				float dwkr_limit = _DwKrLimit;

				float kr = fz * abs(contoursInput.kr);
				float dwkr = fz*fz*contoursInput.dwkr;
				float dwkr2 = (dwkr - dwkr*pow(contoursInput.ndotv, 2)); //this is dwkr*sin(fi)
				float dwkr3 = dwkr*(pow(contoursInput.ndotv, 2)); //this is dwkr*cos(fi)

				//contours
				// podzielenie przez kr jest sprytne, i sprawia że "grubosc" konturu jest stała w różnych miejscach

				float sc_limit2;
				if (_JeroenMethod > 0) {
					sc_limit2 = sc_limit*(kr / dwkr3);
				}
				else {
					sc_limit2 = sc_limit*(dwkr);
				}

				if (sc_limit2 < 1 && dwkr3 < dwkr_limit) {
					color = 1;
				}
				//color = 0;
				//if (abs(contoursInput.kr) < 0.1 && contoursInput.dwkr < 0) {
				//	color = float4(1,0,0,0);
				//}
				return color;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = 0.5;
				color = generateContourColor(color, i.contoursInput);

				return color;
			}
			ENDCG
		}
	}
}
