Shader "Custom/Vegetation/Grass" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_BendingStrength ("BendingStrength", Range(0,1)) = 0.0
		_InitialBendingValue ("InitialBendingValue", Range(-1, 1)) = 0.0
		_PlantBendingStiffness("PlantBendingStiffness", Range(0,1)) = 0.5
		_WindDirection("WindDirection", Vector) = (0.0,0.0, 0.0, 0.0)
		_PlantDirection("PlantDirection", Vector) = (0.0,0.0, 0.0, 0.0)
		_RandSeed("RandSeed", Range(0,1)) = 0
		_DistanceScale("DistanceScale", Range(0,1)) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		//Cull Front
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		
		#include "common.txt"
		#include "noise.hlsl"
		#include "singleGrassGeneration.hlsl"


		half _BendingStrength;
		half _InitialBendingValue;
		half _PlantBendingStiffness;
		half4 _WindDirection;
		half4 _PlantDirection; 
		fixed4 _Color;
		half _RandSeed;
		float _DistanceScale;
		

		void vert(inout appdata_full v, out Input o){
			grass_vert(v, o, _BendingStrength, _InitialBendingValue, _PlantBendingStiffness, _WindDirection, _PlantDirection,
				_Color, _RandSeed, _DistanceScale);
		} 

		void surf (Input IN, inout SurfaceOutputStandard o) {
			grass_surf(IN, o, _Color);  
		} 
		ENDCG
	}
	FallBack "Diffuse"
}
