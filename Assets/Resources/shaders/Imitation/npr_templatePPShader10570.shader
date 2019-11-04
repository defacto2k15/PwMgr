Shader "Custom/NPR/IMITATION/Template/PPShader-10570" {
	Properties
	{
		_MainTex("MainTex", 2D) = "green"{}
		_TexBuffer0("_TexBuffer0", 2D) = "green"{}
		_TexBuffer1("_TexBuffer1", 2D) = "green"{}
		_TexBuffer2("_TexBuffer2", 2D) = "green"{}
		_TexBuffer3("_TexBuffer3", 2D) = "green"{}
		_DebugSlider("DebugSlider", Float) = 0.0

			_sh_IntensityDifferenceTreshold("_IntensityDifferenceTreshold", Range(0,0.5)) = 0.1
			_sh_BrighterCountFactor("_BrighterCountFactor", Range(0,1)) = 0.74
			_sh_FilterRadius("FilterRadius",Int) = 2

			_sc_IntensityDifferenceTreshold("_IntensityDifferenceTreshold", Range(0,0.5)) = 0.1
			_sc_DarkerCountFactor("_DarkerCountFactor", Range(0,1)) = 0.36
			_sc_FilterRadius("FilterRadius",Int) = 2

			_irv_ZeroEpsilon("irv_ZeroEpsilon", Range(0,1)) = 0.0001
			_irv_MovingFactor("MovingFactor", Range(-1,1)) = 1
			_irv_UpperC("irv_UpperC", Range(0,1)) = 0.1
			_irv_LowerC("irv_LowerC", Range(0,1)) = 0.01

			_ha_TauFactor("ha_TauFactor", Range(0,100)) = 1
			_ha_Lambda("ha_Lambda", Range(0,10)) = 1
	}

		CGINCLUDE

#define IN_IMITATION (1)


	#define IN_objectid_TEXTURE_INDEX 3
	#define IN_objectid_TEXTURE_SUFFIX rg

	#define IN_normals_TEXTURE_INDEX 1
	#define IN_normals_TEXTURE_SUFFIX rgba

	#define IN_depth_TEXTURE_INDEX 1
	#define IN_depth_TEXTURE_SUFFIX rgba


#if IN_IMITATION
#define IN_sc_TEXTURE_INDEX 0
#define IN_sc_TEXTURE_SUFFIX rgba
#define IN_ha_TEXTURE_INDEX 0
#define IN_ha_TEXTURE_SUFFIX rgba
#define IN_ha_DESTINATION_TEXTURE_INDEX 0
#define IN_ha_DESTINATION_TEXTURE_SUFFIX rgba
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
//IMITATION USAGE LINE
//IMITATION USAGE LINE
//IMITATION USAGE LINE
#else
	#define IN_sh_TEXTURE_INDEX 2
	#define IN_sh_TEXTURE_SUFFIX r
	#define IN_sh_DESTINATION_TEXTURE_INDEX 0
	#define IN_sh_DESTINATION_TEXTURE_SUFFIX rgb

	#define IN_sc_TEXTURE_INDEX 2
	#define IN_sc_TEXTURE_SUFFIX r
	#define IN_sc_DESTINATION_TEXTURE_INDEX 0
	#define IN_sc_DESTINATION_TEXTURE_SUFFIX rgb

	#define IN_irv_TEXTURE_INDEX 2
	#define IN_irv_TEXTURE_SUFFIX r
	#define IN_irv_DESTINATION_TEXTURE_INDEX 0
	#define IN_irv_DESTINATION_TEXTURE_SUFFIX rgb

	#define IN_ha_TEXTURE_INDEX 3
	#define IN_ha_TEXTURE_SUFFIX rgb
	#define IN_ha_DESTINATION_TEXTURE_INDEX 0
	#define IN_ha_DESTINATION_TEXTURE_SUFFIX rgb

	#define IN_objectid_TEXTURE_INDEX 3
	#define IN_objectid_TEXTURE_SUFFIX rg

	#define IN_normals_TEXTURE_INDEX 1
	#define IN_normals_TEXTURE_SUFFIX rgba

	#define IN_depth_TEXTURE_INDEX 1
	#define IN_depth_TEXTURE_SUFFIX rgba

	#define IN_filter0_TRESHOLD 0.3
	#define IN_filter0_COLOR (float4(1.0, 0.0, 0.0, 0.0))
	#define IN_filter0_DESTINATION_TEXTURE_INDEX 0
	#define IN_filter0_DESTINATION_TEXTURE_SUFFIX rgba

	#define IN_filter1_TRESHOLD 0.7
	#define IN_filter1_COLOR (float4(0.0, 1.0, 0.0, 0.0))
	#define IN_filter1_DESTINATION_TEXTURE_INDEX 0
	#define IN_filter1_DESTINATION_TEXTURE_SUFFIX rgba
#endif


#define conc(a,b) a ## b
#define PP_FEATURE_TEXTURE_NAME(featureName) ( conc(_TexBuffer , IN_##featureName##_TEXTURE_INDEX))
#define PP_SAMPLE_FEATURE_TEXTURE_SELECTING_SUFFIX( featureName, pixel) conc(pixel., IN_##featureName##_TEXTURE_SUFFIX) 
#define PP_SAMPLE_FEATURE_TEXTURE( featureName, uv) PP_SAMPLE_FEATURE_TEXTURE_SELECTING_SUFFIX( featureName, tex2D( PP_FEATURE_TEXTURE_NAME(featureName), uv ) )

#define PP_SET_DESTINATION_FEATURE( featureName, inColors, newColor) conc( inColors[IN_##featureName##_DESTINATION_TEXTURE_INDEX]. , IN_##featureName##_DESTINATION_TEXTURE_SUFFIX) = newColor


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


	float _sh_IntensityDifferenceTreshold;
	float _sh_BrighterCountFactor;
	int _sh_FilterRadius;

	float _sc_IntensityDifferenceTreshold;
	float _sc_DarkerCountFactor;
	int _sc_FilterRadius;

	float _irv_ZeroEpsilon;
	float _irv_MovingFactor;
	float _irv_UpperC;
	float _irv_LowerC;

	float _ha_TauFactor;
	float _ha_Lambda;

#define SCREEN_TEX_SAMPLE_INTEGER(tex, int_uv){ \
	return tex2D(tex, float2( int_uv.x / _ScreenParams.x, int_uv.y / _ScreenParams.y ));\
}

	uint2 uv_to_intScreenCoords(float2 uv) {
		return uint2(floor(uv.x * _ScreenParams.x), floor(uv.y * _ScreenParams.y));
	}

	float2 intScreenCoords_to_uv(int2 coords) {
		return float2(coords.x / _ScreenParams.x, coords.y / _ScreenParams.y);
	}


#if IN_IMITATION
	#include "../PPFeatures/Filters.txt"
#else
	#include "PPFeatures/Filters.txt"
#endif

#if IN_IMITATION
#include "../PPFeatures/sc_ppFeature.txt"
#include "../PPFeatures/ha_ppFeature.txt"
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
//IMITATION INCLUDE LINE
#else
#include "PPFeatures/sc_ppFeature.txt"
#include "PPFeatures/sh_ppFeature.txt"
#include "PPFeatures/irv_ppFeature.txt"
#include "PPFeatures/ha_ppFeature.txt"
#endif


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
	pp_generate_filter_roberts(normals)
#endif

	//// Simple combiner
	half4 frag(v2f_img i) : SV_Target
	{
		float2 uv = i.uv;

		float4 inColors[4];
		inColors[0] = tex2D(_TexBuffer0, uv);
		inColors[1] = tex2D(_TexBuffer1, uv);
		inColors[2] = tex2D(_TexBuffer2, uv);
		inColors[3] = tex2D(_TexBuffer3, uv);


#if IN_IMITATION
ha_ppApplication(uv, inColors);
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
		//sh_ppApplication(uv, inColors);
		//sc_ppApplication(uv, inColors);
		//irv_ppApplication(uv, inColors);
		//ha_ppApplication(uv, inColors);
		pp_apply_usage(0, depth, freiChen, i.uv, inColors);
		pp_apply_usage(1, normals, roberts, i.uv, inColors);
#endif

		return inColors[0];
	}

		ENDCG

		SubShader
	{
		Cull Off ZWrite Off ZTest Always
			Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 5.0
			ENDCG
		}
	}
}
