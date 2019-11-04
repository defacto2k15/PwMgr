#ifndef LOD_DITHERING_INC
#define LOD_DITHERING_INC

#include "noise.hlsl"

void lodDitheringClip( float2 xyPos, float2 ditheringResolution, float cutoff, float seed){
	float2 pixelPosition = floor(xyPos * ditheringResolution);
	half randomValue = rand2(pixelPosition+seed);
	clip( randomValue - cutoff);
}

sampler2D _DitherMaskLOD2D; 
void MyUnityApplyDitherCrossFade(float4 screenPos, float alphaCutoff)
{
	float2 finalScreenPos = screenPos.xy;
	finalScreenPos /= screenPos.w;
	finalScreenPos.x *= _ScreenParams.x/3;
	finalScreenPos.y *= _ScreenParams.y/3;

	float2 vpos = finalScreenPos.xy;
	vpos /= 4; // the dither mask texture is 4x4

	float originalAlphaCutoff = alphaCutoff;
	alphaCutoff = clamp(0,0.999,alphaCutoff);

	float amount = floor(alphaCutoff * 16) / 16;
	vpos.y = frac(vpos.y) * 0.0625 /* 1/16 */ + amount; // quantized lod fade by 16 levels
	float texPixelValue = tex2D(_DitherMaskLOD2D, vpos).a;
	clip(( texPixelValue - 0.5)  );
}

#include "common.txt"
void MyDitherByDistance(float4 ditheringInfo, float distance, float4 screenPos){
	float min1 = ditheringInfo[0];
	float max1 = ditheringInfo[1];
	float min2 = ditheringInfo[2];
	float max2 = ditheringInfo[3];

	float cutoff1 = invLerpClamp2(min1, max1, distance);
	float cutoff2 = 1-invLerpClamp2(min2, max2, distance);

	float alphaCutoff = min(cutoff1, cutoff2);
	MyUnityApplyDitherCrossFade(screenPos, alphaCutoff);	
}

#include "UnityCG.cginc"

float Calculate2DDistanceFromCameraInVertShader( float4 vertex){
	float4x4 mm = UNITY_MATRIX_M;
	return distance (_WorldSpaceCameraPos.xz, mul(mm, vertex).xz);
}


#endif