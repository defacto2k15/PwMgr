using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass.Container
{
    class GameObjectEntitySplat : IEntitySplat
    {
        private readonly List<GameObject> _objects;
        private readonly GameObjectGrassInstanceContainer _container;
        private readonly int? _splatId;

        public GameObjectEntitySplat(List<GameObject> objects, GameObjectGrassInstanceContainer container,
            int? splatId = null)
        {
            _splatId = splatId;
            _objects = objects;
            _container = container;
        }

        public IEnumerable<GameObject> GameObjects
        {
            get { return _objects; }
        }

        public void Remove()
        {
            Preconditions.Assert(_splatId.HasValue, "Splatid is not set, cant remove");
            _container.RemoveSplat(_splatId.Value);
        }

        public IEntitySplat Copy()
        {
            return
                new GameObjectEntitySplat(_objects,
                    _container); //todo this is ugly, but for now must work. No GameObjects grass updating
        }

        public void SetMesh(Mesh newMesh)
        {
            foreach (var gameObject in _objects)
            {
                gameObject.GetComponent<MeshFilter>().mesh = newMesh;
            }
        }

        public void Enable()
        {
            // do nothing
        }
    }
}