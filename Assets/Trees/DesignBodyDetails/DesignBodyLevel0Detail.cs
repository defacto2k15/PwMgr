using UnityEngine;

namespace Assets.Trees.DesignBodyDetails
{
    public class DesignBodyLevel0Detail
    {
        public Vector2 Pos2D;
        public float Size;
        public float Radius;
        public VegetationSpeciesEnum SpeciesEnum;

        public override string ToString()
        {
            return
                $"{nameof(Pos2D)}: {Pos2D}, {nameof(Size)}: {Size}, {nameof(Radius)}: {Radius}, {nameof(SpeciesEnum)}: {SpeciesEnum}";
        }
    }
}