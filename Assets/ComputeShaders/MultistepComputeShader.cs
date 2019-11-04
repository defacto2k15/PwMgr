using System;
using System.Collections.Generic;
using Assets.Utils;
using UnityEngine;

namespace Assets.ComputeShaders
{
    public class MultistepComputeShader
    {
        private ComputeShader _shader;
        private IntVector3 _workspaceSize;
        private int _groupsDivisor;
        private ComputeShaderParametersUsageContainer _parametersUsageContainer;
        private Dictionary<MyKernelHandle, string> _kernelNamesMap;
        private Dictionary<MyKernelHandle, MyInstancedKernelHandle> _kernelNameTranslationMap;

        public MultistepComputeShader(ComputeShader shader, IntVector2 workspaceSize)
        {
            _workspaceSize = new IntVector3(workspaceSize.X, workspaceSize.Y,1);
            _groupsDivisor = 1;
            _shader = shader;
            _parametersUsageContainer = new ComputeShaderParametersUsageContainer();
            _kernelNamesMap = new Dictionary<MyKernelHandle, string>();
        }

        public MultistepComputeShader(ComputeShader shader, IntVector3 workspaceSize)
        {
            _workspaceSize = workspaceSize;
            _groupsDivisor = 1;
            _shader = shader;
            _parametersUsageContainer = new ComputeShaderParametersUsageContainer();
            _kernelNamesMap = new Dictionary<MyKernelHandle, string>();
        }

        public Dictionary<MyKernelHandle, string> KernelNamesMap => _kernelNamesMap;

        public MyKernelHandle AddKernel(string kernelName)
        {
            var handle = new MyKernelHandle();
            _kernelNamesMap[handle] = kernelName;
            return handle;
        }

        public void SetBuffer(string bufferName, MyComputeBufferId bufferId, List<MyKernelHandle> kernelHandles)
        {
            _parametersUsageContainer.AddBuffer(bufferName, bufferId, kernelHandles);
        }

        public void SetTexture(string textureName, MyComputeShaderTextureId textureId,
            List<MyKernelHandle> kernelHandles)
        {
            _parametersUsageContainer.AddTexture(textureName, textureId, kernelHandles);
        }

        public void SetGlobalUniform(string name, float value)
        {
            _parametersUsageContainer.AddGlobalUniform(name, value);
        }

        public void SetGlobalUniform(string name, Vector4 value)
        {
            _parametersUsageContainer.AddGlobalUniform(name, value);
        }

        public void SetGlobalUniform(string name, int value)
        {
            _parametersUsageContainer.AddGlobalUniform(name, value);
        }

        public void ImmediatelySetGlobalUniform(string name, int value)
        {
            _shader.SetInt(name, value);
        }

        public void Dispatch(List<MyKernelHandle> myKernelHandles)
        {
            foreach (var aHandle in myKernelHandles)
            {
                var handleId = _kernelNameTranslationMap[aHandle].HandleId;

                _shader.Dispatch(handleId,
                    Mathf.FloorToInt(_workspaceSize.X / (float) _groupsDivisor),
                    Mathf.FloorToInt(_workspaceSize.Y / (float) _groupsDivisor),
                    Mathf.FloorToInt(_workspaceSize.Z / (float) _groupsDivisor));
            }
        }

        public void CreateKernelHandleTranslationMap()
        {
            _kernelNameTranslationMap = new Dictionary<MyKernelHandle, MyInstancedKernelHandle>();
            foreach (var pair in _kernelNamesMap)
            {
                var kernelId = _shader.FindKernel(pair.Value);
                _kernelNameTranslationMap[pair.Key] = new MyInstancedKernelHandle(kernelId);
            }
        }

        public void SetCreatedParameters(ComputeShaderCreatedParametersContainer createdParametersContainer)
        {
            foreach (var pair in _parametersUsageContainer.IntGlobals)
            {
                _shader.SetInt(pair.Key, pair.Value);
            }

            foreach (var pair in _parametersUsageContainer.FloatGlobals)
            {
                _shader.SetFloat(pair.Key, pair.Value);
            }

            foreach (var pair in _parametersUsageContainer.VectorGlobals)
            {
                _shader.SetVector(pair.Key, pair.Value);
            }

            foreach (var usage in _parametersUsageContainer.BufferUsages)
            {
                var buffer = createdParametersContainer.RetriveBuffer(usage.BufferId);
                foreach (var kernelHandle in usage.Handles)
                {
                    var handleId = _kernelNameTranslationMap[kernelHandle].HandleId;
                    _shader.SetBuffer(handleId, usage.BufferName, buffer);
                }
            }

            foreach (var usage in _parametersUsageContainer.TextureUsages)
            {
                var texture = createdParametersContainer.RetriveTexture(usage.TextureId);
                foreach (var bufferHandle in usage.Handles)
                {
                    var handleId = _kernelNameTranslationMap[bufferHandle].HandleId;
                    _shader.SetTexture(handleId, usage.TextureName, texture);
                }
            }
        }
    }
}