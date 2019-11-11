#ifndef GENERIC_BILLBOARD_SURF_INC
#define GENERIC_BILLBOARD_SURF_INC


void generic_billboard_surf(in Input i, inout SurfaceOutput o,  sampler2D l_CollageTex, float l_BillboardCount, float l_ColumnsCount, float l_RowsCount, float l_BaseYRotation) {
   float angle_degrees = i.angle_degrees + l_BaseYRotation;	

   float billboardIndex1 = floor(angle_degrees/360 * l_BillboardCount);
   float billboardIndex2 = ceil(angle_degrees/360 * l_BillboardCount);
   float fracValue = frac(angle_degrees/360 * l_BillboardCount);

   float4 bigColor1 = getSubtextureValue(billboardIndex1, i.pos, l_CollageTex, l_ColumnsCount, l_RowsCount);
   float4 bigColor2 = getSubtextureValue(billboardIndex2, i.pos, l_CollageTex, l_ColumnsCount, l_RowsCount);

   fixed4 outColor = lerp( bigColor1, bigColor2, fracValue);

   clip( outColor.a- 0.5);
   o.Albedo = outColor.rgb;
//   o.Alpha = outColor.a;
} 


#endif
