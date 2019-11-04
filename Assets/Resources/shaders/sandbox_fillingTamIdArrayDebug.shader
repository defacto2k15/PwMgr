Shader "Custom/Sandbox/Filling/TamIdArrayDebug" {
	Properties{
		_TamIdTexArray("TamIdTexArray", 2DArray) = "black"{}
		_FragmentTexWidth("FragmentTexWidth", Range(0,512)) = 0

		_ArrayElementSelector("ArrayElementSelector", Range(0,16)) = 0
		_ArrayLodSelector("ArrayLodSelector", Range(0,16)) = 0
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
			#pragma target 5.0
			
			#include "UnityCG.cginc"
#include "tamIss_common.txt" 

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;

			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 worldSpacePos : ANY_WORLD_SPACE_POS;
				float2 uv : ANY_UV;
			};

			UNITY_DECLARE_TEX2DARRAY(_TamIdTexArray);
			int _FragmentTexWidth;
			float _ArrayElementSelector;
			float _ArrayLodSelector;
			AppendStructuredBuffer<TamIdFragment> _AppendBuffer;

			v2f vert(appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.worldSpacePos = in_v.vertex;
				o.uv = in_v.uv;  

				return o; 
			}

			float4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;
				float4 color = UNITY_SAMPLE_TEX2DARRAY_LOD(_TamIdTexArray, float3(uv, _ArrayElementSelector), _ArrayLodSelector);
				color = UNITY_SAMPLE_TEX2DARRAY(_TamIdTexArray, float3(uv, _ArrayElementSelector));
				bool ex = false;
				if (color.a > 0) {
					int retrivedId = round(color.r * 255)+round(color.g * 255 * 255);
					TamIdFragment f = make_TamIdFragment(
						  input.pos.x / _ScreenParams.x
						, input.pos.y / _ScreenParams.y
						, color.b
						, retrivedId);
					_AppendBuffer.Append(f);

				}
				//if (color.x > 0.99) {
				//	color = float4(1, 0, 0, 0);
				//}
				//color = color.b;
				return color;
			}
			ENDCG
		}
	}
}
