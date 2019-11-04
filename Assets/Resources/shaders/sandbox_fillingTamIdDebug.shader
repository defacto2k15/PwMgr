Shader "Custom/Sandbox/Filling/TamIdDebug" {
	Properties{
		_TamIdTex("TamIdTex", 2D) = "black"{}
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

			sampler2D _TamIdTex;
			int _FragmentTexWidth;
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
				float4 color = tex2D(_TamIdTex, uv);
				if (color.a > 0) {
					TamIdFragment f = make_TamIdFragment(  
						input.pos.x / _ScreenParams.x
						, input.pos.y / _ScreenParams.y
						, color.x
						, round(color.y * _FragmentTexWidth) + round(color.z * _FragmentTexWidth * _FragmentTexWidth));
					_AppendBuffer.Append(f);

				}
				return color;
			}
			ENDCG
		}
	}
}
