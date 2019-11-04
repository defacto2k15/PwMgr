Shader "Custom/Sandbox/Filling/TamIdCoefficientsSolver" {
	Properties{
		_AggregateTex("AggregateTex", 2D) = "white"{}
		_MinMaxTex("MinMaxTex", 2D) = "white"{}
		_DebugScalar("DebugScalar", Range(0,1024)) = 0
		_DebugScalarX("DebugScalarX", Range(0,1)) = 0
		_DebugScalarY("DebugScalarY", Range(0,1)) = 0

		_DebugBigScalarX("DebugBigScalarX", Range(0,1280)) = 0
		_DebugBigScalarY("DebugBigScalarY", Range(0,256)) = 0

		_FragmentTexWidth("FragmentTexWidth", Range(0,512)) = 0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
		// THIS SHADER TAKES AGGREGATE TEXTURE AND SOLVES THE COEFFICIENTS
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#include "UnityCG.cginc"
			#include "text_printing.hlsl"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : ANY_UV;
			};

			sampler2D _AggregateTex;
			sampler2D _MinMaxTex;
			int _FragmentTexWidth;
			float _DebugScalarX;
			float _DebugScalarY;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				return o;
			}

			float4x1 conjgrad(float4x4 A, float4x1 b) {
				float4x1 x0 = 0;

				float4x1 r = b - mul(A, x0);
				float4x1 w = -r; //this is pk
				float4x1 z = mul(A, w); // this is A*pk
				float a = (mul(transpose(r), w)) / (mul(transpose(w), z));
				float4x1 x = x0 + a * w;

				for (int i = 0; i < 4; i++) {
					r = r - a * z;
					if (length(r[0]) < 0.00001) {
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

#define AGGREGATE_TEX_STRIDE (6)
			float4 frag(v2f input) : SV_Target
			{
				float4 color = 0;
				float2 uv =  input.projPos.xy;
				float2 orgUv =  input.projPos.xy;

				int gridWidth = _FragmentTexWidth;

				uint2 inScreenCoords = uint2(floor(uv.x * _ScreenParams.x), floor(uv.y*_ScreenParams.y));
				//inScreenCoords = 0;
				//inScreenCoords = uint2(inScreenCoords.x % (gridWidth*AGGREGATE_TEX_STRIDE), inScreenCoords.y%gridWidth);
				uv = float2(inScreenCoords.x / ((float)gridWidth*AGGREGATE_TEX_STRIDE), inScreenCoords.y / ((float)gridWidth));
				uv += float2(1 / ((float)gridWidth*AGGREGATE_TEX_STRIDE), 1 / (float)gridWidth)/2.0;
				return tex2D(_AggregateTex, uv);

				uint id = floor(uv.x * gridWidth) + floor(uv.y * gridWidth)*gridWidth;
				id = 0;

				float4 e[AGGREGATE_TEX_STRIDE];

				uint2 gridCoords = uint2(id % gridWidth, floor(id / ((float)gridWidth)));

				for (int i = 0; i < AGGREGATE_TEX_STRIDE; i++) {
					float2 nuv = float2(((float)gridCoords.x*AGGREGATE_TEX_STRIDE + i) / (gridWidth*AGGREGATE_TEX_STRIDE), 0);
					nuv += float2(1 / ((float)gridWidth*AGGREGATE_TEX_STRIDE), 1 / (float)gridWidth)/2.0;
					e[i] = tex2D(_AggregateTex, nuv);
				}

				float2 muv = float2(((float)gridCoords.x) / (gridWidth), ((float)gridCoords.y) / (gridWidth));
				muv += float2(1 / ((float)gridWidth), 1 / (float)gridWidth)/2.0;
				float4 mm = tex2D(_MinMaxTex, muv);
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
				float approxPointCount = 100;
				float4x4 D;
				for (int i = 0; i < 4; i++) {
					for (int j = 0; j < 4; j++) {
						D[i][j] = approxPointCount * (pow(maxT, i + j+1) - pow(minT, i + j+1)) / (i + j+1);
					}
				}
				float4x1 cv = conjgrad( T + (D), V);

				color = boxedPrintVector(cv, orgUv, float4(0, 0, 1, 1));

				//float4 xx = tex2D(_MinMaxTex, );
				//color = boxedPrintVector(xx, orgUv, float4(0, 0, 1, 1));
				return color;  
			}


			 ENDCG

		}

	}
}

