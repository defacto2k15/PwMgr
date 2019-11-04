#ifndef H_COLOR_INC
#define H_COLOR_INC

float3 Hue(float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R,G,B));
}
 
float3 RGBtoHSV3(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 0.000001;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 HSVtoRGB3(in float3 HSV)
{
    return (((Hue(HSV.x) - 1) * HSV.y + 1) * HSV.z);
}

float4 HSVtoRGB(in float3 HSV)
{
	float3 rgb = HSVtoRGB3(HSV.xyz);
    return float4(rgb.x, rgb.y, rgb.z, 1);
}

half3 generateGrassColor(half hueBase, half saturationBase, half valueBase){
		float hue = lerp(0.25, 0.4, hueBase);
		float saturation = lerp(0.7, 0.9, saturationBase);
		float value = lerp(0.2, 0.5, valueBase);

		return HSVtoRGB3(float3(hue, saturation, value));
}

#endif
