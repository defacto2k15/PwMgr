using Assets.Utils;

namespace Assets.Grass.Generating
{
    internal interface IEntityPositionProvider
    {
        void SetPosition(GrassEntitiesSet aGrass, MapAreaPosition globalPosition);
    }
}