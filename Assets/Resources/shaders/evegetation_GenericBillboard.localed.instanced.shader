Shader "Custom/EVegetation/GenericBillboardLocaledInstanced" {
	Properties{
		_CollageTextureArray("_CollageTextureArray", 2DArray) = "white" {}
		_ImagesInArrayCount("_ImagesInArrayCount", int) = 4
		_BaseYRotation("BaseYRotation", Range(0, 360)) = 0
		_Color("Color", Vector) = (1,1,1,1)
		_ScopeLength("ScopeLength", Range(0,300)) = 0
		_Debug("Debug", Range(0,360)) = 0
	}
	SubShader {
		LOD 200  

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf SimpleLambert addshadow vertex:vert 
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		#pragma multi_compile_instancing

	UNITY_DECLARE_TEX2DARRAY(_CollageTextureArray);
		int _ImagesInArrayCount;
		int _ScopeLength;    
		float _Debug;

		UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(float, _BaseYRotation)
#define _BaseYRotation_arr Props
			UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
#define _Color_arr Props
			UNITY_DEFINE_INSTANCED_PROP(float, _Pointer)
		UNITY_INSTANCING_BUFFER_END(Props)

         half4 LightingSimpleLambert (SurfaceOutput s, half3 lightDir, half atten) {
			float3 normal = lerp(s.Normal, -normalize(float3(lightDir.x, 0, lightDir.z) + float3(0, -0.7, 0)), s.Alpha);
			half NdotL = (dot(normal, lightDir));
			 half4 c;
			 c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten);
			 c.a = 1; 
			 return c;
         }

		#include "GenericBillboard.hlsl"
		#include "eterrain_EPropLocaleHeightAccessing.hlsl"


		void evegetation_billboard_vert (inout float4 vertex, out Input o){
			fixed2 oldPos = vertex.xy + 0.5;
			o.pos = oldPos;

			half3 rot = mul(unity_ObjectToWorld, fixed4(0,0,0,1)).xyz - _WorldSpaceCameraPos;      //Calculate current rotation vector
			   
			half angle_radians = atan2(rot.x, rot.z);
			half angle_degrees = degrees(angle_radians);   
			vertex = RotateAroundYInDegrees(vertex, 360-angle_degrees);
			o.angle_degrees = fmod(angle_degrees, 360);
			o.screenPos = ComputeScreenPos( UnityObjectToClipPos (vertex));   
			o.flatDistanceToCamera =  Calculate2DDistanceFromCameraInVertShader(vertex);
		}

		//Our Vertex Shader 
		void vert (inout appdata_full v, out Input o){
			evegetation_billboard_vert(v.vertex, o); 

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

		   int billboardIndexes[2];
		   billboardIndexes[0] = floor(angle_degrees/360 * l_BillboardCount);
		   billboardIndexes[1] = ceil(angle_degrees/360 * l_BillboardCount);
		   float fracValue = frac(angle_degrees/360 * l_BillboardCount);

		   evegetation_decodedTexel texels[2];
		   for (int j = 0; j < 2; j++) {
			   float4 encodedPixel = evegetation_getSubtextureValue(billboardIndexes[j], i.pos);
			   texels[j] = decodeTexel(encodedPixel);
			   float quantisizedBillboardAngle = billboardIndexes[j] * 360.0 / l_BillboardCount - l_BaseYRotation;
			   texels[j].normal = RotateAroundYInDegrees(float4(texels[j].normal, 1), -quantisizedBillboardAngle).xyz;
		   }

		   return make_evegetation_samplingResult(texels, fracValue);
		} 

		#include "evegetation_color_common.hlsl"

		float _GlobalX;
		void surf(in Input i, inout SurfaceOutput o) {	//TODO add normals coloring
			float baseYRotation = UNITY_ACCESS_INSTANCED_PROP(_BaseYRotation_arr, _BaseYRotation);
			evegetation_samplingResult samplingResult =  evegetation_billboard_surf(i, _ImagesInArrayCount, baseYRotation);

			float mergedAlpha = lerp(samplingResult.texels[0].alpha, samplingResult.texels[1].alpha, samplingResult.blendWeight);
			clip(mergedAlpha-0.9);

			bool mergedColorMarker = !round(lerp(samplingResult.texels[0].colorMarker, samplingResult.texels[1].colorMarker, samplingResult.blendWeight));
			float3 propColor = UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color).xyz;
			float3 finalColor = EVegetationCalculateColor(mergedColorMarker, propColor);
			o.Albedo = finalColor;

			float3 mergedNormal = normalize(lerp(samplingResult.texels[0].normal, samplingResult.texels[1].normal, samplingResult.blendWeight));
			o.Normal = mergedNormal;
			o.Alpha = saturate(i.flatDistanceToCamera/1000.0); // TODO - hack. Says how much we want to lerp between normal calculated here and generic billboard up normal;
		}
		ENDCG 
	} 

	FallBack Off
}
 