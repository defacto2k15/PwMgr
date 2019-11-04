Shader  "Custom/Vegetation/GrassBushBillboard"
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
		_RandSeed("RandSeed", float) = 0
    }
    SubShader
    {
		Cull off
		LOD 200  

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf BlinnPhong fullforwardshadows vertex:vert 
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		#pragma multi_compile_instancing
 
		#include "billboardGrass2Shader.hlsl"

        sampler2D _MainTex;
		half _BendingStrength;
		half _InitialBendingValue; 
		half _PlantBendingStiffness;
		half4 _WindDirection;
		half4 _PlantDirection; 
		fixed4 _Color;
		float _RandSeed;
 
		void vert(inout appdata_full v, out Input o){
			billboard_vert(v,o ,_BendingStrength, _InitialBendingValue, _PlantBendingStiffness, _WindDirection, _PlantDirection, _RandSeed, 0, 1);
		}  

		void surf(Input IN, inout SurfaceOutput o)
		{
			billboard_surf_characteristics2(IN, o, _MainTex, _WorldSpaceCameraPos);
		}
        ENDCG
    }
    FallBack "Standard" 
}
 