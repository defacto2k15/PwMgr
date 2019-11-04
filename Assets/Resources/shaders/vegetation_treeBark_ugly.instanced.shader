Shader "Custom/Nature/Tree Creator Bark Optimized" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
		_BumpSpecMap ("Normalmap (GA) Spec (R)", 2D) = "bump" {}
		_TranslucencyMap ("Trans (RGB) Gloss(A)", 2D) = "white" {}
		_DitheringMode("Dithering Mode", Float) = 1

		// These are here only to provide default values
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_TreeInstanceColor ("TreeInstanceColor", Vector) = (1,1,1,1)
		_TreeInstanceScale ("TreeInstanceScale", Vector) = (1,1,1,1)
		_SquashAmount ("Squash", Float) = 1
	}

	SubShader { 
		Tags { "IgnoreProjector"="True" "RenderType"="TreeBark" }
		LOD 200

		CGPROGRAM
		#pragma surface surf BlinnPhong vertex:MyTreeVertBark addshadow
		#pragma multi_compile_instancing
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _BumpSpecMap;
		sampler2D _TranslucencyMap;
		float _DitheringMode;

		struct Input {
			float2 uv_MainTex;
			fixed4 color : COLOR;
			float4 screenPos ;
			float flatDistanceToCamera;
		}; 

		#include "MyUnityBuiltin3xTreeLibrary.hlsl"
		 
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex); 
			o.Albedo = c.rgb * IN.color.rgb * IN.color.a;

			fixed4 trngls = tex2D (_TranslucencyMap, IN.uv_MainTex);
			o.Gloss = trngls.a * _Color.r; 
			o.Alpha = c.a;

			half4 norspc = tex2D (_BumpSpecMap, IN.uv_MainTex);
			o.Specular = norspc.r;
			o.Normal = UnpackNormalDXT5nm(norspc);

#ifdef UNITY_PASS_FORWARDADD
			MyDitherByDistance( RetriveDitheringMode(_DitheringMode), IN.flatDistanceToCamera, IN.screenPos);
#endif 
		}
		ENDCG
	}

	//Dependency "BillboardShader" = "Hidden/Nature/Tree Creator Bark Rendertex"
}
