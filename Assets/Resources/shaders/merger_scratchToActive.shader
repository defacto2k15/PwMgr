Shader "Custom/TerrainDetailMerger/ScratchToActive"
	{
	Properties 
	{
		_ScratchTex ("ScratchTex", 2D) = "white" {}
		_ActiveTex ("ActiveTex", 2D) = "white" {}
		_ActiveCornerIndex("ActiveCornerIndex", Range(0,3)) = 0
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
    
			float _ActiveCornerIndex;
			sampler2D _ScratchTex;
			sampler2D _ActiveTex;
    
			//Our Fragment Shader
			float frag (v2f i) : Color{
				half2 uv = i.uv;
				return tex2D(_ScratchTex, uv);
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}
