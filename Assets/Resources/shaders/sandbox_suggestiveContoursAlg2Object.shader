Shader "Custom/Sandbox/SuggestiveContoursAlg2Object" {
    Properties {
    }
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
						
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
			};

			StructuredBuffer<float3> _InterpolatedNormalsBuffer;

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);

				float3 vertexWorldPos = mul(unity_ObjectToWorld , in_v.vertex);
				o.vertexWorldPos = vertexWorldPos;

				float3 objectNrm = _InterpolatedNormalsBuffer[vid];
				float3 worldNrm = normalize(mul((float3x3)unity_ObjectToWorld, normalize(objectNrm))); //n(p) z (1)
				o.worldNrm = worldNrm;

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.vertexWorldPos); //camera to vertex v(p) z (1)
				float intensity = dot(viewDir, normalize(i.worldNrm));

				return intensity;
			}
			ENDCG
		}
	}
}
