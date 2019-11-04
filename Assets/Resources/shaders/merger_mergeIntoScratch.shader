Shader "Custom/TerrainDetailMerger/MergeIntoScratch"
	{
	Properties 
	{
		_ScratchTex ("ScratchTex", 2D) = "white" {}
		_CornerTopLeftTex ("CornerTopLeftTex", 2D) = "white" {}
		_CornerTopRightTex ("CornerTopRightTex", 2D) = "white" {}
		_CornerBottomRightTex("CornerBottomRightTex", 2D) = "white" {}
		_CornerBottomLeftTex("CornerBottomLeftTex", 2D) = "white" {}

		_ActiveCornerIndex("ActiveCornerIndex", Range(0,3)) = 0
		_CornersMerged("CornersMerged", Vector) = (0,0,0,0)
		_MergeMargin("MergeMargin", Range(0,1)) = 0.1

	}
 
	SubShader 
	{
		Pass
		{
		ZTest Always Cull Off ZWrite Off Fog { Mode Off } //Rendering settings
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "common.txt"
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
    
			sampler2D _ScratchTex;
			sampler2D _CornerBottomRightTex;
			sampler2D _CornerBottomLeftTex;
			sampler2D _CornerTopLeftTex;
			sampler2D _CornerTopRightTex;

			float _ActiveCornerIndex;
			float4 _CornersMerged;
			float _MergeMargin;
    
			float MergedHeightOfBottomRight(half2 uv) {
				float ourHeight = tex2D(_CornerBottomRightTex, half2(0, 0.5) + uv / 2.0).r;
				if (_CornersMerged[2] > 0) { // arleady merged
					return ourHeight;
				}
				else {
					float leftMarginHeight = tex2D(_CornerBottomLeftTex, half2(1, uv.y / 2.0 + 0.5)).r;
					float mergingStrength = 1 - invLerp(0, _MergeMargin, uv.x);
					return lerp(ourHeight, leftMarginHeight, mergingStrength);
				}
			}

			float MergedHeightOfTopLeft(half2 uv) {
				float ourHeight2 = tex2D(_CornerTopLeftTex, half2(0.5, 0) + uv / 2.0).r;
				if (_CornersMerged[0] > 0) { // arleady merged
					return ourHeight2;
				}
				else {
					float bottomMarginHeight = tex2D(_CornerBottomLeftTex, half2(uv.x / 2.0 + 0.5, 1)).r;
					float mergingStrength = 1 - invLerp(0, _MergeMargin, uv.y);
					return lerp(ourHeight2, bottomMarginHeight, mergingStrength);
				}
			}

			float MergedHeightOfTopRight(half2 uv) {
				float ourHeight = tex2D(_CornerTopRightTex, half2(0, 0) + uv / 2.0).r;
				if (_CornersMerged[1] > 0) { // arleady merged
					return ourHeight;
				}
				else {
					float bottomMarginHeight = MergedHeightOfBottomRight(half2(uv.x, 1));
					float leftMarginHeight = MergedHeightOfTopLeft(half2(1, uv.y));

					float leftMergingStrength = 1 - invLerp(0, _MergeMargin, uv.x);
					float bottomMergingStrength = 1 - invLerp(0, _MergeMargin, uv.y);
					if (leftMergingStrength <= 0 && bottomMergingStrength <= 0) {
						return ourHeight;
					}
					else if (leftMergingStrength <= 0 && bottomMergingStrength > 0) {
						return lerp(ourHeight, bottomMarginHeight, bottomMergingStrength);
					}
					else if (leftMergingStrength > 0 && bottomMergingStrength <= 0) {
						return lerp(ourHeight, leftMarginHeight, leftMergingStrength);
					}
					else {
						// interesting both take part!
						float postLeft = max(0, 1 - bottomMergingStrength); 
						float postBottom = max(0, 1 - leftMergingStrength);
						float postSum = (postLeft + postBottom);
						float postFinal;
						if (postSum < 0.0001) {
							postFinal = bottomMarginHeight;
						}
						else {
							postFinal = (bottomMarginHeight * postBottom + leftMarginHeight * postLeft) / postSum;
						}

						float postOur = max(bottomMergingStrength, leftMergingStrength);
						return lerp( ourHeight, postFinal, postOur);
					}
				}
			}

			//Our Fragment Shader
			float frag (v2f i) : Color{
				half2 uv = i.uv;
				if (_ActiveCornerIndex < 0.5) { // we merge top left -> we are bottom right
					return  MergedHeightOfBottomRight(uv);
				}
				else if (_ActiveCornerIndex < 1.5){ // top right -> we dont change anything
					return tex2D(_CornerBottomLeftTex, uv/2 + half2(0.5, 0.5));
				}
				else if (_ActiveCornerIndex < 2.5) { // bottom right -> we need top left
					return MergedHeightOfTopLeft(uv);
				}
				else if (_ActiveCornerIndex < 3.5) { // bottom left
					return MergedHeightOfTopRight(uv);
				}
				else {
					return 0.1;
				}
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}
