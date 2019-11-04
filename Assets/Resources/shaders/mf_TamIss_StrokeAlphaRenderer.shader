Shader "Custom/Measurements/Filling/TamIssStrokeAlphaRenderer" {
	Properties{
		_StrokeTex("StrokeTex", 2D) = "blue"{}
		_FragmentsTex("FragmentsTex", 2D) = "blue"{}
		_AggregateTex("AggregateTex", 2D) = "white"{}
		_MinMaxTex("MinMaxTex", 2D) = "white"{}
		_DebugScalar("DebugScalar", Range(0,10000)) = 1
		_FragmentTexWidth("FragmentTexWidth", Range(0,512)) = 0
		_DebugStrokeAlpha("DebugStrokeAlpha",Range(0,1)) = 1.0
		_TLengthFilterTreshold("TLengthFilterTreshold",Range(0,1)) = 0.5
		_StrokeWidth("StrokeWidth",Range(0,0.1)) = 0.02
	}

	SubShader
	{

		Tags {"Queue"="Transparent"  "RenderType" = "Transparent" }
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
		// THIS SHADER TAKES FRAGMENT BUFFER AND RENDERS IT ON MIN MAX TEXTURE
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma target 5.0
			#include "UnityCG.cginc"
#include "filling_common.txt"

			struct v2g {
				float4 pos : SV_POSITION;
				float4x1 cx : ANY_CX;
				float4x1 cy : ANY_CY;
				float4x1 cv : ANY_CV;
				float2 minMax : ANY_MINMAX;
				uint id : ANY_ID;
			};

			struct g2f {
				float4 pos : SV_POSITION;
				float2 uv : ANY_UV;
				float4x1 cv : ANY_CV;
				uint id : ANY_ID;
			};

			g2f make_g2f(float4 pos, float2 uv, float4x1 cv, uint id) {
				g2f o;
				o.pos = pos;
				o.uv = uv;
				o.cv = cv;
				o.id = id;
				return o;
			}

			sampler2D _StrokeTex;
			Texture2D<float4> _FragmentsTex;
			Texture2D<float4> _WorldPositionTex;
			Texture2D<float4> _AggregateTex;
			Texture2D<float4> _MinMaxTex;
			float _DebugScalar;
			int _FragmentTexWidth;
			float _DebugStrokeAlpha;
			float _TLengthFilterTreshold;
			float _StrokeWidth;


			float4x1 conjgrad(float4x4 A, float4x1 b) {
				float4x1 x0 = 0;

				float4x1 r = b - mul(A, x0);
				float4x1 w = -r; //this is pk
				float4x1 z = mul(A, w); // this is A*pk
				float a = (mul(transpose(r), w)) / (mul(transpose(w), z));
				float4x1 x = x0 + a * w;

				for (int i = 0; i < 4; i++) {
					r = r - a * z;
					if (length(r[0]) < 0.01) {
						break;
					}
					float B = mul(transpose(r), z) / mul(transpose(w), z);
					w = -r + B * w;
					z = mul(A, w);
					a = mul(transpose(r), w) / mul(transpose(w), z);
					x = x + a * w;
				}
				return x;
			}

			v2g vert(uint id : SV_VertexID) 
			{
				uint gridWidth = _FragmentTexWidth;
				uint aggregateTexStride = 6;
				uint lineWidth = gridWidth * aggregateTexStride;

				float4 e[6];
				uint2 gridCoords = uint2( id % gridWidth, floor( id / ((float)gridWidth))) ;

				for (int i = 0; i < aggregateTexStride; i++) {
					e[i] = _AggregateTex[int2(gridCoords.x*aggregateTexStride + i, gridCoords.y)];
				}

				float4 mm = _MinMaxTex[gridCoords];

				float maxT = mm.x;
				float minT = 1-mm.y;

				float4x1 CX = {
					e[3][0], e[3][1], e[3][2], e[3][3]
				};
				float4x1 CY = {
					e[4][0], e[4][1], e[4][2], e[4][3]
				};
				float4x4 T = {
					e[0][0], e[0][1], e[0][2], e[0][3],
					e[0][1], e[1][0], e[1][1], e[1][2],
					e[0][2], e[1][1], e[1][3], e[2][0],
					e[0][3], e[1][2], e[2][0], e[2][1]
				};

				float4x1 cx = conjgrad(T, CX);
				float4x1 cy = conjgrad(T, CY);

				float4x1 V = {
					e[5][0], e[5][1], e[5][2], e[5][3]
				};
				float approxPointCount = T[0][0];
				float4x4 D;
				for (int i = 0; i < 4; i++) {
					for (int j = 0; j < 4; j++) {
						D[i][j] = approxPointCount * (pow(maxT, i + j+1) - pow(minT, i + j+1)) / (i + j+1);
					}
				}
				float4x1 cv = conjgrad(T+ D, V);

				v2g o;
				o.pos = 0.5;
				o.cx = cx;
				o.cy = cy;
				o.cv = cv;
				o.minMax = float2(minT, maxT);
				o.id = id;
				return o;
			}

			float cubic(float4x1 c, float t) {
				return c[0][0] + c[1][0] * t + c[2][0] * t*t + c[3][0] * t*t*t;
			}

#define SEGMENT_COUNT 7

			[maxvertexcount(SEGMENT_COUNT*3*3)]
			void geom(point v2g input[1], uint pid : SV_PrimitiveID, inout TriangleStream<g2f> outStream)
			{
				float minMaxDistanceTreshold = _TLengthFilterTreshold;
				float segmentWidth = _StrokeWidth;

					v2g  thisInput = input[0];

					if (thisInput.minMax[1] - thisInput.minMax[0] < minMaxDistanceTreshold) {
						return;
					}

					float2 centerPoints[SEGMENT_COUNT];

					for (int i = 0; i < SEGMENT_COUNT; i++) {
						float t = thisInput.minMax[0] + (((float)i) / (SEGMENT_COUNT - 1))*(thisInput.minMax[1] - thisInput.minMax[0]);
						float2 pos = float2(
							cubic(thisInput.cx, t),
							cubic(thisInput.cy, t));
						pos.xy = (pos.xy * 2) - 1;
						pos.y *= -1;
						//pos.xy /= 10;

						centerPoints[i] = pos;
					}

					float2 segVectors[SEGMENT_COUNT - 1];
					float distances[SEGMENT_COUNT - 1];
					for (int i = 0; i < SEGMENT_COUNT - 1; i++) {
						float2 delta = centerPoints[i + 1] - centerPoints[i];
						distances[i] = length(delta);
						segVectors[i] = normalize(delta);
					}

					float2 perpVectors[SEGMENT_COUNT];
					for (int i = 0; i < SEGMENT_COUNT; i++) {
						float2 currentSegVec = segVectors[min(max(0, i), SEGMENT_COUNT - 2)];
						float2 prevSegVec = segVectors[min(max(0, i - 1), SEGMENT_COUNT - 2)];
						float2 averageVec = normalize(currentSegVec + prevSegVec);
						float2 perpVec = float2(averageVec.y, -averageVec.x);

						perpVectors[i] = perpVec;
					}

					float2 newPoints[SEGMENT_COUNT * 2];
					for (int i = 0; i < SEGMENT_COUNT; i++) {
						newPoints[i * 2 + 0] = centerPoints[i] + perpVectors[i] * segmentWidth;
						newPoints[i * 2 + 1] = centerPoints[i] - perpVectors[i] * segmentWidth;
					}

					for (int i = 0; i < SEGMENT_COUNT; i++) {
						//outStream.Append(make_g2f(float4(centerPoints[i], 1, 1), 0));
						float t = thisInput.minMax[0] + (((float)i) / (SEGMENT_COUNT - 1))*(thisInput.minMax[1] - thisInput.minMax[0]);

						outStream.Append(make_g2f(float4(newPoints[i * 2 + 1], 1, 1), float2(t, 0), thisInput.cv, thisInput.id));
						outStream.Append(make_g2f(float4(newPoints[i * 2 + 0], 1, 1), float2(t, 1), thisInput.cv, thisInput.id));
					}
					outStream.RestartStrip();
			}

#include "filling_common.txt"
#include "mf_common.txt"

			mf_MRTFragmentOutput frag(g2f input) : SV_Target
			{
				float t = input.uv.x;
				float v = cubic(input.cv, t);
				float visibility = step(_DebugScalar,cubic(input.cv, t));
				uint id = input.id;

				//return float4(debugIntToColor(id % (6 * 6 * 6)),v/100.0);

				float lower = (id % 256) / 255.0;
				float higher = round(((float)(id - lower)) / 256.0) / 255.0;
				//return float4( v/100.0,lower, higher, 1);

				float tParam = t;

				float4 strokePixel = tex2D(_StrokeTex, input.uv);
				float blendingHatchStrength =  strokePixel.a;
				float3 worldSpacePos = _WorldPositionTex[input.pos.xy] ;
				float4 fragmentPixel = _FragmentsTex[input.pos.xy];
				float lightIntensity = fragmentPixel.r;

				if (fragmentPixel.a <= 0) {
					discard;
				}

				mf_retrivedHatchPixel pixel = make_mf_retrivedHatchPixel(id, tParam, true, blendingHatchStrength, 0);

				mf_MRTFragmentOutput output = retrivedPixelToFragmentOutputAlphaArtistic(pixel, worldSpacePos, lightIntensity);
				//output.dest0.rg = input.uv;
				return output;
			}


			 ENDCG

		}

	}
}

