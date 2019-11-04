Shader "Custom/Sandbox/Filling/TamIdBufferAggregateRenderer" {
	Properties{
		_DebugScalar("DebugScalar", Range(0,10)) = 0
		_DebugScalarX("DebugScalar", Range(-1,1)) = 0
		_DebugScalarY("DebugScalar", Range(-1,1)) = 0
		_DebugScalarZ("DebugScalarZ", Range(0,10)) = 0
		_FragmentTexWidth("FragmentTexWidth", Range(0,512)) = 0
	}

	SubShader
	{
		Tags {"Queue"="Transparent"  "RenderType" = "Transparent" }
		LOD 100
        ZWrite Off
        Blend One One

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
		// THIS SHADER TAKES FRAGMENT BUFFER AND RENDERS IT ON AGGREGATE TEXTURE
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma target 5.0
			#pragma multi_compile __ MEASUREMENT
			#pragma multi_compile __ LIGHT_SHADING_ON
			#pragma multi_compile __ DIRECTION_PER_LIGHT
			#include "UnityCG.cginc"
#include "tamIss_common.txt" 

			struct v2g {
				float4 pos : SV_POSITION;
			};

			struct g2f {
				float4 pos : SV_POSITION;
				float x : ANY_X;
				float y : ANY_Y;
				float t : ANY_T;
				int perIdIndex : ANY_PER_ID_INDEX;
			};

			g2f make_g2f(float4 pos, float x, float y, float t, int perIdIndex) {
				g2f o;
				o.pos = pos;
				o.x = x;
				o.y = y;
				o.t = t;
				o.perIdIndex = perIdIndex;
				return o;
			}

			StructuredBuffer<TamIdFragment> _FragmentsBuffer;
			int _FragmentTexWidth;
			float _DebugScalar;
			float _DebugScalarX;
			float _DebugScalarY;
			float _DebugScalarZ;

			v2g vert(uint id : SV_VertexID) {
				v2g o;

				TamIdFragment fragment = _FragmentsBuffer[id];
				float4 pos = float4( retriveXUvFromTamIdFragment(fragment), retriveYUvFromTamIdFragment(fragment), 1, 1);
				pos.xy = (pos.xy * 2) - 1;
				o.pos = pos;
				return o;
			}

#define AGGREGATE_TEX_STRIDE (6)
			[maxvertexcount(AGGREGATE_TEX_STRIDE)]
			void geom(point v2g input[1], uint pid : SV_PrimitiveID, inout PointStream<g2f> outStream)
			{
				TamIdFragment fragment = _FragmentsBuffer[pid];
				uint fragmentId = retriveIdFromTamIdFragment(fragment);
				float fragmentT = retriveTFromTamIdFragment(fragment);
				uint grid_size = _FragmentTexWidth;
				uint grid_stride = AGGREGATE_TEX_STRIDE; //6 pixels per id
				uint line_width = grid_stride * grid_size;

				uint2 inGridPosition = uint2(fragmentId % grid_size, floor(fragmentId / ((float)grid_size))  );

				for (int i = 0; i < grid_stride; i++) {
					float2 screenUv = float2(
						(inGridPosition.x*grid_stride+i) / ((float)line_width),
						(inGridPosition.y) / ((float)grid_size));
					screenUv += float2(1 / ((float)line_width), 1 / ((float)grid_size));
					screenUv.y = 1 - screenUv.y;

					screenUv = (screenUv * 2 - 1);
						outStream.Append(make_g2f(
							float4( screenUv.x, screenUv.y, 0, 1 ),
							retriveXUvFromTamIdFragment(fragment), retriveYUvFromTamIdFragment(fragment), fragmentT, i));
					}
			}

			float4 frag(g2f input) : SV_Target
			{
				float4x1 T;
				T[0][0] = 1;
				T[1][0] = input.t;
				T[2][0] = input.t*input.t;
				T[3][0] = input.t*input.t*input.t;

				float4x1 B = T * input.x;
				float4x1 C = T * input.y;
				float4x1 V = T * 100; //visibility

				float4x4 A = mul(T, transpose(T));

				if (input.perIdIndex == 0) {
					return float4(A[0][0], A[1][0], A[2][0], A[3][0]);
				}
				else if (input.perIdIndex == 1) {
					return float4(A[1][1], A[2][1], A[3][1], A[2][2]);
				}
				else if (input.perIdIndex == 2) {
					return float4(A[3][2], A[3][3], 0, 0);
				}
				else if (input.perIdIndex == 3) {
					return float4(B[0][0], B[1][0], B[2][0], B[3][0]);
				}
				else if (input.perIdIndex == 4) {
					return float4(C[0][0], C[1][0], C[2][0], C[3][0]);
				}
				else if (input.perIdIndex == 5) {
					return float4(V);
				}

				return 0;
			}


			 ENDCG

		}

	}
}

