Shader  "Custom/Precomputation/GrassBushBillboardGenerator"
	{
	Properties 
	{
		_MainTex ("", 2D) = "white" {}
		_Seed("Seed", Range(0,1000)) = 0
		_BladesCount("BladesCount", Range(0,100)) = 50
	}
 
	SubShader 
	{
		Pass
		{
		ZTest Always Cull Off ZWrite Off Fog { Mode Off } //Rendering settings
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc" 
			#include "billboardGrassGenerator.hlsl"
    
			struct v2f {
				float4 pos : POSITION; // niezbedna wartosc by dzialal shader
				half2 uv : TEXCOORD0;
			};
   
			v2f vert (appdata_img v){
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex); //niezbedna linijka by dzialal shader
				o.uv = MultiplyUV (UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				return o; 
			}
    
			sampler2D _MainTex; //Reference in Pass is necessary to let us use this variable in shaders
			float _Seed;
			float _BladesCount;
    
			//Our Fragment Shader
			fixed4 frag (v2f i) : Color{
				// sample texture and return it
				half2 pos = i.uv;
				pos.x = remapNeg(pos.x); 
				float4 characteristic = billboard_grass_generator_surf_point_characteristics( pos, _Seed/31.2, _BladesCount);

				float seed = characteristic.r;
				float distanceToCenter = characteristic.b;
				float alpha = characteristic.a;

				float4 col = float4(0.0, 0.0, 0.0, 0.0);

				col.w = step(0.1,alpha); 
				col.r = fmod(seed * 12345, 453) / 453;
				col.g = distanceToCenter;

				return col;
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}