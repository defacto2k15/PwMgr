using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Repositioning;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.ShaderUtils;
using Assets.Utils;
using GeoAPI.Geometries;
using UnityEngine;

namespace Assets.Ring2.Devising
{
    public class Ring2Deviser
    {
        private Ring2PlateMeshRepository _meshRepository;
        private Repositioner _repositioner;

        public Ring2Deviser(Ring2PlateMeshRepository meshRepository, Repositioner repositioner)
        {
            _meshRepository = meshRepository;
            _repositioner = repositioner;
        }

        public Ring2PatchDevised DevisePatch(Ring2Patch patch)
        {
            int sliceLayerIndex = patch.Slices.Count - 1;
            List<Ring2Plate> allPlates = new List<Ring2Plate>();
            foreach (var slice in patch.Slices)
            {
                var sliceMeshes = CreateSliceMeshes(patch.SliceArea, slice);
                MoveLayersUp(sliceMeshes, sliceLayerIndex);
                var plates = CreatePlates(sliceMeshes, slice, patch.SliceArea);
                allPlates.AddRange(plates);
                sliceLayerIndex--;
            }
            return new Ring2PatchDevised(allPlates, patch.SliceArea.ToUnityCoordPositions2D());
        }

        private void MoveLayersUp(List<Ring2PatchMesh> sliceMeshes, int sliceLayerIndex)
        {
            foreach (var patchMesh in sliceMeshes)
            {
                var position = patchMesh.TransformTriplet.Position;
                position = new Vector3(position.x, position.y + sliceLayerIndex * 0.01f, position.z);
                patchMesh.TransformTriplet.Position = position;
            }
        }

        private List<Ring2Plate> CreatePlates(List<Ring2PatchMesh> sliceMeshes, Ring2Slice slice, Envelope sliceArea)
        {
            sliceArea = _repositioner.Move(sliceArea);
            var propertyBlock = new MaterialPropertyBlockTemplate();
            propertyBlock.SetVectorArray("_Palette",
                slice.SlicePalette.Palette.Select(c => new Vector4(c.r, c.g, c.b, c.a)).ToArray());
            propertyBlock.SetVector("_Dimensions",
                new Vector4((float) sliceArea.MinX, (float) sliceArea.MinY, (float) sliceArea.CalculatedWidth(),
                    (float) sliceArea.CalculatedHeight()));
            propertyBlock.SetTexture("_ControlTex", slice.IntensityPattern.Texture); //todo texture garbage collector
            propertyBlock.SetVector("_LayerPriorities", slice.LayerPriorities);
            propertyBlock.SetVector("_LayerPatternScales", slice.LayerPatternScales);
            var materialTemplate = new MaterialTemplate(Ring2ShaderNames.RuntimeTerrainTexture, slice.Keywords,
                propertyBlock);

            List<Ring2Plate> plates = new List<Ring2Plate>();
            foreach (var sliceMesh in sliceMeshes)
            {
                plates.Add(new Ring2Plate(sliceMesh.Mesh, sliceMesh.TransformTriplet.ToLocalToWorldMatrix(),
                    materialTemplate));
            }
            return plates;
        }

        private List<Ring2PatchMesh> CreateSliceMeshes(Envelope envelope, Ring2Slice slice)
        {
            envelope = _repositioner.Move(envelope);
            var position = new Vector3((float) envelope.Centre.X, 0, (float) envelope.Centre.Y);
            var rotation = new Vector3(90, 0, 0);
            var scale = new Vector3((float) envelope.CalculatedWidth(), 1, (float) envelope.CalculatedHeight());

            return new List<Ring2PatchMesh>()
            {
                new Ring2PatchMesh(_meshRepository.Quad, new MyTransformTriplet(position, rotation, scale))
            };
        }
    }

    public class MaterialTemplate
    {
        private string _shaderName;
        private ShaderKeywordSet _keywordSet;
        private MaterialPropertyBlockTemplate _propertyBlock;

        public MaterialTemplate(string shaderName, ShaderKeywordSet keywordSet,
            MaterialPropertyBlockTemplate propertyBlock)
        {
            _keywordSet = keywordSet;
            _propertyBlock = propertyBlock;
            _shaderName = shaderName;
        }

        public ShaderKeywordSet KeywordSet => _keywordSet;

        public MaterialPropertyBlockTemplate PropertyBlock => _propertyBlock;

        public string ShaderName => _shaderName;
    }

    public class MaterialPropertyBlockTemplate
    {
        private Dictionary<string, float> _floatDict = new Dictionary<string, float>();
        private Dictionary<string,int > _intDict = new Dictionary<string, int>();
        private Dictionary<string, Vector4[]> _vectorArrayDict = new Dictionary<string, Vector4[]>();
        private Dictionary<string, Vector4> _vectorDict = new Dictionary<string, Vector4>();
        private Dictionary<string, Texture> _textureDict = new Dictionary<string, Texture>();

        public void SetFloat(string name, float value)
        {
            _floatDict[name] = value;
        }

        public void SetVectorArray(string name, Vector4[] value)
        {
            _vectorArrayDict[name] = value;
        }

        public void SetVector(string name, Vector4 value)
        {
            _vectorDict[name] = value;
        }

        public void SetTexture(string name, Texture value)
        {
            _textureDict[name] = value;
        }

       public void SetInt(string layerindex, int value)
       {
           _intDict[layerindex] = value;
       }

        public MaterialPropertyBlock CreateBlock()
        {
            var mpb = new MaterialPropertyBlock();
            foreach (var pair in _floatDict)
            {
                mpb.SetFloat(pair.Key, pair.Value);
            }
            foreach (var pair in _intDict)
            {
                mpb.SetInt(pair.Key, pair.Value);
            }
            foreach (var pair in _vectorArrayDict)
            {
                mpb.SetVectorArray(pair.Key, pair.Value);
            }
            foreach (var pair in _vectorDict)
            {
                mpb.SetVector(pair.Key, pair.Value);
            }
            foreach (var pair in _textureDict)
            {
                mpb.SetTexture(pair.Key, pair.Value);
            }
            return mpb;
        }

        public void FillMaterial(Material material)
        {
            foreach (var pair in _floatDict)
            {
                material.SetFloat(pair.Key, pair.Value);
            }
            foreach (var pair in _intDict)
            {
                material.SetInt(pair.Key, pair.Value);
            }
            foreach (var pair in _vectorArrayDict)
            {
                material.SetVectorArray(pair.Key, pair.Value);
            }
            foreach (var pair in _vectorDict)
            {
                material.SetVector(pair.Key, pair.Value);
            }
            foreach (var pair in _textureDict)
            {
                material.SetTexture(pair.Key, pair.Value);
            }
        }

        public MaterialPropertyBlockTemplate Clone()
        {
            return new MaterialPropertyBlockTemplate()
            {
                _floatDict = _floatDict.ToDictionary(c=>c.Key, c=>c.Value),
                _intDict =  _intDict.ToDictionary(c=>c.Key,c=>c.Value),
                _textureDict = _textureDict.ToDictionary(c=>c.Key, c=>c.Value),
                _vectorArrayDict = _vectorArrayDict.ToDictionary(c=>c.Key, c=>c.Value),
                _vectorDict = _vectorDict.ToDictionary(c=>c.Key, c=>c.Value)
            };
        }

     }
}