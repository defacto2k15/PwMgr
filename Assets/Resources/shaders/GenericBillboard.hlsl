#ifndef GENERIC_BILLBOARD_INC
#define GENERIC_BILLBOARD_INC

#include "UnityCG.cginc" 
#include "common.txt"
#include "lodDithering.hlsl"
#include "treeDitheringInfo.hlsl"
 
struct Input {
	float2 pos;
	float4 screenPos ;
	float flatDistanceToCamera;
	float angle_degrees; 
};

float4 RotateAroundYInDegrees (float4 vertex, float degrees)
{
	float alpha = degrees * fPI() / 180.0;
	float sina;
	float cosa;
	sincos(alpha, sina, cosa);
	float2x2 m = float2x2(cosa, -sina, sina, cosa);
	return float4(mul(m, vertex.xz), vertex.yw).xzyw;
}

//Our Vertex Shader 
void generic_billboard_vert (inout appdata_base v, out Input o){
	fixed2 oldPos = v.vertex.xy + 0.5;
	o.pos = oldPos;

	half3 rot = mul(unity_ObjectToWorld, fixed4(0,0,0,1)).xyz
		 - _WorldSpaceCameraPos;                            //Calculate current rotation vector
	   
	half angle_radians = atan2(rot.x, rot.z);
	half angle_degrees = degrees(angle_radians);                                   //Convert from rad to degree
	v.vertex = RotateAroundYInDegrees(v.vertex, 360-angle_degrees);
	v.normal = fixed3(-sin(angle_radians), 0, -cos(angle_radians));
	o.angle_degrees = fmod(angle_degrees, 360);
	o.screenPos = ComputeScreenPos( mul (UNITY_MATRIX_MVP, v.vertex));   
	o.flatDistanceToCamera =  Calculate2DDistanceFromCameraInVertShader(v.vertex);
}

fixed4 getSubtextureValue( float billboardIndex, fixed2 uv, sampler2D l_CollageTex, float l_ColumnsCount, float l_RowsCount){
   float rowIndex = floor(billboardIndex / l_ColumnsCount);
   float columnIndex = billboardIndex - rowIndex * l_ColumnsCount; 

   fixed2 textureCellSize = fixed2( 1/l_ColumnsCount, 1/l_RowsCount);
   fixed2 baseUv = fixed2( textureCellSize.x * columnIndex, textureCellSize.y * rowIndex);
   fixed2 offsetUv = fixed2( textureCellSize.x * uv.x, textureCellSize.y * uv.y);
   fixed2 finalUv = baseUv + offsetUv;

   return tex2D(l_CollageTex, finalUv);
}
#endif
