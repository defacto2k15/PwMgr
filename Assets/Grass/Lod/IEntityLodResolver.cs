namespace Assets.Grass.Lod
{
    internal interface IEntityLodResolver
    {
        int GetEntityLod(int globalLod);
    }
}