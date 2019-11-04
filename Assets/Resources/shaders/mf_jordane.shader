Shader "Custom/Measurements/Filling/Jordane" {
	Properties{
		_Quantization("Qunatization", Range(0,100)) = 1
		_DebugScalar("DebugScalar", Range(0,1)) = 1

		_TamIdTexScale("TextureScale", Vector) = (1.0, 1.0, 0.0, 0.0)
		_TamIdTex("TamIdTex", 2DArray) = "blue" {}
		_TamIdTexBias("TamIdTexBias", Range(-5,5)) = 0.0
		_TamIdTexTonesCount("TamIdTexTonesCount", Range(0,10)) = 1.0
		_TamIdTexLayersCount("TamIdTexLayersCount", Range(0,10)) = 1.0
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
				#pragma geometry geom
				#pragma multi_compile __ MEASUREMENT
				#include "UnityCG.cginc"
				#include "common.txt"
				#include "npr_adjacency.hlsl"
				#include "text_printing.hlsl"
#include "filling_common.txt"
#include "mf_common.txt"

		////////////////////////

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
			float _Quantization;
			float _DebugScalar;

			geometry_in vert (appdata in_v, uint vid : SV_VertexID)
			{
				geometry_in o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.worldSpaceNormal = UnityObjectToWorldNormal(_InterpolatedNormalsBuffer[vid]);
				o.worldSpacePos = mul(unity_ObjectToWorld, float4(in_v.vertex.xyz,1));

				return o;
			}
			
			float3 getNormal( float3 v1, float3 v2, float3 v3){
				return normalize(cross(normalize(v2-v1), normalize(v3-v1)));
			}

			triangleProjectionInfo ComputeProjectionInfo(float3 v1, float3 v2, float3 v3) {
				// wektor normalny trójkąta
				float3 N = getNormal(v1,v2,v3);
				// barycentric center of triangle
				// barycentryczny środek trójkąta
				float3 G = (v1+v2+v3) / 3.0;
				//light direction
				// wektor od środka trójkąta to światła
				float3 L = mf_getDirectionVector(G);
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

			float3 projectPointOnPlane(float3 p, float3 normal, float3 origin) {
				float3 v = p - origin;
				float dist = dot(v, normal);
				return p - dist * normal;
			}

			float quantization_dot(float3 v1, float3 v2) {
				//return length(v1)*length(v2)* round(dot(normalize(v1), normalize(v2))*_Quantization)/_Quantization;
				return dot(v1, v2);
			}

			[maxvertexcount(3)]
			void geom(triangle geometry_in input[3], uint pid : SV_PrimitiveID, inout TriangleStream<geometry_out> outStream)
			{
				AdjacencyInfo adjacent = _AdjacencyBuffer[pid];

				// infos for main triangle and 3 adjacent triangles 
				triangleProjectionInfo infos[4];
				infos[0] = ComputeProjectionInfo(input[0].worldSpacePos, input[1].worldSpacePos, input[2].worldSpacePos);
				for (uint i = 0; i < 3; i += 1) {
					uint auxIndex = (i + 1) % 3;
					infos[i+1] = ComputeProjectionInfo(input[i].worldSpacePos, mul(unity_ObjectToWorld, float4(adjacent.pos[i],1)).xyz, input[auxIndex].worldSpacePos);
				}


				hatchingDirective hatching[3][4]; // first by vertex, than by triangle
				// dla każdego vertexa określamy jak duży wpływ ma na niego każdy z 4 trójkatów
				// i obliczamy wspólrzędne z danego trójkąta
				for (uint vIndex = 0; vIndex < 3; vIndex++) {
					for (uint tIndex = 0; tIndex < 4; tIndex++) {
						float3 ViL = mf_getDirectionVector(  input[vIndex].worldSpacePos);
						if (tIndex == 0) { //main triangle
							hatching[vIndex][tIndex].st.x = dot((input[vIndex].worldSpacePos), infos[tIndex].T); 
							hatching[vIndex][tIndex].st.y = dot((input[vIndex].worldSpacePos), infos[tIndex].B);
							hatching[vIndex][tIndex].f = 1;  //full contribution of main triangle

						}
						else if (!VertexBelongInTriangle(tIndex, vIndex)) {
							//float3 V_dash = lerp(input[vIndex].worldSpacePos, projectPointOnPlane(input[vIndex].worldSpacePos, -infos[tIndex].N, infos[tIndex].G), _DebugScalar);
							float3 V_dash =projectPointOnPlane(input[vIndex].worldSpacePos, -infos[tIndex].N, infos[tIndex].G);
							hatching[vIndex][tIndex].st.x = dot(V_dash, infos[tIndex].T); 
							hatching[vIndex][tIndex].st.y = dot(V_dash, infos[tIndex].B);
							hatching[vIndex][tIndex].f = -1; //no contribution of main triangle
						}
						else {
							hatching[vIndex][tIndex].st.x = dot(input[vIndex].worldSpacePos, infos[tIndex].T); 
							hatching[vIndex][tIndex].st.y = dot(input[vIndex].worldSpacePos, infos[tIndex].B);
							hatching[vIndex][tIndex].f = (saturate(dot(infos[0].N, infos[tIndex].N))); // between this adjacent triangle and main triangle. More acute angle - less weight
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

			mf_MRTFragmentOutput frag (geometry_out i) : SV_Target
			{
				float2 screenUv = float2(i.pos.x/_ScreenParams.x, i.pos.y/_ScreenParams.y);
				float3 lightWorldPos = float3(
					unity_4LightPosX0[0],
					unity_4LightPosY0[0],
					unity_4LightPosZ0[0]  
					);

				float3 lightDir = normalize(lightWorldPos - i.worldSpacePos);

				float lightIntensity = computeLightIntensity(i.worldSpacePos, normalize(i.worldSpaceNormal));

				// Jest po jednej hatchingDirective na TRÓJKĄT!!!
				// .f w hatching directive mówi jak duży ma być blending z danego trójkąta

				mf_weightedRetrivedData retrivedDatas[MAX_HATCH_BLEND_COUNT];   
				for (int t = 0; t < MAX_HATCH_BLEND_COUNT; t++) {
					float2 uv = i.hatchingDirectives[t].st;
					float weight = saturate(i.hatchingDirectives[t].f);
					mf_retrivedHatchPixel retrivedPixels[MAX_TAMID_LAYER_COUNT];

					// X-Min X-Max Y-Min Y-Max
					sampleTamIdTexture(retrivedPixels, uv, lightIntensity,-1,0);
					retrivedDatas[t] = make_mf_weightedRetrivedData(retrivedPixels, weight);
				}

				mf_retrivedHatchPixel maxPixel = findMaximumActivePixel(retrivedDatas);

					//float2 uv2 = i.hatchingDirectives[0].st;
					//mf_retrivedHatchPixel retrivedPixels2[MAX_TAMID_LAYER_COUNT];
					//sampleTamIdTexture(retrivedPixels2, uv2, lightIntensity,-1,0);

				mf_MRTFragmentOutput fo = retrivedPixelToFragmentOutput(maxPixel, i.worldSpacePos, lightIntensity);
				return fo;
			}					

			ENDCG
		}
	}
}
