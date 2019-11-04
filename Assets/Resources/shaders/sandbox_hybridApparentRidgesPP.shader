Shader "Custom/Sandbox/HybridApparentRidgesPP" {
	Properties
	{
			_MainTex("", any) = "" {}
			_TauFactor("TauFactor", Range(0,100)) = 1
			_Lambda("Lambda", Range(0,1)) = 1
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

				struct HybridApparentRidgesStage1 {
					float q1;
					float2 t1;
				};

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
				float _TauFactor;
				float _Lambda;

				int2 uv_to_intScreenCoords(float2 uv) {
					return int2(floor(uv.x * _ScreenParams.x), floor(uv.y * _ScreenParams.y));
				}

				float2 intScreenCoords_to_uv(int2 coords) {
					return float2(coords.x / _ScreenParams.x, coords.y / _ScreenParams.y);
				}

				HybridApparentRidgesStage1 UnpackStage1Result(float3 input) {
					HybridApparentRidgesStage1 o; 
					o.q1 = input.r; // we ignore Tau and pow(2 !!!
					o.t1 = input.gb * 2 - 1;
					return o;
				}


				float4 frag(v2f i) : COLOR {
					float pi = 3.14159;

					float2 uv = i.projPos.xy;
					int2 coords = uv_to_intScreenCoords(uv);
					HybridApparentRidgesStage1 Stages[3][3];

					for (int x = -1; x <= 1; x++) {
						for (int y = -1; y <= 1; y++) {
							float3  pixel = tex2D(_MainTex, intScreenCoords_to_uv(coords + int2(x, y))).rgb; 
							Stages[x + 1][y + 1] = UnpackStage1Result(pixel);
						}
					}


					float a = sqrt(2.0) / 2.0;
					float b = cos(pi / 8);
					float lambda = _Lambda;
					float l1 = 1 + lambda;
					float l2 = 1 + 2 * _Lambda;
					float eplison = 0.00000000000001;

					float v = 0;
					float2 diff;

					fixed4 color;
					color = 0;
					float2 t1 =(Stages[1][1].t1);
					float2 t1n = normalize(t1);
					if (length(t1) > eplison ) { 
						float2 d1 = float2(1, 0);
						float2 d2 = float2(a, a);
						float2 d3 = float2(0, 1);
						float2 d4 = float2(-a, a);

						v = (8 + 8 * lambda)*Stages[1][1].q1;

						// wartosci zgodne z figure4.4
						float M[3][3];
						if (abs(dot(t1n, d1)) > b) { 
							color = float4(1, 0, 0,0);
							M[0][0] = l1;	M[0][1] = 1;	M[0][2] = l1;
							M[1][0] = l2;	M[1][1] = 0;	M[1][2] = l2;
							M[2][0] = l1;	M[2][1] = 1;	M[2][2] = l1;
						}
						else if (abs(dot(t1n, d2)) > b) {
							color = float4(1, 1, 0,0);
							M[0][0] = 1;	M[0][1] = l1;	M[0][2] = l2;
							M[1][0] = l1;	M[1][1] = 0;	M[1][2] = l1;
							M[2][0] = l2;	M[2][1] = l1;	M[2][2] = 1;
						}
						else if (abs(dot(t1n, d3)) > b) {
							color = float4(0, 1, 0,0);
							M[0][0] = l1;	M[0][1] = l2;	M[0][2] = l1;
							M[1][0] = 1;	M[1][1] = 0;	M[1][2] = 1;
							M[2][0] = l1;	M[2][1] = l2;	M[2][2] = l1;
						}
						else {
							color = float4(0, 1, 1,0);
							M[0][0] = l2;	M[0][1] = l1;	M[0][2] = 1;
							M[1][0] = l1;	M[1][1] = 0;	M[1][2] = l1;
							M[2][0] = 1;	M[2][1] = l1;	M[2][2] = l2;
						}

						for (int x = 0; x > 3; x++) {
							for (int y = 0; y < 3; y++) {
								v -= Stages[x][y].q1 * M[x][y];
							}
						}
					}

					color = 1;
					if (v > 0.2) {
						color = 1 - (v-0.2)*(1/0.8);
					}
					//color = Stages[1][1].q1;
					//color.gb = Stages[1][1].t1;
					return color;
				}
				ENDCG
			}
	}
}
