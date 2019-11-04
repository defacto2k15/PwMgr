using System.Collections.Generic;
using System.Linq;
using Assets.Utils;
using Assets.Utils.Quadtree;
using Enyim.Collections;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;
using Envelope = GeoAPI.Geometries.Envelope;

namespace Assets.TerrainMat
{
    public class BiomeInstancesContainer
    {
        private readonly MyQuadtree<BiomeInstanceInfo> _tree;
        private readonly BiomesContainerConfiguration _configuration;

        public BiomeInstancesContainer(BiomesContainerConfiguration configuration)
        {
            _configuration = configuration;
            _tree = new MyQuadtree<BiomeInstanceInfo>();
        }

        public void AddBiome(BiomeInstanceInfo instanceInfo)
        {
            Debug.Log("Biome was added!");
            _tree.Add(instanceInfo);
        }

        public List<List<BiomeInstanceCharacteristicsWithArea>> BulkGetBiomeTypesIn(List<MyPolygon> positions)
        {
            int index = 0;
            var toReturn = positions.AsParallel().Select(c =>
            {
                return GetBiomeTypeIn(c);
            }).ToList();
            return toReturn;
        }

        public List<BiomeInstanceCharacteristicsWithArea> GetBiomeTypeIn(MyPolygon intrestingPolygon)
        {
            var msw = new MyStopWatch();
            msw.StartSegment("QueryBounding");
            var queryPolygonEnvelope = intrestingPolygon.CalculateEnvelope();
            var foundBoundingElements =
                _tree.Query /*WithIntersection*/(MyNetTopologySuiteUtils.ToGeometryEnvelope(queryPolygonEnvelope));

            var ntsQueryPolygon = MyNetTopologySuiteUtils.ToPolygon(intrestingPolygon);

            List<BiomeInstanceCharacteristicsWithArea> biomeAndArea;

            var searchQuality = ComputeSearchQuality(ntsQueryPolygon);
            msw.StartSegment("BiomesSearch");
            if (searchQuality == SearchQuality.Low)
            {
                biomeAndArea = LowQualityBiomesSearch(ntsQueryPolygon, foundBoundingElements);
            }
            else
            {
                biomeAndArea = HighQualityBiomesSearch(ntsQueryPolygon, foundBoundingElements);
            }
            msw.StartSegment("Rest: " + foundBoundingElements.Count);

            var EPSILON = 0.00000001;
            var wholeArea = ntsQueryPolygon.Area;
            var biomesWithNormalizedArea =
                biomeAndArea.Where(p => p.Area > EPSILON)
                    .Select(p => new BiomeInstanceCharacteristicsWithArea(p.Characteristics, p.Area / wholeArea))
                    .ToList();

            var notAssignedArea = 1 - biomesWithNormalizedArea.Sum(c => c.Area);
            if (notAssignedArea > 0.8)
            {
                Debug.Log("Not assigned adding!");
                biomesWithNormalizedArea.Add(new BiomeInstanceCharacteristicsWithArea(
                    new BiomeInstanceCharacteristics(_configuration.DefaultType, new BiomeInstanceId(999999999), 0),
                    notAssignedArea));
            }

            biomesWithNormalizedArea = biomesWithNormalizedArea
                .GroupBy(a => a.Characteristics.Type, a => a)
                .Select(v => new BiomeInstanceCharacteristicsWithArea(
                    new BiomeInstanceCharacteristics(
                        v.Key,
                        new BiomeInstanceId((uint) v.Select(c => c.Characteristics.InstanceId.Id)
                            .Aggregate((a, b) => HashUtils.AddToHash(a, b))), //todo use better algorithm
                        v.Max(c => c.Characteristics.Priority)),
                    v.Sum(x => x.Area))).ToList();

            //Debug.Log("T44: "+msw.CollectResults());
            return biomesWithNormalizedArea;
        }

        private static List<BiomeInstanceCharacteristicsWithArea> LowQualityBiomesSearch(
            IPolygon searchPolygon,
            List<BiomeInstanceInfo> foundBoundingElements)
        {
            var ntsQueryPolygon = searchPolygon.EnvelopeInternal;

            var biomeAndArea
                = foundBoundingElements.Where(p => p.VisibleAtLowQuality()).Select(
                    p => new
                    {
                        intersection = ntsQueryPolygon.Intersection(p.CalculateEnvelope()),
                        biomeInstance = p
                    }
                ).Select(
                    p => new BiomeInstanceCharacteristicsWithArea(
                        new BiomeInstanceCharacteristics(p.biomeInstance.Type, p.biomeInstance.InstanceId,
                            p.biomeInstance.Priority),
                        p.intersection.Area)
                ).ToList();
            return biomeAndArea;
        }

        private static List<BiomeInstanceCharacteristicsWithArea> HighQualityBiomesSearch(
            IPolygon searchPolygon,
            List<BiomeInstanceInfo> foundBoundingElements)
        {
            var ntsQueryPolygon = searchPolygon;

            var biomeAndArea
                = foundBoundingElements.Select(
                    p => new
                    {
                        intersectionArea = p.IntersectionArea(ntsQueryPolygon),
                        data = p
                    }
                ).Select(
                    p => new BiomeInstanceCharacteristicsWithArea(
                        new BiomeInstanceCharacteristics(p.data.Type, p.data.InstanceId, p.data.Priority),
                        p.intersectionArea)
                ).ToList();
            return biomeAndArea;
        }

        private enum SearchQuality
        {
            Low,
            High
        }

        private SearchQuality ComputeSearchQuality(IPolygon queryPolygon)
        {
            var centertoid = queryPolygon.Centroid;
            var distanceToQualityCenter =
                Vector2.Distance(
                    new Vector2((float) centertoid.X, (float) centertoid.Y),
                    _configuration.Center);
            if (distanceToQualityCenter < _configuration.HighQualityQueryDistance)
            {
                return SearchQuality.High;
            }
            else
            {
                return SearchQuality.Low;
            }
        }
    }

    public class BiomesContainerConfiguration
    {
        public Vector2 Center = new Vector2(0, 0);
        public float HighQualityQueryDistance = 5;
        public BiomeType DefaultType = BiomeType.Sand;
    }

    public class BiomeInstanceCharacteristicsWithArea
    {
        private readonly BiomeInstanceCharacteristics _characteristics;
        private readonly double _area;

        public BiomeInstanceCharacteristicsWithArea(BiomeInstanceCharacteristics characteristics, double area)
        {
            _characteristics = characteristics;
            _area = area;
        }

        public BiomeInstanceCharacteristics Characteristics => _characteristics;

        public double Area => _area;
    }

    public class BiomeInstanceCharacteristics
    {
        private readonly BiomeType _type;
        private readonly BiomeInstanceId _instanceId;
        private readonly int _priority;

        public BiomeInstanceCharacteristics(BiomeType type, BiomeInstanceId instanceId, int priority)
        {
            _type = type;
            _instanceId = instanceId;
            _priority = priority;
        }

        public BiomeType Type => _type;

        public BiomeInstanceId InstanceId => _instanceId;

        public int Priority => _priority;

        protected bool Equals(BiomeInstanceCharacteristics other)
        {
            return _type == other._type && _instanceId.Equals(other._instanceId) && other._priority.Equals(_priority);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BiomeInstanceCharacteristics) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) _type * 397) ^ _instanceId.GetHashCode();
            }
        }
    }

    public abstract class BiomeInstanceInfo : IHasEnvelope, ICanTestIntersect
    {
        private readonly BiomeType _type;
        private readonly int _priority;
        private readonly BiomeInstanceId _instanceId;

        protected BiomeInstanceInfo(BiomeType type, int priority, BiomeInstanceId instanceId)
        {
            _type = type;
            _priority = priority;
            _instanceId = instanceId;
        }

        public BiomeType Type
        {
            get { return _type; }
        }

        public int Priority
        {
            get { return _priority; }
        }

        public BiomeInstanceId InstanceId => _instanceId;

        public abstract Envelope CalculateEnvelope();
        public abstract bool Intersects(IGeometry geometry);
        public abstract float IntersectionArea(IGeometry geometry);

        public virtual bool VisibleAtLowQuality()
        {
            return true;
        }
    }

    public class PolygonBiomeInstanceInfo : BiomeInstanceInfo
    {
        private readonly IPolygon _polygon;

        public PolygonBiomeInstanceInfo(BiomeType type, IPolygon polygon, BiomeInstanceId instanceId, int priority)
            : base(type, priority, instanceId)
        {
            _polygon = polygon;
        }


        public override Envelope CalculateEnvelope()
        {
            return _polygon.EnvelopeInternal;
        }

        public override bool Intersects(IGeometry geometry)
        {
            return _polygon.Intersects(geometry);
        }

        public override float IntersectionArea(IGeometry geometry)
        {
            return (float) _polygon.Intersection(geometry).Area;
        }
    }

    public struct BiomeInstanceId
    {
        private readonly uint _id;

        public BiomeInstanceId(uint id)
        {
            _id = id;
        }

        public uint Id => _id;

        public bool Equals(BiomeInstanceId other)
        {
            return _id == other._id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is BiomeInstanceId && Equals((BiomeInstanceId) obj);
        }

        public override int GetHashCode()
        {
            return (int) _id;
        }
    }
}