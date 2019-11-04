using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;

namespace Assets.Ring2.BaseEntities
{ //todo remove
    //public class Ring2ContinuousRegion
    //{
    //    private IPolygon _area;
    //    private Ring2Substance _substance;
    //    private float _magnitude;

    //    public Ring2ContinuousRegion(IPolygon area, Ring2Substance substance, float magnitude)
    //    {
    //        _area = area;
    //        _substance = substance;
    //        _magnitude = magnitude;
    //    }

    //    public IPolygon Area
    //    {
    //        get { return _area; }
    //    }

    //    public Ring2Substance Substance
    //    {
    //        get { return _substance; }
    //    }

    //    public float Magnitude
    //    {
    //        get { return _magnitude; }
    //    }

    //    public List<Ring2ContinuousRegion> Intersection(IGeometry sliceArea)
    //    {
    //        var intersectionGeometry = _area.Intersection(sliceArea);
    //        return ToContinousRegionWithNewArea(this, intersectionGeometry);
    //    }

    //    //public static List<Ring2ContinuousRegion> ToContinousRegion(Ring2Region ring2Region)
    //    //{
    //    //    if (ring2Region.Area is IPolygon)
    //    //    {
    //    //        return new List<Ring2ContinuousRegion>()
    //    //        {
    //    //            new Ring2ContinuousRegion(ring2Region.Area as IPolygon, ring2Region.Substance, ring2Region.Magnitude)
    //    //        };
    //    //    } 
    //    //    else if (ring2Region.Area is IMultiPolygon)
    //    //    {
    //    //        var mp = ring2Region.Area as IMultiPolygon;
    //    //        return Enumerable.Range(0, mp.Count)
    //    //            .Select(i => new Ring2ContinuousRegion(mp.GetGeometryN(i) as IPolygon, ring2Region.Substance, ring2Region.Magnitude))
    //    //            .ToList();
    //    //    } else if (ring2Region.Area is IPoint)
    //    //    {
    //    //        return new List<Ring2ContinuousRegion>();
    //    //    }
    //    //    else
    //    //    {
    //    //        Preconditions.Fail("Cannot transform to continous region: "+ring2Region.Area.GetType());
    //    //        return null;
    //    //    }
    //    //}

    //    //public static List<IPolygon> SplitAreaToContinous( IGeometry geometry)
    //    //{
    //    //    if (geometry is IPolygon)
    //    //    {
    //    //        return new List<IPolygon>(){geometry as IPolygon};
    //    //    }
    //    //    else if (geometry is IMultiPolygon)
    //    //    {
    //    //        var multipolygon = geometry as IMultiPolygon;
    //    //        return
    //    //            Enumerable.Range(0, multipolygon.Count)
    //    //                .Select(i => multipolygon.GetGeometryN(i))
    //    //                .Cast<IPolygon>()
    //    //                .ToList();
    //    //    }
    //    //    else if (geometry is IPoint)
    //    //    {
    //    //        return new List<IPolygon>();
    //    //    }
    //    //    else
    //    //    {
    //    //        Preconditions.Fail("Cannot split geometry area of type "+geometry.GetType());
    //    //        return null;
    //    //    }
    //    //}

    //    //public static List<Ring2ContinuousRegion> ToContinousRegionWithNewArea(Ring2ContinuousRegion ring2Region, IGeometry newArea)
    //    //{
    //    //    return
    //    //        SplitAreaToContinous(newArea)
    //    //            .Select(c => new Ring2ContinuousRegion(c, ring2Region.Substance, ring2Region.Magnitude))
    //    //            .ToList();
    //    //}

    //}
}