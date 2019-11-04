using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoAPI.Geometries;

namespace Assets.Utils.Quadtree
{
    public interface ICanTestIntersect
    {
        bool Intersects(IGeometry geometry);
    }
}