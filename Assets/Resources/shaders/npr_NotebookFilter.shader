Shader "Custom/NPR/NotebookFilter" {
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
	// Na bazie kodu https://www.shadertoy.com/view/XtVGD1
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

			struct pixelSample {
				float3 normal;
				float depth;
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

			float getRand1(float2 pos)
			{
				return (1+noise1D(pos.x*32 + pos.y*21.11))/2;
			}

			float4 getRand(float2 pos)
			{
				return tex2D(_NoiseTex, frac(pos*1.11));
			}

			float4 getCol(float2 pos)
			{
				// this generates margins, we not use
				float2 uv=((pos-_MainTex_TexelSize.zw*.5)/_MainTex_TexelSize.w*_MainTex_TexelSize.w)/_MainTex_TexelSize.zw+.5;
				float4 c1=tex2D(_MainTex,uv);

				// this has to generate blue lines, we not use
				float4 e=smoothstep(float4(-0.05,-0.05,-0.05, -0.05),float4(-0.0, -0.0, -0.0, -0.0),float4(uv,float2(1,1)-uv));
				c1=lerp(float4(1,1,1,0),c1,e.x*e.y*e.z*e.w);

				// this change colour
				float d=clamp(dot(c1.xyz,float3(-.5,1.,-.5)),0.0,1.0);
				float4 c2=float4(.7, .7, .7, .7);
				return min(lerp(c1,c2,1.8*d),.7);
			}

			// colour balance change
			float4 getColHT(float2 pos)
			{
				return smoothstep(.95,1.05,getCol(pos)*.8+.2+getRand(pos*.7)*0.9);
			}

			// used to calculate gradient
			float getVal(float2 pos)
			{
				float4 c=getCol(pos);
				return pow(dot(c.xyz,float3(.333, .333, .333)),1.)*1.;
			}

			// colour? change gradient
			float2 getGrad(float2 pos, float eps)
			{
				float2 d=float2(eps,0);
				return float2(
						getVal(pos+d.xy)-getVal(pos-d.xy),
						getVal(pos+d.yx)-getVal(pos-d.yx)
				)/eps/2.;
			}

			#define AngleNum 3
			#define SampNum 16
			#define PI2 6.28318530717959

			float4 frag(v2f i) : COLOR {
				int2 srcSize = _MainTex_TexelSize.zw;
				float2 uv = i.projPos.xy;
				float3 originalColor = tex2D(_MainTex, uv);


				float2 pos = uv * _MainTex_TexelSize.zw; //+4.0*sin(iTime*1.*float2(1,1.7))*iResolution.y/400.;
				// col was float3, but only .x was used
				// col is the black lines!
				float col = (0);
				
				// col2 are black colors
				float3 col2 = (0);
				float sum= 0;
				for(int i=0;i<AngleNum;i++)
				{
						float ang=PI2/float(AngleNum)*(float(i)+.8);
						float2 v=float2(cos(ang),sin(ang));
						for(int j=0;j<SampNum;j++)
						{
							// the latter elements are params
							// delta pos
							float2 dpos  = v.yx*float2(1,-1)*float(j)*_ColorSharpness;
							float2 dpos2 = v.xy*float(j*j)/float(SampNum)*.5*_ColorSharpness;
							float2 g;
							float fact;
							float fact2;

							for(float s=-1.;s<=1.;s+=2.)
							{
								float2 pos2=pos+s*dpos+dpos2;
								float2 pos3=pos+(s*dpos+dpos2).yx*float2(1,-1)*2.;
								g=getGrad(pos2,.4);
								fact=dot(g,v)-.5*abs(dot(g,v.yx*float2(1,-1)))/**(1.-getVal(pos2))*/;
								fact2=dot(normalize(g+float2(.0001, .0001)),v.yx*float2(1,-1));
									
								fact=clamp(fact,0.,.05);
								fact2=abs(fact2);
								
								fact*=1.-float(j)/float(SampNum);
								col += fact;
								col2 += fact2*getColHT(pos3).xyz;
								sum+=fact2;
							}
						}
				}
				// possible param
				col /= float(SampNum*AngleNum)*.75/sqrt(_MainTex_TexelSize.w);
				col2 /= sum;
				col *=(.6+.8*getRand1(pos*.7).x);
				col =1.-col;
				col *=col *col;

				// karo param was for notebook lines
				// Image vignette was here, it is not used
				float4 outColor = float4((lerp(1, col, _ShowLines ) * lerp(1, col2, _ShowColors)), 1);

				return float4(lerp(outColor, originalColor, _FilterFlag) , 1); 
			}
			ENDCG
		}
	}
}