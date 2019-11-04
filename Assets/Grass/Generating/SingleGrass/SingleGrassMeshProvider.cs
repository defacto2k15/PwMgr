using UnityEngine;

namespace Assets.Grass.Generating
{
    class SingleGrassMeshProvider : IMeshProvider
    {
        private Assets.Grass.GrassMeshGenerator _meshGenerator;

        public SingleGrassMeshProvider(Assets.Grass.GrassMeshGenerator meshGenerator)
        {
            this._meshGenerator = meshGenerator;
        }

        public Mesh GetMesh(int lod)
        {
            Debug.Log("L12 Getting mesh with lod " + lod);
            return _meshGenerator.GetGrassBladeMesh(lod);
        }
    }
}