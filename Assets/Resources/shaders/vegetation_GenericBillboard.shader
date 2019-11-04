Shader "Custom/Vegetation/GenericBillboard" {
	Properties{
		_CollageTex ("Collage", 2D) = "white" {}
		_BillboardCount ("BillboardCount", float) = 4 
		_ColumnsCount ("ColumnsCount", float) = 4 
		_RowsCount ("RowsCount", float) = 4 
		_BaseYRotation("BaseYRotation", Range(0, 360)) = 0
	}
	SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Cull Off 
        LOD 200
		ZWrite On
		ColorMask 0
		//Cull Front
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Lambert fullforwardshadows vertex:vert alpha:fade

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#include "UnityCG.cginc"  
		 
		sampler2D _CollageTex; 
		float _BillboardCount;
		float _ColumnsCount;
		float _RowsCount;
		float _BaseYRotation;

		struct Input {  
			float2 pos; // w pos.x przechowywany jest kąt
			float angle_degrees;
		};

		#include "GenericBillboard.hlsl"

		//Our Vertex Shader 
		void vert (inout appdata_base v, out Input o){
			generic_billboard_vert(v, o);
		}

		void surf(in Input i, inout SurfaceOutput o) {
			generic_billboard_surf(i,o, _CollageTex, _BillboardCount, _ColumnsCount, _RowsCount, _BaseYRotation);

			o.Alpha = 1;

			float ax = UNITY_ACCESS_INSTANCED_PROP(_BaseYRotation_arr, _BaseYRotation)/360;
			float3 col = float3(
				ax,
				frac(ax * i.angle_degrees* 8),
				frac(ax * 64));

			o.Albedo = col;
		} 

		ENDCG
	}
	FallBack "Diffuse"
}
