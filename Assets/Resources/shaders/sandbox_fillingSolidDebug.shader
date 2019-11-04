Shader "Custom/Sandbox/Filling/SolidDebug" {
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


#include "KeijiroShaders/ClassicNoise3D.hlsl"
#include "noise.hlsl"

GEN_fractalNoise3D( cloudNoise, 5, snoise3D, -1, 1)


float4 sampleWithWrapMargins(float3 coords, float repeatScale, float marginSize, float intraScale, float offset) {
	coords = mymod(coords*repeatScale, 1);

	float standardSample = cloudNoise(coords*intraScale+offset);
	float3 topSamples = float3(
		cloudNoise(float3(-(1-coords.x), coords.y, coords.z)*intraScale+offset),
		cloudNoise(float3(coords.x, -(1-coords.y), coords.z)*intraScale+offset),
		cloudNoise(float3(coords.x, coords.y, -(1-coords.z))*intraScale+offset));

	float margin = marginSize;
	float3 weights = float3(
		invLerp(1 - margin, 1, coords.x),
		invLerp(1 - margin, 1, coords.y),
		invLerp(1 - margin, 1, coords.z));

	float standardWeight = saturate(1 - weights.x - weights.y - weights.z);
	return (standardSample*standardWeight
		+ topSamples.x * weights.x
		+ topSamples.y * weights.y
		+ topSamples.z * weights.z) / (standardWeight + weights.x + weights.y + weights.z);
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

				color = o1 + o2 + o3 + o4;
				//if (color.r > 0.5) {
				//	color = 1;
				//}
				//else {
				//	color = 0;
				//}


				return color;
			}
			ENDCG
		}
	}
}
