using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Shape
{
    public class HeightPyramidSegmentShapeGroup
    {
        public GameObject CentralShape;
        public Dictionary<int, List<GameObject>> ShapesPerRing;
        public GameObject ParentGameObject;

        public List<Material> ETerrainMaterials
        {
            get
            {
                var gameObjects = new List<GameObject>();
                if (ShapesPerRing != null)
                {
                    gameObjects.AddRange(ShapesPerRing.Values.SelectMany(c=>c).ToList());
                }
                if (CentralShape != null)
                {
                    gameObjects.Add(CentralShape);
                }

                return gameObjects.Select(c => c.GetComponent<MeshRenderer>().material).ToList();
            }
        }

        public void MoveBy(Vector2 delta)
        {
            ParentGameObject.transform.localPosition += new Vector3(delta.x, 0, delta.y);
        }

        public void DisableShapes()
        {
            ParentGameObject.SetActive(false);
        }
    }
}