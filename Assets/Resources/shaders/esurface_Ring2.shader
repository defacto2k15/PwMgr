Shader "Custom/ESurface/Ground"
{
	Properties 
	{
		_DebugScalar("DebugScalar", Range(-10,10))=1
		_LodLevel("LodLevel", Range(-10,10))=1
		_HeightmapLodOffset("HeightmapLodOffset", Range(-10,10))=0

		_DetailComplexity("DetailComplexity", Range(0,1)) = 1.
		_ControlTex("ControlTex", 2D) = "white"{}
		_Dimensions("Dimensions", Vector) = (0.0,0.0,0.0,0.0) //to jest coords do controlTex
		_LayerPriorities("LayerPriorities", Vector) = (1.0, 1.0, 1.0, 1.0)
	}
	SubShader 
	{
		Cull Off ZWrite On  //Rendering settings
        //Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        //LOD 100

        //ZWrite Off
        //Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma  surface surf Standard addshadow /*noshadow nolightmap noambient nodynlightmap*//* alpha:fade*/ //tessellate:tessFixed
			#pragma target 4.6

			#define DYNAMIC_NORMAL_GENERATION
			#define OT_BASE
			#define OT_DRY_SAND
			#define OT_GRASS 
			#define OT_DOTS

			#include "common.txt"
			#include "UnityCG.cginc"  
			#include "noise.hlsl"

			float _DebugScalar;
			float _HeightmapLodOffset;

			float4 _Palette[16]; 
			float _DetailComplexity;
			sampler2D _ControlTex;
			float4 _Dimensions;
			float4 _LayerPriorities;
			#include "esurface_ring2_testShader.hlsl"

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
			  
			float3 ComputeSimpleNormal( float2 pos, float intensity){
				float3 dx = ddx( float3(pos.x, intensity, pos.y));
				float3 dy = ddy( float3(pos.x, intensity, pos.y));

				float3 lenX = length( dx.xz);
				float3 lenY = length( dy.xz);

				float sdx = dx.y / lenX;
				float sdy = dy.y / lenY;

				return normalize( float3(sdx, sdy, 1.0));
			}

			void surf (in Input IN, inout SurfaceOutputStandard o)    
			{
				float3 worldPos = IN.worldPos;
				float3 normal = ComputeSimpleNormal( worldPos.xz, worldPos.y); 
				
				TextureLayerOutput output = ring2_surf(worldPos, IN.uv_HeightmapTex.xy);
				o.Albedo = output.color;  
				o.Normal = normalize(normal + output.normal * 4);



				float distanceToCamera = length(_WorldSpaceCameraPos - IN.worldPos);
				float2 distanceFactorRanging = float2(4, 7);
				float distanceToCameraFactor = 1- normalizeTo(distanceFactorRanging[0], distanceFactorRanging[1], distanceToCamera);
				//o.Alpha = distanceToCameraFactor;
				//clip(output.outAlpha- 0.01 - (1-distanceToCameraFactor)  );  //ODBLOKUJ TO BY BYŁ FADING
				//clip(output.outAlpha - 0.01); 
			} 
			 
			ENDCG
	} 
	FallBack "Diffuse"
}
