using Assets.Utils;

namespace Assets.Grass.Lod
{
    internal class LodEntitySplat
    {
        private readonly MapAreaPosition _position;
        private readonly IEntityLodResolver _entityLodResolver;
        private readonly IEntitySplatGenerator _splatGenerator;
        private readonly IEntitySplatUpdater _splatUpdater;
        private IEntitySplat _splat;
        private int _entityLodLevel;
        private int _referencesCount;

        public LodEntitySplat(MapAreaPosition position, IEntityLodResolver entityLodResolver,
            IEntitySplatGenerator splatGenerator, IEntitySplatUpdater splatUpdater)
        {
            this._position = position;
            this._entityLodResolver = entityLodResolver;
            this._splatGenerator = splatGenerator;
            _splatUpdater = splatUpdater;
            _referencesCount = 1;
        }

        private LodEntitySplat(MapAreaPosition position, IEntityLodResolver entityLodResolver,
            IEntitySplatGenerator splatGenerator,
            int entityLodLevel, IEntitySplat newSplat, IEntitySplatUpdater splatUpdater) : this(position,
            entityLodResolver, splatGenerator, splatUpdater)
        {
            this._splat = newSplat;
            this._entityLodLevel = entityLodLevel;
        }

        public LodEntitySplat UpdateLod(int newLod)
        {
            int entityLod = _entityLodResolver.GetEntityLod(newLod);
            if (_entityLodLevel != entityLod)
            {
                var newLevel = entityLod;
                var newSplat =
                    _splatUpdater.UpdateSplat(_splat, newLevel); //_splatGenerator.GenerateSplat(_position, newLevel);
                return new LodEntitySplat(_position, _entityLodResolver, _splatGenerator, newLevel, newSplat,
                    _splatUpdater);
            }
            else
            {
                _referencesCount = _referencesCount + 1;
                return this;
            }
        }

        public void Remove()
        {
            _referencesCount--;
            if (_referencesCount <= 0)
            {
                _splat.Remove();
            }
        }

        public void Initialize(int lodLevel)
        {
            _entityLodLevel = lodLevel;
            _splat = _splatGenerator.GenerateSplat(_position, _entityLodLevel);
        }

        public void Enable()
        {
            _splat.Enable();
        }
    }
}