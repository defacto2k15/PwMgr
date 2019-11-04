using System.Collections.Generic;
using Assets.Grass.Container;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass.Instancing
{
    class GameObjectGrassInstanceGenerator
    {
        public List<GameObject> Generate(GrassEntitiesWithMaterials grassEntitiesWithMaterials)
        {
            var gameObjects = new List<GameObject>();

            foreach (var aGrass in grassEntitiesWithMaterials.Entities)
            {
                var grassInstance = new GameObject("grassInstance");
                grassInstance.AddComponent<MeshFilter>().mesh = grassEntitiesWithMaterials.Mesh;
                grassInstance.transform.localPosition = aGrass.Position;
                grassInstance.transform.localEulerAngles = MyMathUtils.RadToDeg(aGrass.Rotation);
                grassInstance.transform.localScale = aGrass.Scale;
                var rend = grassInstance.AddComponent<MeshRenderer>();
                rend.material = grassEntitiesWithMaterials.Material;

                foreach (var uniform in aGrass.GetFloatUniforms())
                {
                    rend.material.SetFloat(uniform.Name, uniform.Get());
                }
                foreach (var uniform in aGrass.GetVector4Uniforms())
                {
                    rend.material.SetVector(uniform.Name, uniform.Get());
                }

                gameObjects.Add(grassInstance);
            }

            return gameObjects;
        }
    }
}