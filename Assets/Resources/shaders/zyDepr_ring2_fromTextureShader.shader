Shader "Custom/Ring2FromImageTerrainTexture"
{
	Properties 
	{
		_MainTex("_MainTex", 2D) = "white" {}
		_NormalTex("_NormalTex", 2D) = "white"{}
	}
 
	SubShader 
	{
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Cull Off
        LOD 200
		ZWrite On
		ColorMask 0
			CGPROGRAM

			#pragma surface surf BlinnPhong  fullforwardshadows vertex:vert alpha:fade
			#include "UnityCG.cginc" 

			sampler2D _MainTex;
			sampler2D _NormalTex;

			struct Input {
				float4 pos ; 
				half2 uv_MainTex ;
				float2 flatPos;
			}; 

			void vert (inout appdata_full v, out Input o){
				o.pos = mul( unity_ObjectToWorld, v.vertex);
				o.flatPos = o.pos.xz;
				o.uv_MainTex = v.texcoord.xy;
			} 

			void surf(in Input i, inout SurfaceOutput o) {
				float4 color = tex2D(_MainTex, i.uv_MainTex);
				float3 normal = tex2D(_NormalTex, i.uv_MainTex);
				normal = (normal * 2) - 1;

				o.Albedo = color.rgb;
				o.Alpha = color.a;
				o.Normal = normal;
			}

			ENDCG
	} 
	FallBack "Diffuse"
}
