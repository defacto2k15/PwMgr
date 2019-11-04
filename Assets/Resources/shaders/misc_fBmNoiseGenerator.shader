// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Misc/FbmNoiseGenerator"
	{
	Properties 
	{
		_MainTex ("", 2D) = "white" {}
		_Scale("Scale", Range(0,100)) = 1.0
		_Coords("Coords", Vector) = (0.0, 0.0, 1.0, 1.0)
		_OutValuesRange("OutValuesRange", Vector) = (0.0, 1.0, 1.0, 1.0)
	}
 
	SubShader 
	{
		Pass
		{
		ZTest Always Cull Off ZWrite Off Fog { Mode Off } //Rendering settings
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc" 
			//we include "UnityCG.cginc" to use the appdata_img struct
			#include "billboardGrassGenerator.hlsl"
    
			struct v2f {
				float4 pos : POSITION; // niezbedna wartosc by dzialal shader
				half2 uv : TEXCOORD0;
			};
   
			//Our Vertex Shader 
			v2f vert (appdata_img v){
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex); //niezbedna linijka by dzialal shader
				o.uv = MultiplyUV (UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				return o; 
			}
    
			sampler2D _MainTex; //Reference in Pass is necessary to let us use this variable in shaders
			float _Scale;
			float4 _Coords;
			float4 _OutValuesRange;
    
			GEN_fractalNoise( fractal_SimplexNoise, 10, snoise2D, 0, 1.8)

			//Our Fragment Shader
			fixed4 frag (v2f i) : Color{
				// sample texture and return it
				half2 pos = i.uv;
				pos.x *= _Coords[2];
				pos.y *= _Coords[3];

				pos += _Coords.xy;

				pos *= _Scale;
				half outScalar = fractal_SimplexNoise(pos + half2(312.22, -32.1))+0.5;

				outScalar = lerp(_OutValuesRange[0], _OutValuesRange[1], outScalar);
				//half outScalar = pos.x*pos.y;
				return fixed4(outScalar, outScalar, outScalar, 0.1);
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}
