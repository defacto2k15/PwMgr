Shader "Custom/NPR/ArtMapWrapping"
	{
	Properties 
	{
		_InputTex ("InputTex", 2D) = "white" {}
		_Margin("Margin", Range(0,1)) = 0
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
    
			float _Margin;
			sampler2D _InputTex;

			float2 toOuterUv(half2 uv){
			
			}
    
			//Our Fragment Shader
			float4 frag (v2f i) : Color{
				half2 uv = i.uv;
				half innerMargin = _Margin / (1 - _Margin*2);

				half2 innerUv = _Margin + uv * (1-_Margin*2);
				
				half4 currentColor = tex2D(_InputTex, innerUv);

				if(uv.x < innerMargin){
					currentColor = max(currentColor, tex2D(_InputTex, innerUv + half2(1 - 2*_Margin,0)));
				}
				if(uv.y < innerMargin){
					currentColor = max(currentColor, tex2D(_InputTex, innerUv + half2(0,1 - 2*_Margin)));
				}
				if(uv.x < innerMargin && uv.y < innerMargin){
					currentColor = max(currentColor, tex2D(_InputTex, innerUv + half2(1-2*_Margin,1 - 2*_Margin)));
				}

				if(uv.x > 1-innerMargin){
					currentColor = max(currentColor, tex2D(_InputTex, innerUv + half2(-(1 - 2*_Margin), 0)));
				}
				if(uv.y > 1-innerMargin){
					currentColor = max(currentColor, tex2D(_InputTex, innerUv + half2(0,-(1 - 2*_Margin))));
				}
				if(uv.x > 1-innerMargin && uv.y > 1-innerMargin){
					currentColor = max(currentColor, tex2D(_InputTex, innerUv + half2(-(1 - 2*_Margin),-(1 - 2*_Margin))));
				}

				if(uv.x < innerMargin && uv.y > 1-innerMargin){
					currentColor = max(currentColor, tex2D(_InputTex, innerUv + half2(1 - 2*_Margin,-(1 - 2*_Margin))));
				}
				if(uv.x > 1-innerMargin && uv.y < innerMargin){
					currentColor = max(currentColor, tex2D(_InputTex, innerUv + half2(-(1 - 2*_Margin),1 - 2*_Margin)));
				}


				return currentColor;
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}
