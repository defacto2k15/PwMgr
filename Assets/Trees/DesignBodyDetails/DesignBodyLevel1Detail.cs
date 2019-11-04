using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Trees.SpotUpdating;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails
{
    public class DesignBodyLevel1Detail
    {
        public VegetationSpeciesEnum SpeciesEnum;
        public VegetationDetailLevel DetailLevel;
        public float Size;
        public float Radius;
        public Vector2 Pos2D;
        public int Seed;

        public static DesignBodyLevel1Detail FromLevel0(DesignBodyLevel0Detail detail, VegetationDetailLevel level) 
        {
            return new DesignBodyLevel1Detail()
            {
                DetailLevel = level,
                Pos2D = detail.Pos2D,
                Radius = detail.Radius,
                Seed = CreateSeedFromPosition(detail.Pos2D),
                Size = detail.Size,
                SpeciesEnum = detail.SpeciesEnum,
            };
        }

        private static int CreateSeedFromPosition(Vector2 pos2D)
        {
            unchecked
            {
                return (pos2D.x.GetHashCode() * 397) ^ pos2D.y.GetHashCode();
            }
        }
    }


    public class DesignBodyLevel1DetailWithSpotModification
    {
        public DesignBodyLevel1Detail Level1Detail;
        public DesignBodySpotModification SpotModification;
    }
}