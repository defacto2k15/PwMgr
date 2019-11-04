Shader "Custom/NPR/Sample/ApparentRelief"
{
	Properties 
	{
		_DummyTexture("DummyTexture", 2D) = "white"{}
		_ApparentReliefMapTexture("ApparentReliefMapTexture", 2D) = "white"{}
	}

CGINCLUDE
			sampler2D _DummyTexture;
			sampler2D _ApparentReliefMapTexture;

			#include "UnityCG.cginc"

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
		Tags { "RenderType"="ApparentRelief"}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.6

			fixed4 frag (v2f i) : SV_Target
			{
				float3 color = 1;

				float2 kx = normalize(float2(i.principalDirection1.w, i.principalDirection2.w));

				color = tex2D(_ApparentReliefMapTexture, (kx+1)/2).rgb;
				//color = float3((kx+1)/2,0);
				return float4(color,1);

				//float3 nn = mul(UNITY_MATRIX_V, i.nrm);
				//nn.b = 0;
				//return float4(nn,1);
			}

			ENDCG
		}
	} 
	FallBack "Diffuse"
}
