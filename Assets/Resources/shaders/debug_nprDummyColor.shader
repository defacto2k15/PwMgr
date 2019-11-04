Shader "Custom/Debug/NPR/DummyColor" {
	Properties
	{
		   _MainTex("", any) = "" {}
		   _Sigma("Sigma", Range(0,2)) = 0
		   _DummyColor("DummyColor", Vector) = (1.0,0.0,0.0, 1.0)
		   _ObjectID("ObjectID", Int) = 0
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
				#include "common.txt"

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
					float4 nz : TEXCOORD1;
				};

				struct MRTFragmentOutput
				{
					half4 dest0 : SV_Target0;
					half4 dest1 : SV_Target1;
					half4 dest2 : SV_Target2;
					half4 dest3 : SV_Target3;
				};

				MRTFragmentOutput make_MRTFragmentOutput(half4 dest0, half4 dest1, half4 dest2, half4 dest3) {
					MRTFragmentOutput o;
					o.dest0 = dest0;
					o.dest1 = dest1;
					o.dest2 = dest2;
					o.dest3 = dest3;
					return o;
				}

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _DummyColor;
				int _ObjectID;


				float2 out_ObjectID() {
					return PackUInt16Bit(_ObjectID);
				}

				v2f vert(appdata_base v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

					o.nz.xyz = COMPUTE_VIEW_NORMAL;
					o.nz.w = COMPUTE_DEPTH_01;

					return o;
				}

				MRTFragmentOutput frag(v2f i) : SV_Target
				{
					MRTFragmentOutput o = make_MRTFragmentOutput(0,0,0,0);
					o.dest0 = _DummyColor;
					o.dest1 = EncodeDepthNormal(i.nz.w, i.nz.xyz);
					o.dest0.xy = out_ObjectID();

					return o;
				}

				ENDCG
			}
		   }
}