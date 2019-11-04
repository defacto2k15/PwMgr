// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Custom/TestPlainInstancingMaterial" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_DbgValue ("DbgValue", Range(0,1)) = 1.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		// And generate the shadow pass with instancing support
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		// Enable instancing for this shader
		#pragma multi_compile_instancing

		// Config maxcount. See manual page.
		// #pragma instancing_options


		UNITY_INSTANCING_BUFFER_START(Props)  
			UNITY_DEFINE_INSTANCED_PROP(fixed4,_Color)	 
			UNITY_DEFINE_INSTANCED_PROP(fixed, _DbgValue )	
		UNITY_INSTANCING_BUFFER_END(Props)

		struct Input {
			float3 normal; 
		};

		void vert(inout appdata_full v, out Input o){
			o.normal = float3(0.5, 0.5, 0.5);			
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			o.Albedo =  UNITY_ACCESS_INSTANCED_PROP(Props, _Color) * UNITY_ACCESS_INSTANCED_PROP(Props, _DbgValue); 
			o.Alpha = 1.0;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
