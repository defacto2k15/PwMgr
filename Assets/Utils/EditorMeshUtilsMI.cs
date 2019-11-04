using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Utils
{
    public class EditorMeshUtilsMI : MonoBehaviour
    {

#if UNITY_EDITOR
	    [MenuItem("Tools/UniquefyVertices")]
        public static void UniquefyVertices()
	    {
            string targetPath = "Assets/NPRResources/Venus-Unique.asset";

	        var objects = Selection.objects;

	        if (objects.Length != 1)
	        {
	            Debug.LogError("E854 Must select exacly one object  - one with mesh");
	            return;
	        }
            var obj = objects[0];

	        Mesh oldMesh = null;
	        if (obj is GameObject)
	        {
	            var go = obj as GameObject;
	            if (go.GetComponent<MeshFilter>() != null)
	            {
	                oldMesh = go.GetComponent<MeshFilter>().mesh;
	            }
	            else
	            {
	                oldMesh = go.GetComponentInChildren<MeshFilter>().mesh;
	            }
	        }

            if(oldMesh == null)
	        {
	            Preconditions.Fail($"Selected object (${obj}) is not mesh/does not have mesh");
	            return;
	        }

	        if (AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) != null)
	        {
	            Preconditions.Fail($"There arleady is object at (${targetPath})");
	        }

	        var newMesh = UniquefyMesh(oldMesh);
            AssetDatabase.CreateAsset(newMesh, targetPath);
            Debug.Log("Created asset at "+targetPath);
	    }
#endif

        public static Mesh UniquefyMesh(Mesh input)
        {
            var newMesh = new Mesh();

            var trianglesIndexCount = input.triangles.Length;
            int[] newTriangles = new int[trianglesIndexCount];
            Vector3[] newVertices = new Vector3[trianglesIndexCount];
            Vector3[] newNormals = new Vector3[trianglesIndexCount];
            Vector4[] newTangents = new Vector4[trianglesIndexCount];
            Vector2[] newUv = new Vector2[trianglesIndexCount];

            var sw = new MyStopWatch();
            sw.StartSegment("a1");
            for (int i = 0; i < trianglesIndexCount; i++)
            {
                newTriangles[i] = i;
                var j = input.triangles[i];

                newVertices[i] = input.vertices[j];
                newNormals[i] = input.normals[j];
                newTangents[i] = input.tangents[j];
                if (input.uv.Length > 0)
                {
                    newUv[i] = input.uv[j];
                }
            }
            newMesh.vertices = newVertices;
            newMesh.triangles = newTriangles;
            newMesh.normals = newNormals;
            newMesh.tangents = newTangents;
            newMesh.uv = newUv;

            newMesh.RecalculateBounds();
            newMesh.UploadMeshData(false);

            return newMesh;
        }
    }
}
