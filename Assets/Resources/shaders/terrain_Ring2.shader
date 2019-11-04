Shader "Custom/Terrain/Ring2"
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

		_DetailComplexity("DetailComplexity", Range(0,1)) = 1.
		_ControlTex("ControlTex", 2D) = "white"{}
		_Dimensions("Dimensions", Vector) = (0.0,0.0,0.0,0.0) //to jest coords do controlTex
		_LayerPriorities("LayerPriorities", Vector) = (1.0, 1.0, 1.0, 1.0)

		_WeldTexture("WeldTexture", 2D) = "white" {}
		_LeftWeldTextureUvRange("LeftWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_RightWeldTextureUvRange("RightWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_TopWeldTextureUvRange("TopWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_BottomWeldTextureUvRange("BottomWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
	}
	SubShader 
	{
		Cull Off ZWrite On  //Rendering settings
        //Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        //LOD 100

        //ZWrite Off
        //Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma  surface surf Standard addshadow /*noshadow nolightmap noambient nodynlightmap*/ vertex:disp /* alpha:fade*/ //tessellate:tessFixed
			#pragma target 4.6
			#pragma shader_feature  DYNAMIC_NORMAL_GENERATION
			#pragma shader_feature  OT_BASE
			#pragma shader_feature  OT_DRY_SAND 
			#pragma shader_feature  OT_GRASS  
			#pragma shader_feature  OT_DOTS 


			//#define DYNAMIC_NORMAL_GENERATION
			//#define OT_BASE
			//#define OT_DRY_SAND
			//#define OT_GRASS 
			//#define OT_DOTS

			#include "common.txt"
			#include "UnityCG.cginc"  
			#include "noise.hlsl"

			sampler2D _HeightmapTex;
			float4 _HeightmapUv;
			sampler2D _NormalmapTex;
			float4 _NormalmapUv;

			float _DebugScalar;
			float _HeightmapLodOffset;

			float4 _Palette[16]; 
			float _DetailComplexity;
			sampler2D _ControlTex;
			float4 _Dimensions;
			float4 _LayerPriorities;
			#include "ring2_testShader.hlsl"

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
			///// RING2 GROUND ///////////////////////////////////
			//////////////////////////////////////////////////////


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

#ifdef DYNAMIC_NORMAL_GENERATION
				normal = ComputeSimpleNormal( worldPos.xz, worldPos.y); 
#else
				normal = ComputeSimpleNormal( worldPos.xz, worldPos.y); 
				//float3 encodedNormal = tex2D(_NormalmapTex, normalUv);
				//normal = decodeNormal(encodedNormal);
#endif
				
				TextureLayerOutput output = ring2_surf(worldPos, IN.uv_HeightmapTex.xy);

				o.Albedo = output.color;  
				o.Normal = normalize(normal + output.normal * 4);

				float distanceToCamera = length(_WorldSpaceCameraPos - IN.worldPos);
				float2 distanceFactorRanging = float2(4, 7);
				float distanceToCameraFactor = 1- normalizeTo(distanceFactorRanging[0], distanceFactorRanging[1], distanceToCamera);
				//o.Alpha = distanceToCameraFactor;
				clip(output.outAlpha- 0.01 - (1-distanceToCameraFactor)  );  //ODBLOKUJ TO BY BYŁ FADING
				clip(output.outAlpha - 0.01); 
			} 
			 
			ENDCG
	} 
	FallBack "Diffuse"
}
