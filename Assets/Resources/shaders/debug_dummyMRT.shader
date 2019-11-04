Shader "Custom/Debug/NPR/DummyMRT"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "green"{}
		_TexBuffer0("_TexBuffer0", 2D) = "green"{}
		_TexBuffer1("_TexBuffer1", 2D) = "green"{}
		_TexBuffer2("_TexBuffer2", 2D) = "green"{}
		_TexBuffer3("_TexBuffer3", 2D) = "green"{}
		_DebugSlider("DebugSlider", Float) = 0.0
	}

		CGINCLUDE

#define IN_IMITATION (0)

#define IN_SAMPLER_objectid_TEXTURE_INDEX 2
#define IN_SAMPLER_objectid_SUFFIX xy
#define IN_SAMPLER_normals_TEXTURE_INDEX 1
#define IN_SAMPLER_normals_SUFFIX xyzw

#define conc(a,b) a ## b
#define PP_FACTOR_TEXTURE_NAME(factorName) ( conc(_TexBuffer , IN_SAMPLER_##factorName##_TEXTURE_INDEX))
#define PP_SAMPLE_FACTOR_TEXTURE( factorName, pixel) conc(pixel., IN_SAMPLER_##factorName##_SUFFIX) 
#define PP_SAMPLE_FACTOR_TEXTURE2( factorName, uv) PP_SAMPLE_FACTOR_TEXTURE( factorName, tex2D( PP_FACTOR_TEXTURE_NAME(factorName), uv ) )


#if IN_IMITATION
//IMITATION USAGE LINE
//IMITATION USAGE LINE
//IMITATION USAGE LINE
//IMITATION USAGE LINE
//IMITATION USAGE LINE
//IMITATION USAGE LINE
//IMITATION USAGE LINE
//IMITATION USAGE LINE
//IMITATION USAGE LINE
//IMITATION USAGE LINE
#else
	#define IN_USAGE_0_COLOR float4(1,0,1,0)
	#define IN_USAGE_0_TRESHOLD 0.1
#endif

#include "UnityCG.cginc"

#if IN_IMITATION
#include "../common.txt"
#include "../text_printing.hlsl"
#else
#include "text_printing.hlsl"
#include "common.txt"
#endif


	sampler2D _MainTex;
	sampler2D _TexBuffer0;
	sampler2D _TexBuffer1;
	sampler2D _TexBuffer2;
	sampler2D _TexBuffer3;
	float _DebugSlider;

	// MRT shader
	struct FragmentOutput
	{
		half4 dest0 : SV_Target0;
		half4 dest1 : SV_Target1;
		half4 dest2 : SV_Target2;
		half4 dest3 : SV_Target3;
	};

	FragmentOutput frag_mrt(v2f_img i) : SV_Target
	{
		FragmentOutput o;
		o.dest0 = frac(i.uv.x * 10);
		o.dest1 = frac(i.uv.y * 10);
		o.dest2 = frac(i.uv.x * i.uv.x * 10);
		o.dest3 = frac(i.uv.y * i.uv.y * 10);
		return o;
	}

#define SCREEN_TEX_SAMPLE_INTEGER(tex, int_uv){ \
	return tex2D(tex, float2( int_uv.x / _ScreenParams.x, int_uv.y / _ScreenParams.y ));\
}

	uint2 uv_to_intScreenCoords(float2 uv) {
		return uint2(floor(uv.x * _ScreenParams.x), floor(uv.y * _ScreenParams.y));
	}

	float2 intScreenCoords_to_uv(int2 coords) {
		return float2(coords.x / _ScreenParams.x, coords.y / _ScreenParams.y);
	}


	float pp_sampler_objectid(float2 uv, float2 centerUv) {
		float2 smp = PP_SAMPLE_FACTOR_TEXTURE2(objectid, uv);
		return UnpackUInt16Bit(smp);
	}

	float pp_sampler_normals(float2 uv, float2 centerUv) {
		half4 t0 = PP_SAMPLE_FACTOR_TEXTURE2(normals, centerUv);
		float3 normalValueCenter;
		float depthValueCenter;
		DecodeDepthNormal(t0, depthValueCenter, normalValueCenter);

		float4 t = PP_SAMPLE_FACTOR_TEXTURE2(normals, uv);
		float3 normalValue;
		float depthValue;
		DecodeDepthNormal(t, depthValue, normalValue);

		return length(normalValue - normalValueCenter);
	}

	float pp_sampler_depth(float2 uv, float2 centerUv) {
		float4 t = PP_SAMPLE_FACTOR_TEXTURE2(normals, uv);
		float3 normalValue;
		float depthValue;
		DecodeDepthNormal(t, depthValue, normalValue);

		return depthValue;
	}

#define pp_generate_filter_sobel(factorName) \
	float pp_filter_sobel_##factorName(float2 uv) {	\
		float3x3 G[2]; \
		G[0] = float3x3(1.0, 2.0, 1.0, 0.0, 0.0, 0.0, -1.0, -2.0, -1.0);	\
		G[1] = float3x3(1.0, 0.0, -1.0, 2.0, 0.0, -2.0, 1.0, 0.0, -1.0);	\
		float3x3 I;	\
		for (int i = 0; i < 3; i++) {	\
			for (int j = 0; j < 3; j++) {	\
				I[i][j] =  pp_sampler_##factorName( uv + intScreenCoords_to_uv(int2(i - 1, j - 1)), uv);	\
			}	\
		}	\
		float cnv[2];	\
		for (int i=0; i<2; i++) {	\
			float dp3 = dot(G[i][0], I[0]) + dot(G[i][1], I[1]) + dot(G[i][2], I[2]);	\
			cnv[i] = dp3 * dp3;		\
		}	\
		return 0.5 * sqrt(cnv[0] * cnv[0] + cnv[1] * cnv[1]);	\
	}


#define pp_generate_filter_roberts(factorName) \
	float pp_filter_roberts_##factorName(float2 uv) {	\
		float2x2 G[2];	\
		G[0] = float2x2(1,0,0,-1);	\
		G[1] = float2x2(0, 1, -1, 0);	\
		float2x2 I;	\
		for (int i = 0; i < 2; i++) {	\
			for (int j = 0; j < 2; j++) {	\
				I[i][j] =  pp_sampler_#factorName( uv + intScreenCoords_to_uv(int2(i, j)), uv );	\
			}	\
		}	\
		float cnv[2];	\
		for (int i=0; i<2; i++) {	\
			float dp3 = dot(G[i][0], I[0]) + dot(G[i][1], I[1]);	\
			cnv[i] = dp3 * dp3;		\
		}	\
		return sqrt(cnv[0] * cnv[0] + cnv[1] * cnv[1]);	\
	}

#define pp_generate_filter_fwidth(factorName)	\
	float pp_filter_fwidth_##factorName(float2 uv) {	\
		return	\
			fwidth(pp_sampler_##factorName(uv, uv)) +	\
			fwidth(pp_sampler_##factorName( uv + intScreenCoords_to_uv(int2(1,1)),		uv)) +	\
			fwidth(pp_sampler_##factorName( uv + intScreenCoords_to_uv(int2(-1,-1)),	uv))	\
			;	\
	}

#define pp_generate_filter_freiChen(factorName)	\
	float pp_filter_freiChen_##factorName(float2 uv) {	\
		float3x3 G[9];	\
		G[0] = 1.0/(2.0*sqrt(2.0)) * float3x3( 1.0, sqrt(2.0), 1.0, 0.0, 0.0, 0.0, -1.0, -sqrt(2.0), -1.0 );	\
		G[1] = 1.0/(2.0*sqrt(2.0)) * float3x3( 1.0, 0.0, -1.0, sqrt(2.0), 0.0, -sqrt(2.0), 1.0, 0.0, -1.0 );	\
		G[2] = 1.0/(2.0*sqrt(2.0)) * float3x3( 0.0, -1.0, sqrt(2.0), 1.0, 0.0, -1.0, -sqrt(2.0), 1.0, 0.0 );	\
		G[3] = 1.0/(2.0*sqrt(2.0)) * float3x3( sqrt(2.0), -1.0, 0.0, -1.0, 0.0, 1.0, 0.0, 1.0, -sqrt(2.0) );	\
		G[4] = 1.0/2.0 * float3x3( 0.0, 1.0, 0.0, -1.0, 0.0, -1.0, 0.0, 1.0, 0.0 );	\
		G[5] = 1.0/2.0 * float3x3( -1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, -1.0 );	\
		G[6] = 1.0/6.0 * float3x3( 1.0, -2.0, 1.0, -2.0, 4.0, -2.0, 1.0, -2.0, 1.0 );	\
		G[7] = 1.0/6.0 * float3x3( -2.0, 1.0, -2.0, 1.0, 4.0, 1.0, -2.0, 1.0, -2.0 );	\
		G[8] = 1.0/3.0 * float3x3( 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 );	\
		float3x3 I;	\
		for (int i = 0; i < 3; i++) {	\
			for (int j = 0; j < 3; j++) {	\
				I[i][j] = pp_sampler_##factorName(uv + intScreenCoords_to_uv(int2(i - 1, j - 1)), uv);	\
			}	\
		}	\
		float cnv[9];	\
		for (int i=0; i<9; i++) {	\
			float dp3 = dot(G[i][0], I[0]) + dot(G[i][1], I[1]) + dot(G[i][2], I[2]);	\
			cnv[i] = dp3 * dp3;		\
		}	\
		float M = (cnv[0] + cnv[1]) + (cnv[2] + cnv[3]);	\
		float S = (cnv[4] + cnv[5]) + (cnv[6] + cnv[7]) + (cnv[8] + M);		\
		return sqrt(M/S);	\
	}

#define pp_apply_usage(usageIndex, factorName, filterName, uv, color) { \
	if(pp_filter_##filterName##_##factorName(uv) > IN_USAGE_##usageIndex##_TRESHOLD){	\
		color = IN_USAGE_##usageIndex##_COLOR;	\
	}	\
}

#if IN_IMITATION
//IMITATION GENERATION LINE
//IMITATION GENERATION LINE
//IMITATION GENERATION LINE
//IMITATION GENERATION LINE
//IMITATION GENERATION LINE
//IMITATION GENERATION LINE
//IMITATION GENERATION LINE
//IMITATION GENERATION LINE
//IMITATION GENERATION LINE
//IMITATION GENERATION LINE
#else
	pp_generate_filter_freiChen(depth)
#endif


	//// Simple combiner
	half4 frag_combine(v2f_img i) : SV_Target
	{
		float4 color = 0;
#if IN_IMITATION
//IMITATION APPLY LINE
//IMITATION APPLY LINE
//IMITATION APPLY LINE
//IMITATION APPLY LINE
//IMITATION APPLY LINE
//IMITATION APPLY LINE
//IMITATION APPLY LINE
//IMITATION APPLY LINE
//IMITATION APPLY LINE
//IMITATION APPLY LINE
#else
		pp_apply_usage(0, depth, freiChen, i.uv, color);
#endif
		return color;
		//return boxedPrintNumber(a, i.uv, float4(0,0,1,1));
	}

		ENDCG

		SubShader
	{
		Cull Off ZWrite Off ZTest Always
			Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag_combine
			ENDCG
		}
	}
}