using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NPR.PostProcessing
{
    public class NPRFilterRenderer : MonoBehaviour
    {
        public Material Material;

        private Camera _camera;

        private Material SSTMaterial;
        private Material GaussMaterial;
        private Material TFMMaterial;
        private Texture2D NoiseTexture;

        private RenderTexture SstTexture;
        private RenderTexture GaussTexture;
        private RenderTexture TfmTexture;

        [Range(0,1)]
        public float SmoothingParam = 0.5f; 
        [Range(0,1)]
        public float  SigmaFactor = 0.25f;

        [Range(0, 10)]
        public float GaussSigma = 2.0f;


        public void Start()
        {
            _camera = GetComponent<Camera>();
            _camera.depthTextureMode = DepthTextureMode.Depth;
            ResetKernelTexture();

            SSTMaterial = new Material(Shader.Find("Custom/NPR/PostProcessingChain/Sst" ));
            GaussMaterial = new Material(Shader.Find("Custom/NPR/PostProcessingChain/Gauss" ));
            TFMMaterial = new Material(Shader.Find("Custom/NPR/PostProcessingChain/Tfm" ));

            int width = Screen.width;
            int height = Screen.height;

            SstTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            SstTexture.Create();
            GaussTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            GaussTexture.Create();
            TfmTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            TfmTexture.Create();
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            float sigma = GaussSigma;

            Graphics.Blit(src, SstTexture, SSTMaterial);
            GaussMaterial.SetFloat("_Sigma", sigma);
            Graphics.Blit(SstTexture, GaussTexture, GaussMaterial);
            Graphics.Blit(GaussTexture, TfmTexture, TFMMaterial);

            Material.SetTexture("_TfmTex", TfmTexture);

            Graphics.Blit(src, dest, Material);
        }

        public void ResetKernelTexture()
        {
            var noiseTex = CreateNoiseTexture(Screen.width, Screen.height);
            NoiseTexture = noiseTex;

            CreateAndSetKernelTextures();
        }

        private void CreateAndSetKernelTextures()
        {
            const int N = 8; 
            float smoothing = SmoothingParam; 
            const int krnl_size = 32;
            float sigma = SigmaFactor * (krnl_size - 1);

            var kernels = new float[4][];
            for (int k = 0; k < 4; ++k)
            {
                kernels[k] = new float[krnl_size * krnl_size];
                make_sector(kernels[k], k, N, krnl_size, sigma, smoothing * sigma);
            }
            
            var tex0 = new Texture2D(krnl_size, krnl_size, TextureFormat.RFloat, false);
            tex0.wrapMode = TextureWrapMode.Clamp;
            tex0.filterMode = FilterMode.Point;

            for (int x = 0; x < krnl_size; x++)
            {
                for (int y = 0; y < krnl_size; y++)
                {
                    tex0.SetPixel(x,y, new Color(kernels[0][x + y * krnl_size],0,0));
                }
            }
            tex0.Apply();
            Material.SetTexture("_KernelTex", tex0);

            var tex4 = new Texture2D(krnl_size, krnl_size, TextureFormat.RGBA32, false);
            tex4.wrapMode = TextureWrapMode.Clamp;
            tex4.filterMode = FilterMode.Point;
            for (int x = 0; x < krnl_size; x++)
            {
                for (int y = 0; y < krnl_size; y++)
                {
                    var indexInKernel = x + y * krnl_size;
                    tex4.SetPixel(x,y, new Color(
                        kernels[0][indexInKernel],
                        kernels[1][indexInKernel],
                        kernels[2][indexInKernel],
                        kernels[3][indexInKernel]
                        ));
                }
            }

            tex4.Apply();
            Material.SetTexture("_Kernel4Tex", tex4);
        }


        private void make_sector(float[] krnl, int k, int N, int size, float sigma_r, float sigma_s)
        {
            int p = 0;
            for (int j = 0; j < size; ++j)
            {
                for (int i = 0; i < size; ++i)
                {
                    double x = i - 0.5 * size + 0.5;
                    double y = j - 0.5 * size + 0.5;
                    double r = Math.Sqrt(x * x + y * y);

                    double a = 0.5 * Math.Atan2(y, x) / Math.PI + k * 1.0 / N;
                    if (a > 0.5)
                        a -= 1.0;
                    if (a < -0.5)
                        a += 1.0;

                    if ((Math.Abs(a) <= 0.5 / N) && (r < 0.5 * size))
                    {
                        krnl[p] = 1;
                    }
                    else
                    {
                        krnl[p] = 0;
                    }

                    ++p;
                }
            }

            gauss_filter(krnl, size, size, sigma_s);

            p = 0;
            double mx = 0.0; // max value in krenel
            for (int j = 0; j < size; ++j)
            {
                for (int i = 0; i < size; ++i)
                {
                    double x = i - 0.5 * size + 0.5;
                    double y = j - 0.5 * size + 0.5;
                    double r = Math.Sqrt(x * x + y * y);
                    krnl[p] *= (float)Math.Exp(-0.5 * r * r / sigma_r / sigma_r);
                    if (krnl[p] > mx) mx = krnl[p];
                    ++p;
                }
            }

            p = 0;
            for (int j = 0; j < size; ++j)
            {
                for (int i = 0; i < size; ++i)
                {
                    krnl[p] /= (float)mx;
                    ++p;
                }
            }
        }

        private static void gauss_filter(float[] data, int width, int height, float sigma)
        {
            double twoSigma2 = 2.0 * sigma * sigma;
            int halfWidth = (int) Mathf.Ceil((float) (2.0 * sigma));

            float[] src_data = data.ToArray();

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float sum = 0;
                    float w = 0;

                    for (int i = -halfWidth; i <= halfWidth; ++i)
                    {
                        for (int j = -halfWidth; j <= halfWidth; ++j)
                        {
                            int xi = x + i;
                            int yj = y + j;
                            if ((xi >= 0) && (xi < width) && (yj >= 0) && (yj < height))
                            {
                                double r = Math.Sqrt((float) (i * i + j * j));
                                double k = Math.Exp(-r * r / twoSigma2);
                                w += (float)k;
                                sum += (float)k * src_data[xi + yj * width];
                            }
                        }
                    }

                    data[x + y * width] = sum / w;
                }
            }
        }

        private static Texture2D CreateNoiseTexture(int w, int h)
        {
            var random = new System.Random();
            float[] noise = new float[w * h];

            int p = 0;
            for (int j = 0; j < h; ++j)
            {
                for (int i = 0; i < w; ++i)
                {
                    noise[p++] = (float) (0.5f + 2.0f * ((float) random.NextDouble() - 0.5));
                }
            }

            p = 0;
            for (int j = 0; j < h; ++j)
            {
                noise[p] = (3 * noise[p] + noise[p + 1]) / 4;
                ++p;
                for (int i = 1; i < w - 1; ++i)
                {
                    noise[p] = (noise[p - 1] + 2 * noise[p] + noise[p + 1]) / 4;
                    ++p;
                }

                noise[p] = (noise[p - 1] + 3 * noise[p]) / 4;
                ++p;
            }

            p = 0;
            for (int i = 0; i < w; ++i)
            {
                noise[p] = (3 * noise[p] + noise[p + w]) / 4;
                ++p;
            }

            for (int j = 1; j < h - 1; ++j)
            {
                for (int i = 0; i < w; ++i)
                {
                    noise[p] = (noise[p - w] + 2 * noise[p + 0] + noise[p + w]) / 4;
                    ++p;
                }
            }

            for (int i = 0; i < w; ++i)
            {
                noise[p] = (noise[p - w] + 3 * noise[p + 0]) / 4;
                ++p;
            }

            var tex = new Texture2D(w,h, TextureFormat.RFloat, false);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    tex.SetPixel(x,y, new Color(noise[x + y * w],0,0,0));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
