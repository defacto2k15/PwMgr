Shader "Custom/TerGen/ErosionThermal"
	{
	Properties 
	{
		_MainInputTex ("", 2D) = "white" {}
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
   
			//Our Vertex Shader 
			v2f vert (appdata_img v){
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex); //niezbedna linijka by dzialal shader
				o.uv = MultiplyUV (UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				o.uv = v.texcoord.xy;
				return o; 
			}
    
			sampler2D _MainInputTex; //Reference in Pass is necessary to let us use this variable in shaders
    
			//Our Fragment Shader
			fixed4 frag (v2f i) : Color{
				// sample texture and return it
				half2 uv = i.uv;
				float4 col = tex2D(_MainInputTex, uv);

				return col;
			}
			ENDCG
		}

        GrabPass
        {
				"GrabTextureXX"
        }

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
   
			//Our Vertex Shader 
			v2f vert (appdata_img v){
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex); //niezbedna linijka by dzialal shader
				o.uv = MultiplyUV (UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				return o; 
			}
    
			sampler2D _GrabTextureXX; //Reference in Pass is necessary to let us use this variable in shaders
    
			//Our Fragment Shader
			fixed4 frag (v2f i) : Color{
				// sample texture and return it
				half2 uv = i.uv;
				float4 col = tex2D(_GrabTextureXX, uv);
				col.r = uv.x;
				col.g = uv.y;
				return col;
			}
			ENDCG
		}
       Pass
       {
           Color (0,1,0,1)
       }
	} 
	FallBack "Diffuse"
}
