using System.Collections.Generic;
using System.Linq;
using Assets.Trees.RuntimeManagement;
using GeoAPI.Geometries;

namespace Assets.Grass2
{
    public class CompositeVegetationSubjectsPositionProvider : IVegetationSubjectsPositionsProvider
    {
        private List<IVegetationSubjectsPositionsProvider> _sources;

        public CompositeVegetationSubjectsPositionProvider(List<IVegetationSubjectsPositionsProvider> sources)
        {
            _sources = sources;
        }

        public List<VegetationSubjectEntity> GetEntiesFrom(IGeometry area, VegetationDetailLevel level)
        {
            return _sources.SelectMany(c => c.GetEntiesFrom(area, level)).ToList();
        }
    }
}