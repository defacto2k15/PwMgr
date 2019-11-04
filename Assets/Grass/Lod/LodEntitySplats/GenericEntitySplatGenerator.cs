using System;
using Assets.Grass.Lod;
using Assets.Utils;

namespace Assets.Grass
{
    internal class GenericEntitySplatGenerator : ILodEntitySplatGenerator
    {
        private Func<MapAreaPosition, LodEntitySplat> _generatingFunc;

        public GenericEntitySplatGenerator(Func<MapAreaPosition, LodEntitySplat> generatingFunc)
        {
            _generatingFunc = generatingFunc;
        }

        public LodEntitySplat Generate(MapAreaPosition position)
        {
            return _generatingFunc(position);
        }
    }
}