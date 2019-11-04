Shader "Custom/NPR/Sample/ApparentReliefExtractor"
{
	Properties 
	{
		_DummyTexture("DummyTexture", 2D) = "white"{}
	}

CGINCLUDE
			sampler2D _DummyTexture;

			#include "UnityCG.cginc"
			#include "common.txt"

			struct appdata
			{
				float4 pos : POSITION;
				float3 nrm: NORMAL;
				float4 principalDirection1 : TEXCOORD1;
				float4 principalDirection2 : TEXCOORD2;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 nrm : NORMAL;
				float4 principalDirection1 : TEXCOORD1;
				float4 principalDirection2 : TEXCOORD2;
			};

			v2f vert (appdata i)
			{
				v2f o;
				o.principalDirection1 = float4(i.principalDirection1.xyz * sign(i.principalDirection1.x), i.principalDirection1.w);
				o.principalDirection2 = float4(i.principalDirection2.xyz * sign(i.principalDirection2.x), i.principalDirection2.w);
				o.pos =  UnityObjectToClipPos(i.pos);
				o.nrm = UnityObjectToWorldNormal(i.nrm); // world space normal
				return o;
			}

ENDCG
	SubShader 
	{
		Tags{ "RenderType"="Opaque"}
		Pass
		{
			Color(0,0,0,0)
		}
	} 

	SubShader 
	{
		Tags{  "RenderType"="ApparentRelief" }
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag  
			#pragma target 4.6

			fixed4 frag (v2f i) : SV_Target
			{
				float3 color = 1;

				float k1 = i.principalDirection1.w;
				float k2 = i.principalDirection2.w;

				float2 vSpaceNorm = mul(UNITY_MATRIX_V, i.nrm).rg;
				vSpaceNorm = remap_2f(vSpaceNorm);

				return float4( remap_2f(normalize(float2(k1,k2))),vSpaceNorm);
				//return float4(nn,1);
			}

			ENDCG
		}
	} 
	FallBack "Diffuse"
}
