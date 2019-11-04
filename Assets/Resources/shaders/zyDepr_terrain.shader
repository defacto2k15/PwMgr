Shader "Custom/Terrain/Terrain1"
{
	Properties 
	{
		_HeightmapTex ("HeightmapTex", 2D) = "white" {}
		_NormalsTex ("NormalsTex", 2D) = "white" {}
		_TessalationTex("_TessalationTex", 2D) = "white" {}
		_MaxHeight("MaxHeight", Range(1,10000))=1
		_TerrainTextureUvPositions("TerrainTextureUvPositions", Vector) = (0.0, 0.0, 1.0, 1.0)
		_LodTexture("LodTexture", 2D) = "white" {}
		_BaseTrianglesCount("BaseTrianglesCount", float) = 32
		_LodTextureUvOffset("LodTextureUvOffset", Vector) = (0.0, 0.0, 0.0, 0.0)
	}
	SubShader 
	{
		Cull Off ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma  surface surf Standard noshadow nolightmap noambient nodynlightmap vertex:disp tessellate:tessFixed
			#pragma target 4.6

			#include "HeightColorTransform.hlsl"

			sampler2D _HeightmapTex; //Reference in Pass is necessary to let us use this variable in shaders
			float _MaxHeight;
			sampler2D _LodTexture;
			sampler2D _TessalationTex;
			sampler2D _NormalsTex;
			float4 _LodTextureUvOffset;
			float4 _TerrainTextureUvPositions;
			float _BaseTrianglesCount;

			struct Input{
				float2 uv_HeightmapTex : TEXCOORD0;
				float3 worldPos;
			};

			struct appdata {
				float4 vertex : POSITION;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
			};

			float2 uvInRing1(float2 inUv){
				return inUv/2 + _TerrainTextureUvPositions.xy;
			}

			float4 tessFixed(appdata v0, appdata v1, appdata v2){
				float avgCoord = (v0.texcoord + v1.texcoord + v2.texcoord)/3;
				float tesTexValue1 = tex2Dlod(_TessalationTex, float4( uvInRing1(avgCoord),0,0)).r;
				//float tesTexValue2 = tex2Dlod(_TessalationTex, float4(uvInRing1(v1.texcoord),0,0)).r;
				//float tesTexValue3 = tex2Dlod(_TessalationTex, float4(uvInRing1(v2.texcoord),0,0)).r;
				float lodTexValue = tex2Dlod(_LodTexture, float4( uvInRing1(avgCoord), 0, 0)).r;
				float tesFactor = tesTexValue1; 

				//return (1 + step(0.50,tesFactor)*(3))* step(0.01f, lodTexValue);
				//return 6+( step( fmod(avgCoord.x*  _BaseTrianglesCount;, 2) ,1));
				return 1;
			}

			float2 backToNormalizedUV(float2 uv, float2 textureSize){
				return uv / textureSize;
			}

			float takeValueFromTextureWithInterpolation(float2 uv, float2 textureSize){
				float2 positionInTextureSpace = uv * textureSize;
				float2 integerPosition = floor(positionInTextureSpace);

				float2 weight = frac(positionInTextureSpace);
				float4 bottom = lerp( 
						tex2Dlod(_HeightmapTex, float4( backToNormalizedUV(integerPosition, textureSize),0,0)),
						tex2Dlod(_HeightmapTex, float4( backToNormalizedUV(integerPosition + float2(1,0), textureSize),0,0)),
						weight.x);

				float4 top = lerp( 
						tex2Dlod(_HeightmapTex, float4( backToNormalizedUV(integerPosition + float2(0,1), textureSize),0,0)),
						tex2Dlod(_HeightmapTex, float4( backToNormalizedUV(integerPosition + float2(1,1), textureSize),0,0)),
						weight.x);
				
				return lerp(bottom.r, top.r, weight.y);
			}

			float getHeightmapTextureValue( float2 uvPos){ //position in object
				float2 ring1Texture;
				ring1Texture.x = (_TerrainTextureUvPositions[0] + uvPos.x*_TerrainTextureUvPositions[2]);
				ring1Texture.y = (_TerrainTextureUvPositions[1] + uvPos.y*_TerrainTextureUvPositions[3]);
			//	float heightmapValue = takeValueFromTextureWithInterpolation(ring1Texture, 2048).r; 
				float heightmapValue = decodeHeight(tex2Dlod(_HeightmapTex, float4( ring1Texture,0,0)));
				return heightmapValue;
			}

			//Our Vertex Shader 
			void disp (inout appdata v){
				float wholeTextureWidth = 2048;
				float delta = 0.0001;
				float2 uv_xy = v.texcoord.xy;
				float fmodXCoord = fmod(uv_xy.x, 1/ _BaseTrianglesCount);
				float fmodYCoord =  fmod(uv_xy.y, 1/_BaseTrianglesCount);
				bool pointIsInTesselationEdge = (fmodXCoord ==0) ||( fmodXCoord ==0) || 
					(fmodYCoord < delta) || (fmodYCoord > 1/_BaseTrianglesCount -delta); 
					v.vertex.y += getHeightmapTextureValue(v.texcoord.xy) ;
			}

			float3 decodeNormal( float3 input){
				return input*2-1;
			}

			float3 encodeNormal( float3 input ){
				return (input+1)/2;
			}

			 // Project the surface gradient (dhdx, dhdy) onto the surface (n, dpdx, dpdy)
			 float3 CalculateSurfaceGradient(float3 n, float3 dpdx, float3 dpdy, float dhdx, float dhdy) {
				float3 r1 = cross(dpdy, n);
				float3 r2 = cross(n, dpdx);
		   
				return (r1 * dhdx + r2 * dhdy) / dot(dpdx, r1);
			 }
		   
			 // Move the normal away from the surface normal in the opposite surface gradient direction
			 float3 PerturbNormal(float3 normal, float3 dpdx, float3 dpdy, float dhdx, float dhdy) {
				return normalize(normal - CalculateSurfaceGradient(normal, dpdx, dpdy, dhdx, dhdy));
			 }
		   
			 // Calculate the surface normal using screen-space partial derivatives of the height field
			 float3 CalculateSurfaceNormal(float3 position, float3 normal, float height) {
				float3 dpdx = ddx(position);
				float3 dpdy = ddy(position);
			 
				float dhdx = ddx(height);
				float dhdy = ddy(height);
		   
				return PerturbNormal(normal, dpdx, dpdy, dhdx, dhdy);
			 }


    
			void surf (Input IN, inout SurfaceOutputStandard o)  
			{
				float divisor = _LodTextureUvOffset.w;
				half2 globalUv = IN.uv_HeightmapTex/divisor + _LodTextureUvOffset.xy;

//				float color = tex2D(_LodTexture, (IN.uv_HeightmapTex/2)+ _LodTextureUvOffset.xy).r;
				float tesalationColor = tex2D(_TessalationTex, globalUv).r/2;
				float heightColor =  tex2D(_HeightmapTex, globalUv).r/2;


//				o.Albedo = tex2D(_NormalsTex, globalUv);
				
				float3 oldNormal = decodeNormal(tex2D(_NormalsTex, globalUv));
				float3 funkyNormals = encodeNormal(  CalculateSurfaceNormal(IN.worldPos, float3(0,1,0), IN.worldPos.y*2)); // i tutaj jest normal
				o.Albedo = encodeNormal(oldNormal) + (0.1);
				o.Normal = oldNormal;
				o.Alpha = 1; 
			}

			ENDCG
	} 
	FallBack "Diffuse"
}
