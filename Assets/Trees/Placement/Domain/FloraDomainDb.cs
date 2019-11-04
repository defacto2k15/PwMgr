using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Grass2.Billboards;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Spatial;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Trees.Placement.Domain
{
    public class FloraDomainDbProxy : BaseOtherThreadProxy
    {
        private Dictionary<HabitatAndZoneType, ISpatialDb<FloraDomainIntensityArea>> _db;

        public FloraDomainDbProxy(Dictionary<HabitatAndZoneType, ISpatialDb<FloraDomainIntensityArea>> db) :
            base("FloraDomainDbProxy", false)
        {
            _db = db;
        }

        public Task<FloraDomainIntensityPack> Query(MyRectangle queryArea, HabitatType habitatType,
            HeightZoneType zoneType)
        {
            var tcs = new TaskCompletionSource<FloraDomainIntensityPack>();
            PostAction(async () =>
            {
                var key = new HabitatAndZoneType()
                {
                    HabitatType = habitatType,
                    ZoneType = zoneType
                };
                var x = await _db[key].ProvidePartsAt(queryArea);
                tcs.SetResult(new FloraDomainIntensityPack(x.Uv, x.CoordedPart.Part));
            });
            return tcs.Task;
        }

        public Task<FloraDomainIntensitySample> Query(Vector2 point, HabitatType habitatType, HeightZoneType zoneType)
        {
            var tcs = new TaskCompletionSource<FloraDomainIntensitySample>();
            PostAction(async () =>
            {
                var key = new HabitatAndZoneType()
                {
                    HabitatType = habitatType,
                    ZoneType = zoneType
                };
                var queryArea = new MyRectangle(point.x, point.y, 0.001f, 0.001f);
                var x = await _db[key].ProvidePartsAt(queryArea);
                var part = x.CoordedPart.Part;
                var toReturn = part.Sample(x.Uv.Center);

                tcs.SetResult(toReturn);
            });
            return tcs.Task;
        }
    }

    public class HabitatAndZoneType
    {
        public HabitatType HabitatType;
        public HeightZoneType ZoneType;

        protected bool Equals(HabitatAndZoneType other)
        {
            return HabitatType == other.HabitatType && ZoneType == other.ZoneType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HabitatAndZoneType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) HabitatType * 397) ^ (int) ZoneType;
            }
        }
    }

    public class FloraDomainIntensityArea
    {
        private Dictionary<FloraDomainType, IntensityFieldFigure> _domainIntensityFigures;

        public FloraDomainIntensityArea(Dictionary<FloraDomainType, IntensityFieldFigure> domainIntensityFigures)
        {
            _domainIntensityFigures = domainIntensityFigures;
        }

        public FloraDomainIntensitySample Sample(Vector2 uv)
        {
            return new FloraDomainIntensitySample(
                _domainIntensityFigures.ToDictionary(c => c.Key, c => c.Value.GetPixelWithUv(uv)));
        }

        public Dictionary<FloraDomainType, IntensityFieldFigure> DomainIntensityFigures => _domainIntensityFigures;
    }

    public class FloraDomainIntensityPack
    {
        private MyRectangle _uv;
        private FloraDomainIntensityArea _intensityArea;

        public FloraDomainIntensityPack(MyRectangle uv, FloraDomainIntensityArea intensityArea)
        {
            _uv = uv;
            _intensityArea = intensityArea;
        }

        public FloraDomainIntensitySample Sample(Vector2 uv)
        {
            var finalUv = RectangleUtils.CalculateSubPosition(_uv, uv);
            return _intensityArea.Sample(finalUv);
        }

        public FloraDomainIntensityArea IntensityArea => _intensityArea;
    }

    public class FloraDomainIntensitySample
    {
        private Dictionary<FloraDomainType, float> _intensities;

        public FloraDomainIntensitySample(Dictionary<FloraDomainType, float> intensities)
        {
            _intensities = intensities;
        }

        public float Retrive(FloraDomainType type)
        {
            Preconditions.Assert(_intensities.ContainsKey(type), "There is not calcualted intensity for type: " + type);
            return _intensities[type];
        }
    }

    public enum FloraDomainType
    {
        Domain1,
        Domain2,
        Domain3,
        Domain4,

        BeechForrest,
        BeechFirSpruceForrest,
        SpruceOnlyForrest,
        LowClearing,
        Pinus,
        HighClearing
    }
}