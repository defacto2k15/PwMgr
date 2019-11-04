using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Ring2.BaseEntities;
using UnityEngine;

namespace Assets.Ring2.Devising
{
    public class Ring2MultishaderMaterialRepository
    {
        private Dictionary<string, Ring2MaterialRepositiory> _materialRepositiories =
            new Dictionary<string, Ring2MaterialRepositiory>();

        public Ring2MultishaderMaterialRepository(Ring2PlateShaderRepository shaderRepository, List<string> shaderNames)
        {
            foreach (var shaderName in shaderNames)
            {
                _materialRepositiories[shaderName] = new Ring2MaterialRepositiory(shaderRepository, shaderName);
            }
        }

        public Material RetriveMaterial(string shaderName, ShaderKeywordSet keywords)
        {
            return _materialRepositiories[shaderName].RetriveMaterial(keywords);
        }
    }
}