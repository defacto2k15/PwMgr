Shader "Custom/Presentation/SzecsiManualShow" {
	Properties{
		_UniformCycle("UniformCycle", Int) = 0
		_UniformCycleX("UniformCycleX", Int) = 0
		_UniformCycleY("UniformCycleY", Int) = 0
		_FSeedDensity("FSeedDensity",Range(0,1000)) = 1
		_MParam("MParam", Range(-8,2)) = 1

		_ScaleChangeRate("ScaleChangeRate", Range(0,10)) = 1
		_DetailFadeRate("DetailFadeRate", Range(0,10)) = 1

		_HatchTex("HatchTex", 2D) = "pink"{}
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
			#include "UnityCG.cginc"
						
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 objectSpacePos : ANY_OBJECT_SPACE_POS;
				float3 worldSpacePos : ANY_WORLD_SPACE_POS;
				float2 uv : ANY_UV;
			};

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.objectSpacePos = in_v.vertex.xyz;
				o.uv = in_v.uv;
				o.worldSpacePos = mul(unity_ObjectToWorld, in_v.vertex); 

				return o;
			}

			float _FSeedDensity;
			uint _UniformCycle;
			uint _UniformCycleX;
			uint _UniformCycleY;
			float _MParam;
			float _ScaleChangeRate;
			float _DetailFadeRate;

			sampler2D _HatchTex;

			Buffer<int> _UniformCyclesBuf;

			float2x2 createRotationMatrix(float fi) {
				float sinX = sin (fi);
				float cosX = cos (fi);
				float2x2 rotationMatrix = float2x2( cosX, -sinX, sinX, cosX);
				return rotationMatrix;
			}

			float2 waveNotation(float2 a) {
				//return float2(frac(a.x + 0.5) - 0.5, frac(a.y + 0.5) - 0.5);
				return float2(frac(a.x + 0.5) , frac(a.y + 0.5) );
				//return a;
			}

			float2 elementwiseDivision(float2 a, float2 b) {
				return float2(a.x / b.x, a.y / b.y);
			}

			float myMod1_f(float x) {
				float a = frac(x);
				if (a < 0) {
					return -1;
				}
				else {
					return a;
				}
			}

			float2 myMod1_f2(float2 x) {
				return float2(myMod1_f(x.x), myMod1_f(x.y));
			}


			fixed4 frag (v2f input) : SV_Target
			{
				fixed4 color = 1;

				float2 uv = input.uv;

				uint N = 16;
				int cycleLength = 32;
				float max = pow(2, 32);

				float F = _FSeedDensity; //global seed density
				float2 strokeSize = 1; //stroke size;
				float2x2 rotationMatrix = createRotationMatrix(0);
				float aTone = 1; //tone from illumiation

				float2 hDirection = normalize(float2(0, 1));
				float hAngle = atan2(hDirection.x, hDirection.y);

				float2 uv_dd = float2(length(ddx(input.uv)), length(ddy(input.uv)));
				float T = abs(cos(hAngle)) * uv_dd.x + abs(sin(hAngle))*uv_dd.y;

				float2 pos_dd = float2(length(ddx(input.worldSpacePos)), length(ddy(input.worldSpacePos)));
				float G = abs(cos(hAngle)) * pos_dd.x + abs(sin(hAngle))*pos_dd.y;

				//detail factor
				float logInside = G * F  * T;
				logInside = pow(logInside, _ScaleChangeRate);
				float M = log2(logInside); //there used to be - in front
				M = _MParam;
				//M = -round(2);

				float m = M - floor(M); //m == 0 w oddali od granicy. m == 1 przy granicy między poziomami

				float2 s_uv = uv * pow(2, -floor(M)); //seed space
				float2 cUv = floor(s_uv * round(4));
				if ((cUv.x + cUv.y) % 2 == 0) {
					color = float4(0.5,1,0.2,0);
				}
				else {
					color = 0.5;
				}
				
				if (s_uv.x < 0.05 && s_uv.y < 0.05) {
					color = 0;
				}


				//uint cycle = asuint(_UniformCycle);
				uint2 cycles = uint2 (asuint(_UniformCycleX), asuint(_UniformCycleY));
				//uint2 cycles = uint2 (asuint(_UniformCyclesBuf[1]), asuint(_UniformCyclesBuf[2]));
				uint topMask = 4294901760; // 0x‭FFFF0000‬;


				for (int i = 0; i < 16; i++) { // for each seed

					float2 seedPos = 0;

					seedPos.x = cycles.x / max;
					seedPos.y = cycles.y / max;

					float alpha = 1;

					int2 w = floor( ((s_uv) - seedPos) + float2(0.5, 0.5));

					bool seedIsInSparselevelToo = ((w.x % 2) == cycles.x % 2) && (cycles.y % 2 == (w.y % 2));

					if (!seedIsInSparselevelToo) {
						alpha = saturate((1 - m)*_DetailFadeRate);
					}

					float dotPaintingRadius = 0.03 *pow(2, m);
					if (length( seedPos - myMod1_f2(s_uv)) < dotPaintingRadius) {
						if (seedIsInSparselevelToo) {
							return float4(1, 0, 0, 1);
						}
						else {
							return  lerp(color, float4(0, 0, 1, 0), alpha);
						}
					}

					cycles.x = ((cycles.x << 1) | (cycles.x >> (cycleLength - 1)));
					cycles.y = ((cycles.y << 1) | (cycles.y >> (cycleLength - 1)));
				}


				return color;
			}
			ENDCG
		}
	}
}
