Shader "Custom/Sandbox/IlluminationRidgeValleyPP" {
	Properties
	{
			_MainTex("", any) = "" {}
			_ZeroEpsilon("ZeroEpsilon", Range(0,1)) = 0.0001
			_MovingFactor("DebugScalar", Range(-1,1)) = 1
				_UpperC("UpperC", Range(0,1)) = 0.1
				_LowerC("LowerC", Range(0,1)) = 0.01
	}
		SubShader{
			Tags { "RenderType" = "Opaque" }
			Pass {
				Fog { Mode Off }
				Cull Off
				CGPROGRAM
				#pragma target 5.0
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				struct v2f {
					float4 pos : SV_POSITION;
					float4 projPos : TEXCOORD0;
				};

				float4x4 _ClipToWorld;

				v2f vert(appdata_base v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.projPos = ComputeScreenPos(o.pos);

					return o;
				}

				sampler2D _MainTex;

				int2 uv_to_intScreenCoords(float2 uv) {
					return int2(floor(uv.x * _ScreenParams.x), floor(uv.y * _ScreenParams.y));
				}

				float2 intScreenCoords_to_uv(int2 coords) {
					return float2(coords.x / _ScreenParams.x, coords.y / _ScreenParams.y);
				}


				struct eigenResult {
					float2 v1; //these are eigenvectors
					float2 v2;
					float lambda1; //these are eigenvalues
					float lambda2;
				};

				struct ridgeSearchResult {
					//float dist;
					//float2 nonZeroV;
					//float nonZeroLambda;
					//float firstPrincipalCurvature;
					float max_curv;
					float axis_dist;
					float2 axis;
				};

				// z https://courses.cs.washington.edu/courses/cse590b/02wi/eig2x2.cpp
				// compute the eigenvalue decomposition of a symmetric 2x2
				// matrix in the form A=[a b;b c], so that
				//    A*v1   = v1 * lambda1
				//    A*v2 =   v2 * lambda2
				eigenResult evdecomposesymm(double a, double b,double c)
				{
				  float disc = sqrt((a-c)*(a-c)+4*b*b)/2;

				  float lambda1 = (a+c)/2 + disc;
				  float lambda2 = (a+c)/2 - disc;

				  float v1x = -b;  float v1y = a-lambda1;
				  float v2x = -b;  float v2y = a-lambda2;
				  float v1mag = sqrt(v1x*v1x + v1y*v1y);  
				  float v2mag = sqrt(v2x*v2x + v2y*v2y);
				  v1x/= v1mag;  v1y/=v1mag;
				  v2x/= v2mag;  v2y/=v2mag;

				  eigenResult result;
				  result.v1 = float2(v1x, v1y);
				  result.v2 = float2(v2x, v2y);
				  result.lambda1 = lambda1;
				  result.lambda2 = lambda2;

				  return result;
				}

				float2x2 inverse(float2x2 A)
				{
				  float2x2 C;

				  float det = determinant(A);
				  C[0][0] = A._m11;
				  C[1][0] = -A._m01;
				  C[0][1] = -A._m10;
				  C[1][1] = A._m00;

				  return C / det;
				}

				float _ZeroEpsilon;
				float _MovingFactor;
				float _UpperC;
				float _LowerC;

				ridgeSearchResult searchForRidge(float2 uv) {
					const float EPSILON = 0.0000000000001;
					int2 centerCoords = uv_to_intScreenCoords(uv);

					int radius = 1;
					float H[6][9];
					H[0][0] = 0.166666666666667;
					H[0][1] = 0.166666666666667;
					H[0][2] = 0.166666666666667;
					H[0][3] = -0.333333333333333;
					H[0][4] = -0.333333333333333;
					H[0][5] = -0.333333333333333;
					H[0][6] = 0.166666666666667;
					H[0][7] = 0.166666666666667;
					H[0][8] = 0.166666666666667;
					H[1][0] = 0.125;
					H[1][1] = 0;
					H[1][2] = -0.125;
					H[1][3] = 0;
					H[1][4] = 0;
					H[1][5] = 0;
					H[1][6] = -0.125;
					H[1][7] = 0;
					H[1][8] = 0.125;
					H[2][0] = 0.166666666666667;
					H[2][1] = -0.333333333333333;
					H[2][2] = 0.166666666666667;
					H[2][3] = 0.166666666666667;
					H[2][4] = -0.333333333333333;
					H[2][5] = 0.166666666666667;
					H[2][6] = 0.166666666666667;
					H[2][7] = -0.333333333333333;
					H[2][8] = 0.166666666666667;
					H[3][0] = -0.166666666666667;
					H[3][1] = -0.166666666666667;
					H[3][2] = -0.166666666666667;
					H[3][3] = 0;
					H[3][4] = 0;
					H[3][5] = 0;
					H[3][6] = 0.166666666666667;
					H[3][7] = 0.166666666666667;
					H[3][8] = 0.166666666666667;
					H[4][0] = -0.166666666666667;
					H[4][1] = 0;
					H[4][2] = 0.166666666666667;
					H[4][3] = -0.166666666666667;
					H[4][4] = 0;
					H[4][5] = 0.166666666666667;
					H[4][6] = -0.166666666666667;
					H[4][7] = 0;
					H[4][8] = 0.166666666666667;
					H[5][0] = -0.111111111111111;
					H[5][1] = 0.222222222222222;
					H[5][2] = -0.111111111111111;
					H[5][3] = 0.222222222222222;
					H[5][4] = 0.555555555555556;
					H[5][5] = 0.222222222222222;
					H[5][6] = -0.111111111111111;
					H[5][7] = 0.222222222222222;

#define N  (9)
					float T[N];

					int stepSizeInPixels = 3;
					for (int x = -radius; x <= radius; x++) {
						for (int y = -radius; y <= radius; y++) {
							T[(x + radius) * 3 + (y + radius)] = tex2D(_MainTex, intScreenCoords_to_uv(
								centerCoords + int2(x*stepSizeInPixels, y*stepSizeInPixels)));
						}
					}

					float A[6];
					for (int l = 0; l < 6; l++) {
						float rowSum = 0;
						for (int m = 0; m < N; m++) {
							rowSum += T[m] * H[l][m];
						}
						A[l] = rowSum;
					}

					eigenResult eigen = evdecomposesymm(A[0], A[1], A[2]);

					// eigenvalues of M 
					float l1 = eigen.lambda1;
					float l2 = eigen.lambda2;

					// max_curv: maximum curvature
					float max_curv;
					if (abs(l1) >= abs(l2)) {
						max_curv = l1;
					}
					else {
						max_curv = l2;
					}

					float2 axis; // principal axis
					if (abs(A[1]) > EPSILON) { // ax + by = l * x
						if (abs(l1) >= abs(l2)) {
							axis = eigen.v1;
						}
						else {
							axis = eigen.v2;
						}
					}
					else { // b = 0
						if (abs(A[0]) > abs(A[1])) {
							axis.x = 1.0;
							axis.y = 0.0;
						}
						else {
							axis.x = 0.0;
							axis.y = 1.0;
						}
					}

					float axis_dist = 0;
					float max_dist = 100;



					float2x2 M = float2x2(A[0], A[1], A[1], A[2]);
					float det = determinant(M);
					if (abs(det) < EPSILON) {
						float2 c = -0.5 * inverse(M)*float2x1(A[3], A[4]);

						axis_dist = axis.x*c.x + axis.y*c.y;
					}
					else if (abs(A[1]*A[3]- A[4]*A[0]) < EPSILON) {
						axis_dist = -2.0 * (A[3] * A[4]) / max_curv;
					}
					else {
						axis_dist = max_dist;
					}

					ridgeSearchResult result;
					result.max_curv = max_curv;
					result.axis = axis;
					result.axis_dist = axis_dist;
					return result;
				}

				// z https://github.com/benardp/ActiveStrokes/blob/master/libnpr/shaders/Lee_lines.frag
				float compute_line_opacity(float center_val, float max_curv, float dist, float upper_c, float lower_c)
				{
					float val = 1.0;

					int curv_opacity_ctrl = 1;
					int  dist_opacity_ctrl = 1;
					int  tone_opacity_ctrl = 1;
						if (curv_opacity_ctrl == 1) {
							val *= clamp((abs(max_curv) - lower_c) / (upper_c - lower_c), 0.0, 1.0);
						}
						else if (abs(max_curv) < upper_c) {
							val = 0.0;
						}

						if (dist_opacity_ctrl == 1) {
							float a = sqrt(2.0 - clamp(dist, 0.0, 2.0))*0.5;

							if (a > 0.6)
								a = 1.0;

							val *= a;
						}

						if (tone_opacity_ctrl == 1) {
							val *= clamp(1.0 - center_val, 0.0, 1.0);
						}

						return val;
				}

				float4 frag(v2f i) : COLOR {

					float4 color = 0;
					float2 uv = i.projPos.xy;

					ridgeSearchResult ridgeResult;
					bool is_line = true;
					// z wielokrotnego wyszukania na razie niestety nie korzystam. Jak DebugScalar jest > 0 to są problemy
#define ITERATIONS (1) //should be 5
					for (int it = 0; it < ITERATIONS; it++) {
						ridgeResult = searchForRidge(uv);
						if (abs(ridgeResult.max_curv) < _LowerC) {
							is_line = false;
							return 0;
						}
						else {
							float half_width = 1;
							float moving_factor = _MovingFactor;
							float2 diff = (ridgeResult.axis_dist * ridgeResult.axis * half_width) * moving_factor;
							uv = uv + diff;
						}
					}
					if (is_line) {
						if (ridgeResult.max_curv > 0) {
							float dist = length(i.projPos.xy - uv); // allways 0 for now
							float center_val = tex2D(_MainTex, i.projPos.xy).r;

							//dark line
							color.r = compute_line_opacity(center_val, ridgeResult.max_curv, dist, _UpperC, _LowerC);
						}
						else {
							color.g = 1; //highlight
						}
					}
					return color;
				}
				ENDCG
			}
	}
}
