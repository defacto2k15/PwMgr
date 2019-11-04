Shader "Custom/NPR/IMITATION/Template/ObjectShader-10590" {
    Properties {
		_MainTex("MainTex", 2D)  = "white" {}

//IMITATION UNIFORMS PROPERTY START
		_ridgeTreshold("ridgeTreshold", Range(-2,2)) = 0
		_valleyTreshold("valleyTreshold", Range(-2,2)) = 0
		_EdgeSize("EdgeSize", Range(0,0.2)) = 0.01
		_LineClosingFactor("LineClosingFactor", Range(0,3)) = 1
		_SurfaceLineWidth("SurfaceLineWidth", Range(0,10)) = 0

		_sc_FeatureSize("sc_FeatureSize", Range(0,20)) = 11.9
		_sc_ContourLimit("sc_ContourLimit", Range(0,20)) = 2.2
		_sc_SuggestiveContourLimit("sc_SuggestiveContourLimit", Range(0,10)) = 2.4
		_sc_DwKrLimit("sc_DwKrLimit", Range(0,0.8)) = 0.07
		_sc_JeroenMethod("sc_JeroenMethod", Int) = 1

		_sh_FeatureSize("sh_FeatureSize", Range(0,20)) = 0.07
		_sh_SuggestiveContourLimit("sh_SuggestiveContourLimit", Range(-2,10)) = -0.281
		_sh_DwKrLimit("sh_DwKrLimit", Range(-1,0.8)) = -0.1
		_sh_JeroenMethod("sh_JeroenMethod", Int) = 1

		_ph_Test6Cutoff("ph_Test6Cutoff", Range(-1,1)) = 0.1 //Torus=0.1
		_ph_TestDerivativeCutoff("ph_TestDerivativeCutoff", Range(0, 100)) =3 //T=4.2

		_happ_TauFactor("TauFactor", Range(-30, 30)) = 2.5


		 _obj_ObjectID("dn_ObjectID", Int) = 0

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
#define APPLY_MODE_FILLING (3)
#define APPLY_MODE_LINE_FILLING (4)

#define IN_ridge_FEATURE_APPLY_MODE (0)
#define IN_valley_FEATURE_APPLY_MODE (1) 
#define IN_silhouette_FEATURE_APPLY_MODE (0)

#define IN_obj_TARGET_INDEX 2
#define IN_dn_TARGET_INDEX (1)
#define IN_happ_TARGET_INDEX 3
#define IN_ph_TARGET_INDEX 0
#define IN_ridge_TARGET_INDEX 0
#define IN_sc_TARGET_INDEX (0)
#define IN_scpp_TARGET_INDEX 2
#define IN_sh_TARGET_INDEX 0
#define IN_silhouette_TARGET_INDEX 0
#define IN_valley_TARGET_INDEX 0


#define IN_barycentric_ASPECT_MODE (1)

#define IN_USE_GEOMETRY_SHADER (0)

#define IN_IMITATION (1)

#include "UnityCG.cginc"
#if IN_IMITATION
#include "../npr_adjacency.hlsl"
#include "../npr_feature_common.txt"
#include "../npr_feature_ridgeValley.hlsl"
#include "../common.txt"
#else
#include "npr_adjacency.hlsl"
#include "npr_feature_common.txt"
#include "npr_feature_ridgeValley.hlsl"
#include "common.txt"
#endif

#define IN_ridge_FEATURE_DETECTION_MODE DETECTION_MODE_VERTEX
#define IN_ridge_FEATURE_APPLY_MODE APPLY_MODE_SURFACE_LINE

#define IN_valley_FEATURE_DETECTION_MODE DETECTION_MODE_VERTEX
#define IN_valley_FEATURE_APPLY_MODE APPLY_MODE_SURFACE_LINE

#define IN_silhouette_FEATURE_DETECTION_MODE DETECTION_MODE_OFF
#define IN_silhouette_FEATURE_APPLY_MODE APPLY_MODE_SURFACE_OFF

#define IN_sc_FEATURE_DETECTION_MODE (1)
#define IN_sc_FEATURE_APPLY_MODE (3)

#define IN_sh_FEATURE_DETECTION_MODE DETECTION_MODE_VERTEX
#define IN_sh_FEATURE_APPLY_MODE APPLY_MODE_LINE_FILLING

#define IN_ph_FEATURE_DETECTION_MODE DETECTION_MODE_VERTEX
#define IN_ph_FEATURE_APPLY_MODE APPLY_MODE_FILLING

#define IN_scpp_FEATURE_DETECTION_MODE DETECTION_MODE_PIXEL
#define IN_scpp_FEATURE_APPLY_MODE APPLY_MODE_FILLING

#define IN_happ_FEATURE_DETECTION_MODE DETECTION_MODE_PIXEL
#define IN_happ_FEATURE_APPLY_MODE APPLY_MODE_FILLING

#define IN_dn_FEATURE_DETECTION_MODE (3)
#define IN_dn_FEATURE_APPLY_MODE (3)

#define IN_obj_FEATURE_DETECTION_MODE DETECTION_MODE_PIXEL
#define IN_obj_FEATURE_APPLY_MODE APPLY_MODE_FILLING

// IN FEATURE MACRO START
#define FEATURE1( a )  a(sc)
#define FEATURE2( a )  a(dn)
#define FEATURE3( a )  
#define FEATURE4( a ) 
#define FEATURE5( a ) 
#define FEATURE6( a ) 
#define FEATURE7( a ) 
#define FEATURE8( a ) 
#define FEATURE9( a ) 
#define FEATURE9( a ) 
#define FEATURE10( a ) 
// IN FEATURE MACRO END

// IN ASPECT MACRO START
#define ASPECT1( a ) a(barycentric)
#define ASPECT2( a ) 
#define ASPECT3( a ) 
#define ASPECT4( a ) 
#define ASPECT5( a ) 
// IN ASPECT MACRO END


struct appdata
{
	float4 pos : POSITION;
	float2 uv : TEXCOORD0;
	float3 nrm : NORMAL;
};

struct VertexSituation {
	uint pid;
	uint vid;
};

struct UniformVertexSituation {
};

struct FeatureFragmentOut {
	bool applyColor;
	fixed4 color;
	int renderTargetIndex;
};

struct FragmentSurfaceLineSituation {
	int edgeIndex;
};

			struct MRTFragmentOutput
			{
				half4 dest0 : SV_Target0;
				half4 dest1 : SV_Target1;
				half4 dest2 : SV_Target2;
				half4 dest3 : SV_Target3;
			};

			MRTFragmentOutput make_MRTFragmentOutput(half4 dest0, half4 dest1, half4 dest2, half4 dest3) {
				MRTFragmentOutput o;
				o.dest0 = dest0;
				o.dest1 = dest1;
				o.dest2 = dest2;
				o.dest3 = dest3;
				return o;
			}

			struct PrincipalCurvatureInfo {
				float3 direction1;
				float value1;
				float3 direction2;
				float value2;
				float4 derivative;
			};


			StructuredBuffer<float2> _BarycentricCoordinatesBuffer;
			StructuredBuffer<AdjacencyInfo> _AdjacencyBuffer;
			StructuredBuffer<float> _EdgeAngleBuffer;
			StructuredBuffer<PrincipalCurvatureInfo> _PrincipalCurvatureBuffer;
			StructuredBuffer<float3> _InterpolatedNormalsBuffer;

			sampler2D _MainTex;
			float4 _MainTex_ST;

//IMITATION UNIFORMS DECLARATION START
			float _EdgeSize;
			float _ridgeTreshold;
			float _valleyTreshold;
			float _LineClosingFactor;
			float _SurfaceLineWidth;

			float _sc_ContourLimit;
			float _sc_DwKrLimit;
			float _sc_SuggestiveContourLimit;
			float _sc_FeatureSize; 
			int _sc_JeroenMethod;
			  
			float _sh_DwKrLimit;
			float _sh_SuggestiveContourLimit;
			float _sh_FeatureSize; 
			int _sh_JeroenMethod;

			float _ph_Test6Cutoff;
			float _ph_TestDerivativeCutoff;

			float _happ_TauFactor;

			int _obj_ObjectID;

//IMITATION UNIFORMS DECLARATION END

#if IN_IMITATION
#include "../Features/sc_feature.txt"
#include "../Features/dn_feature.txt"
#include "../Aspects/barycentric_aspect.txt"
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
#else
#include "Features/ridge_feature.txt"
#include "Features/valley_feature.txt"
#include "Features/silhouette_feature.txt"
#include "Features/sc_feature.txt"
#include "Features/sh_feature.txt"
#include "Features/ph_feature.txt"
#include "Features/scpp_feature.txt"
#include "Features/happ_feature.txt"
#include "Features/dn_feature.txt"
#include "Features/obj_feature.txt"
#include "Aspects/barycentric_aspect.txt"
#endif

			#pragma vertex vert
			#pragma fragment frag   
			////#pragma geometry geom

			#pragma target 5.0



			struct geometry_in {
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float3 nrm : NORMAL;

#define geometry_in_struct_feature_macro_filler( f ) f##_VertexOutBuffer f##_buffer : ANY_##f##_BUFFER;
#define FEATURE_LIST_INSIDE  geometry_in_struct_feature_macro_filler
#include "macro/featureListMacro1.txt"

#define aspect_macro_func( f ) f##_VertexOutBuffer f##_buffer : ANY_##f##_BUFFER;
#define ASPECT_LIST_INSIDE aspect_macro_func
#include "macro/aspectListMacro1.txt"

			};

			geometry_in make_geometry_in(in appdata a) {
				geometry_in g;
				g.pos = a.pos;
				g.uv = a.uv;
				g.nrm = a.nrm;
				return g;
			} 

			struct pixelIn
			{
				float4 pos : SV_POSITION;
				float3 nrm : TEXCOORD1;
				int triangleType : ANY;

#define pixel_in_struct_feature_macro_filler( f ) f##_FragmentInBuffer f##_buffer : ANY_##f##_BUFFER;
#define FEATURE_LIST_INSIDE  pixel_in_struct_feature_macro_filler
#include "macro/featureListMacro2.txt"

#define aspect_macro_func( f ) f##_FragmentInBuffer f##_buffer : ANY_##f##_BUFFER;
#define ASPECT_LIST_INSIDE aspect_macro_func
#include "macro/aspectListMacro2.txt"

			};

			pixelIn make_pixelIn( float4 pos, float3 nrm, int triangleType  ) {
				pixelIn i;
				i.pos = pos;
				i.nrm = nrm;
				i.triangleType = triangleType;
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


#if IN_USE_GEOMETRY_SHADER
#define vertexOut_t geometry_in
#else
#define vertexOut_t pixelIn
#endif

			vertexOut_t vert(appdata v, uint vid : SV_VertexID)
			{
				uint pid = vid / 3;

#if IN_USE_GEOMETRY_SHADER
				geometry_in g = make_geometry_in(v);
#else
				pixelIn g = make_pixelIn(
					UnityObjectToClipPos(v.pos),
					normalize(mul(float4(v.nrm, 0.0), unity_WorldToObject).xyz),
					TRIANGLE_TYPE_STANDARD
				);

#endif

				VertexSituation vertexSituation;
				vertexSituation.pid = pid;
				vertexSituation.vid = vid;

#define vertex_filter_feature_macro_filler( f ) g.##f##_buffer = f##_VertexFilter(vertexSituation, v);
#define FEATURE_LIST_INSIDE vertex_filter_feature_macro_filler
#include "macro/featureListMacro3.txt"

#define aspect_macro_func( f ) g.##f##_buffer = f##_VertexFilter(vertexSituation);
#define ASPECT_LIST_INSIDE aspect_macro_func
#include "macro/aspectListMacro3.txt"
				return g;
			}

			appdata initialize_appdata(float3 position, float3 normal, float2 uv) {
				appdata v;
				v.pos = float4(position,1);
				v.nrm = normal;
				v.uv = uv;
				return v;
			} 

			[maxvertexcount(21)]
			void geom(triangle geometry_in input[3], uint pid : SV_PrimitiveID, inout TriangleStream<geometryOut> outStream)
			{
				AdjacencyInfo adjacent = _AdjacencyBuffer[pid];


				float3 normalTrian = getNormal(input[0].pos.xyz, input[1].pos.xyz, input[2].pos.xyz);
				float3 viewDirect = normalize((input[0].pos.xyz + input[1].pos.xyz + input[2].pos.xyz) / 3 - UNITY_MATRIX_IT_MV[3].xyz);

#define feature_macro_func( f ) f##_GeometryOutBuffer f##_fin_outBuffer; f##_GeometryOutBuffer f##_outBuffers[3]; f##_VertexOutBuffer f##_inBuffers[3]; if( Transfer_##f##_VertexOutBuffer ){ f##_inBuffers[0] = input[0].f##_buffer; f##_inBuffers[1] = input[1].f##_buffer; f##_inBuffers[2] = input[2].f##_buffer;}
#define FEATURE_LIST_INSIDE feature_macro_func
#include "macro/featureListMacro4.txt"

#define aspect_macro_func( f ) f##_GeometryOutBuffer f##_fin_outBuffer; f##_GeometryOutBuffer f##_outBuffers[3]; f##_VertexOutBuffer f##_inBuffers[3]; if( Transfer_##f##_VertexOutBuffer ){ f##_inBuffers[0] = input[0].f##_buffer; f##_inBuffers[1] = input[1].f##_buffer; f##_inBuffers[2] = input[2].f##_buffer;}
#define ASPECT_LIST_INSIDE aspect_macro_func
#include "macro/aspectListMacro4.txt"

					[loop]
					for (uint i = 0; i < 3; i += 1) {
						uint auxIndex = (i + 1) % 3;
						if (input[auxIndex].pos.z < -99999) {
							continue;
						}
						float3 auxNormal = getNormal(input[i].pos.xyz, adjacent.pos[i].xyz, input[auxIndex].pos.xyz);
						float3 auxDirect = normalize((input[i].pos.xyz + adjacent.pos[i].xyz + input[auxIndex].pos.xyz) / 3 - UNITY_MATRIX_IT_MV[3].xyz);

						geometry_edge_situation situation = make_geometry_edge_situation(input[i].pos, input[auxIndex].pos,
							normalTrian, auxNormal, i, auxIndex);
						geometry_camera_situation cameraSituation = make_geometry_camera_situation(viewDirect, auxDirect);

						bool shouldCreateFins = false;

#define feature_macro_func( f ) f##_GeometryFilter(f##_inBuffers, f##_outBuffers, situation, cameraSituation, i, shouldCreateFins, f##_fin_outBuffer);
#define FEATURE_LIST_INSIDE feature_macro_func
#include "macro/featureListMacro5.txt"

#define aspect_macro_func( f )  f##_GeometryFilter(f##_inBuffers, f##_outBuffers, i, f##_fin_outBuffer);
#define ASPECT_LIST_INSIDE aspect_macro_func
#include "macro/aspectListMacro5.txt"

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

							/////// UVS For extruded vertices
							for (int k = 0; k < 6; k++) {
								outVert[k].pos.z *= _LineClosingFactor; // TODO do innych shaderow tego uzyj
								outVert[k].nrm = float3(1,0,0); //todo
								outVert[k].triangleType = TRIANGLE_TYPE_FIN;

#define feature_macro_func( f ) outVert[k].f##_buffer = f##_fin_outBuffer;
#define FEATURE_LIST_INSIDE feature_macro_func
#include "macro/featureListMacro8.txt"

#define aspect_macro_func( f )  outVert[k].f##_buffer = f##_fin_outBuffer;
#define ASPECT_LIST_INSIDE aspect_macro_func
#include "macro/aspectListMacro8.txt"
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

#define feature_macro_func( f ) if(Transfer_##f##_GeometryOutBuffer){ v[i2].f##_buffer = f##_outBuffers[i2]; };
#define FEATURE_LIST_INSIDE feature_macro_func
#include "macro/featureListMacro6.txt"

#define aspect_macro_func( f )  if(Transfer_##f##_GeometryOutBuffer){ v[i2].f##_buffer = f##_outBuffers[i2]; };
#define ASPECT_LIST_INSIDE aspect_macro_func
#include "macro/aspectListMacro6.txt"
					}


					for (int i3 = 0; i3 < 3; i3++) {
						geometryOut go;
						go.i = v[i3];
						go.pid = pid;
						outStream.Append(make_geometryOut(v[i3], pid));
					}
					outStream.RestartStrip();
				}


#if IN_barycentric_ASPECT_MODE
				// TODO: this do not work perfectly, there are ugly hooks due to triangles not having equal side length
				// and we are not calculating real closest verticles index in 2d space
				bool pixel_surfaceLine_shouldApply(pixelIn i, float lineWidth) {
					float3 barys;
					barys.xy = i.barycentric_buffer.barycentric_coords;
					barys.z = 1 - barys.x - barys.y;
					float3 deltas = fwidth(barys)*lineWidth;
					barys = step(deltas, barys);
					float minBary = min(barys.x, min(barys.y, barys.z));
					return  minBary < 0.0001;
				}

				bool pixel_surfaceLine_shouldApply_no_hook(pixelIn i, float lineWidth, int edgeIndex) {
					float3 barys;
					barys.xy = i.barycentric_buffer.barycentric_coords;
					barys.z = 1 - barys.x - barys.y;
					float3 deltas = fwidth(barys)*lineWidth;
					barys = step(deltas, barys);
					float minBary = min(barys.x, min(barys.y, barys.z));
					return  barys[edgeIndex]< 0.0001;
				}

				

				uint pixel_get_closestVertexIndex(pixelIn i) {
					float3 barys;
					barys.xy = i.barycentric_buffer.barycentric_coords;
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
					barys.xy = i.barycentric_buffer.barycentric_coords;
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

			MRTFragmentOutput frag (pixelIn i, uint pid : SV_PrimitiveID ) : SV_Target
			{
				float lineWidth = _SurfaceLineWidth;

				float4 outColors[4];
				outColors[0] = 0;
				outColors[1] = 0;
				outColors[2] = 0;
				outColors[3] = 0;

				if (i.triangleType == TRIANGLE_TYPE_STANDARD) {

#define feature_macro_func( f )  f##_FragmentFillingFilter(i.f##_buffer, outColors[IN_##f##_TARGET_INDEX]);
#define FEATURE_LIST_INSIDE feature_macro_func
#include "macro/featureListMacro10.txt"

#if IN_barycentric_ASPECT_MODE
					uint edgeIndex = pixel_get_closestEdgeIndex(i);

					if (pixel_surfaceLine_shouldApply_no_hook(i, lineWidth, (edgeIndex + 2) % 3)) {
					//if (pixel_surfaceLine_shouldApply(i, lineWidth)) { 
						FragmentSurfaceLineSituation situation;
						situation.edgeIndex = edgeIndex;


#define feature_macro_func( f )  f##_FragmentSurfaceLineFilter(situation, i.f##_buffer,  outColors[IN_##f##_TARGET_INDEX]);
#define FEATURE_LIST_INSIDE feature_macro_func
#include "macro/featureListMacro7.txt"
						 
					}
#endif
				}
				else {

#define feature_macro_func( f ) f##_FragmentFinFilter(i.f##_buffer, outColors[IN_##f##_TARGET_INDEX]); 
#define FEATURE_LIST_INSIDE feature_macro_func
#include "macro/featureListMacro9.txt"
				}

				return make_MRTFragmentOutput(outColors[0], outColors[1], outColors[2], outColors[3]);
			}
			ENDCG
		}
	}
}

