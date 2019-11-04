using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.Utils;
using Assets.Utils.UTUpdating;

namespace Assets.Random.Fields
{
    public class RandomFieldFigureGeneratorUTProxy : BaseUTTransformProxy<IntensityFieldFigure,
        RandomFieldFigureGeneratorUTProxy.RandomFieldFigureGeneratorOrder>
    {
        private Ring2RandomFieldFigureGenerator _generator;

        public RandomFieldFigureGeneratorUTProxy(Ring2RandomFieldFigureGenerator generator)
        {
            _generator = generator;
        }

        public void Start()
        {
            _generator.Initialize();
        }

        public Task<IntensityFieldFigure> GenerateRandomFieldFigureAsync(RandomFieldNature nature, float seed,
            MyRectangle segmentCoords)
        {
            return BaseUtAddOrder(new RandomFieldFigureGeneratorOrder()
            {
                Nature = nature,
                Seed = seed,
                SegmentCoords = segmentCoords,
            });
        }

        protected override IntensityFieldFigure ExecuteOrder(RandomFieldFigureGeneratorOrder order)
        {
            return _generator.Generate(order.Nature, order.Seed, order.SegmentCoords);
        }

        public class RandomFieldFigureGeneratorOrder
        {
            public RandomFieldNature Nature;
            public float Seed;
            public MyRectangle SegmentCoords;
        }
    }
}