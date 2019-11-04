using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ComputeShaders.Templating;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Assets.NPR.Filling.Solid
{
    public class SolidPatternTextureGeneratorGO : MonoBehaviour
    {
        public string Path;
        public float CooldownTime = 0.5f;
        public int SideSize = 64;
        public bool CreateAsset = true;
        public SolidTextureInside TextureInside = SolidTextureInside.Checkerboard;
        public int SlicesPerDispatch = 4;

        private SolidTextureGeneratorUsingComputeShader _generator;
        private UnityThreadComputeShaderExecutorObject _unityThreadComputeShaderExecutorObject;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            _unityThreadComputeShaderExecutorObject = new UnityThreadComputeShaderExecutorObject();
            _generator = new SolidTextureGeneratorUsingComputeShader("npr_solidTexture_noise_comp", _unityThreadComputeShaderExecutorObject);

            StartCoroutine(GenerateSolidTexture());
        }

        IEnumerator GenerateSolidTexture()
        {
            int i = 0;
            foreach (var tex in _generator.Generate(SideSize, SlicesPerDispatch, TextureInside))
            {
                Debug.Log("Loop "+i);
                i++;

                if (!tex.Done)
                {
                    yield return new WaitForSeconds(CooldownTime);
                    GetComponent<MeshRenderer>().material.SetTexture("_SolidTex", tex.SolidTexture);
                }
                else
                {
                    if (CreateAsset)
                    {
                        var transformer = new RenderCubeToTexture3DTransformer(_unityThreadComputeShaderExecutorObject);
                        var tex3D = transformer.Transform(tex.SolidTexture, SideSize, SideSize);
                        GetComponent<MeshRenderer>().material.SetTexture("_SolidTex", tex3D);
                        MyAssetDatabase.CreateAndSaveAsset(tex3D, Path);
                    }
                }
            }
        }
    }

    public class SolidTextureGeneratingStatus
    {
        public Texture SolidTexture;
        public bool Done;
    }

    public enum SolidTextureInside
    {
        Noise,Checkerboard
    }

    public class SolidTextureGeneratorUsingComputeShader
    {
        private readonly string _shaderName;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;

        public SolidTextureGeneratorUsingComputeShader(string shaderName, UnityThreadComputeShaderExecutorObject shaderExecutorObject)
        {
            _shaderName = shaderName;
            _shaderExecutorObject = shaderExecutorObject;
        }

        public IEnumerable<SolidTextureGeneratingStatus> Generate(int sideSize, int slicesPerDistatch, SolidTextureInside inside)
        {
            string kernelName;
            if (inside == SolidTextureInside.Checkerboard)
            {
                kernelName = "CS_Checkerboard";
            }
            else if (inside == SolidTextureInside.Noise)
            {
                kernelName = "CS_FractalNoise";
            }
            else
            {
                Preconditions.Fail("Unknown solidTextureInside "+inside);
                return null;
            }
            return Generate(sideSize, sideSize * sideSize * slicesPerDistatch, kernelName);
        }

        private IEnumerable<SolidTextureGeneratingStatus> Generate(int sideSize, int maxPixelsPerDispatch, string kernelName)
        {
            var slicesPerDispatch = (int)Math.Floor((float) maxPixelsPerDispatch / (sideSize * sideSize));
            Preconditions.Assert(slicesPerDispatch > 0 , "MaxPixelsPerDispatch must be bigger, now it is 0");

            var texture = new RenderTexture(sideSize, sideSize, 0, RenderTextureFormat.R8,RenderTextureReadWrite.Default);
            texture.dimension = TextureDimension.Tex3D;
            texture.enableRandomWrite = true;
            texture.volumeDepth = sideSize;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.Create();

            for (int i = 0; i < Mathf.CeilToInt((float)sideSize/slicesPerDispatch); i++)
            {
                ExecuteDispatch(sideSize, i, slicesPerDispatch, texture, kernelName).Wait();
                yield return new SolidTextureGeneratingStatus()
                {
                    Done = false,
                    SolidTexture = texture
                };
            }
            yield return new SolidTextureGeneratingStatus()
            {
                Done = true,
                SolidTexture = texture
            };
        }

        private async Task ExecuteDispatch(int sideSize, int dispatchOffset, int slicesPerDispatch, RenderTexture texture, string kernelName)
        {
            var parametersContainer = new ComputeShaderParametersContainer();

            MultistepComputeShader computeShader =
                new MultistepComputeShader(ComputeShaderUtils.LoadComputeShader(_shaderName), new IntVector3(sideSize, sideSize, slicesPerDispatch));

            var kernel = computeShader.AddKernel(kernelName);
            var allKernels = new List<MyKernelHandle>()
            {
                kernel
            };

            var newTextureId = parametersContainer.AddExistingComputeShaderTexture(texture);

            computeShader.SetGlobalUniform("g_SideSize",sideSize);
            computeShader.SetGlobalUniform("g_SlicesPerDispatch", slicesPerDispatch);
            computeShader.SetGlobalUniform("g_DispatchOffset", dispatchOffset);
            computeShader.SetTexture("_OutTexture3D", newTextureId, allKernels);

            ComputeBufferRequestedOutParameters outParameters = new ComputeBufferRequestedOutParameters( new List<MyComputeShaderTextureId>() {newTextureId}, new List<MyComputeBufferId> {});
            await _shaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
            {
                ParametersContainer = parametersContainer,
                OutParameters = outParameters,
                WorkPacks = new List<ComputeShaderWorkPack>()
                {
                    new ComputeShaderWorkPack()
                    {
                        Shader = computeShader, 
                        DispatchLoops = new List<ComputeShaderDispatchLoop>()
                        {
                            new ComputeShaderDispatchLoop()
                            {
                                DispatchCount = 1,
                                KernelHandles = allKernels
                            }
                        }
                    },
                }
            });
        }
    }

    public class RenderCubeToTexture3DTransformer
    {
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;

        public RenderCubeToTexture3DTransformer(UnityThreadComputeShaderExecutorObject shaderExecutorObject)
        {
            _shaderExecutorObject = shaderExecutorObject;
        }

        public Texture Transform(Texture cube, int slicesCount, int sideLength)
        {
            var cpuBuffer = new Texture2D(sideLength,sideLength, TextureFormat.RGBA32, false);
            cpuBuffer.Apply();

            var renderBuffer = new RenderTexture(sideLength, sideLength, 0, RenderTextureFormat.ARGB32);
            renderBuffer.enableRandomWrite = true;
            renderBuffer.Create();

            var array = new Color[sideLength * sideLength * slicesCount];
            for (int i = 0; i < slicesCount; i++)
            {
                MoveSliceToBuffer(renderBuffer, cube, sideLength, i);
                MoveRenderToCpu(renderBuffer, cpuBuffer);
                MoveSliceToArray(cpuBuffer,array, i);
            }
            GraphicsFormat textureFormat = GraphicsFormat.R8G8B8A8_UNorm;
            var tex3d = new Texture3D(sideLength, sideLength, slicesCount, textureFormat, TextureCreationFlags.None);
            tex3d.SetPixels(array);
            tex3d.Apply();
            return tex3d;
        }

        private void MoveRenderToCpu(RenderTexture renderBuffer, Texture2D cpuBuffer)
        {
            RenderTexture.active = renderBuffer;
            cpuBuffer.ReadPixels(new Rect(0, 0, cpuBuffer.width, cpuBuffer.height), 0, 0);
            cpuBuffer.Apply();
        }

        private void MoveSliceToArray(Texture2D buffer, Color[] arr, int sliceIndex)
        {
            for (int x = 0; x < buffer.width; x++)
            {
                for (int y = 0; y < buffer.height; y++)
                {
                    arr[sliceIndex * buffer.width * buffer.height + x * buffer.width + y] = buffer.GetPixel(x, y);
                }
            }
        }

        private void MoveSliceToBuffer(Texture buffer, Texture cube, int sideLength, int sliceIndex)
        {
            var parametersContainer = new ComputeShaderParametersContainer();

            MultistepComputeShader computeShader =
                new MultistepComputeShader(ComputeShaderUtils.LoadComputeShader("util_moveCubeSlice_comp"), new IntVector3(sideLength, sideLength, 1));

            var kernel = computeShader.AddKernel("CS_Main");
            var allKernels = new List<MyKernelHandle>()
            {
                kernel
            };

            var bufferTexId = parametersContainer.AddExistingComputeShaderTexture(buffer);
            var cubeTexId = parametersContainer.AddExistingComputeShaderTexture(cube);

            computeShader.SetGlobalUniform("g_SliceIndex", sliceIndex);
            computeShader.SetTexture("_BufferTexture", bufferTexId, allKernels);
            computeShader.SetTexture("_CubeTexture", cubeTexId, allKernels);

            ComputeBufferRequestedOutParameters outParameters = new ComputeBufferRequestedOutParameters( new List<MyComputeShaderTextureId>() {}, new List<MyComputeBufferId> {});
            _shaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
            {
                ParametersContainer = parametersContainer,
                OutParameters = outParameters,
                WorkPacks = new List<ComputeShaderWorkPack>()
                {
                    new ComputeShaderWorkPack()
                    {
                        Shader = computeShader, 
                        DispatchLoops = new List<ComputeShaderDispatchLoop>()
                        {
                            new ComputeShaderDispatchLoop()
                            {
                                DispatchCount = 1,
                                KernelHandles = allKernels
                            }
                        }
                    },
                }
            }).Wait();
        }
    }
}
