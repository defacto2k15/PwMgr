Shader "Custom/Sandbox/Filling/TamIdTest" {
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

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;

			};

			AppendStructuredBuffer<float3> appendBuffer;
			float size;
			float width;

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				return o;
			}

			float4 frag(v2f input) : SV_Target
			{
				float3 pos = float3(input.uv.xy,0);
				pos = (pos - 0.5)*2.0*size;
				pos.z = 0;

				int2 id = input.uv.xy * width;
				if (id.x % 2 == 0 && id.y % 2 == 0) {
					appendBuffer.Append(pos);
				}

				return 1;
			}


			 ENDCG



		}

	}
}

