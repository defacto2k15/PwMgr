Shader "Custom/Ring2TerrainTextureTest1Feature"
	{
	Properties 
	{
		_DetailStrength("DetailStrength", Range(0,1)) = 1.
		_DetailComplexity("DetailComplexity", Range(0,1)) = 1.
		_DebugScalar("DebugScalar", Range(-1,1)) = 0
		_HeightTex("HeightTex", 2D) = "white" {}

		_DebugGroundLayerStrength("DebugGroundLayerStrength", Range(0,1)) = 1
		_DebugLayer0Strength("GroundLayer0Strength", Range(0,1)) = 1
		_DebugLayer1Strength("GroundLayer1Strength", Range(0,1)) = 1
		_DebugLayer2Strength("GroundLayer2Strength", Range(0,1)) = 1

		_ControlTex("ControlTex", 2D) = "white"{}
		_Dimensions("Dimensions", Vector) = (0.0,0.0,0.0,0.0)
		_LayerPriorities("LayerPriorities", Vector) = (1.0, 1.0, 1.0, 1.0)
	}
 
	SubShader 
	{
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Cull Off
        LOD 200
		ZWrite On
		ColorMask 0
			CGPROGRAM

			#pragma enable_d3d11_debug_symbols
			#pragma surface surf BlinnPhong  fullforwardshadows vertex:vert alpha:fade
			#pragma shader_feature OT_BASE
			#pragma shader_feature OT_DRY_SAND
			#pragma shader_feature OT_GRASS
			#pragma shader_feature OT_DOTS

			#include "UnityCG.cginc" 

			float _TerrainTextureSize;
			float _DetailStrength;
			float _DetailComplexity;
			float _DebugScalar;
			sampler2D _HeightTex;
			float _DebugGroundLayerStrength;
			float _DebugLayer0Strength;
			float _DebugLayer1Strength;
			float _DebugLayer2Strength;

			float4 _Palette[16];
			sampler2D _ControlTex;
			float4 _Dimensions;
			float4 _LayerPriorities;

			struct Input {
				float4 pos ; 
				half2 uv_MainTex ;
				float2 flatPos;
			}; 

			#include "ring2_testShader.hlsl"

			void vert (inout appdata_full v, out Input o){
				UNITY_INITIALIZE_OUTPUT(Input,o);
				o.pos = mul( unity_ObjectToWorld, v.vertex);
				o.flatPos = o.pos.xz;
				o.uv_MainTex = v.texcoord.xy;
			} 

			void surf(in Input i, inout SurfaceOutput o) {
				TextureLayerOutput output = ring2_surf(i.pos.xyz, i.uv_MainTex);

				o.Albedo = output.color;
				o.Normal = output.normal;
				o.Alpha = output.outAlpha;
			}

			ENDCG
	} 
	FallBack "Diffuse"
}
