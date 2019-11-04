Shader "Custom/Vegetation/GenericBillboard.Instanced" {
	Properties{
		_CollageTextureArray("_CollageTextureArray", 2DArray) = "white" {}
		_ImagesInArrayCount("_ImagesInArrayCount", int) = 4 
		_BaseYRotation("BaseYRotation", Range(0, 360)) = 0
		_Color("Color", Vector) = (1,1,1,1)
	}
	SubShader {
		LOD 200  

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf NoLighting addshadow vertex:vert 
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		#pragma multi_compile_instancing

	UNITY_DECLARE_TEX2DARRAY(_CollageTextureArray);

		UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(float, _BaseYRotation)
#define _BaseYRotation_arr Props
			UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
#define _Color_arr Props
		UNITY_INSTANCING_BUFFER_END(Props)

		#include "GenericBillboard.hlsl"

		//Our Vertex Shader 
		void vert (inout appdata_base v, out Input o){
			generic_billboard_vert(v, o); 
		} 
		
		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
		{
			fixed4 c;
			c.rgb = s.Albedo; 
			c.a = s.Alpha; 
			return c;
		}  

fixed4 evegetation_getSubtextureValue( int billboardIndex, fixed2 uv){
	return  UNITY_SAMPLE_TEX2DARRAY(_CollageTextureArray, float3(uv, billboardIndex));
}

void evegetation_billboard_surf(in Input i, inout SurfaceOutput o,  sampler2D l_CollageTex, float l_BillboardCount, float l_BaseYRotation) {
   float angle_degrees = i.angle_degrees + l_BaseYRotation;	

   float billboardIndex1 = floor(angle_degrees/360 * l_BillboardCount);
   float billboardIndex2 = ceil(angle_degrees/360 * l_BillboardCount);
   float fracValue = frac(angle_degrees/360 * l_BillboardCount);

   float4 bigColor1 = evegetation_getSubtextureValue(billboardIndex1, i.pos);
   float4 bigColor2 = evegetation_getSubtextureValue(billboardIndex2, i.pos);

   fixed4 outColor = lerp( bigColor1, bigColor2, fracValue);

   clip( outColor.a- 0.5);
   o.Albedo = outColor.rgb;
//   o.Alpha = outColor.a;
} 

		void surf(in Input i, inout SurfaceOutput o) {
			
			float4 propColor = UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color);
			o.Albedo.x *= propColor.x;
			o.Albedo.y *= propColor.y;
			o.Albedo.z *= propColor.z;

			o.Albedo *= 1.4;
//#ifdef UNITY_PASS_FORWARDADD // todo commented out
//			MyDitherByDistance( RetriveDitheringMode(_DitheringMode), IN.flatDistanceToCamera, IN.screenPos);
//#endif 
		}
		ENDCG 
	} 

	FallBack Off
}
 
