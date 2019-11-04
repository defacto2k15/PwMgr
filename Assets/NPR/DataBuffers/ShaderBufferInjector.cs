using System;
using Assets.Utils.Editor;
using UnityEngine;

namespace Assets.NPR.Lines
{
    // non OC version of shaderBufferInjectOC
    public abstract class ShaderBufferInjector 
    {
        private bool _automaticReset;
        private string _bufferName;
        private string _name;
        private EditorUpdate2GO _editorUpdate2Go;
        private Material _material;

        private Action _resetingAction;

        protected ShaderBufferInjector(bool automaticReset, string bufferName, string name, EditorUpdate2GO editorUpdate2Go, Material material)
        {
            _automaticReset = automaticReset;
            _bufferName = bufferName;
            _name = name;
            _editorUpdate2Go = editorUpdate2Go;
            _material = material;
        }

        public void Start()
        {
            ResetMeshDetails();
        }

        protected abstract ComputeBuffer ProvideBuffer(bool forceReload);
        protected abstract bool Enabled { get; }

        public void Reset()
        {
            if (_resetingAction != null)
            {

                _resetingAction();
            }
        }

        public void OnValidate()
        {
            ResetMeshDetails();
        }

        public void OnEnable()
        {
            ResetMeshDetails();
        }

        public void RecreateBuffer()
        {
            ResetMeshDetails(true);
            ResetOrder();
        }

        private void ResetMeshDetails(bool forceReload = false)
        {
            if (Enabled)
            {
                var newBuffer = ProvideBuffer(forceReload);
                RecreateBufferWithoutOrder(newBuffer);
                ResetOrder();
            }
        }

        private void RecreateBufferWithoutOrder(ComputeBuffer buffer)
        {
            if (buffer == null) return;

            _resetingAction = () =>
            {
                //Debug.Log(BufferName);
                _material.SetBuffer(_bufferName, buffer);
            };
        }

        private void ResetOrder()
        {
            var orderName = _bufferName + _name + GetType().Name+GetHashCode();
            if (_automaticReset)
            {
                _editorUpdate2Go.SetOrder(orderName, Reset, 3);
            }
            else
            {
                Reset();
                _editorUpdate2Go.RemoveOrder(orderName);
            }
        }
    }
}