Shader "Custom/BillboardTransparent"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_BendingStrength ("BendingStrength", Range(0,1)) = 0.0
		_InitialBendingValue ("InitialBendingValue", Range(-1, 1)) = 0.0
		_PlantBendingStiffness("PlantBendingStiffness", Range(0,1)) = 0.5
		_WindDirection("WindDirection", Vector) = (0.0,0.0, 0.0, 0.0)
		_PlantDirection("PlantDirection", Vector) = (0.0,0.0, 0.0, 0.0)
		_MinUv("MinUv", float) = 0
		_MaxUv("MaxUv", float) = 0.5
		_RandSeed("RandSeed", float) = 0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Cull Off
        LOD 200
		ZWrite On
		ColorMask 0
   
        CGPROGRAM
 
        #pragma  surface surf Standard fullforwardshadows alpha:fade vertex:vert
        #pragma target 3.0
 
		#include "billboardGrassShader.hlsl"

        sampler2D _MainTex;
		half _BendingStrength;
		half _InitialBendingValue;
		half _PlantBendingStiffness;
		half4 _WindDirection;
		half4 _PlantDirection; 
		fixed4 _Color;
		float _MinUv;
		float _MaxUv;
		float _RandSeed;
 
		void vert(inout appdata_full v, out Input o){
			billboard_vert(v,o ,_BendingStrength, _InitialBendingValue, _PlantBendingStiffness, _WindDirection, _PlantDirection, _RandSeed, _MinUv, _MaxUv);
		}  
 
        void surf (Input IN, inout SurfaceOutputStandard o)  
        {
			billboard_surf(IN, o, _MainTex, _MinUv, _MaxUv);  
        }
        ENDCG
    }
    FallBack "Standard"
}
 