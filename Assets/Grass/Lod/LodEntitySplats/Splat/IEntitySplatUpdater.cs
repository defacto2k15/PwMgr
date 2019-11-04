using UnityEngine;

namespace Assets.Grass.Lod
{
    interface IEntitySplatUpdater
    {
        IEntitySplat UpdateSplat(IEntitySplat splat, int newLevel);
    }

    class SingleGrassEntitySplatUpdater : IEntitySplatUpdater
    {
        private IMeshProvider meshProvider;

        public SingleGrassEntitySplatUpdater(IMeshProvider meshProvider)
        {
            this.meshProvider = meshProvider;
        }

        public IEntitySplat UpdateSplat(IEntitySplat splat, int newLevel)
        {
            Mesh newMesh = meshProvider.GetMesh(newLevel);
            var copy = splat.Copy();
            copy.SetMesh(newMesh);
            return copy;
        }
    }

    class DummyEntitySplatUpdater : IEntitySplatUpdater
    {
        public IEntitySplat UpdateSplat(IEntitySplat splat, int newLevel)
        {
            return splat.Copy();
        }
    }
}