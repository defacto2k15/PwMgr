using UnityEngine;

namespace Assets.Ring2.Devising
{
    public class Ring2PlateMeshRepository
    {
        public Mesh Quad;

        public static Ring2PlateMeshRepository Create()
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var quadMesh = gameObject.GetComponent<MeshFilter>().mesh;
            gameObject.SetActive(false);
            return new Ring2PlateMeshRepository()
            {
                Quad = quadMesh
            };
        }
    }
}