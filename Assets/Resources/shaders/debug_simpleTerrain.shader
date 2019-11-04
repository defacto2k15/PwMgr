Shader "Custom/Debug/SimpleTerrain"
{
	Properties 
	{
		_HeightmapTex ("HeightmapTex", 2D) = "white" {}
		_HeightmapTexWidth ("HeightmapTexWidth", Range(0,241)) = 241
		
	}
	SubShader 
	{
		Cull Off ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma  surface surf Standard noshadow nolightmap noambient nodynlightmap vertex:disp 
			#pragma target 4.6

			#include "HeightColorTransform.hlsl"

			sampler2D _HeightmapTex;

			struct Input{
				float2 uv_HeightmapTex : TEXCOORD0;
				float3 worldPos;
			};

			struct appdata {
				float4 vertex : POSITION;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
			};


			//Our Vertex Shader 
			void disp (inout appdata v){
				float heightmapValue = decodeHeight(tex2Dlod(_HeightmapTex, float4(v.texcoord,0,0)));
				v.vertex.y = heightmapValue;
			}

			float3 decodeNormal( float3 input){
				return input*2-1;
			}

			float3 encodeNormal( float3 input ){
				return (input+1)/2;
			}

			float3 ComputeSimpleNormal( float2 pos, float intensity, float normalStrength){
				float3 dx = ddx( float3(pos.x, intensity, pos.y));
				float3 dy = ddy( float3(pos.x, intensity, pos.y));

				float3 lenX = length( dx.xz);
				float3 lenY = length( dy.xz);

				float sdx = dx.y / lenX;
				float sdy = dy.y / lenY;

				sdx *= (0.06 * normalStrength);
				sdy *= (0.06 * normalStrength);

				return normalize( float3(sdx, sdy, 1.0));
			}

    
			void surf (in Input IN, inout SurfaceOutputStandard o)  
			{
				float heightmapTexWidth = 241;
				float2 heightUv = IN.uv_HeightmapTex;
				heightUv *= (heightmapTexWidth - 1) / heightmapTexWidth;
				heightUv += (1 / heightmapTexWidth) / 2;

				float heightmapValue = decodeHeight(tex2Dlod(_HeightmapTex, float4(heightUv,0,0)));
				
				o.Albedo = float3(0.3, 0.5, 0.7);
				o.Normal = ComputeSimpleNormal( IN.worldPos.xz, IN.worldPos.y, 4);
				o.Alpha = 1; 
			}

			ENDCG
	} 
	FallBack "Diffuse"
}
