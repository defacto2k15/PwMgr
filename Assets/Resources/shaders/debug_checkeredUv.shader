Shader "Custom/Debug/CheckeredUv"
{
	Properties 
	{
		_DummyTex ("DummyTex", 2D) = "white" {}
		
	}
	SubShader 
	{
		Cull Off ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma  surface surf Standard noshadow nolightmap noambient nodynlightmap 
			#pragma target 4.6


			sampler2D _DummyTex;
			struct Input{
				float2 uv_DummyTex : TEXCOORD0;
			};
    
			void surf (in Input IN, inout SurfaceOutputStandard o)  
			{
				float2 c = floor(IN.uv_DummyTex) *5;
				float checker = 2 * frac(c.x + c.y);
				float color = checker * 1 + (1 - checker) * 0;
				
				o.Albedo = color;
				//o.Albedo = float3(IN.uv_DummyTex.xy,0);
				o.Alpha = 1; 
			}

			ENDCG
	} 
	FallBack "Diffuse"
}
