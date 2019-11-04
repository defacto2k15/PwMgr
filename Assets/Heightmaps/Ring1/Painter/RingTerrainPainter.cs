using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Painter
{
    public class RingTerrainPainter
    {
        private Dictionary<UInt32, GameObject> _groundPieceGameObjects = new Dictionary<uint, GameObject>();

        private bool _makeTerrainVisible;

        public RingTerrainPainter(bool makeTerrainVisible = true)
        {
            _makeTerrainVisible = makeTerrainVisible;
        }

        public void ProcessOrder(Ring1TerrainPainterOrder order)
        {
            foreach (var groundPieceTemplatePair in order.NewlyCreatedElements)
            {
                var template = groundPieceTemplatePair.Value;
                var newObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                newObject.name = template.Name;
                newObject.transform.localScale = template.TransformTriplet.Scale;
                newObject.transform.localRotation = Quaternion.Euler(template.TransformTriplet.ForcedEulerRotation);
                newObject.transform.localPosition = template.TransformTriplet.Position =
                    template.TransformTriplet.Position;
                newObject.transform.SetParent(template.ParentGameObject.transform);

                var newMaterial = new Material(Shader.Find(template.ShaderName));
                foreach (var keyword in template.ShaderKeywordSet.Keywords)
                {
                    newMaterial.EnableKeyword(keyword);
                }
                template.Uniforms.SetUniformsToMaterial(newMaterial);

                newObject.GetComponent<UnityEngine.Renderer>().material = newMaterial;
                newObject.GetComponent<MeshFilter>().mesh = template.PieceMesh;
                GameObject.Destroy(newObject.GetComponent<MeshCollider>());

                _groundPieceGameObjects[groundPieceTemplatePair.Key] = newObject;
                template.Modifier.SetMaterial(newMaterial);
            }

            foreach (var pair in order.ActivationChanges)
            {
                _groundPieceGameObjects[pair.Key].SetActive(pair.Value && _makeTerrainVisible);
            }
        }
    }
}