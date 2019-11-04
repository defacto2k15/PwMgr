Shader  "Custom/Vegetation/GrassBushBillboard.Instanced"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_BendingStrength("BendingStrength", Range(0,1)) = 0.0
		_InitialBendingValue("InitialBendingValue", Range(-1, 1)) = 0.0
		_PlantBendingStiffness("PlantBendingStiffness", Range(0,1)) = 0.5
		_WindDirection("WindDirection", Vector) = (0.0,0.0, 0.0, 0.0)
		_PlantDirection("PlantDirection", Vector) = (0.0,0.0, 0.0, 0.0)
		_RandSeed("RandSeed", float) = 0
		_ArrayTextureIndex("ArrayTextureIndex", float) = 0
	}
		SubShader
		{
			Cull off
			LOD 200

			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf BlinnPhong vertex:vert addshadow
			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 5.0
			#pragma multi_compile_instancing

			#include "billboardGrass2Shader.hlsl"
			#include "color.hlsl"

	UNITY_DECLARE_TEX2DARRAY(_BladeSeedTex);  //r - seed
	UNITY_DECLARE_TEX2DARRAY(_DetailTex);   //r - seed // r - distance to center g-alpha
		half _BendingStrength;
		half4 _WindDirection;

		UNITY_INSTANCING_BUFFER_START(Props)  
			UNITY_DEFINE_INSTANCED_PROP(float, _InitialBendingValue)	 
#define _InitialBendingValue_arr Props
			UNITY_DEFINE_INSTANCED_PROP(float, _PlantBendingStiffness)	 
#define _PlantBendingStiffness_arr Props
			UNITY_DEFINE_INSTANCED_PROP(float4, _Color)	 
#define _Color_arr Props
			UNITY_DEFINE_INSTANCED_PROP(float4, _PlantDirection)	 
#define _PlantDirection_arr Props
			UNITY_DEFINE_INSTANCED_PROP(float, _RandSeed)	 
#define _RandSeed_arr Props  
			UNITY_DEFINE_INSTANCED_PROP(float, _ArrayTextureIndex)	 
#define _ArrayTextureIndex_arr Props
		UNITY_INSTANCING_BUFFER_END(Props)
 
		void vert(inout appdata_full v, out Input o){
			float seed = UNITY_MATRIX_MVP[0] + UNITY_MATRIX_MVP[1] + UNITY_MATRIX_MVP[2];
			billboard_vert(v,o ,_BendingStrength, 
				UNITY_ACCESS_INSTANCED_PROP(_InitialBendingValue_arr, _InitialBendingValue),
				UNITY_ACCESS_INSTANCED_PROP(_PlantBendingStiffness_arr, _PlantBendingStiffness),
				_WindDirection,
				UNITY_ACCESS_INSTANCED_PROP(_PlantDirection_arr, _PlantDirection),
				//UNITY_ACCESS_INSTANCED_PROP(_RandSeed),
				seed,
				0, 1);
		}  

		void billboard_surf_characteristics3(Input l_IN, inout SurfaceOutput l_o, 
				float3 l_WorldSpaceCameraPos, float colorSeed, float2 detailInfo, float3 l_DriverColor) { //todo arguments names to have prefix l_ ?
			float distanceToCamera = length(l_WorldSpaceCameraPos - l_IN.worldPos);
			float2 distanceFactorRanging = float2(10, 40);

			float distanceToCameraFactor =  normalizeTo(distanceFactorRanging[0], distanceFactorRanging[1], distanceToCamera);
			// 0 - close
			//1 - far

			float alpha = detailInfo.g;
			clip(alpha - 0.6);

			
			float3 colorDeltas = float3(0.05, 0.3, 0.3);
			float3 driverColor = RGBtoHSV3(l_DriverColor);

			colorDeltas.r = lerp(-colorDeltas.r, colorDeltas.r, fmod(rand(colorSeed)*12.33,1));
			colorDeltas.g = lerp(-colorDeltas.g, colorDeltas.g, fmod(rand(colorSeed)*15.57,1));
			colorDeltas.b = lerp(-colorDeltas.b, colorDeltas.b, fmod(rand(colorSeed)*18.98,1));

			float3 outColor = HSVtoRGB3(driverColor + colorDeltas * (1 - distanceToCameraFactor));

			float innerDistance = detailInfo.r;
			innerDistance = lerp(0.7, 1, innerDistance);
			innerDistance = pow(innerDistance, 2);

			l_o.Albedo = outColor *lerp(innerDistance, 0.7, distanceToCameraFactor);
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
			float textureArrayIndex = UNITY_ACCESS_INSTANCED_PROP(_ArrayTextureIndex_arr, _ArrayTextureIndex);
			float colorSeed =  UNITY_SAMPLE_TEX2DARRAY(_BladeSeedTex, float3(IN.uv_MainTex, textureArrayIndex)).r;
			float2 detailInfo = UNITY_SAMPLE_TEX2DARRAY(_DetailTex, float3(IN.uv_MainTex, textureArrayIndex)).rg;

			billboard_surf_characteristics3(
				IN,
				o, 
				_WorldSpaceCameraPos,
				colorSeed,
				detailInfo,
				UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color).rgb
			);
		}
        ENDCG
    }
    FallBack "Standard" 
}
 