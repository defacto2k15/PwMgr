using System.Collections.Generic;
using Assets.Trees.Generation.ETree;
using UnityEngine;

namespace Assets.Trees.Generation
{
    public class ETreeClanTemplate
    {
        private List<ETreePyramidTemplate> _treePyramids;

        public ETreeClanTemplate(List<ETreePyramidTemplate> treePyramids)
        {
            _treePyramids = treePyramids;
        }

        public List<ETreePyramidTemplate> TreePyramids => _treePyramids;
    }


    public class ETreePyramidTemplate
    {
        private readonly EBillboardTextureArray _billboardTextureArray;
        private readonly Mesh _fullTreeMesh;
        private readonly Mesh _simplifiedTreeMesh;

        public ETreePyramidTemplate(EBillboardTextureArray billboardTextureArray, Mesh fullTreeMesh, Mesh simplifiedTreeMesh)
        {
            _billboardTextureArray = billboardTextureArray;
            _fullTreeMesh = fullTreeMesh;
            _simplifiedTreeMesh = simplifiedTreeMesh;
        }

        public EBillboardTextureArray BillboardTextureArray => _billboardTextureArray;

        public Mesh FullTreeMesh => _fullTreeMesh;

        public Mesh SimplifiedTreeMesh => _simplifiedTreeMesh;
    }
}