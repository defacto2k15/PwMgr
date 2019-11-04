Shader "Custom/Measurements/TestImagesRenderer" {
     Properties
     {
			_HatchMainRenderTex ("HatchMainRenderTex", 2D) = "blue" {}
			_IdRenderTex ("IdRenderTex", 2D) = "blue" {}
			_WorldPos1RenderTex ("WorldPos1RenderTex", 2D) = "blue" {}
			_WorldPos2RenderTex ("WorldPos2RenderTex", 2D) = "blue" {}
			_LinesWidthIllustrationTex ("LinesWidthIllustrationTex", 2D) = "blue" {}
			_LinesLayoutIllustrationTex ("LinesLayoutIllustrationTex", 2D) = "blue" {}
			_StrokesPixelCountIllustrationTex ("StrokesPixelCountIllustrationTex", 2D) = "blue" {}
			_BlockSpecificationIllustrationTex ("BlockSpecificationIllustrationTex", 2D) = "blue" {}
			_ArtisticMainRenderTex("ArtisticMainRenderTex", 2D) = "blue" {}

			_TextureSelector("TextureSelector", Range(0,9)) = 0.0
			_SliderToMain("SliderToMain",Range(0,1)) = 0.0

			_SelectedId("SelectedId", Int) = 0.0
			_SelectedIdMargin("SelectedIdMargin", Range(0,10000)) = 0.0
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
				float4 projPos : ANY_PROJ_POS;
			}; 


			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				return o;
			}

			sampler2D _ArtisticMainRenderTex;
			sampler2D _HatchMainRenderTex;
			sampler2D _IdRenderTex;
			sampler2D _WorldPos1RenderTex;
			sampler2D _WorldPos2RenderTex;
			sampler2D _LinesWidthIllustrationTex;
			sampler2D _LinesLayoutIllustrationTex;
			sampler2D _StrokesPixelCountIllustrationTex;
			sampler2D _BlockSpecificationIllustrationTex;
			
			int _TextureSelector;
			float _SliderToMain;
			int _SelectedId;
			int _SelectedIdMargin;

			float4 frag(v2f i) : COLOR {
				float2 uv = i.projPos.xy / i.projPos.w;

				float4 sp = 0;

				if (_TextureSelector == 0) {
					sp = tex2Dlod(_ArtisticMainRenderTex, float4(uv,0,0));
				}else
				if (_TextureSelector == 1) {
					sp = tex2Dlod(_HatchMainRenderTex, float4(uv,0,0));
				}else
				if (_TextureSelector == 2) {
					sp = tex2Dlod(_IdRenderTex, float4(uv,0,0));

					uint id = round(sp.r * 255) + round(sp.g * 255) * 256 + round(sp.b * 255) * 256 * 256 + round(sp.a * 255) * 256 * 256 * 256;

					if (id > 0) {
						float idF = (float)id;
						float idDifference = id - _SelectedId;
						int uIdDifference = (int)idDifference;
						if (abs(idDifference) < _SelectedIdMargin) {
							if (uIdDifference < 0) {
								sp = float4(1, 0, 0, 1);
							}
							else if (uIdDifference > 0) {
								sp = float4(0, 0, 1, 1);
							}
							else {
								sp = float4(0, 1, 0, 1);
							}
						}

					}

				}else
				if (_TextureSelector == 3) {
					sp = tex2Dlod(_WorldPos1RenderTex, float4(uv,0,0));
				}else
				if (_TextureSelector == 4) {
					sp = tex2Dlod(_WorldPos2RenderTex, float4(uv,0,0));
				}else
				if (_TextureSelector == 5) {
					sp = tex2Dlod(_LinesWidthIllustrationTex, float4(uv,0,0));
				}else
				if (_TextureSelector == 6) {
					sp = tex2Dlod(_LinesLayoutIllustrationTex, float4(uv,0,0));
				}else
				if (_TextureSelector == 7) {
					sp = tex2Dlod(_StrokesPixelCountIllustrationTex, float4(uv,0,0));
				}else
				if (_TextureSelector == 8) {
					sp = tex2Dlod(_BlockSpecificationIllustrationTex, float4(uv,0,0));
				}
				
				float4 mainC = tex2Dlod(_ArtisticMainRenderTex, float4(uv, 0, 0));
				return   lerp(mainC, sp, _SliderToMain);

				return sp;
			}
			ENDCG
		}
	}
}