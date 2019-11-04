using System.Collections.Generic;
using System.Linq;
using Assets.Grass.Lod;
using Assets.Utils;

namespace Assets.Grass
{
    internal class LodGroupsProvider : ILodGroupsProvider
    {
        private readonly List<ILodEntitySplatGenerator> _lodEntitySplatGenerators;

        public LodGroupsProvider(List<ILodEntitySplatGenerator> lodEntitySplatGenerators)
        {
            _lodEntitySplatGenerators = lodEntitySplatGenerators;
        }

        public LodGroup GenerateLodGroup(MapAreaPosition position)
        {
            return new LodGroup(_lodEntitySplatGenerators.Select(c => c.Generate(position)).ToList(), position);
        }
    }
}