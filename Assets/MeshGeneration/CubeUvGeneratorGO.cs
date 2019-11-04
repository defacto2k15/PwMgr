using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.MeshGeneration
{
    public class CubeUvGeneratorGO : MonoBehaviour
    {
        public Mesh OriginalMesh;
        public string NewMeshName;

        public void Start()
        {
            Debug.Log(OriginalMesh.normals.Select(c => c.ToString()).Aggregate((a,b) => a+" "+b));
            Debug.Log(OriginalMesh.vertices.Select(c => c.ToString()).Aggregate((a,b) => a+" "+b));

            var newUvArray = new Vector2[OriginalMesh.uv.Length];
            for (var i = 0; i < OriginalMesh.vertexCount; i++)
            {
                var vertexPosition = OriginalMesh.vertices[i];
                var normal = OriginalMesh.normals[i];
                var EPSILON = 0.0001f;

                if ((normal - Vector3.up).magnitude < EPSILON)
                {
                    newUvArray[i] = PlaceOnGrid(0,3, GenerateUvOn2DSpace(new Vector2(vertexPosition.x, vertexPosition.z)));
                }
                else if ((normal - Vector3.down).magnitude < EPSILON)
                {
                    newUvArray[i] = PlaceOnGrid(1,3,GenerateUvOn2DSpace(new Vector2(vertexPosition.x, vertexPosition.z)));
                }
                else if ((normal - Vector3.left).magnitude < EPSILON)
                {
                    newUvArray[i] = PlaceOnGrid(2,3, GenerateUvOn2DSpace(new Vector2(vertexPosition.y, vertexPosition.z)));
                }
                else if ((normal - Vector3.right).magnitude < EPSILON)
                {
                    newUvArray[i] = PlaceOnGrid(3,3, GenerateUvOn2DSpace(new Vector2(vertexPosition.y, vertexPosition.z)));
                }
                else if ((normal - Vector3.back).magnitude < EPSILON)
                {
                    newUvArray[i] = PlaceOnGrid(4,3,GenerateUvOn2DSpace(new Vector2(vertexPosition.x, vertexPosition.y)));
                }

                if ((normal - Vector3.forward).magnitude < EPSILON)
                {
                    newUvArray[i] = PlaceOnGrid(5,3,GenerateUvOn2DSpace(new Vector2(vertexPosition.x, vertexPosition.y)));
                }
            }

            var newMesh = new Mesh {vertices = OriginalMesh.vertices, triangles = OriginalMesh.triangles, tangents = OriginalMesh.tangents, uv = newUvArray, normals = OriginalMesh.normals};
            newMesh.RecalculateBounds();

            MyAssetDatabase.CreateAndSaveAsset(newMesh,  $"Assets/NPRResources/{NewMeshName}.asset");
        }

        private Vector2 PlaceOnGrid(int cellIndex, int cellOnSideCount, Vector2 uv)
        {
            var uvWidth = 1f / cellOnSideCount;
            var offset = new Vector2(cellIndex % cellOnSideCount, Mathf.FloorToInt( ((float)cellIndex)/cellOnSideCount )) * uvWidth;

            return offset + uv * uvWidth;
        }

        public Vector2 GenerateUvOn2DSpace(Vector2 position)
        {
            return new Vector2(position.x + 0.5f, position.y + 0.5f);
        }
    }
}
