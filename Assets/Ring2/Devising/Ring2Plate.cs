using UnityEngine;

namespace Assets.Ring2.Devising
{
    public class Ring2Plate
    {
        private Mesh _mesh;
        private Matrix4x4 _transformMatrix;
        private MaterialTemplate _materialTemplate;

        public Ring2Plate(Mesh mesh, Matrix4x4 transformMatrix, MaterialTemplate materialTemplate)
        {
            _mesh = mesh;
            _transformMatrix = transformMatrix;
            _materialTemplate = materialTemplate;
        }


        public Mesh Mesh
        {
            get { return _mesh; }
        }

        public Matrix4x4 TransformMatrix
        {
            get { return _transformMatrix; }
        }

        public MaterialTemplate MaterialTemplate
        {
            get { return _materialTemplate; }
        }
    }
}