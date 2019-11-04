using Assets.Heightmaps.Ring1.valTypes;

namespace Assets.Trees.Placement.BiomesMap
{
    public interface IVegetationBiomesMap
    {
        VegetationBiomeLevelComposition RetriveBiomesAt(MyRectangle queryRectangle);
    }
}