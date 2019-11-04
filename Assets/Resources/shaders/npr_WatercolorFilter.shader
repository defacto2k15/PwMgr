Shader "Custom/NPR/WatercolorFilter" {
     Properties
     {
			_MainTex ("", any) = "" {}
			_FilterFlag("FilterFlag", Range(0,1)) = 0
			_ShowLines("ShowLines", Range(0,1)) = 1
			_ShowColors("ShowColors", Range(0,1)) = 1
			_ColorSharpness("ColorSharpness", Range(0,2)) = 1
			_NoiseTex("NoiseTex", 2D) = "white" {}
     }
	SubShader{
	// Na bazie kodu https://www.shadertoy.com/view/ltyGRV
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
			}; 


			float4x4 _ClipToWorld;

			sampler2D _CameraDepthTexture;
			sampler2D _CameraDepthNormalsTexture;
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D _NoiseTex;

			float _FilterFlag;
			float _ShowLines;
			float _ShowColors;
			float _ColorSharpness;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				return o;
			}

			#define Res0  (_MainTex_TexelSize.zw)

			float4 getCol(float2 pos)
			{
					float2 uv=pos/Res0;
					float4 c1 = tex2D(_MainTex,uv);
					float4 c2 =(.4); // gray on greenscreen
					float d = clamp(dot(c1.xyz,float3(-0.5,1.0,-0.5)),0.0,1.0);
					return lerp(c1,c2,1.8*d);
			}

			float4 getCol2(float2 pos)
			{
					float2 uv=pos/Res0;
					float4 c1 = tex2D(_MainTex,uv);
					float4 c2 =(1.5); // bright white on greenscreen
					float d = clamp(dot(c1.xyz,float3(-0.5,1.0,-0.5)),0.0,1.0);
					return lerp(c1,c2,1.8*d);
			}

			float2 getGrad(float2 pos,float delta)
			{
					float2 d=float2(delta,0);
					return float2(
							dot((getCol(pos+d.xy)-getCol(pos-d.xy)).xyz,float3(.333,.333,.333)),
							dot((getCol(pos+d.yx)-getCol(pos-d.yx)).xyz,float3(.333,.333,.333))
					)/delta;
			}

			float2 getGrad2(float2 pos,float delta)
			{
					float2 d=float2(delta,0);
					return float2(
							dot((getCol2(pos+d.xy)-getCol2(pos-d.xy)).xyz,float3(.333,.333,.333)),
							dot((getCol2(pos+d.yx)-getCol2(pos-d.yx)).xyz,float3(.333,.333,.333))
					)/delta;
			}


			float4 getRand(float2 pos)
			{
				return tex2D(_NoiseTex, pos/_MainTex_TexelSize.zw);
			}

			float htPattern(float2 pos)
			{
					float p;
					float r=getRand(pos*.4/.7*1.).x;
					p=clamp((pow(r+.3,2.)-.45),0.,1.);
					return p;
			}

			float getVal(float2 pos, float level)
			{
					return length(getCol(pos).xyz)+0.0001*length(pos-0.5*Res0);
					return dot(getCol(pos).xyz,float3(.333,.333,.333));
			}
					
			float4 getBWDist(float2 pos)
			{
					return (smoothstep(.9,1.1,getVal(pos,0.)*.9+htPattern(pos*.7)));
			}

			#define SampNum 12

			#define N(a) (a.yx*float2(1,-1))

			float4 frag(v2f i) : COLOR {
				int2 srcSize = _MainTex_TexelSize.zw;
				float2 uv = i.projPos.xy;
				float3 originalColor = tex2D(_MainTex, uv);

				float2 pos= uv *  _MainTex_TexelSize.zw;
				float2 pos2=pos;
				float2 pos3=pos;
				float2 pos4=pos;
				float2 pos0=pos;
				float3 col=(0);
				float3 col2=(0);
				float cnt=0.0;
				float cnt2=0.;
				for(int i=0;i<1*SampNum;i++)
				{   // TODO zrozum to!
						// gradient for outlines (gray on green screen)
						float2 gr =getGrad(pos, 2.0)+.0001*(getRand(pos ).xy-.5);
						float2 gr2=getGrad(pos2,2.0)+.0001*(getRand(pos2).xy-.5);
						
						// gradient for wash effect (white on green screen)
						float2 gr3=getGrad2(pos3,2.0)+.0001*(getRand(pos3).xy-.5);
						float2 gr4=getGrad2(pos4,2.0)+.0001*(getRand(pos4).xy-.5);
						
						float grl=clamp(10.*length(gr),0.,1.);
						float gr2l=clamp(10.*length(gr2),0.,1.);

						// outlines:
						// stroke perpendicular to gradient
						pos +=.8 *normalize(N(gr));
						pos2-=.8 *normalize(N(gr2));
						float fact=1.-float(i)/float(SampNum);
						col+=fact*lerp(float3(1.2,1.2,1.2),getBWDist(pos).xyz*2.,grl);
						col+=fact*lerp(float3(1.2,1.2,1.2),getBWDist(pos2).xyz*2.,gr2l);
						
						// colors + wash effect on gradients:
						// color gets lost from dark areas
						pos3+=.25*normalize(gr3)+.5*(getRand(pos0*.07).xy-.5);
						// to bright areas
						pos4-=.5 *normalize(gr4)+.5*(getRand(pos0*.07).xy-.5);
						
						float f1=3.*fact;
						float f2=4.*(.7-fact); 
						col2+=f1*(getCol2(pos3).xyz+.25+.4*getRand(pos3*1.).xyz);
						col2+=f2*(getCol2(pos4).xyz+.25+.4*getRand(pos4*1.).xyz);
						
						cnt2+=f1+f2;
						cnt+=fact;
				}
				// normalize
				col/=cnt*2.5;
				col2/=cnt2*1.65;
				
				// outline + color
				col = clamp(clamp(col*.9+.1,0.,1.)*col2,0.,1.);
				
				//  paper color and grain turned OFF					
				col = col*float3(.93,0.93,0.85) +.15*getRand(pos0*2.5).x;
				// vignetting, removed

				float4 outColor = float4((lerp(1, col, _ShowLines ) * lerp(1, col2, _ShowColors)), 1);
				outColor = sqrt(outColor); // make image lighter

				return float4(lerp(outColor, originalColor, _FilterFlag) , 1); 
			}
			ENDCG
		}
	}
}