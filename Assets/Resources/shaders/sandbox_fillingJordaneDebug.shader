Shader "Custom/Sandbox/Filling/JordaneDebug" {
	Properties{
		_DebugTex("DebugTex", 2D) = "blue" {}
		_TextureScale("TextureScale", Range(0,10)) = 1
		_Quantization("Qunatization", Range(0,100)) = 1
		_DebugScalar("DebugScalar", Range(0,1)) = 1
	}

		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100

			Pass
			{
				Tags{ "LightMode" = "ForwardBase" }

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma geometry geom
				#include "UnityCG.cginc"
				#include "common.txt"
				#include "npr_adjacency.hlsl"

			struct hatchingDirective {
				float2 st;
				float f; //weight
			};

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
			};

			struct geometry_in
			{
				float4 pos : SV_POSITION;
				float3 worldSpaceNormal: ANY_WORLD_SPACE_NORMAL;
				float3 worldSpacePos : ANY_WORLD_SPACE_POS;
			};

			struct geometry_out {
				float4 pos : SV_POSITION;
				float3 worldSpaceNormal : ANY_WORLD_SPACE_NORMAL;
				float3 worldSpacePos : ANY_WORLD_SPACE_POS;
				hatchingDirective hatchingDirectives[4] : ANY_HATCHING_DIRECTIVES;
				float3 trianglesWorldSpaceNormals[4] : ANY_TRIANGLES_NORMALS;
			};

			struct triangleProjectionInfo {
				float3 T;
				float3 B;
				float3 N;
				float3 G;
			};

			StructuredBuffer<float3> _InterpolatedNormalsBuffer; 
			StructuredBuffer<AdjacencyInfo> _AdjacencyBuffer;
			sampler2D _DebugTex;
			float _TextureScale;
			float _Quantization;
			float _DebugScalar;

			geometry_in vert (appdata in_v, uint vid : SV_VertexID)
			{
				geometry_in o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.worldSpaceNormal = UnityObjectToWorldNormal(_InterpolatedNormalsBuffer[vid]);
				o.worldSpacePos = mul(unity_ObjectToWorld, in_v.vertex);

				return o;
			}
			
			float3 getNormal( float3 v1, float3 v2, float3 v3){
				return normalize(cross(normalize(v2-v1), normalize(v3-v1)));
			}

			triangleProjectionInfo ComputeProjectionInfo(float3 v1, float3 v2, float3 v3, float3 lightWorldPos) {
				// wektor normalny trójkąta
				float3 N = getNormal(v1,v2,v3);
				// barycentric center of triangle
				// barycentryczny środek trójkąta
				float3 G = (v1,v2,v3) / 3.0;
				//light direction
				// wektor od środka trójkąta to światła
				float3 L= normalize(lightWorldPos - G);
				// co ciekawe, linie tekstury ustawiają się pod kątem 90* w stosunku do N i L

				// Binomial, vector from triangle towards light, projected on the triangle plane
				//kiedy B jest takie samo, to linie tekstury sa ciągłe między trójkątami
				// B można ładnie zkwantyzować, ale mergingi są wtedy problemem
				float3 B = normalize(L - dot(N, dot(L, N)));

				B = normalize( round(B*_Quantization)/_Quantization); //- dobre miejsce na kwantyzacje!!

				//Tangent, pod kątem prostym do nich
				float3 T = cross(B, N);

				triangleProjectionInfo o;
				o.G = G;
				o.N = N;
				o.B = B;
				o.T = T;
				return o;
			}

			bool VertexBelongInTriangle(int triangleIndex, int vertexIndex) {
				if (triangleIndex == 0) {
					return true;
				}
				else {
					int firstIndex = triangleIndex - 1;
					int auxIndex = (firstIndex + 1) % 3;
					return (vertexIndex == firstIndex || vertexIndex == auxIndex);
				}
			}

			float3 projectPointOnPlane(float3 p, float3 planeNormal, float3 planeOrigin) {
				return p - dot(p - planeOrigin, planeNormal)*planeNormal;
			}

			float quantization_dot(float3 v1, float3 v2) {
				//return length(v1)*length(v2)* round(dot(normalize(v1), normalize(v2))*_Quantization)/_Quantization;
				return dot(v1, v2);
			}

			[maxvertexcount(3)]
			void geom(triangle geometry_in input[3], uint pid : SV_PrimitiveID, inout TriangleStream<geometry_out> outStream)
			{
				float3 lightWorldPos = float3(
					unity_4LightPosX0[0],
					unity_4LightPosY0[0],
					unity_4LightPosZ0[0]
					);


				AdjacencyInfo adjacent = _AdjacencyBuffer[pid];

				// infos for main triangle and 3 adjacent triangles 
				triangleProjectionInfo infos[4];
				infos[0] = ComputeProjectionInfo(input[0].worldSpacePos, input[1].worldSpacePos, input[2].worldSpacePos, lightWorldPos);
				for (uint i = 0; i < 3; i += 1) {
					uint auxIndex = (i + 1) % 3;
					infos[i+1] = ComputeProjectionInfo(input[i].worldSpacePos, mul(unity_ObjectToWorld,adjacent.pos[i]).xyz, input[auxIndex].worldSpacePos, lightWorldPos);
				}

				hatchingDirective hatching[3][4]; // first by vertex, than by triangle

				// dla każdego vertexa określamy jak duży wpływ ma na niego każdy z 4 trójkatów
				// i obliczamy wspólrzędne z danego trójkąta
				float ts = _TextureScale;
				for (uint vIndex = 0; vIndex < 3; vIndex++) {
					for (uint tIndex = 0; tIndex < 4; tIndex++) {
						float3 ViL = normalize(lightWorldPos - input[vIndex].worldSpacePos);
						if (tIndex == 0) { //main triangle
							hatching[vIndex][tIndex].st.x = dot((input[vIndex].worldSpacePos), infos[tIndex].T)*ts; 
							hatching[vIndex][tIndex].st.y = dot((input[vIndex].worldSpacePos), infos[tIndex].B)*ts;
							hatching[vIndex][tIndex].f = 1;  //full contribution of main triangle

						}
						else if (!VertexBelongInTriangle(tIndex, vIndex)) {
							float3 V_dash = lerp(input[vIndex].worldSpacePos,
								 projectPointOnPlane(input[vIndex].worldSpacePos, -infos[tIndex].N, infos[tIndex].G), _DebugScalar);
							hatching[vIndex][tIndex].st.x = dot(V_dash, infos[tIndex].T)*ts; 
							hatching[vIndex][tIndex].st.y = dot(V_dash, infos[tIndex].B)*ts;
							hatching[vIndex][tIndex].f = -1;  //no contribution of main triangle
							//distances[vIndex] = input[vIndex].worldSpacePos - V_dash;
						}
						else {
							hatching[vIndex][tIndex].st.x = dot(input[vIndex].worldSpacePos, infos[tIndex].T)*ts; 
							hatching[vIndex][tIndex].st.y = dot(input[vIndex].worldSpacePos, infos[tIndex].B)*ts;
							hatching[vIndex][tIndex].f =  (saturate(dot(infos[0].N, infos[tIndex].N))); // between this adjacent triangle and main triangle. More acute angle - less weight
						}  
					}
				}


				geometry_out go[3];
				for (int i = 0; i < 3; i++) {
					go[i].pos = input[i].pos;
					go[i].worldSpaceNormal = input[i].worldSpaceNormal;
					go[i].worldSpacePos = input[i].worldSpacePos;

					for (int j = 0; j < 4; j++) {
						go[i].hatchingDirectives[j] = hatching[i][j];
						go[i].trianglesWorldSpaceNormals[j] = infos[j].N;
					}

					outStream.Append(go[i]);
				}

				outStream.RestartStrip();
			}

			fixed4 frag (geometry_out i) : SV_Target
			{
				float3 lightWorldPos = float3(
					unity_4LightPosX0[0],
					unity_4LightPosY0[0],
					unity_4LightPosZ0[0]
					);

				float3 lightDir = normalize(lightWorldPos - i.worldSpacePos);

				float3 triangleColors[4];
				for (int t = 0; t < 4; t++) {
					float tone = saturate(dot(lightDir, normalize(i.trianglesWorldSpaceNormals[t])));
					triangleColors[t] = tex2D(_DebugTex, (i.hatchingDirectives[t].st)).rgb;
				}

				float4 color = 0;
				float weightsSum = 0;
				for (int j = 0; j < 4; j++) {
					float weight = saturate(i.hatchingDirectives[j].f);
					weightsSum += weight;
					color.rgb += triangleColors[j]*weight ;
				}
				color = color / weightsSum;

				// TODO: quantization 
				return color;
			}					

			ENDCG
		}
	}
}
