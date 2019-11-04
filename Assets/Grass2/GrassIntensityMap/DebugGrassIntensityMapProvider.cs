using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Grass2.Growing;
using Assets.Grass2.IntenstityDb;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Spatial;

namespace Assets.Grass2.GrassIntensityMap
{
    public class DebugGrassIntensityMapProvider : IGrassIntensityMapProvider
    {
        private readonly List<GrassTypeWithUvedIntensity> _toReturn;

        public DebugGrassIntensityMapProvider(List<GrassTypeWithUvedIntensity> toReturn)
        {
            _toReturn = toReturn;
        }

        public Task<UvdCoordedPart<List<Grass2TypeWithIntensity>>> ProvideMapsAtAsync(MyRectangle queryArea)
        {
            return TaskUtils.MyFromResult(new UvdCoordedPart<List<Grass2TypeWithIntensity>>()
            {
                Uv = _toReturn.First().Figure.Uv,
                CoordedPart = new CoordedPart<List<Grass2TypeWithIntensity>>()
                {
                    Coords = new MyRectangle(0, 0, 1, 1),
                    Part = _toReturn.Select(c => new Grass2TypeWithIntensity()
                    {
                        GrassType = c.Type,
                        IntensityFigure = c.Figure.FieldFigure
                    }).ToList()
                }
            });
        }
    }
}