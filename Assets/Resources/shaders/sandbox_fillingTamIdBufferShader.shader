Shader "Custom/Sandbox/Filling/TamIdBufferShader" {
	Properties{
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
			#pragma target 5.0

			//TEST ONLY, NOT USED NOW
			StructuredBuffer<float3> buffer;

			struct v2f {
				float4 pos : SV_POSITION;
			};

			v2f vert(uint id : SV_VertexID) {
				float4 pos = float4(buffer[id], 1);

				v2f o;
				o.pos = UnityObjectToClipPos(pos);
				return o;
			}

			float4 frag(v2f input) : SV_Target
			{
				return 1;
			}


			 ENDCG



		}

	}
}

