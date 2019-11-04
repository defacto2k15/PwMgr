using System;
using System.Collections.Generic;
using Assets.Ring2.BaseEntities;
using UnityEngine;

namespace Assets.Ring2.Devising
{
    public class Ring2MaterialRepositiory
    {
        private Ring2PlateShaderRepository _shaderRepository;
        private List<KeywordsMaterialPair> _pairsList = new List<KeywordsMaterialPair>();
        private string _shaderName;

        public Ring2MaterialRepositiory(Ring2PlateShaderRepository shaderRepository, string shaderName)
        {
            _shaderRepository = shaderRepository;
            _shaderName = shaderName;
        }

        public Material RetriveMaterial(ShaderKeywordSet keywords)
        {
            foreach (var pair in _pairsList)
            {
                bool equal = keywords.Equals(pair.Keywords);
                if (equal)
                {
                    return pair.Material;
                }
            }

            var newMaterial = new Material(_shaderRepository.GetShader(_shaderName));
            foreach (var keyword in keywords.Keywords)
            {
                newMaterial.EnableKeyword(keyword);
            }
            _pairsList.Add(new KeywordsMaterialPair(keywords, newMaterial));
            return newMaterial;
        }

        private class KeywordsMaterialPair
        {
            private ShaderKeywordSet _keywords;
            private Material _material;

            public KeywordsMaterialPair(ShaderKeywordSet keywords, Material material)
            {
                _keywords = keywords;
                _material = material;
            }

            public ShaderKeywordSet Keywords
            {
                get { return _keywords; }
            }

            public Material Material
            {
                get { return _material; }
            }
        }
    }
}