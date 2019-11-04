using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Grass
{
    class GrassEntitiesWithMaterials
    {
        private readonly List<GrassEntity> _entities;
        private readonly Material _material;
        private readonly Mesh _mesh;
        private readonly ContainerType _containerType;

        public GrassEntitiesWithMaterials(List<GrassEntity> entities, Material material, Mesh mesh,
            ContainerType containerType)
        {
            this._entities = entities;
            this._material = material;
            this._mesh = mesh;
            this._containerType = containerType;
        }

        public List<GrassEntity> Entities
        {
            get { return _entities; }
        }

        public Material Material
        {
            get { return _material; }
        }

        public Mesh Mesh
        {
            get { return _mesh; }
        }

        public ContainerType ContainerType
        {
            get { return _containerType; }
        }
    }
}