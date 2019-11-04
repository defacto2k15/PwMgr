using Assets.MeshGeneration;
using UnityEngine;

namespace Assets.Heightmaps
{
    class SubmapPlane
    {
        private readonly GameObject planeObj;

        public GameObject GameObject
        {
            get { return planeObj; }
        }

        public SubmapPlane(GameObject planeObj)
        {
            this.planeObj = planeObj;
        }

        public static SubmapPlane CreatePlaneObject(float[,] heightArray)
        {
            var planeObj = new GameObject {name = "myTestPlane"};
            PlaneGenerator.createPlaneMeshFilter(planeObj.AddComponent<MeshFilter>(), 1, 1, heightArray);

            var renderer = planeObj.AddComponent<MeshRenderer>();
            renderer.material.shader = Shader.Find("Particles/Additive");
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.green);
            tex.Apply();
            renderer.material.mainTexture = tex;
            renderer.material.color = Color.green;

            return new SubmapPlane(planeObj);
        }
    }
}