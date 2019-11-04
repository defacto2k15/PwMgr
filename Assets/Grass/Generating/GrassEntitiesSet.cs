using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass
{
    class GrassEntitiesSet
    {
        private readonly List<GrassEntity> _entities;
        private MyTransformTriplet _parentTriplet = new MyTransformTriplet(Vector3.zero, Vector3.zero, Vector3.one);

        public GrassEntitiesSet(List<GrassEntity> entities)
        {
            _entities = entities;
        }

        public List<GrassEntity> EntitiesBeforeTransform
        {
            get { return _entities; }
        }

        public List<GrassEntity> EntitiesAfterTransform
        {
            get
            {
                var transformedTriplets = TransformUtils.MakeParentChildTransformations(_parentTriplet,
                    _entities.Select(c => new MyTransformTriplet(
                        c.Position,
                        c.Rotation, c.Scale)).ToList());

                for (var i = 0; i < _entities.Count; i++)
                {
                    _entities[i].Position = transformedTriplets[i].Position;
                    _entities[i].Rotation = transformedTriplets[i].Rotation;
                    _entities[i].Scale = transformedTriplets[i].Scale;
                }
                return _entities;
            }
        }

        public Vector3 Rotation
        {
            get { return _parentTriplet.Rotation; }
            set { _parentTriplet.Rotation = value; }
        }

        public Vector3 Position
        {
            get { return _parentTriplet.Position; }
            set { _parentTriplet.Position = value; }
        }

        public void TranslateBy(Vector3 vector3)
        {
            // doto delete
            _entities.ForEach(e => e.Position = e.Position + vector3);
        }
    }
}