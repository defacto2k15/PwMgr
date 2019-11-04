using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Ring2.Devising;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Utils;
using UnityEngine;

namespace Assets.Ring2.Painting
{
    public class Ring2PatchesPainter
    {
        private Ring2MultishaderMaterialRepository _materialRepositiory;

        private Dictionary<uint, Ring2PatchDevised> _devisedPatchedDictionary =
            new Dictionary<uint, Ring2PatchDevised>();

        private UInt32 _lastId = 0;

        public Ring2PatchesPainter(Ring2MultishaderMaterialRepository materialRepositiory)
        {
            _materialRepositiory = materialRepositiory;
        }

        public OverseedPatchId AddPatch(Ring2PatchesPainterCreationOrder creationOrder)
        {
            return new OverseedPatchId()
            {
                Ids = creationOrder.Patches.Select(p => AddPatch(p)).ToList()
            };
        }

        private UInt32 AddPatch(Ring2PatchDevised patch)
        {
            _devisedPatchedDictionary[_lastId++] = patch;
            AddObject(patch);
            return _lastId - 1;
        }

        private void AddObject(Ring2PatchDevised devisedPatch)
        {
            foreach (var plate in devisedPatch.Plates)
            {
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var scale = plate.TransformMatrix.ExtractScale();
                primitive.transform.localScale = new Vector3(scale.x, scale.z, scale.y);
                primitive.transform.localRotation =
                    Quaternion.Euler(new Vector3(90, 0, 0)); // plate.TransformMatrix.ExtractRotation();
                primitive.transform.localPosition = plate.TransformMatrix.ExtractPosition();

                primitive.GetComponent<Renderer>().material = RetriveMaterial(plate.MaterialTemplate);
                primitive.GetComponent<Renderer>().SetPropertyBlock(plate.MaterialTemplate.PropertyBlock.CreateBlock());

                primitive.name = "PatchPrimitive. Patch " + _lastId;
            }
        }

        public void RemovePatch(OverseedPatchId idToRemove)
        {
            idToRemove.Ids.ForEach(RemovePatch);
        }

        private void RemovePatch(UInt32 idToRemove)
        {
            Preconditions.Assert(_devisedPatchedDictionary.ContainsKey(idToRemove),
                "There is no patch with id " + idToRemove);
            _devisedPatchedDictionary.Remove(idToRemove);
        }

        public void Update()
        {
            foreach (var devisedPatch in _devisedPatchedDictionary.Values)
            {
                foreach (var plate in devisedPatch.Plates)
                {
                    Graphics.DrawMesh(plate.Mesh, plate.TransformMatrix, RetriveMaterial(plate.MaterialTemplate), 0,
                        Camera.main, 0,
                        plate.MaterialTemplate.PropertyBlock.CreateBlock());
                }
            }
        }

        private Material RetriveMaterial(MaterialTemplate plateMaterialTemplate)
        {
            var material =
                _materialRepositiory.RetriveMaterial(plateMaterialTemplate.ShaderName,
                    plateMaterialTemplate.KeywordSet);
            return material;
        }
    }
}