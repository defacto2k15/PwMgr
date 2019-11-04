using Assets.Utils;

namespace Assets.Grass.Lod
{
    internal interface IEntitySplatGenerator
    {
        IEntitySplat GenerateSplat(MapAreaPosition position, int entityLodLevel);
    }
}