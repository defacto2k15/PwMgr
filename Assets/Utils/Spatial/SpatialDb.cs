using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Grass2.Growing;
using Assets.Grass2.IntensitySampling;
using Assets.Grass2.IntenstityDb;
using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Utils.Spatial
{
    public interface ISpatialDb<T>
    {
        Task<UvdCoordedPart<T>> ProvidePartsAt(MyRectangle queryArea);
    }

    public class SpatialDb<T> : ISpatialDb<T>
    {
        private IStoredPartsGenerator<T> _partsGenerator;
        private SpatialDbConfiguration _configuration;
        private StoredPartsRepository<T> _partsRepository;

        public SpatialDb(IStoredPartsGenerator<T> partsGenerator, SpatialDbConfiguration configuration)
        {
            _partsGenerator = partsGenerator;
            _configuration = configuration;
            _partsRepository = new StoredPartsRepository<T>();
        }

        public async Task<UvdCoordedPart<T>> ProvidePartsAt(MyRectangle queryArea)
        {
            var alignedArea = SpatialDbUtils.AlignQueryArea(queryArea, _configuration.QueryingCellSize);

            var retrivedPart = await _partsRepository.TryRetriveAsync(alignedArea.CoveredArea);
            if (retrivedPart == null)
            {
                _partsRepository.AddPartsPromise(alignedArea.CoveredArea);
                var generatedPart = await _partsGenerator.GeneratePartAsync(alignedArea.CoveredArea);
                _partsRepository.AddPart(alignedArea.CoveredArea, generatedPart.Part);

                retrivedPart = generatedPart.Part;
            }

            return new UvdCoordedPart<T>()
            {
                CoordedPart = new CoordedPart<T>()
                {
                    Part = retrivedPart,
                    Coords = alignedArea.CoveredArea
                },
                Uv = alignedArea.SubelementUv
            };
        }
    }

    public static class SpatialDbUtils
    {
        public static CoveredAreaWithSubelementUv AlignQueryArea(MyRectangle queryArea,
            Vector2 queryingCellSize)
        {
            var alignedX = Mathf.FloorToInt(queryArea.X / queryingCellSize.x) *
                           queryingCellSize.x;
            var alignedY = Mathf.FloorToInt(queryArea.Y / queryingCellSize.y) *
                           queryingCellSize.y;

            var alignedArea = new MyRectangle(
                alignedX,
                alignedY,
                queryingCellSize.x,
                queryingCellSize.y
            );

            var subelementUv = RectangleUtils.CalculateSubelementUv(alignedArea, queryArea);
            Preconditions.Assert(RectangleUtils.IsNormalizedUv(subelementUv),
                $"Given query area: {queryArea} cannot be aligned to querying unit size {queryingCellSize}");

            return new CoveredAreaWithSubelementUv()
            {
                CoveredArea = alignedArea,
                SubelementUv = subelementUv
            };
        }

        public class CoveredAreaWithSubelementUv
        {
            public MyRectangle CoveredArea;
            public MyRectangle SubelementUv;
        }
    }

    public class CacheingSpatialDb<T> : ISpatialDb<T>
    {
        private SpatialDbConfiguration _configuration;
        private ISpatialDb<T> _internalSpatialDb;

        private MyRectangle _lastAlignedQuery = null;
        private CoordedPart<T> _lastValue;

        public CacheingSpatialDb(ISpatialDb<T> internalSpatialDb, SpatialDbConfiguration configuration)
        {
            _internalSpatialDb = internalSpatialDb;
            _configuration = configuration;
        }

        public async Task<UvdCoordedPart<T>> ProvidePartsAt(MyRectangle queryArea)
        {
            var alignedArea = SpatialDbUtils.AlignQueryArea(queryArea, _configuration.QueryingCellSize);
            if (_lastAlignedQuery != null && _lastAlignedQuery.Equals(alignedArea.CoveredArea))
            {
                return new UvdCoordedPart<T>()
                {
                    CoordedPart = _lastValue,
                    Uv = alignedArea.SubelementUv
                };
            }
            else
            {
                _lastAlignedQuery = alignedArea.CoveredArea;
                var outValue = await _internalSpatialDb.ProvidePartsAt(queryArea);
                _lastValue = outValue.CoordedPart;
                return outValue;
            }
        }
    }

    public class SpatialDbConfiguration
    {
        public Vector2 QueryingCellSize;
    }

    public interface IStoredPartsGenerator<T>
    {
        Task<CoordedPart<T>> GeneratePartAsync(MyRectangle queryArea);
    }

    public class CoordedPart<T>
    {
        public MyRectangle Coords;
        public T Part;
    }

    public class UvdCoordedPart<T>
    {
        public CoordedPart<T> CoordedPart;
        public MyRectangle Uv;
    }
}