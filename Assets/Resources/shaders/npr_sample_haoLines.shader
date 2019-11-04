Shader "Custom/NPR/Sample/LinesDrawing" {
    Properties {
		_MainTex("MainTex", 2D)  = "white" {}

//IMITATION UNIFORMS PROPERTY START
		_RidgeTreshold("RidgeTreshold", Range(-2,2)) = 0
		_ValleyTreshold("ValleyTreshold", Range(-2,2)) = 0
		_EdgeSize("EdgeSize", Range(0,0.2)) = 0.01
		_LineClosingFactor("LineClosingFactor", Range(0,3)) = 1
		_SurfaceLineWidth("SurfaceLineWidth", Range(0,10)) = 0





//IMITATION UNIFORMS PROPERTY END
    }
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM

#define TRIANGLE_TYPE_STANDARD (1)
#define TRIANGLE_TYPE_FIN (2)
#define COMMA ,

#define  IN_ridge_FEATURE_STATUS_INDEX (0)
#define  IN_valley_FEATURE_STATUS_INDEX (1)
#define  IN_silhouette_FEATURE_STATUS_INDEX (2)

//2 - generated in geometry stage, 1 - taken from buffer(vertexMode), 3 - pixel stage
// Obliczanie features w pixel shaderze (3) WYMAGA przekazania tam barycentric_coords

#define DETECTION_MODE_OFF (0)
#define DETECTION_MODE_VERTEX (1)
#define DETECTION_MODE_GEOMETRY (2)
#define DETECTION_MODE_PIXEL (3)

#define IN_ridge_FEATURE_DETECTION_MODE (1)
#define IN_valley_FEATURE_DETECTION_MODE (1)
#define IN_silhouette_FEATURE_DETECTION_MODE (0)

#define APPLY_MODE_OFF (0)
#define APPLY_MODE_FINS (1)
#define APPLY_MODE_SURFACE_LINE (2)

#define IN_ridge_FEATURE_APPLY_MODE (0)
#define IN_valley_FEATURE_APPLY_MODE (1)
#define IN_silhouette_FEATURE_APPLY_MODE (0)

#define IN_barycentric_ASPECT_MODE (1) 

#define IN_USE_GEOMETRY_SHADER (1)

#define IN_USE_BUFFER_TO_PIXEL_TRANSFER (1)
#if IN_USE_BUFFER_TO_PIXEL_TRANSFER
	#define CHECK_IN_USE_BUFFER_TO_PIXEL_TRANSFER(x) x
	#define CHECK_IN_USE_BUFFER_TO_PIXEL_TRANSFER_C(x) ,x
#else
	#define CHECK_IN_USE_BUFFER_TO_PIXEL_TRANSFER(x) 
	#define CHECK_IN_USE_BUFFER_TO_PIXEL_TRANSFER_C(x) 
#endif

#define IN_IMITATION (0)

#if IN_IMITATION
#include "../npr_adjacency.hlsl"
#include "../npr_feature_common.txt"
#include "../npr_feature_ridgeValley.hlsl"
#else
#include "npr_adjacency.hlsl"
#include "npr_feature_common.txt"
#include "npr_feature_ridgeValley.hlsl"
#endif

			#pragma vertex vert
			#pragma fragment frag   
			#pragma geometry geom

			#pragma target 5.0

			struct appdata
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float3 nrm : NORMAL;
			};

CHECK_IN_USE_BUFFER_TO_PIXEL_TRANSFER(
			struct bufferToPixel {
#if IN_barycentric_ASPECT_MODE
				float2 barycentric_coords : ANY_barycentric_COORDS; 
#endif
			};

			bufferToPixel make_bufferToPixel(float2 barycentric_coords) {
				bufferToPixel b;
				b.barycentric_coords = barycentric_coords;
				return b;
			}
)

			struct geometry_in {
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float3 nrm : NORMAL;
				// per-vertex line status
				float4 lineStatus : ANY_LINE_STATUS;
				// this is data describing 3 edges of triangle. The same for all of 3 vertex that made this triangle
				half4 triangleEdgeStatus[3] :  ANY_TRAINGLE_EDGE_STATUS;
				CHECK_IN_USE_BUFFER_TO_PIXEL_TRANSFER(bufferToPixel transfer;)
			};

			geometry_in make_geometry_in(in appdata a, float4 lineStatus) {
				geometry_in g;
				g.pos = a.pos;
				g.uv = a.uv;
				g.nrm = a.nrm;
				g.lineStatus = lineStatus;
				return g;
			}

			struct pixelIn
			{
				float4 pos : SV_POSITION;
				float3 nrm : TEXCOORD1;
				int triangleType : ANY;
				half4 lineStatus : ANY1;
				half4 triangleEdgeStatus[3] : ANY_TRAINGLE_EDGE_STATUS;
				CHECK_IN_USE_BUFFER_TO_PIXEL_TRANSFER(bufferToPixel transfer;)
			};

			pixelIn make_pixelIn( float4 pos, float3 nrm, int triangleType, half4 lineStatus, half4 triangleEdgeStatus[3] CHECK_IN_USE_BUFFER_TO_PIXEL_TRANSFER_C(bufferToPixel transfer) ) {
				pixelIn i;
				i.pos = pos;
				i.nrm = nrm;
				i.triangleType = triangleType;
				i.lineStatus = lineStatus;
				i.triangleEdgeStatus = triangleEdgeStatus;
				CHECK_IN_USE_BUFFER_TO_PIXEL_TRANSFER(i.transfer = transfer;)
				return i;
			}

			struct geometryOut {
				pixelIn i;
				uint pid : SV_PrimitiveID;
			};

			geometryOut make_geometryOut(pixelIn i, uint pid) {
				geometryOut go;
				go.i = i;
				go.pid = pid;
				return go;
			}

			StructuredBuffer<float2> _BarycentricCoordinatesBuffer;
			StructuredBuffer<AdjacencyInfo> _AdjacencyBuffer;
			StructuredBuffer<float> _EdgeAngleBuffer;

			sampler2D _MainTex;
			float4 _MainTex_ST;

//IMITATION UNIFORMS DECLARATION START
			float _EdgeSize;
			float _RidgeTreshold;
			float _ValleyTreshold;
			float _LineClosingFactor;
			float _SurfaceLineWidth;




//IMITATION UNIFORMS DECLARATION END

			//# Needs _RidgeTreshold
			//# Needs  IN_ridge_FEATURE_STATUS_INDEX
			// this is per Edge
			geometry_edge_featureSpecification ridgeFeature_geometry_filter(float trait ) {
				bool flag = trait < _RidgeTreshold;
				return make_geometry_edge_featureSpecification(flag);
			}

			//# Needs _ValleyTreshold
			//# Needs  IN_valley_FEATURE_STATUS_INDEX
			// this is per Edge
			geometry_edge_featureSpecification valleyFeature_geometry_filter(float trait) {
				bool flag = trait > _ValleyTreshold;
				return make_geometry_edge_featureSpecification(flag);
			}

			float silhouetteFeature_geometry_traitGenerator(geometry_edge_situation s, geometry_camera_situation cs) {
				bool flag = (dot(s.t1Norm, cs.viewDirect) < 0) && (dot(s.t2Norm, cs.t2Direct) >= 0.0f);
				if (flag) {
					return 1;
				}
				else {
					return 0;
				}
			}

			geometry_edge_featureSpecification silhouetteFeature_geometry_filter(float trait) {
				bool flag = trait > 0.5;
				return make_geometry_edge_featureSpecification(flag);
			}

#define vertex_filterFeature(featureName, lineStatus, trait, z){ \
			if( IN_##featureName##_FEATURE_DETECTION_MODE == DETECTION_MODE_VERTEX){ \
				geometry_edge_featureSpecification spec = featureName##Feature_geometry_filter(trait); \
				int statusIndex = IN_##featureName##_FEATURE_STATUS_INDEX; \
					if (spec.flag) { \
						lineStatus[z][statusIndex] = 1; \
					} \
				} \
			}			


#if IN_USE_GEOMETRY_SHADER
#define vertexOut_t geometry_in
#else
#define vertexOut_t pixelIn
#endif

			vertexOut_t vert(appdata v, uint vid : SV_VertexID)
			{
				float4 lineStatus = 0;

				uint pid = vid / 3;
				uint vIdx = vid - pid * 3;
				uint auxVIdx = (vIdx + 1) % 3;
				uint auxVIdx2 = (vIdx + 2) % 3;
				uint auxVid = pid * 3 + auxVIdx;
				uint auxVid2 = pid * 3 + auxVIdx2;

				half4 triangleEdgeStatus[3];

				for (int i1 = 0; i1 < 3; i1++) {
					triangleEdgeStatus[i1] = 0;
				}
				for (int i2 = 0; i2 < 3; i2++) {
					float edgeAngle = _EdgeAngleBuffer[pid * 3 + i2];
					vertex_filterFeature(ridge, triangleEdgeStatus,edgeAngle, i2);
					vertex_filterFeature(valley, triangleEdgeStatus,edgeAngle, i2);
				}

				float2 barycentric_coords = 0;
#if IN_barycentric_ASPECT_MODE==DETECTION_MODE_VERTEX
				barycentric_coords = _BarycentricCoordinatesBuffer[vid];
#endif

#if IN_USE_GEOMETRY_SHADER
				geometry_in g = make_geometry_in(v, lineStatus);
				g.triangleEdgeStatus = triangleEdgeStatus;
				g.transfer.barycentric_coords = barycentric_coords;
#else
				pixelIn g = make_pixelIn(
					UnityObjectToClipPos(v.pos),
					normalize(mul(float4(v.nrm, 0.0), unity_WorldToObject).xyz),
					TRIANGLE_TYPE_STANDARD,
					lineStatus,
					triangleEdgeStatus
					CHECK_IN_USE_BUFFER_TO_PIXEL_TRANSFER_C(make_bufferToPixel(barycentric_coords))
				);
#endif


				return g;
			}

			appdata initialize_appdata(float3 position, float3 normal, float2 uv) {
				appdata v;
				v.pos = float4(position,1);
				v.nrm = normal;
				v.uv = uv;
				return v;
			}

			//todo make possible to use lineStatus here
#define geometry_filterFeature(featureName, situation, cameraSituation, lineStatus, triangleEdgeStatus, shouldCreateFins ){ \
			int statusIndex = IN_##featureName##_FEATURE_STATUS_INDEX; \
			if(  IN_##featureName##_FEATURE_DETECTION_MODE == DETECTION_MODE_GEOMETRY){ \
					float trait = featureName##Feature_geometry_traitGenerator(situation, cameraSituation); \
					geometry_edge_featureSpecification spec = featureName##Feature_geometry_filter(trait); \
						if (spec.flag) { \
							triangleEdgeStatus[situation.v1Index][statusIndex] = 1; \
							lineStatus[situation.v1Index][statusIndex] = 1; \
							lineStatus[situation.v2Index][statusIndex] = 1; \
							if( IN_##featureName##_FEATURE_APPLY_MODE == APPLY_MODE_FINS){ \
								shouldCreateFins = true; \
							} \
						} \
				}else {\
					if( IN_##featureName##_FEATURE_APPLY_MODE == APPLY_MODE_FINS){ \
						if(triangleEdgeStatus[situation.v1Index][statusIndex] == 1){ \
							shouldCreateFins = true; \
						} \
					} \
				}\
			}			


			[maxvertexcount(21)]
			void geom(triangle geometry_in input[3], uint pid : SV_PrimitiveID, inout TriangleStream<geometryOut> outStream)
			{
				AdjacencyInfo adjacent = _AdjacencyBuffer[pid];


				float3 normalTrian = getNormal(input[0].pos.xyz, input[1].pos.xyz, input[2].pos.xyz);
				float3 viewDirect = normalize((input[0].pos.xyz + input[1].pos.xyz + input[2].pos.xyz) / 3 - UNITY_MATRIX_IT_MV[3].xyz);

				half4 lineStatus[3];
				half4 triangleEdgeStatus[3];
				triangleEdgeStatus = input[0].triangleEdgeStatus;
				for (int i1 = 0; i1 < 3; i1++) {
					lineStatus[i1] =  input[i1].lineStatus;
				}

				//[branch]
				//if(  ){
					[loop]
					for (uint i = 0; i < 3; i += 1) {
						uint auxIndex = (i + 1) % 3;
						if (input[auxIndex].pos.z < -99999) {
							continue;
						}
						float3 auxNormal = getNormal(input[i].pos.xyz, adjacent.pos[i].xyz, input[auxIndex].pos.xyz);
						float3 auxDirect = normalize((input[i].pos.xyz + adjacent.pos[i].xyz + input[auxIndex].pos.xyz) / 3 - UNITY_MATRIX_IT_MV[3].xyz);

						bool silhouetteFlag = (dot(normalTrian, viewDirect) < 0) && (dot(auxNormal, auxDirect) >= 0.0f);

						geometry_edge_situation situation = make_geometry_edge_situation(input[i].pos, input[auxIndex].pos,
							normalTrian, auxNormal, i, auxIndex);
						geometry_camera_situation cameraSituation = make_geometry_camera_situation(viewDirect, auxDirect);

						bool shouldCreateFins = false; //todo

						geometry_filterFeature(ridge, situation, cameraSituation, lineStatus, triangleEdgeStatus, shouldCreateFins);
						geometry_filterFeature(valley, situation, cameraSituation, lineStatus, triangleEdgeStatus, shouldCreateFins);
						geometry_filterFeature(silhouette, situation, cameraSituation, lineStatus, triangleEdgeStatus, shouldCreateFins);

						//if (valleyFlag || ridgeFlag) {
						if (shouldCreateFins) {
							// we have a silhouette edge!
							//transform position to screen space
							// polorzenie wierzcholka 1 w screen-sapce
							float edgeSize = _EdgeSize;
							float4 transPos1 = UnityObjectToClipPos(input[i].pos);
							float o1 = transPos1.w;
							transPos1 = transPos1 / transPos1.w;

							// polorzenie wierzcholka 2 w screen-sapce
							float4 transPos2 = UnityObjectToClipPos(input[auxIndex].pos);
							float o2 = transPos2.w;
							transPos2 = transPos2 / transPos2.w;

							// calculate edge direction in screen space
							float2 edgeDirection = normalize(transPos1.xy - transPos2.xy);

							//extrude vector in screen space
							float4 extrudeDirection = float4(normalize(float2(-edgeDirection.y, edgeDirection.x)), 0, 0);
							float4 normExtrude1 = UnityObjectToClipPos(input[i].pos + adjacent.nrm[i]);
							normExtrude1 = normExtrude1 / normExtrude1.w;
							normExtrude1 = normExtrude1 - transPos1;
							normExtrude1 = float4(normalize(normExtrude1.xy),0.0f ,0.0f);

							float4 normExtrude2 = UnityObjectToClipPos(input[auxIndex].pos + adjacent.nrm[auxIndex]);
							normExtrude2 = normExtrude2 / normExtrude2.w;
							normExtrude2 = normExtrude2 - transPos2;
							normExtrude2 = float4(normalize(normExtrude2.xy),0.0f ,0.0f);

							//Scale the extrude directions with the edge size.
							normExtrude1 = normExtrude1 * edgeSize;
							normExtrude2 = normExtrude2 * edgeSize;
							extrudeDirection = extrudeDirection * edgeSize;

							//Calculate the extruded vertices .
							float4 normVertex1 = transPos1 + normExtrude1;
							float4 extruVertex1 = transPos1 + extrudeDirection;
							float4 normVertex2 = transPos2 + normExtrude2;
							float4 extruVertex2 = transPos2 + extrudeDirection;

							pixelIn outVert[6];

							half4 nullTriangleEdgeStatus[3];
							nullTriangleEdgeStatus[0] = 0;
							nullTriangleEdgeStatus[1] = 0;
							nullTriangleEdgeStatus[2] = 0;

							outVert[0].pos = float4(normVertex1.xyz*o1, o1);
							outVert[1].pos = float4(extruVertex1.xyz*o1, o1); //(v0 + e)
							outVert[2].pos = float4(transPos1.xyz*o1, o1); // v0
							outVert[3].pos = float4(normVertex2.xyz*o2, o2);
							outVert[4].pos = float4(extruVertex2.xyz*o2, o2); // v1 + e
							outVert[5].pos = float4(transPos2.xyz*o2, o2); //v1

							half4 finTriangleEdgeStatus[3];
							finTriangleEdgeStatus[0] = triangleEdgeStatus[situation.v1Index]; //for fins data is transfered only in triangleEdgestatus[0]
							finTriangleEdgeStatus[1] = 0;
							finTriangleEdgeStatus[2] = 0;

							/////// UVS For extruded vertices
							for (int k = 0; k < 6; k++) {
								outVert[k].pos.z *= _LineClosingFactor; // TODO do innych shaderow tego uzyj
								outVert[k].nrm = float3(1,0,0); //todo
								outVert[k].triangleType = TRIANGLE_TYPE_FIN;
								outVert[k].lineStatus = 0;
								outVert[k].triangleEdgeStatus = finTriangleEdgeStatus; //todo
								outVert[k].transfer = input[i].transfer; //todo
							}

							uint finPid = 10000000 + pid * 6 + i*2;
							outStream.Append(make_geometryOut(outVert[0], finPid+0));
							outStream.Append(make_geometryOut(outVert[1], finPid+0));
							outStream.Append(make_geometryOut(outVert[2], finPid+0));
							outStream.Append(make_geometryOut(outVert[4], finPid+1));
							outStream.Append(make_geometryOut(outVert[5], finPid+1));
							outStream.Append(make_geometryOut(outVert[3], finPid+1));

							outStream.RestartStrip();
						}

					}

					pixelIn v[3];

					for (int i2 = 0; i2 < 3; i2++) {
						v[i2].pos = UnityObjectToClipPos(input[i2].pos);
						v[i2].nrm = normalize(mul(float4(input[i2].nrm, 0.0), unity_WorldToObject).xyz);
						v[i2].triangleType = TRIANGLE_TYPE_STANDARD;
						v[i2].lineStatus = lineStatus[i2];
						v[i2].triangleEdgeStatus = triangleEdgeStatus;
						v[i2].transfer = input[i2].transfer;
					}

#if IN_barycentric_ASPECT_MODE==DETECTION_MODE_GEOMETRY
					v[0].transfer.barycentric_coords = float2(1, 0);
					v[1].transfer.barycentric_coords = float2(0, 1);
					v[2].transfer.barycentric_coords = float2(0, 0);
#endif

					for (int i3 = 0; i3 < 3; i3++) {
						geometryOut go;
						go.i = v[i3];
						go.pid = pid;
						outStream.Append(make_geometryOut(v[i3], pid));
					}
					outStream.RestartStrip();
				}


				struct pixel_line_applyData {
					bool shouldApply;
					fixed4 newColor;
				};

				pixel_line_applyData make_pixel_line_applyData(bool shouldApply, fixed4 newColor) {
					pixel_line_applyData data;
					data.shouldApply = shouldApply;
					data.newColor = newColor;
					return data;
				}


				pixel_line_applyData ridgeFeature_line_pixelApply( half4 lineStatus, float lineWidth) {
					fixed4 color = fixed4(1,0,0,1);
					bool shouldApply = false;
					if (lineStatus[IN_ridge_FEATURE_STATUS_INDEX] > 0.5){
						shouldApply = true;
					}
					return make_pixel_line_applyData(shouldApply, color);
				}

				pixel_line_applyData valleyFeature_line_pixelApply( half4 lineStatus, float lineWidth) {
					fixed4 color = fixed4(0,1,0,1);
					bool shouldApply = false;
					if (lineStatus[IN_valley_FEATURE_STATUS_INDEX] > 0.5){
						shouldApply = true;
					}
					return make_pixel_line_applyData(shouldApply, color);
				}

				pixel_line_applyData silhouetteFeature_line_pixelApply( half4 lineStatus, float lineWidth) {
					fixed4 color = fixed4(0,0,1,1);
					bool shouldApply = false;
					if (lineStatus[IN_silhouette_FEATURE_STATUS_INDEX] > 0.5){
						shouldApply = true;
					}
					return make_pixel_line_applyData(shouldApply, color);
				}

				pixel_line_applyData ridgeFeature_fin_pixelApply( half4 lineStatus, float lineWidth) {
					return ridgeFeature_line_pixelApply(lineStatus, lineWidth);
				}

				pixel_line_applyData valleyFeature_fin_pixelApply( half4 lineStatus, float lineWidth) {
					return valleyFeature_line_pixelApply(lineStatus, lineWidth);
				}

				pixel_line_applyData silhouetteFeature_fin_pixelApply( half4 lineStatus, float lineWidth) {
					return silhouetteFeature_line_pixelApply(lineStatus, lineWidth);
				}

#if IN_barycentric_ASPECT_MODE
				// TODO: this do not work perfectly, there are ugly hooks due to triangles not having equal side length
				// and we are not calculating real closest verticles index in 2d space
				bool pixel_surfaceLine_shouldApply(pixelIn i, float lineWidth) {
					float3 barys;
					barys.xy = i.transfer.barycentric_coords;
					barys.z = 1 - barys.x - barys.y;
					float3 deltas = fwidth(barys)*lineWidth;
					barys = step(deltas, barys);
					float minBary = min(barys.x, min(barys.y, barys.z));
					return  minBary < 0.0001;
				}

				bool pixel_surfaceLine_shouldApply_no_hook(pixelIn i, float lineWidth, int edgeIndex) {
					float3 barys;
					barys.xy = i.transfer.barycentric_coords;
					barys.z = 1 - barys.x - barys.y;
					float3 deltas = fwidth(barys)*lineWidth;
					barys = step(deltas, barys);
					float minBary = min(barys.x, min(barys.y, barys.z));
					return  barys[edgeIndex]< 0.0001;
				}

				

				uint pixel_get_closestVertexIndex(pixelIn i) {
					float3 barys;
					barys.xy = i.transfer.barycentric_coords;
					barys.z = 1 - barys.x - barys.y;
					if (barys.x > barys.y) {
						if (barys.x > barys.z) {
							return 0;
						}
						else {
							return 2;
						}
					}
					else {
						if (barys.y > barys.z) {
							return 1;
						}
						else {
							return 2;
						}
					}
				}

				uint pixel_get_furthestVertexIndex(pixelIn i) {
					float3 barys;
					barys.xy = i.transfer.barycentric_coords;
					barys.z = 1 - barys.x - barys.y;
					if (barys.x < barys.y) {
						if (barys.x < barys.z) {
							return 0;
						}
						else {
							return 2;
						}
					}
					else {
						if (barys.y < barys.z) {
							return 1;
						}
						else {
							return 2;
						}
					}
				}

				uint pixel_get_closestEdgeIndex(pixelIn i) {
					uint vClosest = pixel_get_closestVertexIndex(i);
					uint vFurthest = pixel_get_furthestVertexIndex(i);
					if (vClosest == 0) {
						if (vFurthest == 2) {
							return 0; //edge 0-1
						}
						else {
							return 2; //edge 0-2
						}
					}

					if (vClosest == 1) {
						if (vFurthest == 2) {
							return 0; // edge 0-1
						}
						else {
							return 1; // edge 1-2
						}
					}

					if (vClosest == 2) {
						if (vFurthest == 1) {
							return 2; // edge 0-2
						}
						else {
							return 1; // edge 1-2
						}
					}
					return 99;
				}
#endif


#define pixel_surfaceLine_featureApply(featureName, color, lineStatus, lineWidth  ) { \
				if(IN_##featureName##_FEATURE_DETECTION_MODE != DETECTION_MODE_OFF ){ \
					if(IN_##featureName##_FEATURE_APPLY_MODE == APPLY_MODE_SURFACE_LINE ){ \
						pixel_line_applyData data = featureName##Feature_line_pixelApply(lineStatus, lineWidth); \
							if (data.shouldApply) { \
								color = data.newColor; \
							} \
						}\
					}\
				}


#define pixel_fin_featureApply(featureName, color, lineStatus, lineWidth  ) { \
				if(IN_##featureName##_FEATURE_DETECTION_MODE != DETECTION_MODE_OFF ){ \
					if(IN_##featureName##_FEATURE_APPLY_MODE == APPLY_MODE_FINS){ \
						pixel_line_applyData data = featureName##Feature_fin_pixelApply(lineStatus, lineWidth); \
							if (data.shouldApply) { \
								color = data.newColor; \
							} \
						}\
					}\
				}


			fixed4 frag (pixelIn i, uint pid : SV_PrimitiveID ) : SV_Target
			{
				fixed4 color = 1;
				float lineWidth = _SurfaceLineWidth;

				if (i.triangleType == TRIANGLE_TYPE_STANDARD) {
#if IN_barycentric_ASPECT_MODE
					uint edgeIndex = pixel_get_closestEdgeIndex(i);

					half4 oneTriangleEdgeStatus = i.triangleEdgeStatus[edgeIndex];

					if (pixel_surfaceLine_shouldApply_no_hook(i, lineWidth, (edgeIndex + 2) % 3)) {
						//if (pixel_surfaceLine_shouldApply(i, lineWidth)) { 
						pixel_surfaceLine_featureApply(ridge, color, oneTriangleEdgeStatus, lineWidth);
						pixel_surfaceLine_featureApply(valley, color, oneTriangleEdgeStatus, lineWidth);
						pixel_surfaceLine_featureApply(silhouette, color, oneTriangleEdgeStatus, lineWidth);
					}
#endif
				}
				else {
					half4 finTriangleEdgeStatus = i.triangleEdgeStatus[0];
					pixel_fin_featureApply(ridge, color, finTriangleEdgeStatus, lineWidth);
					pixel_fin_featureApply(valley, color, finTriangleEdgeStatus, lineWidth);
					pixel_fin_featureApply(silhouette, color, finTriangleEdgeStatus, lineWidth);
				}

				return color;
			}
			ENDCG
		}
	}
}
