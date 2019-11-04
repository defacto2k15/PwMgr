Shader "Custom/Terrain/Ring1DirectHeight"
{
	Properties 
	{
	}
	SubShader 
	{
		Cull Off ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma  surface surf Standard noshadow nolightmap noambient nodynlightmap vertex:disp
			#pragma target 4.6

			struct Input{
				float2 uv_HeightmapTex : TEXCOORD0;
			};

			struct appdata {
				float4 vertex : POSITION;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
			};

			//Our Vertex Shader 
			void disp (inout appdata v){
			}
    
			void surf (Input IN, inout SurfaceOutputStandard o)  
			{
				o.Albedo = float3(1,0,0);
				o.Alpha = 1; 
			}

			ENDCG
	} 
	FallBack "Diffuse"
}
