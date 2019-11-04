// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.


Shader "Custom/Vegetation/Grass.Instanced" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_BendingStrength("BendingStrength", Range(0,1)) = 0.0
		_InitialBendingValue("InitialBendingValue", Range(-1, 1)) = 0.0
		_PlantBendingStiffness("PlantBendingStiffness", Range(0,1)) = 0.5
		_WindDirection("WindDirection", Vector) = (0.0,0.0, 0.0, 0.0)
		_PlantDirection("PlantDirection", Vector) = (0.0,0.0, 0.0, 0.0)
		_RandSeed("RandSeed", Range(0,1)) = 0
		_DbgColor("DgbColor", Vector) = (1.0,1.0, 1.0, 1.0)
	}
		SubShader{
			Tags { "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		// And generate the shadow pass with instancing support
		#pragma surface surf Standard vertex:vert addshadow

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		// Enable instancing for this shader
		#pragma multi_compile_instancing

		// Config maxcount. See manual page.
		// #pragma instancing_options


		#include "singleGrassGeneration.hlsl"  

	fixed _BendingStrength; 
	fixed4 _WindDirection;  

		UNITY_INSTANCING_BUFFER_START(Props)    
			//UNITY_DEFINE_INSTANCED_PROP(fixed, _BendingStrength )	
			//UNITY_DEFINE_INSTANCED_PROP(fixed4, _WindDirection) 

			UNITY_DEFINE_INSTANCED_PROP(fixed4,_Color)  
#define _Color_arr Props
			UNITY_DEFINE_INSTANCED_PROP(half,  _InitialBendingValue )
#define _InitialBendingValue_arr Props
			UNITY_DEFINE_INSTANCED_PROP(fixed, _PlantBendingStiffness) 
#define _PlantBendingStiffness_arr Props
			UNITY_DEFINE_INSTANCED_PROP(fixed4, _PlantDirection) 
#define _PlantDirection_arr Props
			UNITY_DEFINE_INSTANCED_PROP(half, _RandSeed) 
#define _RandSeed_arr Props
			UNITY_DEFINE_INSTANCED_PROP(half4, _DbgColor) 
#define _DbgColor_arr Props
		UNITY_INSTANCING_BUFFER_END(Props) 

		void vert(inout appdata_full v, out Input o){ 
			float seed = UNITY_MATRIX_MVP[0] + UNITY_MATRIX_MVP[1] + UNITY_MATRIX_MVP[2];
			grass_vert(v, o,  
				(_BendingStrength),
				UNITY_ACCESS_INSTANCED_PROP(_InitialBendingValue_arr, _InitialBendingValue), 
				UNITY_ACCESS_INSTANCED_PROP(_PlantBendingStiffness_arr, _PlantBendingStiffness), 
				(_WindDirection), 
				UNITY_ACCESS_INSTANCED_PROP(_PlantDirection_arr, _PlantDirection),
				UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color),
				//UNITY_ACCESS_INSTANCED_PROP(_RandSeed),
				seed,  
				1); 
		} 

		void surf (Input IN, inout SurfaceOutputStandard o) {
			grass_surf(IN, o, UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color)); 
			 
			//half4 dbgColor = UNITY_ACCESS_INSTANCED_PROP(_DbgColor);
			//o.Albedo *= dbgColor;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
