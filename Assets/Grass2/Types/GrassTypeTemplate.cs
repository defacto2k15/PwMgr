using Assets.Random;
using Assets.Utils;

namespace Assets.Grass2.Types
{
    public class GrassTypeTemplate
    {
        public Triplet<RandomCharacteristics> Color;
        public Pair<RandomCharacteristics> FlatSize;
        public RandomCharacteristics InitialBendingValue;
        public RandomCharacteristics InitialBendingStiffness;
        public float InstancesPerUnitSquare;
    }
}