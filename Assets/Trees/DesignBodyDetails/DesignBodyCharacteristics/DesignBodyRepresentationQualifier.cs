using Assets.Trees.RuntimeManagement;

namespace Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics
{
    public struct DesignBodyRepresentationQualifier
    {
        public VegetationSpeciesEnum SpeciesEnum;
        public VegetationDetailLevel DetailLevel;

        public DesignBodyRepresentationQualifier(VegetationSpeciesEnum speciesEnum, VegetationDetailLevel detailLevel)
        {
            SpeciesEnum = speciesEnum;
            DetailLevel = detailLevel;
        }

        public bool Equals(DesignBodyRepresentationQualifier other)
        {
            return SpeciesEnum == other.SpeciesEnum && DetailLevel == other.DetailLevel;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is DesignBodyRepresentationQualifier && Equals((DesignBodyRepresentationQualifier) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) SpeciesEnum * 397) ^ (int) DetailLevel;
            }
        }
    }
}