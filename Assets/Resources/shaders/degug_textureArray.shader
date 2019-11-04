Shader "Custom/Debug/TextureArray" {
    Properties {
		_ArrayElementSelector("ArrayElementSelector", Range(0,16)) = 0
		_MainTex("MainTex", 2D) = "white" {}
		_ArrayTex("ArrayTex", 2DArray) = "white"{}
    }
    SubShader {
	Tags { "RenderType" = "Opaque" }
	CGPROGRAM
	#pragma surface surf Lambert

	struct Input {
		float2 uv_MainTex;
	};

	float _ArrayElementSelector;
	sampler2D _MainTex;
	UNITY_DECLARE_TEX2DARRAY(_ArrayTex);

	void surf (Input IN, inout SurfaceOutput o) {
		float4 color =  UNITY_SAMPLE_TEX2DARRAY(_ArrayTex, float3(IN.uv_MainTex, _ArrayElementSelector));
		o.Albedo = color.rgb;
		o.Alpha = color.a;
	}

	ENDCG
    } 
    Fallback "Diffuse"
}