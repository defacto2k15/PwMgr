using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.ComputeShaders.Templating;
using Assets.Utils;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.ComputeShaders
{
    public class UnityThreadComputeShaderExecutorObject : BaseUTTransformProxy<object, ComputeShaderOrder>
    {
        public Task DispatchComputeShader(ComputeShaderOrder order)
        {
            return BaseUtAddOrder(order);
        }

        protected override object ExecuteOrder(ComputeShaderOrder order) //todo multistep execution
        {
            var parametersContainer = order.ParametersContainer;
            var createdParameters = CreateParameters(parametersContainer);

            foreach (var pack in order.WorkPacks)
            {
                var shader = pack.Shader;
                shader.CreateKernelHandleTranslationMap();
                shader.SetCreatedParameters(createdParameters);

                foreach (var dispatchLoop in pack.DispatchLoops)
                {
                    for (int i = 0; i < dispatchLoop.DispatchCount; i++)
                    {
                        pack.Shader.ImmediatelySetGlobalUniform("g_DispatchLoopIndex", i);
                        pack.Shader.Dispatch(dispatchLoop.KernelHandles);
                    }
                }
            }

            var outParameters = order.OutParameters;
            FinalizeParameters(parametersContainer, createdParameters, outParameters);
            return null;
        }

        private static void FinalizeParameters(
            ComputeShaderParametersContainer parametersContainer,
            ComputeShaderCreatedParametersContainer createdParameters,
            ComputeBufferRequestedOutParameters outParameters)
        {
            foreach (var pair in createdParameters.Textures)
            {
                if (outParameters.IsRequestedTextureId(pair.Key))
                {
                    outParameters.AddTexture(pair.Key, pair.Value);
                }
                else
                {
                    if (!parametersContainer.ArleadyCreatedTextures.ContainsKey(pair.Key))
                    {
                        (pair.Value as RenderTexture)?.Release();
                    }
                }
            }
            foreach (var pair in createdParameters.Buffers)
            {
                if (outParameters.IsRequestedBufferId(pair.Key))
                {
                    outParameters.AddBuffer(pair.Key, pair.Value);
                }
                else
                {
                    if (!parametersContainer.ArleadyCreatedComputeBufferTemplates.ContainsKey(pair.Key))
                    {
                        //pair.Value.Dispose();//todo repair
                    }
                }
            }
        }

        private ComputeShaderCreatedParametersContainer CreateParameters(
            ComputeShaderParametersContainer parametersContainer)
        {
            var createdParameters = new ComputeShaderCreatedParametersContainer();

            foreach (var pair in parametersContainer.ArleadyCreatedTextures)
            {
                createdParameters.AddTexture(pair.Key, pair.Value);
            }

            foreach (var pair in parametersContainer.ComputeShaderTextureTemplates)
            {
                createdParameters.AddTexture(pair.Key, CreateTexture(pair.Value));
            }

            foreach (var pair in parametersContainer.ArleadyCreatedComputeBufferTemplates)
            {
                createdParameters.AddBuffer(pair.Key, pair.Value);
            }

            foreach (var pair in parametersContainer.ComputeBufferTemplates)
            {
                var createdBuffer = CreateBuffer(pair.Value);
                if (pair.Value.BufferData != null)
                {
                    createdBuffer.SetData(pair.Value.BufferData);
                }
                createdParameters.AddBuffer(pair.Key, createdBuffer);
            }
            return createdParameters;
        }

        private ComputeBuffer CreateBuffer(MyComputeBufferTemplate template)
        {
            ComputeBuffer buffer = null;
            buffer = new ComputeBuffer(template.Count, template.Stride, template.Type);
            return buffer;
        }

        private Texture CreateTexture(MyComputeShaderTextureTemplate template)
        {
            var texture = new RenderTexture(template.Size.X, template.Size.Y, template.Depth, template.Format);
            texture.enableRandomWrite = template.EnableReadWrite;
            texture.wrapMode = template.TexWrapMode;
            if (template.Dimension.HasValue)
            {
                texture.dimension = template.Dimension.Value;
            }
            if (template.VolumeDepth.HasValue)
            {
                texture.volumeDepth = template.VolumeDepth.Value;
            }
            texture.Create();
            return texture;
        }
    }

    public class ComputeShaderOrder
    {
        public List<ComputeShaderWorkPack> WorkPacks;
        public ComputeShaderParametersContainer ParametersContainer { get; set; }
        public ComputeBufferRequestedOutParameters OutParameters { get; set; }
    }

    public class ComputeShaderWorkPack
    {
        public MultistepComputeShader Shader;
        public List<ComputeShaderDispatchLoop> DispatchLoops;
    }

    public class ComputeShaderDispatchLoop
    {
        public List<MyKernelHandle> KernelHandles;
        public int DispatchCount;
    }
}