Shader "Custom/Sandbox/SuggestiveContours" {
    Properties {
		_FeatureSize("FeatureSize", Range(0,20)) = 11.9
		_ContourLimit("ContourLimit", Range(0,20)) = 2.2
		_SuggestiveContourLimit("SuggestiveContourLimit", Range(0,10)) = 2.4
		_DwKrLimit("DwKrLimit", Range(0,0.8)) = 0.07
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
				o.contoursInput.kr = info.value1/4.0;// ((float)vid) / 1000;
				return o;
			}

			float _ContourLimit;
			float _DwKrLimit;
			float _SuggestiveContourLimit;
			float _FeatureSize; //FeatureSize
			int _JeroenMethod;

			fixed4 generateContourColor(fixed4 color, contoursComputationInput contoursInput) {
				float fz = _FeatureSize;
				float c_limit = _ContourLimit;
				float sc_limit = _SuggestiveContourLimit;
				float dwkr_limit = _DwKrLimit;

				float kr = fz * abs(contoursInput.kr);
				float dwkr = fz*fz*contoursInput.dwkr;
				float dwkr2 = (dwkr - dwkr*pow(contoursInput.ndotv, 2));

				//contours
				// podzielenie przez kr jest sprytne, i sprawia że "grubosc" konturu jest stała w różnych miejscach
				float contour_limit = c_limit*(pow(contoursInput.ndotv, 2.0) / kr);

				float sc_limit2;
				if (_JeroenMethod > 0) {
					sc_limit2 = sc_limit*(kr / dwkr2);
				}
				else {
					sc_limit2 = sc_limit*(dwkr);
				}

				if (contour_limit < 1) {
					color.xyz = 0;
				}
				// alternatywą dla drugiej części członu byłoby dwkr > dwkr_limit, co odpowiada (2)
				else if (sc_limit2 < 1 && dwkr2 > dwkr_limit) {
					color = 1;
				}
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
