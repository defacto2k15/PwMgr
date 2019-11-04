using System.Collections.Generic;
using Assets.MeshGeneration;
using Assets.ShaderUtils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.treeNodeListener
{
    static class Ring1TestingObjects
    {
        private static GameObject _terrainGameObject;
        private static Dictionary<string, GameObject> textureShowingGameObjects = new Dictionary<string, GameObject>();

        public static void CreateTextureShowingGameObject(Texture2D texture, string objectName)
        {
            if (!textureShowingGameObjects.ContainsKey(objectName))
            {
                var newObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                newObject.name = "TestObject " + objectName;
                newObject.transform.localPosition += new Vector3(textureShowingGameObjects.Count, 0, 0);
                var dummyMaterial = new Material(Shader.Find("Sprites/Default"));
                dummyMaterial.SetTexture("_MainTex", texture);
                newObject.GetComponent<Renderer>().material = dummyMaterial;
                textureShowingGameObjects[objectName] = newObject;
            }
            else
            {
                textureShowingGameObjects[objectName].GetComponent<Renderer>().material.SetTexture("_MainTex", texture);
            }
        }

        public static void CreateTerrainShowingGameObject(Texture2D conventionalRing1Texture, float heightDelta)
        {
            _terrainGameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _terrainGameObject.name = "Terrain";
            var terrainMaterial = new Material(Shader.Find("Custom/Terrain/Terrain1"));
            terrainMaterial.SetTexture("_HeightmapTex", conventionalRing1Texture);
            terrainMaterial.SetFloat("_MaxHeight", heightDelta);

            _terrainGameObject.GetComponent<Renderer>().material = terrainMaterial;
            _terrainGameObject.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(256, 250);
            GameObject.Destroy(_terrainGameObject.GetComponent<MeshCollider>());
            _terrainGameObject.transform.localScale = new Vector3(100, 100.0f / 712.0f, 100);
            _terrainGameObject.transform.localPosition = new Vector3(10.0f, -60f, 0f);
        }

        public static void UpdateTerrainShowingGameObject(Texture2D conventionalRing1Texture, UniformsPack uniforms)
        {
            uniforms.SetUniformsToMaterial(_terrainGameObject.GetComponent<Renderer>().material);
            _terrainGameObject.GetComponent<Renderer>().material.SetTexture("_HeightmapTex", conventionalRing1Texture);
        }
    }
}