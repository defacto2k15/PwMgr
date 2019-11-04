Shader "Custom/Terrain/TestTerrainDirectPlain"
{
	Properties 
	{
		_HeightmapTex ("HeightmapTex", 2D) = "white" {}
		_HeightmapUv ("HeightmapUv", Vector) = (0.0, 0.0, 1.0, 1.0)
		_NormalmapTex ("NormalmapTex", 2D) = "white" {}
		_NormalmapUv ("NormalmapUv", Vector) = (0.0, 0.0, 1.0, 1.0)

		_DebugScalar("DebugScalar", Range(-10,10))=1
		_LodLevel("LodLevel", Range(-10,10))=1
		_HeightmapLodOffset("HeightmapLodOffset", Range(-10,10))=0
	}
	SubShader 
	{
		Cull Off ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma  surface surf Standard noshadow nolightmap noambient nodynlightmap vertex:disp tessellate:tessFixed
			#pragma target 4.6
			#include "common.txt"
			#include "UnityCG.cginc" 
			#include "noise.hlsl"

			sampler2D _HeightmapTex;
			float4 _HeightmapUv;
			sampler2D _NormalmapTex;
			float4 _NormalmapUv;

			float _DebugScalar;
			float _HeightmapLodOffset;

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


			float4 tessFixed(appdata v0, appdata v1, appdata v2){
				return 1;
			}

			float2 backToNormalizedUV(float2 uv, float2 textureSize){
				return uv / textureSize;
			}


			float2 getHeightmapTextureValues( float2 uvPos){ //position in object
				float2 ring1Texture = uvPos;
				float heightmapValue = tex2Dlod(_HeightmapTex, float4( ring1Texture,0,5)).r;
				return float2(heightmapValue, heightmapValue);
			}

			//Our Vertex Shader 
			void disp (inout appdata v){
				float2 baseUv = v.texcoord.xy;
				float2 heightUv = v.texcoord.xy;

				heightUv.x *= _HeightmapUv[2];
				heightUv.y *= _HeightmapUv[3];

				heightUv.x += _HeightmapUv[0];
				heightUv.y += _HeightmapUv[1];

				float mipMapIndex = _HeightmapLodOffset;
				if( min(baseUv.x, baseUv.y) < 0.0001 || max(baseUv.x, baseUv.y) > (1-0.0001) ){
					mipMapIndex = 0;
				}
				float heightmapValue = tex2Dlod(_HeightmapTex, float4( heightUv,0,mipMapIndex)).r;

				v.vertex.y += heightmapValue;
			}

			float3 ComputeSimpleNormal( float2 pos, float intensity){
				float3 dx = ddx( float3(pos.x, intensity, pos.y));
				float3 dy = ddy( float3(pos.x, intensity, pos.y));

				float3 lenX = length( dx.xz);
				float3 lenY = length( dy.xz);

				float sdx = dx.y / lenX;
				float sdy = dy.y / lenY;

				return normalize( float3(sdx, sdy, 1.0));
			}

			//////////////////////////////////////////////////////
			///// SURF SHADER ////////////////////////////////////
			//////////////////////////////////////////////////////
    
			void surf (in Input IN, inout SurfaceOutputStandard o)  
			{
				float3 worldPos = IN.worldPos;
				float2 normalUv = IN.uv_HeightmapTex;// IN.worldPos.xz /*/ 10 ;*/;

				float3 normal;

				normal = ComputeSimpleNormal( worldPos.xz, worldPos.y);

				float3 shitNorm = normalize(float3(normal.x, normal.y / 20, normal.z));
				o.Albedo = float4(0.5, 0.5, 0.5, 1);
				o.Normal = shitNorm; // normalize(normal.xyz);
				o.Alpha = 1; 
			}

			ENDCG
	} 
	FallBack "Diffuse"
}
