using Assets.Utils;
using GeoAPI.Geometries;
using UnityEngine;

namespace Assets.Ring2.Devising
{
    public class Ring2PatchMesh
    {
        private Mesh _mesh;
        private MyTransformTriplet _transformTriplet;

        public Ring2PatchMesh(Mesh mesh, MyTransformTriplet transformTriplet)
        {
            _mesh = mesh;
            _transformTriplet = transformTriplet;
        }


        public Mesh Mesh
        {
            get { return _mesh; }
        }

        public MyTransformTriplet TransformTriplet
        {
            get { return _transformTriplet; }
        }
    }
}