Shader "Custom/Sandbox/Filling/MMSeedPlacerDebug" {
	Properties{
		_UniformCycleX("UniformCycleX", Int) = 0
		_UniformCycleY("UniformCycleY", Int) = 0
		_FSeedDensity("FSeedDensity",Range(0,1000)) = 514
		_DebugScalar("DebugScalar", Range(0,10)) = 1

		_ScaleChangeRate("ScaleChangeRate", Range(0,10)) = 0.6
		_DetailFadeRate("DetailFadeRate", Range(0,10)) = 4.7
		_StrokeSize("StrokeSize", Range(0,10)) = 4.6
		_Rotation("Rotation", Range(0,10)) = 0

		_HatchTex("HatchTex", 2D) = "pink"{}
		_SeedPositionTex("SeedPositionTex", 2D) = "" {}
		_SeedPositionTex3D("SeedPositionTex3D", 3D) = "" {}
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
				float3 norm : ANY_NORM;

				float4 projPos : ANY_PROJ_POS;
			};

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.objectSpacePos = in_v.vertex.xyz;
				o.uv = in_v.uv;
				o.worldSpacePos = mul(unity_ObjectToWorld, in_v.vertex); 
				o.projPos = ComputeScreenPos (o.pos);
				o.norm = UnityObjectToWorldNormal(in_v.norm);

				return o;
			}

			float _FSeedDensity;
			uint _UniformCycleX;
			uint _UniformCycleY;
			float _DebugScalar;
			float _ScaleChangeRate;
			float _DetailFadeRate;
			float _StrokeSize;
			float _Rotation;

			float4x4 _CameraInverseProjection;

			sampler2D _HatchTex;
			Texture2D<float4> _SeedPositionTex;
			Texture3D<float4> _SeedPositionTex3D;

			Buffer<int> _UniformCyclesBuf;

			float2x2 createRotationMatrix(float fi) {
				float sinX = sin (fi);
				float cosX = cos (fi);
				float2x2 rotationMatrix = float2x2( cosX, -sinX, sinX, cosX);
				return rotationMatrix;
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

			float3 myMod1_f3(float3 x) {
				return float3(myMod1_f(x.x), myMod1_f(x.y), myMod1_f(x.z));
			}

			float4 debugSurfaceColor3d(float3 s_uv) {

				s_uv += floor(s_uv)*-1;
				float4 color = 0;

				if (min( fmod(s_uv.z/2,1), min(fmod(s_uv.x/2, 1), fmod(s_uv.y/2, 1))) < 0.01) {
					color -= float4(1, 1, 0, 1);
				}
				if (min( min(fmod(s_uv.x, 1), fmod(s_uv.y, 1)), fmod(s_uv.z, 1)) < 0.01) {
					color -= float4(0, 1, 0, 1);
				}

				int3 cUv = floor(s_uv * 4)%4;
				if ((cUv.x + cUv.y + cUv.z) % 2 == 0) {
					color += 1;
				}
				else {
					color += 0.5;
				}
				return color;
			}


			struct ClosestSeedSpecification {
				float3 position;
				bool seedIsInSparseLevelToo;
				bool isActive;
			};

			ClosestSeedSpecification make_ClosestSeedSpecification( float3 position, bool seedIsInSparseLevelToo, bool isActive ){
				ClosestSeedSpecification s;
				s.position = position;
				s.seedIsInSparseLevelToo = seedIsInSparseLevelToo;
				s.isActive = isActive;
				return s;
			}

			ClosestSeedSpecification retriveClosestSeed(float3 s_uv) { // s_uv - nonRepeatable, 0-1 in 4x4
				int3 bigBlockCoords = floor(s_uv); //  nonRepeatable, 0-1 in 4x4
				float3 positive_s_uv = myMod1_f3(s_uv);

				float3 inRepBlockUv = myMod1_f3(s_uv / 2); // repeatable 0-1 in 8x8
				uint3 cellCoords = floor(positive_s_uv * 4) % 4; // repetable, 0-3 in 4x4
				float3 inCellUv = frac(positive_s_uv * 4); // repeatable, 0-1 in 1x1

				int3 downBottomLeftCellOffset = floor( inCellUv - 0.5);
				//downBottomLeftCellOffset = 0;

				ClosestSeedSpecification closestSeed = make_ClosestSeedSpecification(0,0,false);
				for (int x = 0; x < 2; x++) {
					for (int y = 0; y < 2; y++) {
						for (int z = 0; z < 2; z++) {
							int3 baseBlockCoords = cellCoords + downBottomLeftCellOffset + uint3(x, y, z);

							uint3 seed2BlockCoords = (baseBlockCoords+4) % 4;
							float4 seedPosSample = _SeedPositionTex3D[seed2BlockCoords];

							float3 seed2Offset = seedPosSample.xyz;
							float3 seed2Position = seed2Offset / 4.0 +  baseBlockCoords / 4.0 + bigBlockCoords;

							float cycleFloat = seedPosSample.w;
							int cycleInt = round(cycleFloat *  7.0);
							uint3 cycleLastBits = 0;
							cycleLastBits[2] = floor( (cycleInt%8)/4.0);
							cycleLastBits[1] = floor( (cycleInt%4)/2.0);
							cycleLastBits[0] = floor( (cycleInt%2)/1.0);

							float3 positive_seed2Position = myMod1_f3(seed2Position/2)*2; // repeatable 0-2 values in 8x8
							int3 w2 = ( floor(positive_seed2Position)) % 2;
							bool seedIsInSparseLevelToo =
									(w2.x == cycleLastBits.x % 2) && (w2.y == cycleLastBits.y % 2) && (w2.z == cycleLastBits.z % 2);

							if ( !closestSeed.isActive || ( length(s_uv - seed2Position) < length(s_uv - closestSeed.position) )) {
								closestSeed = make_ClosestSeedSpecification(seed2Position, seedIsInSparseLevelToo, true);
							}
						}
					}
				}
				return closestSeed;
			}

			// TEST in 3D
			fixed4 frag (v2f input) : SV_Target
			{
				fixed4 color = 0;

				float3 uv = input.worldSpacePos;
				uv /= 2;

				float dotPaintingRadius = 0.01 * 1;

				float3 s_uv = uv * pow(2, round(_DebugScalar));

				int3 bigBlockCoords = floor(s_uv); //  nonRepeatable, 0-1 in 4x4

				float3 inRepBlockUv = myMod1_f3(s_uv / 2); // repeatable 0-1 in 8x8
				uint3 cellCoords = floor(myMod1_f3(s_uv) * 4) % 4; // repetable, 0-3 in 4x4
				float3 inCellUv = frac(s_uv * 4); // repeatable, 0-1 in 1x1
				//return float4(cellCoords, 0);

				color += debugSurfaceColor3d(s_uv);

				ClosestSeedSpecification closestSeed = retriveClosestSeed((s_uv));

				if (closestSeed.isActive && length(closestSeed.position-(s_uv)) < dotPaintingRadius * 8) {
					if (closestSeed.seedIsInSparseLevelToo) {
						color = float4(1, 1, 0.5, 1);
					}
					else {
						color = float4(0, 1, 0.5, 1);
					}
				}
				
				return color;
			}

			ENDCG
		}
	}
}
