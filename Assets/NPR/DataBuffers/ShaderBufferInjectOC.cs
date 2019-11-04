using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils.Editor;
using UnityEditor;
using UnityEngine;

namespace Assets.NPR.Lines
{
    [ExecuteInEditMode]
    public abstract class ShaderBufferInjectOC : MonoBehaviour
    {
        public bool AutomaticReset = true;
        public string BufferName = "_AdjacencyBuffer";

        private Action _resetingAction;

        public void Start()
        {
            Constructor();
            ResetMeshDetails();
        }

        private void Constructor()
        {
        }

        protected abstract ComputeBuffer ProvideBuffer(bool forceReload);

        public void Reset()
        {
            Constructor();
            if (_resetingAction != null)
            {

                _resetingAction();
            }
        }

        public void OnValidate()
        {
            Constructor();
            ResetMeshDetails();
        }

        public void OnEnable()
        {
            Constructor();
            ResetMeshDetails();
        }


        private void ResetMeshDetails(bool forceReload = false)
        {
            if (this.enabled)
            {
                var newBuffer = ProvideBuffer(forceReload);
                RecreateBufferWithoutOrder(newBuffer);
                ResetOrder();
            }
        }

        private void RecreateBufferWithoutOrder(ComputeBuffer buffer)
        {
            if (buffer == null) return;

            Material material = null;
            if (Application.isEditor)
            {
                material = GetComponent<MeshRenderer>().sharedMaterial;
            }
            else
            {
                material = GetComponent<MeshRenderer>().material;
            }

            _resetingAction = () =>
            {
                //Debug.Log(BufferName);
                material.SetBuffer(BufferName, buffer);
            };
        }

        public void RecreateBuffer()
        {
            ResetMeshDetails(true);
            ResetOrder();
        }

        private void ResetOrder()
        {
            var orderName = BufferName + name + GetType().Name+GetHashCode();
            if (AutomaticReset)
            {
                FindObjectOfType<EditorUpdate2GO>().SetOrder(orderName, Reset, 3);
            }
            else
            {
                Reset();
                FindObjectOfType<EditorUpdate2GO>().RemoveOrder(orderName);
            }
        }
    }
}
