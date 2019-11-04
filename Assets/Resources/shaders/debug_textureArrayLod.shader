Shader "Custom/Debug/TextureArrayLod" {
    Properties {
		_ArrayElementSelector("ArrayElementSelector", Range(0,16)) = 0
		_ArrayLodSelector("ArrayLodSelector", Range(0,16)) = 0
		_MainTex("MainTex", 2DArray) = "white" {}
    }
    SubShader {
	Tags { "RenderType" = "Opaque" }
	CGPROGRAM
	#pragma surface surf Lambert
	#pragma target 4.6

	struct Input {
		float2 uv_MainTex;
	};

	float _ArrayElementSelector;
	float _ArrayLodSelector;
	UNITY_DECLARE_TEX2DARRAY(_MainTex);


 #if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE)
     #define UNITY_LOD_TEX2D(tex,coord) tex.CalculateLevelOfDetailUnclamped (sampler##tex,coord)
 #else
     // Just match the type i.e. define as a float value
     #define UNITY_LOD_TEX2D(tex,coord) float(1)
 #endif

 #if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE)
     #define UNITY_SAMPLE_TEX2D_GRAD(tex,coord,dx,dy) tex.SampleGrad (sampler##tex,coord,dx,dy)
 #else
     // Just match the type i.e. define as a float4 vector
     #define UNITY_SAMPLE_TEX2D_GRAD(tex,coord,dx,dy) float4(1,1,1,1)
 #endif

	void surf (Input IN, inout SurfaceOutput o) {
		float2 uv = IN.uv_MainTex;
		o.Albedo = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(IN.uv_MainTex, _ArrayElementSelector)).a;

		o.Albedo = UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, float3(IN.uv_MainTex, _ArrayElementSelector), _ArrayLodSelector).a;
	}

	ENDCG
    } 
    Fallback "Diffuse"
}