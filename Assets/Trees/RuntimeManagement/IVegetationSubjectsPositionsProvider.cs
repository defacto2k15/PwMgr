using System.Collections.Generic;
using GeoAPI.Geometries;

namespace Assets.Trees.RuntimeManagement
{
    public interface IVegetationSubjectsPositionsProvider
    {
        List<VegetationSubjectEntity> GetEntiesFrom(IGeometry area, VegetationDetailLevel level);
    }
}