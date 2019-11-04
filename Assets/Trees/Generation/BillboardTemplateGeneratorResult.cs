using System.Collections.Generic;
using UnityEngine;

namespace Assets.Trees.Generation
{
    public class BillboardTemplateGeneratorResult
    {
        private List<Texture2D> _generatedTextures;
        private Vector3 _scaleOffsets;

        public BillboardTemplateGeneratorResult(List<Texture2D> generatedTextures, Vector3 scaleOffsets)
        {
            _generatedTextures = generatedTextures;
            _scaleOffsets = scaleOffsets;
        }

        public List<Texture2D> GeneratedTextures
        {
            get { return _generatedTextures; }
        }

        public Vector3 ScaleOffsets
        {
            get { return _scaleOffsets; }
        }
    }
}