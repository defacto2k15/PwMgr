Shader "Custom/ETerrain/Ring2Stamper"
	{
	Properties 
	{ 
		_Coords("Coords", Vector) = (0.0, 0.0, 10.0, 10.0)

		_DetailStrength("DetailStrength", Range(0,1)) = 1.
		_DetailComplexity("DetailComplexity", Range(0,1)) = 1.
		_DebugScalar("DebugScalar", Range(-1,1)) = 0
		_HeightTex("HeightTex", 2D) = "white" {}

		_ControlTex("ControlTex", 2D) = "white"{}
		_Dimensions("Dimensions", Vector) = (0.0,0.0,0.0,0.0)
		_LayerPriorities("LayerPriorities", Vector) = (1.0, 1.0, 1.0, 1.0)
	}
 
	SubShader 
	{
		
		Pass
		{
		ZTest Always Cull Off ZWrite Off Fog { Mode Off } //Rendering settings
			CGPROGRAM
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members uv_HeightTex)
			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile OT_BASE OT_DRY_SAND OT_GRASS OT_DOTS
			#pragma multi_compile GENERATE_COLOR GENERATE_NORMAL
			#include "UnityCG.cginc"    
    
			float _TerrainTextureSize;
			float _DetailStrength; 
			float _DetailComplexity;
			float _DebugScalar;

			int _LayerIndex;
			float4 _Palette[16];   
			sampler2D _ControlTex;
			float4 _Dimensions;
			float4 _LayerPriorities;
			float4 _RandomSeeds;

			#include "esurface_ring2_testShader.hlsl" 

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
    
			float4 _Coords;
    
			#include "common.txt"
			
			//Our Fragment Shader 
			fixed4 frag (v2f i) : Color{ 

				float2 inPos = i.uv; 
				inPos.x *= _Coords[2]; 
				inPos.y *= _Coords[3];
				
				float2 pos = inPos + float2(_Coords[0], _Coords[1]);

				TextureLayerOutput output = ering2_surf( float3( pos.x, 0, pos.y), i.uv.xy, _LayerIndex);

#ifdef GENERATE_COLOR
				if (output.outAlpha <= 0) {
					output.color = 0;
				}

				return fixed4(output.color.x, output.color.y, output.color.z, output.outAlpha);
#else /*GENERATE_NORMAL*/
				float3 normalizedNormal = (output.normal + float3(1, 1, 1)) / 2;
				return fixed4(normalizedNormal.x, normalizedNormal.y, normalizedNormal.z, output.outAlpha);
#endif
			}
			ENDCG   
		}
	} 
	FallBack "Diffuse"
}
