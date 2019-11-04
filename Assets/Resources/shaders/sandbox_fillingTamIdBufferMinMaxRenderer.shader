Shader "Custom/Sandbox/Filling/TamIdBufferMinMaxRenderer" {
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
        BlendOp Max 

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
		// THIS SHADER TAKES FRAGMENT BUFFER AND RENDERS IT ON MIN MAX TEXTURE
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
				float t : ANY_T;
			};

			g2f make_g2f(float4 pos, float t) {
				g2f o;
				o.pos = pos;
				o.t = t;
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
				float4 pos = float4( retriveXUvFromTamIdFragment( fragment), retriveYUvFromTamIdFragment(fragment), 1, 1);
				pos.xy = (pos.xy * 2) - 1;
				o.pos = pos;
				return o;
			}

			[maxvertexcount(5)]
			void geom(point v2g input[1], uint pid : SV_PrimitiveID, inout PointStream<g2f> outStream)
			{
				TamIdFragment fragment = _FragmentsBuffer[pid];
				uint fragmentId = retriveIdFromTamIdFragment(fragment);
				float fragmentT = retriveTFromTamIdFragment(fragment);
				uint grid_size = _FragmentTexWidth;

				uint2 inGridPosition = uint2(fragmentId % grid_size,  floor(fragmentId / ((float)grid_size))  );

				float2 screenUv = float2(
					(inGridPosition.x) / ((float)grid_size),
					(inGridPosition.y) / ((float)grid_size));
				screenUv += float2(1 / ((float)grid_size), 1 / ((float)grid_size));
				screenUv.y = 1 - screenUv.y - 0;
				screenUv = (screenUv * 2 - 1);

				outStream.Append(make_g2f( float4( screenUv.x, screenUv.y, 0, 1 ), fragmentT));
			}

			float4 frag(g2f input) : SV_Target
			{
				float t = input.t;
				return float4(t, 1 - t,1,1);
			}


			 ENDCG

		}

	}
}

