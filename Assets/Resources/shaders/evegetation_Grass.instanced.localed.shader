Shader "Custom/EVegetation/GrassLocaledInstanced" {
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
		#pragma surface surf NoLighting vertex:vert addshadow
		#pragma target 5.0
		#pragma multi_compile_instancing



	fixed _BendingStrength; 
	fixed4 _WindDirection;  
		int _ScopeLength;    

		UNITY_INSTANCING_BUFFER_START(Props)    
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
			UNITY_DEFINE_INSTANCED_PROP(float, _Pointer)
		UNITY_INSTANCING_BUFFER_END(Props) 

		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
		{
			fixed4 c;
			c.rgb = s.Albedo; 
			c.a = s.Alpha; 
			return c;
		}  

		struct Input {
			float3 vertexWorldSpacePos;
			float3 plantNormal;
		};

		#include "eterrain_EPropLocaleHeightAccessing.hlsl"

		void vert(inout appdata_full v, out Input o) {
			float3 plantNormalWorldSpace = mul(unity_ObjectToWorld, float3(0, 0, 1));

			float4 worldSpacePos = mul(unity_ObjectToWorld, v.vertex);

			o.vertexWorldSpacePos = worldSpacePos;
			o.plantNormal = plantNormalWorldSpace;

			float heightOffset =  RetriveHeight();
			float3 objectOffset = mul((float3x3)unity_WorldToObject, float3(0,heightOffset,0));
			v.vertex.xyz += objectOffset;
		}

		void surf(Input i, inout SurfaceOutput o) {
			float3 cameraToVertexDirection = normalize(_WorldSpaceCameraPos - i.vertexWorldSpacePos);
			float cameraToPlantAngle = abs(dot(cameraToVertexDirection, i.plantNormal));

			float distanceToCamera = distance(_WorldSpaceCameraPos, i.vertexWorldSpacePos);
			float grassRemovalFactor = min(max(0, distanceToCamera - 10) / 50, 0.3);

			//clip(cameraToPlantAngle - grassRemovalFactor);
			o.Albedo = UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color)*0.6;
			o.Normal = i.plantNormal;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
