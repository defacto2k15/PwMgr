Shader "Custom/Sandbox/Filling/WolowskiDebug" {
	Properties{
		_DebugTex("DebugTex", 2D) = "blue" {}
		_RotationQuant("RotationQuant", Range(0,1)) = 1
		_DebugScalar("DebugScalar", Range(0,1)) = 0
		_TextureMultiplier("TextureMultiplier", Range(0,10)) = 4
		_MarginSize("MarginSize", Range(0,1)) = 0.2
	}

		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100

			Pass
			{
				Tags{ "LightMode" = "ForwardBase" }

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
#include "common.txt"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldNrm : ANY_WORLD_NRM;
				float3 vertexWorldPos : ANY_VERTEX_WORLD_POS;
				float4 projPos : ANY_PROJ_POS;
			};

			StructuredBuffer<float3> _InterpolatedNormalsBuffer;
			sampler2D _DebugTex;
			float _RotationQuant;
			float _DebugScalar;
			float _TextureMultiplier;
			float _MarginSize;

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.projPos = ComputeScreenPos (o.pos);

				float3 vertexWorldPos = mul(unity_ObjectToWorld , in_v.vertex);
				o.vertexWorldPos = vertexWorldPos;

				float3 objectNrm = _InterpolatedNormalsBuffer[vid];
				o.worldNrm = UnityObjectToWorldNormal(normalize(objectNrm));

				return o;
			}

			half2 rotateUv( half2 pos, half rotation){
				float sinX = sin (rotation);
				float cosX = cos (rotation);
				float sinY = sin (rotation);
				float2x2 rotationMatrix = float2x2( cosX, -sinX, sinY, cosX);
				return mul(pos, rotationMatrix);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 lightWorldPos = float3(
					unity_4LightPosX0[0],
					unity_4LightPosY0[0],
					unity_4LightPosZ0[0]
					);
				float3 lightDir = normalize( lightWorldPos - i.vertexWorldPos);
				float3 alignmentVector = lightDir;

				float blendMargin = _MarginSize;
				float2 uv = (i.projPos.xy / i.projPos.w)*_TextureMultiplier;

				float3 worldSpaceDifferenceVector = normalize(normalize(i.worldNrm) - alignmentVector);
				
				float2 imageSpaceDifferenceVector = normalize(mul((float3x3)UNITY_MATRIX_VP, worldSpaceDifferenceVector)).xy;

				float alpha = acos(dot(imageSpaceDifferenceVector, float2(0, 1))); // in Wolowski was that this should be Y axis vector (0,1)
				alpha += 3.14;

				float quantIndex = round(alpha / _RotationQuant);

				float diff = quantIndex - (alpha / _RotationQuant);
				float aDiff = abs(diff);
				float neighbourWeight = invLerp(0.5-blendMargin, 0.5, aDiff);
				float neighbourQuantIndex = quantIndex - sign(diff);
				
				float ourQuantAlpha = round(quantIndex) * _RotationQuant;
				float ourIntensity = tex2D(_DebugTex, rotateUv(uv, ourQuantAlpha)).r;

				float neighbourQuantAlpha = round(neighbourQuantIndex) * _RotationQuant;
				float neighbourIntensity = tex2D(_DebugTex, rotateUv(uv, neighbourQuantAlpha)).r;

				fixed4 color = lerp(ourIntensity, neighbourIntensity, neighbourWeight);
				color = min(ourIntensity, neighbourIntensity * neighbourWeight + (1 - neighbourWeight));

				float lD = dot(normalize(i.worldNrm), lightDir);
				color += lD;

				return color ;
			}					

			ENDCG
		}
	}
}
