using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.ShaderUtils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    class Ring1Utils
    {
        public static GameObject CreateFlatTerrainPlane(Point2D gameObjectSize, MyRectangle inGamePosition,
            UniformsPack pack)
        {
            var gameObject = new GameObject("Ring1 terrain object " + inGamePosition.ToString());
            var terrainMaterial = new Material(Shader.Find("Custom/Terrain/Terrain1"));
            pack.SetUniformsToMaterial(terrainMaterial);

            gameObject.AddComponent<MeshRenderer>();
            gameObject.GetComponent<Renderer>().material = terrainMaterial;

            gameObject.AddComponent<MeshFilter>().mesh =
                PlaneGenerator.CreateFlatPlaneMesh(gameObjectSize.X, gameObjectSize.Y);

            gameObject.transform.localScale = new Vector3(inGamePosition.Width, 1, inGamePosition.Height);
            gameObject.transform.localPosition = new Vector3(inGamePosition.X, 0, inGamePosition.Y);

            return gameObject;
        }

        public static GameObject CreateHeightedTerrainPlane(Point2D gameObjectSize,
            MyRectangle inGamePosition, UniformsPack pack, HeightmapArray simplifiedMap)
        {
            var gameObject = new GameObject("Ring1 terrain object " + inGamePosition.ToString());
            var terrainMaterial = new Material(Shader.Find("Custom/Terrain/Ring1DirectHeight"));
            pack.SetUniformsToMaterial(terrainMaterial);

            gameObject.AddComponent<MeshRenderer>();
            gameObject.GetComponent<Renderer>().material = terrainMaterial;
            gameObject.AddComponent<MeshFilter>();

            PlaneGenerator.createPlaneMeshFilter(gameObject.GetComponent<MeshFilter>(), 1, 1,
                simplifiedMap.HeightmapAsArray);

            gameObject.transform.localScale = new Vector3(inGamePosition.Width, 1, inGamePosition.Height);
            gameObject.transform.localPosition = new Vector3(inGamePosition.X, 0, inGamePosition.Y);

            return gameObject;
        }
    }
}