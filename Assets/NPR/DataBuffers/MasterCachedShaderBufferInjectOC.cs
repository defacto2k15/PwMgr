using System;
using System.Collections.Generic;
using Assets.NPR.DataBuffers;
using Assets.Utils;
using Assets.Utils.Editor;
using Assets.Utils.MyAsset;
using UnityEngine;

namespace Assets.NPR.Lines
{
    [ExecuteInEditMode]
    public class MasterCachedShaderBufferInjectOC : MonoBehaviour
    {
        public bool AutomaticReset;
        public string BaseName;
        public List<MyShaderBufferType> ShaderBufferTypes = new List<MyShaderBufferType>();
        private List<CachedShaderBufferInjector> _injectors = new List<CachedShaderBufferInjector>();

        public void Start()
        {
            RecreateInjectors();

            _injectors.ForEach(c => c.Start());
        }

        private void RecreateInjectors()
        {
            Material material;
            if (Application.isEditor)
            {
                material = GetComponent<MeshRenderer>().sharedMaterial;
            }
            else
            {
                material = GetComponent<MeshRenderer>().material;
            }


            EditorUpdate2GO editorUpdate2Go = FindObjectOfType<EditorUpdate2GO>();
            Func<bool> isEnabledFunc = () =>
            {
                if (this == null)
                {
                    Debug.Log("W865 This Component was in destroyed object, i repaired problem" );
                    return false;
                }
                return enabled;
            };
            foreach (var type in ShaderBufferTypes)
            {
                var shaderBuffer = RetriveShaderBuffer(type);
                _injectors.Add(new CachedShaderBufferInjector(shaderBuffer, type.Details().StrideInFloats, isEnabledFunc, type.Details().InShaderBufferName,
                    AutomaticReset, this.name, editorUpdate2Go, material));
            }
        }

        private ShaderBufferSE RetriveShaderBuffer(MyShaderBufferType type)
        {
            ShaderBufferSE shaderBuffer = null;

            var bufferAssetName = $"{BaseName}.{type.Details().BufferFileSuffix}";
            if (Application.isEditor )
            {
                var path = $"Assets/NPRResources/BuffersCache/{bufferAssetName}.asset";
                shaderBuffer = MyAssetDatabase.LoadAssetAtPath<ShaderBufferSE>(path);
                Preconditions.Assert(shaderBuffer != null, $"Cannot find buffer of path " + path);
            }
            else
            {
                var listing = FindObjectOfType<MyBufferCacheAssetListingGO>();
                Preconditions.Assert(listing != null, "Cannot find object of type MyBufferCacheAssetListingGO");
                shaderBuffer = listing.RetriveAssetOfName(bufferAssetName) as ShaderBufferSE;
                Preconditions.Assert(shaderBuffer != null, $"Cannot find buffer of  name " + bufferAssetName);
            }

            return shaderBuffer;
        }


        public void Update()
        {
            _injectors.ForEach(c => c.Update());
        }

        public void OnValidate()
        {
            _injectors.ForEach(c => c.OnValidate());
        }

        public void OnEnable()
        {
            _injectors.ForEach(c => c.OnEnable());
        }

        public void RecreateBuffer()
        {
            RecreateInjectors();
            _injectors.ForEach(c => c.RecreateBuffer());
        }

    }

}