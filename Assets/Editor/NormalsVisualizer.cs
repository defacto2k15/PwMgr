using Assets.NPR;
using Assets.NPR.Curvature;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PrincipalCurvatureInjectOC))]
public class NormalsVisualizer : Editor {

    private Mesh mesh;
    private bool _shouldDraw = false;
    private float _lineLength = 0.1f;
    private float startTime;
    private MeshCurvatureDetailSE _curvatureDetail;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var newShouldDraw = EditorGUILayout.Toggle("Should draw", _shouldDraw);
        if (!_shouldDraw && newShouldDraw)
        {
            startTime = (float) EditorApplication.timeSinceStartup;
        }

        _shouldDraw = newShouldDraw;
        _lineLength = EditorGUILayout.Slider("LineLength", _lineLength, 0, 1);
        if (GUILayout.Button("Reset curvature data"))
        {
            var pci = target as PrincipalCurvatureInjectOC;
            pci.ResetMeshDetails(pci.GetComponent<MeshFilter>().sharedMesh);
        }
    }

    void OnEnable() {
        var pci = target as PrincipalCurvatureInjectOC;
        _curvatureDetail = pci.CurvatureDetail;
        var mf = pci.GetComponent<MeshFilter>();
        if (mf != null) {
            mesh = mf.sharedMesh;
        }
    }

    void OnSceneGUI() {
        if (mesh == null) {
            return;
        }

        if (_shouldDraw )
        {
            Handles.matrix = (target as PrincipalCurvatureInjectOC).GetComponent<MeshFilter>().transform.localToWorldMatrix;
            for (int i = 0; i < mesh.vertexCount; i++)
            {

                Handles.color = Color.yellow;
                var verticle = mesh.vertices[i];
                var length = _lineLength;

                var dir = _curvatureDetail.PrincipalDirection1[i];
                Handles.DrawLine(
                    verticle ,
                    verticle + dir * length);

                Handles.color = Color.red;
                var dir2 = _curvatureDetail.PrincipalDirection2[i];
                Handles.DrawLine(
                    verticle ,
                    verticle + dir2 * length);

                //Handles.DrawLine(
                //    mesh.vertices[i],
                //    mesh.vertices[i] + mesh.normals[i] * _lineLength);
            }

            if (EditorApplication.timeSinceStartup - startTime > 1)
            {
                _shouldDraw = false;
            }
        }
    }
}
