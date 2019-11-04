Shader "Custom/Presentation/SolidColors" {
	Properties{
		_SolidTex("SolidTex", 3D) = "blue" {}
		_Scale("Scale", Range(0,10)) = 1
			_Offset("Offset",Range(0,1)) = 0
			_RepeatScale("RepeatScale",Range(0,10)) = 1
			_MarginSize("MarginSize",Range(0,1)) = 0.1
			_IntraScale("IntraScale",Range(0,10)) = 1
		_DebugTex("DebugTex", 2D) = "blue" {}
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
			};

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.objectSpacePos = in_v.vertex.xyz;

				return o;
			}


			sampler3D _SolidTex;
			sampler2D _DebugTex;
			float _Scale;

	float _RepeatScale;
	float _MarginSize;
	float _IntraScale;
	float _Offset;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = 0;
				float3 coord = i.objectSpacePos * _Scale;

				float z_cam = length(UnityObjectToViewPos(i.objectSpacePos));

				float z = log2(z_cam);
				float s = z - floor(z);

				float frag_scale = pow(2, floor(z));
				float a1 = s / 2.0;
				float a2 = 1 / 2.0 - s / 6.0;
				float a3 = 1 / 3.0 - s / 6.0;
				float a4 = 1 / 6.0 - s / 6.0;

				float4 o1 = a1 * tex3D(_SolidTex, coord / frag_scale);
				float4 o2 = a2 * tex3D(_SolidTex, coord*2 / frag_scale);
				float4 o3 = a3 * tex3D(_SolidTex, coord*4 / frag_scale);
				float4 o4 = a4 * tex3D(_SolidTex, coord*8 / frag_scale);


				float r = (floor(z) % 2) / 2.0;
				float g = (2 + floor(z) % 4) / 4.0;
				float b = (4 + floor(z) % 8) / 8.0;

				color.xyz = float3(r, g, b);

				return color;
			}
			ENDCG
		}
	}
}
