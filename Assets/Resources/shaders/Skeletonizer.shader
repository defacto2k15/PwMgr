Shader "Custom/Skeletonizer" {
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
			#include "filling_common.txt"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : TEXCOORD0;
			}; 

			float4x4 _ClipToWorld;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);

				return o;
			}
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			float pixel(int2 thisCoords, int dx, int dy) {
				float2 uv = memberwiseMultiplyF2(thisCoords + int2(dx, dy) + 0.5/_MainTex_TexelSize.zw , 1 / _MainTex_TexelSize.zw);
				return tex2D(_MainTex, uv).r;
			}

			int exists(int2 thisCoords, int dx, int dy) {
				return int(pixel(thisCoords, dx, dy) < 0.5);
			}

			int neighbors(int2 thisCoords) {
				return  exists(thisCoords,-1, +1) +
					exists(thisCoords,0, +1) +
					exists(thisCoords,+1, +1) +
					exists(thisCoords,-1, 0) +
					exists(thisCoords,+1, 0) +
					exists(thisCoords,-1, -1) +
					exists(thisCoords,0, -1) +
					exists(thisCoords,+1, -1);
			}

			int transitions(int2 thisCoords) {
				return int(
					clamp(float(exists(thisCoords,-1, +1)) - float(exists(thisCoords,0, +1)), 0., 1.) +  // (-1,+1) -> (0,+1)
					clamp(float(exists(thisCoords,0, +1)) - float(exists(thisCoords,+1, +1)), 0., 1.) +  // (0,+1) -> (+1,+1)
					clamp(float(exists(thisCoords,+1, +1)) - float(exists(thisCoords,+1, 0)), 0., 1.) +  // (+1,+1) -> (+1,0)
					clamp(float(exists(thisCoords,+1, 0)) - float(exists(thisCoords,+1, -1)), 0., 1.) +  // (+1,0) -> (+1,-1)
					clamp(float(exists(thisCoords,+1, -1)) - float(exists(thisCoords,0, -1)), 0., 1.) +  // (+1,-1) -> (0,-1)
					clamp(float(exists(thisCoords,0, -1)) - float(exists(thisCoords,-1, -1)), 0., 1.) +  // (0,-1) -> (-1,-1)
					clamp(float(exists(thisCoords,-1, -1)) - float(exists(thisCoords,-1, 0)), 0., 1.) +  // (-1,-1) -> (-1,0)
					clamp(float(exists(thisCoords,-1, 0)) - float(exists(thisCoords,-1, +1)), 0., 1.)    // (-1,0) -> (-1,+1)          
					);
			}

			bool MarkedForRemoval(int2 thisCoords) {
				int neib = neighbors(thisCoords);
				int tran = transitions(thisCoords);

				if (exists(thisCoords,0, 0) == 0  // do not remove if already white
					|| neib == 0      // do not remove an isolated point
					|| neib == 1      // do not remove tip of a line
					|| neib == 7      // do not remove located in concavity
					|| neib == 8      // do not remove not a boundary point
					|| tran >= 2      // do not remove on a bridge connecting two or more edge pieces
					)
					return false;
				else
					return true;
			}


			float4 frag(v2f i) : COLOR{
				float2 uv = i.projPos.xy;
				int2 thisCoords = int2(round ( float2(uv.x * _MainTex_TexelSize.z, uv.y * _MainTex_TexelSize.w) - 0.5/_MainTex_TexelSize.zw   ) );
				bool remove = MarkedForRemoval(thisCoords);
				float4 curr = tex2D(_MainTex,uv);
				float rt = (remove)? remove:((curr.r > 0.05)? 1:curr);
				return float4(rt, curr.g, remove, 1);
			}
			ENDCG
		}

		Pass {
				// PASS THAT CHANGES InputMainHatchTexture to black/white texture
			Fog { Mode Off }
			Cull Off
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "noise.hlsl"
			#include "filling_common.txt"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : TEXCOORD0;
			}; 

			float4x4 _ClipToWorld;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);

				return o;
			}
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			float4 frag(v2f i) : COLOR {
				float2 uv = i.projPos.xy;
				float4 c = tex2D(_MainTex, uv);
				if (c.r > 0) {
					return 0;
				}
				else {
					return 1;
				}
			}
			ENDCG
		}

	}
}