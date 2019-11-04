Shader "Custom/Sandbox/Filling/KimObj1" {
	Properties{
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
						

			struct MRTFragmentOutput
			{
				float4 dest0 : SV_Target0;
				float4 dest1 : SV_Target1;
				float4 dest2 : SV_Target2;
				float4 dest3 : SV_Target3;
			};

			MRTFragmentOutput make_MRTFragmentOutput(float4 dest0, float4 dest1, float4 dest2, float4 dest3) {
				MRTFragmentOutput o;
				o.dest0 = dest0;
				o.dest1 = dest1;
				o.dest2 = dest2;
				o.dest3 = dest3;
				return o;
			}

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
				float4 tangent : TANGENT;

			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 worldSpacePos : ANY_WORLD_SPACE_POS;
				float3 norm : ANY_NORM;
				float3 tangent : ANY_TANGENT;
			};

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.norm = in_v.norm;
				o.worldSpacePos = in_v.vertex; 
				o.tangent =(in_v.tangent.xyz);// normalize(mul(unity_ObjectToWorld, in_v.tangent));

				return o;
			}

			MRTFragmentOutput frag (v2f input) : SV_Target
			{
				fixed4 color = fixed4(1,0,0,1);
				float4 normal = float4(normalize(input.norm.xyz), 0);
				float4 tangent = float4(normalize(input.tangent.xyz ),-1);

				float3 aa = normalize(cross(tangent.xyz, normal.xyz));
				float4 tangent2 = -float4(normalize(cross(aa, normal.xyz)),1);

				return make_MRTFragmentOutput(color, input.worldSpacePos, normal, tangent2);
			}
			ENDCG
		}
	}
}
