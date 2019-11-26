using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.ComputeShaders.Templating;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Utils;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.ComputeShaders
{
    public class UnityThreadComputeShaderExecutorObject : BaseGpuWorkUTTransformProxy<object, ComputeShaderOrder>
    {
        private readonly bool _multistepExecution;
        private MultistepComputeShaderExecutorObject _multistepExecutor;

        public UnityThreadComputeShaderExecutorObject(bool multistepExecution = false) : base(30f, automaticExecution:!multistepExecution) // TODO other method for execution time computing
        {
            _multistepExecution = multistepExecution;
            _multistepExecutor = new MultistepComputeShaderExecutorObject(); //todo maybe add to configuration
        }

        public Task<object> AddOrder(ComputeShaderOrder order)
        {
            return BaseUtAddOrder(order);
        }

        public override bool InternalHasWorkToDo()
        {
            if (!_multistepExecution)
            {
                return false;
            }
            return _multistepExecutor.CurrentlyRendering;
        }

        protected override void InternalUpdate()
        {
            if (!_multistepExecution)
            {
                return;
            }
            if (_multistepExecutor.CurrentlyRendering)
            {
                _multistepExecutor.Update();
            }
            else
            {
                // lets add new order
                var transformOrder = TryGetNextFromQueue();
                if (transformOrder != null) { 
                    _multistepExecutor.AddMultistepOrder(new MultistepRenderingProcessOrder<object, ComputeShaderOrder>()
                    {
                        Tcs = transformOrder.Tcs,
                        Order = transformOrder.Order
                    });
                }
            }
        }

        protected override object ExecuteOrder(ComputeShaderOrder order)
        {
            return _multistepExecutor.FufillOrderWithoutMultistep(order);
        }
    }

    public class MultistepComputeShaderExecutionState
    {
        public int CurrentWorkPackIndex;
        public int CurrentDispatchIndex;
        public int DispatchLoopIndex;
        public ComputeShaderCreatedParametersContainer CreatedParametersContainer;
    }

    public class MultistepComputeShaderExecutorObject
    {
        private MultistepRenderingProcessOrder<object, ComputeShaderOrder> _currentOrderWithTask;
        private MultistepComputeShaderExecutionState _executionState;

        public bool CurrentlyRendering => _currentOrderWithTask != null;

        public object FufillOrderWithoutMultistep(ComputeShaderOrder order)
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

        public void AddMultistepOrder(MultistepRenderingProcessOrder<object, ComputeShaderOrder> order)
        {
            Preconditions.Assert(_currentOrderWithTask==null, "There is arleady order set");
            _currentOrderWithTask = order;

            var computeShaderCreatedParametersContainer = CreateParameters(order.Order.ParametersContainer);
            var shader = _currentOrderWithTask.Order.WorkPacks[0].Shader;
            shader.CreateKernelHandleTranslationMap();
            shader.SetCreatedParameters(computeShaderCreatedParametersContainer);

            _executionState = new MultistepComputeShaderExecutionState()
            {
                CurrentDispatchIndex = 0,
                CurrentWorkPackIndex = 0,
                DispatchLoopIndex = 0,
                CreatedParametersContainer = computeShaderCreatedParametersContainer,
            };
        }

        public void Update()
        {
            Preconditions.Assert(_currentOrderWithTask != null, "There is arleady there is no order set");

            var order = _currentOrderWithTask.Order;
            if (_executionState.CurrentWorkPackIndex >= order.WorkPacks.Count)
            {

                var outParameters = order.OutParameters;
                FinalizeParameters(order.ParametersContainer, _executionState.CreatedParametersContainer, outParameters);

                var tcs = _currentOrderWithTask.Tcs;
                _currentOrderWithTask = null;
                _executionState = null;
                tcs.SetResult(null);
                return;
            }

            var pack = order.WorkPacks[_executionState.CurrentWorkPackIndex];
            var dispatchLoop = pack.DispatchLoops[_executionState.DispatchLoopIndex];

            pack.Shader.ImmediatelySetGlobalUniform("g_DispatchLoopIndex",_executionState.CurrentDispatchIndex);
            pack.Shader.Dispatch(dispatchLoop.KernelHandles);

            _executionState.CurrentDispatchIndex++;
            if ( _executionState.CurrentDispatchIndex >= dispatchLoop.DispatchCount)
            {
                _executionState.CurrentDispatchIndex = 0;
                _executionState.DispatchLoopIndex++;

                if (_executionState.DispatchLoopIndex >= pack.DispatchLoops.Count)
                {
                    _executionState.DispatchLoopIndex = 0;
                    _executionState.CurrentWorkPackIndex++;

                    if (_executionState.CurrentWorkPackIndex < order.WorkPacks.Count) { 
                        var shader = _currentOrderWithTask.Order.WorkPacks[_executionState.CurrentWorkPackIndex].Shader;
                        shader.CreateKernelHandleTranslationMap();
                        shader.SetCreatedParameters(_executionState.CreatedParametersContainer);
                    }
                }
            }
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