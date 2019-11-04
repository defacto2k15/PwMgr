Shader "Custom/Terrain/Ring2Stamped"
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

		_MainTex ("MainTex", 2D) = "white" {} 
		_NormalTex ("NormalTex", 2D) = "white" {}

		_WeldTexture("WeldTexture", 2D) = "white" {}
		_LeftWeldTextureUvRange("LeftWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_RightWeldTextureUvRange("RightWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_TopWeldTextureUvRange("TopWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_BottomWeldTextureUvRange("BottomWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
	}
	SubShader 
	{ 
		Cull Off ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma  surface surf Standard addshadow /*noshadow nolightmap noambient nodynlightmap*/ vertex:disp tessellate:tessFixed
			#pragma target 4.6
			#pragma shader_feature DYNAMIC_NORMAL_GENERATION

			#include "common.txt"
			#include "UnityCG.cginc" 
			#include "noise.hlsl"

			sampler2D _HeightmapTex;
			float4 _HeightmapUv;
			sampler2D _NormalmapTex;
			float4 _NormalmapUv;

			float _DebugScalar;
			float _HeightmapLodOffset;

			sampler2D _MainTex;
			sampler2D _NormalTex;

			sampler2D _WeldTexture;
			float4 _LeftWeldTextureUvRange;
			float4 _RightWeldTextureUvRange;
			float4 _TopWeldTextureUvRange;
			float4 _BottomWeldTextureUvRange;

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

			#include "terrain.hlsl"
			//Our Vertex Shader 
			void disp (inout appdata v){
				v.vertex.y =  CalculateHeightWithMargins(v.texcoord.xy);  
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

				normalUv.x *= _NormalmapUv[2]; 
				normalUv.y *= _NormalmapUv[3];

				normalUv.x += _NormalmapUv[0];
				normalUv.y += _NormalmapUv[1];

				float3 normal;

//#ifdef DYNAMIC_NORMAL_GENERATION
				//normal = ComputeSimpleNormal( worldPos.xz, worldPos.y);
//#else
				float3 encodedNormal = tex2D(_NormalmapTex, normalUv);
				normal = decodeNormal(encodedNormal);
//#endif
				
				float2 detailUv = IN.uv_HeightmapTex;
				float4 detailColor = tex2D(_MainTex, detailUv);
				float3 detailNormal = decodeNormal(tex2D(_NormalTex, detailUv));


				o.Albedo =  detailColor.rgb;
				o.Normal = normalize(detailNormal*3 + normal)/2;

				clip(detailColor.a - 0.01  );
			}
			 
			ENDCG
	} 
	FallBack "Diffuse"
}
