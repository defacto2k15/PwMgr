using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass2.IntensitySampling
{
    public class IntensityFromRandomFigureProvider : IIntensitySamplingProvider
    {
        private IntensityFieldFigureWithUv _intensityFieldFigureWithUv;

        public IntensityFromRandomFigureProvider(IntensityFieldFigureWithUv intensityFieldFigureWithUv)
        {
            _intensityFieldFigureWithUv = intensityFieldFigureWithUv;
        }

        public float Sample(Vector2 uv)
        {
            var subUv = RectangleUtils.CalculateSubPosition(_intensityFieldFigureWithUv.Uv, uv);
            var toReturn = _intensityFieldFigureWithUv.FieldFigure.GetPixelWithUv(subUv);
            return toReturn;
        }
    }

    public class IntensityFieldFigureWithUv
    {
        // coords define submap in intensityField. It is used to remap sampling area
        public IntensityFieldFigure FieldFigure;

        public MyRectangle Uv;
    }
}