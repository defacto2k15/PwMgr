// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/EVegetation/GenericBillboardLocaledInstanced" {
	Properties{
		_CollageTextureArray("_CollageTextureArray", 2DArray) = "white" {}
		_ImagesInArrayCount("_ImagesInArrayCount", int) = 4 
		_BaseYRotation("BaseYRotation", Range(0, 360)) = 0
		_Color("Color", Vector) = (1,1,1,1)
		_ScopeLength("ScopeLength", Range(0,300)) = 0
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
		int _ImagesInArrayCount;

		UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(float, _BaseYRotation)
#define _BaseYRotation_arr Props
			UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
#define _Color_arr Props
			UNITY_DEFINE_INSTANCED_PROP(float, _Pointer)
		UNITY_INSTANCING_BUFFER_END(Props)

		#include "GenericBillboard.hlsl"

			int _ScopeLength;    
#include "eterrain_EPropLocaleHeightAccessing.hlsl"


		//Our Vertex Shader 
		void vert (inout appdata_base v, out Input o){
			generic_billboard_vert(v, o); 

			float heightOffset =  RetriveHeight();
			float3 objectOffset = mul((float3x3)unity_WorldToObject, float3(0,heightOffset,0));
			v.vertex.xyz += objectOffset;
		} 
		
		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
		{
			fixed4 c;
			c.rgb = s.Albedo; 
			c.a = s.Alpha; 
			return c;
		}  


struct evegetation_decodedTexel{
	float3 normal;
	int colorMarker;
	float alpha;
};

evegetation_decodedTexel  make_evegetation_decodedTexel ( float3 normal, int colorMarker, float alpha){
	evegetation_decodedTexel texel;
	texel.normal = normal;
	texel.colorMarker = colorMarker;
	texel.alpha = alpha;
	return texel;
}


#define PI 3.1415926536f
half3 decodeNormal (half2 enc)
{
    half2 ang = enc*2-1;
    half2 scth;
    sincos(ang.x * PI, scth.x, scth.y);
    half2 scphi = half2(sqrt(1.0 - ang.y*ang.y), ang.y);
    return half3(scth.y*scphi.x, scth.x*scphi.x, scphi.y);
}

evegetation_decodedTexel decodeTexel(float4 input) {
	float colorFactor = input.x;
	int colorMarker = 0;
	if (colorFactor > 0.5) {
		colorMarker = 1;
	}
	return make_evegetation_decodedTexel(decodeNormal(input.yz), colorMarker, input.a);
}

struct evegetation_samplingResult {
	evegetation_decodedTexel texels[2];
	float blendWeight;
};

evegetation_samplingResult make_evegetation_samplingResult( evegetation_decodedTexel texels[2], float blendWeight) {
	evegetation_samplingResult t;
	t.texels = texels;
	t.blendWeight = blendWeight;
	return t;
}


fixed4 evegetation_getSubtextureValue( int billboardIndex, fixed2 uv){
	return  UNITY_SAMPLE_TEX2DARRAY(_CollageTextureArray, float3(uv, billboardIndex));
}

evegetation_samplingResult evegetation_billboard_surf(in Input i, float l_BillboardCount, float l_BaseYRotation) {
   float angle_degrees = i.angle_degrees + l_BaseYRotation;	

   float billboardIndex1 = floor(angle_degrees/360 * l_BillboardCount);
   float billboardIndex2 = ceil(angle_degrees/360 * l_BillboardCount);
   float fracValue = frac(angle_degrees/360 * l_BillboardCount);

   float4 encodedPixels[2];
   encodedPixels[0] = evegetation_getSubtextureValue(billboardIndex1, i.pos);
   encodedPixels[1] = evegetation_getSubtextureValue(billboardIndex2, i.pos);

   evegetation_decodedTexel texels[2];
   texels[0] = decodeTexel(encodedPixels[0]);
   texels[1] = decodeTexel(encodedPixels[1]);

   return make_evegetation_samplingResult(texels, fracValue);
} 

#include "color.hlsl"

float3 blendTwoColors(float3 baseColor, float3 additionalColor, float blendFactor) {
	float3 baseColorHSV = RGBtoHSV3(baseColor);
	float3 additionalColorHSV = RGBtoHSV3(additionalColor);

	float3 mixedColorHSV = float3(lerp(baseColorHSV.x, additionalColorHSV.x, blendFactor), baseColorHSV.y, baseColorHSV.z);

	return HSVtoRGB3(mixedColorHSV);
}

		void surf(in Input i, inout SurfaceOutput o) {	//TODO add normals coloring
			float baseYRotation = UNITY_ACCESS_INSTANCED_PROP(_BaseYRotation_arr, _BaseYRotation);
			evegetation_samplingResult samplingResult =  evegetation_billboard_surf(i, _ImagesInArrayCount, baseYRotation);

			float mergedAlpha = lerp(samplingResult.texels[0].alpha, samplingResult.texels[1].alpha, samplingResult.blendWeight);
			clip(mergedAlpha-0.9);

			int mergedColorMarker = round(lerp(samplingResult.texels[0].colorMarker, samplingResult.texels[1].colorMarker, samplingResult.blendWeight));

			float3 brownPattern = float3(132,77,16) / 255.0;
			float3 greenPattern = float3(107,178,16) / 255.0;

			float3 finalColor = brownPattern;
			if (mergedColorMarker == 0) {
				float4 propColor = UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color);
				finalColor =  blendTwoColors(greenPattern, propColor,0.5);
			}

			o.Albedo = finalColor;

//#ifdef UNITY_PASS_FORWARDADD // todo commented out
//			MyDitherByDistance( RetriveDitheringMode(_DitheringMode), IN.flatDistanceToCamera, IN.screenPos);
//#endif 
		}
		ENDCG 
	} 

	FallBack Off
}
 