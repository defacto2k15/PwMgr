using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Random.Fields
{
    public class Ring2RandomFieldFigureRepository
    {
        private RandomFieldFigureGeneratorUTProxy _figureGenerator;

        private Dictionary<RandomFieldFigureId, Dictionary<MyRectangle, IntensityFieldFigure>> _figuresDictionary
            = new Dictionary<RandomFieldFigureId, Dictionary<MyRectangle, IntensityFieldFigure>>();

        private readonly Ring2RandomFieldFigureRepositoryConfiguration _configuration;

        public Ring2RandomFieldFigureRepository(RandomFieldFigureGeneratorUTProxy figureGenerator,
            Ring2RandomFieldFigureRepositoryConfiguration configuration)
        {
            _figureGenerator = figureGenerator;
            _configuration = configuration;
        }

        public async Task<List<float>> GetValuesAsync(RandomFieldNature nature, float seed,
            List<Vector2> queryPositions)
        {
            Dictionary<MyRectangle, IntensityFieldFigure> segmentsDict = null;
            var randomFieldFigureId = new RandomFieldFigureId(nature, seed);
            if (!_figuresDictionary.ContainsKey(randomFieldFigureId))
            {
                segmentsDict = new Dictionary<MyRectangle, IntensityFieldFigure>();
                _figuresDictionary[randomFieldFigureId] = segmentsDict;
            }
            else
            {
                segmentsDict = _figuresDictionary[randomFieldFigureId];
            }

            List<MyRectangle> segmentCoordsList = new List<MyRectangle>(queryPositions.Count);
            for (int i = 0; i < queryPositions.Count; i++)
            {
                segmentCoordsList.Add(CalculateSegment(queryPositions[i]));
            }

            List<MyRectangle> distinctCoords = segmentCoordsList.Distinct().ToList();
            List<MyRectangle> coordsToCreateFigureFor =
                distinctCoords.Where(c => !segmentsDict.ContainsKey(c)).ToList();

            if (coordsToCreateFigureFor.Any())
            {
                var createdFigures = await TaskUtils.WhenAll(Enumerable.Range(0, coordsToCreateFigureFor.Count).Select(
                    (i) =>
                    {
                        var coord = coordsToCreateFigureFor[i];
                        return _figureGenerator.GenerateRandomFieldFigureAsync(nature, seed, coord);
                    }));
                for (int i = 0; i < coordsToCreateFigureFor.Count; i++)
                {
                    var coord = coordsToCreateFigureFor[i];
                    segmentsDict[coord] = createdFigures[i];
                }
            }

            List<float> outList = new List<float>(queryPositions.Count);
            for (int i = 0; i < queryPositions.Count; i++)
            {
                var position = queryPositions[i];
                var segmentCoords = segmentCoordsList[i];
                IntensityFieldFigure fieldFigure = segmentsDict[segmentCoords];

                var inFigureUv = new Vector2((position.x - segmentCoords.X) / segmentCoords.Width,
                    (position.y - segmentCoords.Y) / segmentCoords.Height);
                outList.Add(fieldFigure.GetPixelWithUv(inFigureUv));
            }
            return outList;
        }

        private MyRectangle CalculateSegment(Vector2 position)
        {
            Vector2 startPos = new Vector2(
                Mathf.Floor(position.x / _configuration.FigureSegmentSize.x) * _configuration.FigureSegmentSize.x,
                Mathf.Floor(position.y / _configuration.FigureSegmentSize.y) * _configuration.FigureSegmentSize.y
            );
            return new MyRectangle(startPos.x, startPos.y, _configuration.FigureSegmentSize.x,
                _configuration.FigureSegmentSize.y);
        }
    }


    public class RandomFieldFigureId
    {
        private RandomFieldNature _nature;
        private float seed;

        public RandomFieldFigureId(RandomFieldNature nature, float seed)
        {
            _nature = nature;
            this.seed = seed;
        }

        protected bool Equals(RandomFieldFigureId other)
        {
            return Equals(_nature, other._nature) && seed.Equals(other.seed);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RandomFieldFigureId) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_nature != null ? _nature.GetHashCode() : 0) * 397) ^ seed.GetHashCode();
            }
        }
    }
}