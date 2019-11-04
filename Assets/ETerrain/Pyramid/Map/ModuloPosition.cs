using Assets.Utils;

namespace Assets.ETerrain.Pyramid.Map
{
    public class ModuloPosition
    {
        private readonly IntVector2 _slotMapSize;
        private readonly IntVector2 _moduledPosition;

        public ModuloPosition(IntVector2 slotMapSize, IntVector2 position)
        {
            _slotMapSize = slotMapSize;
            _moduledPosition = new IntVector2(MyMathUtils.Mod(position.X , _slotMapSize.X), MyMathUtils.Mod(position.Y , _slotMapSize.Y));
        }

        public int X => _moduledPosition.X;
        public int Y => _moduledPosition.Y;
        public IntVector2 ModuledPosition => _moduledPosition;

        public ModuloPosition GetNeighbourPosition(SegmentNeighbourhoodDiregment diregment)
        {
            return new ModuloPosition(_slotMapSize, _moduledPosition+diregment.Movement);
        }
    }
}