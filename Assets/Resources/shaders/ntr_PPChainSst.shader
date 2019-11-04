Shader "Custom/NPR/PostProcessingChain/Sst" {
     Properties
     {
			_MainTex ("", any) = "" {}
     }
	SubShader{
		Tags { "RenderType" = "Opaque" }
		Pass {
			Fog { Mode Off }
			Cull Off
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "noise.hlsl"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : TEXCOORD0;
				float3 worldDirection : TEXCOORD1;
			}; 

			float4x4 _ClipToWorld;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				COMPUTE_EYEDEPTH(o.projPos.z);

				float4 clip = float4(o.pos.xy, 0.0, 1.0);
				o.worldDirection = mul(_ClipToWorld, clip) - _WorldSpaceCameraPos;

				return o;
			}

			sampler2D _CameraDepthTexture;
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;


// KOD Z // by Jan Eric Kyprianidis <www.kyprianidis.com> 
// Tutaj obliczamy tensory EFG
			float4 frag(v2f i) : COLOR {
				const float PI = 3.14159;
				float3 originalColor = tex2D(_MainTex, i.projPos.xy).rgb;
				float2 uv = i.projPos.xy;

				int2 srcSize = _MainTex_TexelSize.zw;

				float2 d = 1.0 / srcSize;

				float3 c = tex2D(_MainTex, uv).xyz;
				float3 u = (
					   -1.0 * tex2D(_MainTex, uv + float2(-d.x, -d.y)).xyz +
					   -2.0 * tex2D(_MainTex, uv + float2(-d.x,  0.0)).xyz + 
					   -1.0 * tex2D(_MainTex, uv + float2(-d.x,  d.y)).xyz +
					   +1.0 * tex2D(_MainTex, uv + float2( d.x, -d.y)).xyz +
					   +2.0 * tex2D(_MainTex, uv + float2( d.x,  0.0)).xyz + 
					   +1.0 * tex2D(_MainTex, uv + float2( d.x,  d.y)).xyz
					   ) / 4.0;

				float3 v = (
					   -1.0 * tex2D(_MainTex, uv + float2(-d.x, -d.y)).xyz + 
					   -2.0 * tex2D(_MainTex, uv + float2( 0.0, -d.y)).xyz + 
					   -1.0 * tex2D(_MainTex, uv + float2( d.x, -d.y)).xyz +
					   +1.0 * tex2D(_MainTex, uv + float2(-d.x,  d.y)).xyz +
					   +2.0 * tex2D(_MainTex, uv + float2( 0.0,  d.y)).xyz + 
					   +1.0 * tex2D(_MainTex, uv + float2( d.x,  d.y)).xyz
					   ) / 4.0;

				float3 outSST = normalize(float3(dot(u, u), dot(v, v), dot(u, v)));


				return float4(outSST.xyz, 1);
			}
			ENDCG
		}
	}
}