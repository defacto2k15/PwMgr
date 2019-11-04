using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Grass.Instancing;
using Assets.ShaderUtils;
using UnityEngine;

namespace Assets.Grass.Container
{
    class GameObjectGrassInstanceContainer : IGrassInstanceContainer
    {
        private readonly GameObjectGrassInstanceGenerator _generator = new GameObjectGrassInstanceGenerator();

        private readonly Dictionary<int, GameObjectEntitySplat> _gameObjectSplats =
            new Dictionary<int, GameObjectEntitySplat>();

        private int _lastSplatId = 0;

        public void Draw()
        {
        }

        public IEntitySplat AddGrassEntities(GrassEntitiesWithMaterials grassEntitiesWithMaterials)
        {
            _lastSplatId++;
            var newEntitySplat = new GameObjectEntitySplat(_generator.Generate(grassEntitiesWithMaterials), this,
                _lastSplatId);
            _gameObjectSplats[_lastSplatId] = newEntitySplat;
            return newEntitySplat;
        }

        public void SetGlobalColor(string name, Color value)
        {
            ForeachObject((aObject) => aObject.GetComponent<Renderer>().material.SetColor(name, value));
        }

        public void SetGlobalUniform(ShaderUniformName name, float value)
        {
            ForeachObject((aObject) => aObject.GetComponent<Renderer>().material.SetFloat(name.ToString(), value));
        }

        public void SetGlobalUniform(ShaderUniformName name, Vector4 value)
        {
            ForeachObject((aObject) => aObject.GetComponent<Renderer>().material.SetVector(name.ToString(), value));
        }

        private void ForeachObject(Action<GameObject> action)
        {
            foreach (var pair in _gameObjectSplats)
            {
                foreach (var obj in pair.Value.GameObjects)
                {
                    action(obj);
                }
            }
        }

        public void RemoveSplat(int splatId)
        {
            _gameObjectSplats.Remove(splatId);
        }
    }
}