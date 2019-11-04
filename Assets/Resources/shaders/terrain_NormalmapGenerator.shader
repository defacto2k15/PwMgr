Shader "Custom/Terrain/NormalmapGenerator"
	{
	Properties 
	{
		_Coords("Coords", Vector) = (0.0,0.0,1.0,1.0)
		_HeightmapTex("HeightmapTex", 2D) = "white" {}
		_HeightMultiplier("HeightMultiplier", Range(0,100)) = 1
		_GlobalCoords("GlobalCoords", Vector) = (0.0, 0.0, 1.0, 1.0)
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
    
			float4 _Coords;
			sampler2D _HeightmapTex; 
			float _HeightMultiplier;
			float4 _GlobalCoords;
    
			#include "common.txt"

			float3 ComputeNormalXX( float2 pos){
				float delta = 0.001f;
				float2 pos0 = float2(pos.x, pos.y);
				float2 pos1 = float2(pos.x+delta, pos.y);
				float2 pos2 = float2(pos.x, pos.y + delta);

				float3 vec0 = float3( pos0.x, tex2D(_HeightmapTex, pos0).r/1000000.0, pos0.y);
				float3 vec1 = float3( pos1.x, tex2D(_HeightmapTex, pos1).r/1000000.0, pos1.y);
				float3 vec2 = float3( pos2.x, tex2D(_HeightmapTex, pos2).r/1000000.0, pos2.y);

				float3 d1 = vec1 - vec0;
				float3 d2 = vec2 - vec0;

				return normalize(cross((d1),(d2)));
			}

			float3 ComputeNormal( float2 pos, float intensity){
				float3 dx = ddx( float3(pos.x, intensity, pos.y));
				float3 dy = ddy( float3(pos.x, intensity, pos.y));

				float3 lenX = length( dx.xz);
				float3 lenY = length( dy.xz);

				float sdx = dx.y / lenX;
				float sdy = dy.y / lenY;

				return normalize( float3(sdx, sdy, 1.0));
			}
			
			//Our Fragment Shader
			fixed4 frag (v2f i) : Color{
				float2 inPos = i.uv;
				inPos.x *= _Coords[2];
				inPos.y *= _Coords[3];
				
				float2 pos = inPos + float2(_Coords[0], _Coords[1]);
				float height = tex2D(_HeightmapTex,pos);
				height = DenormalizePlainHeight(height);

				float2 globalPos = float2(
					_GlobalCoords[0] + _GlobalCoords[2] * i.uv.x,
					_GlobalCoords[1] + _GlobalCoords[3] * i.uv.y
					);
				float3 normal = ComputeNormal(globalPos,height*_HeightMultiplier);

				return float4(encodeNormal(normalize(normal)), 1);
				//return float4(1, 0, 1, 1);
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}
