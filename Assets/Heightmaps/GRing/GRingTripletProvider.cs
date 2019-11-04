using Assets.Heightmaps.Ring1.valTypes;
using Assets.Repositioning;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    /// ////////////////////
    public class GRingTripletProvider
    {
        private readonly MyRectangle _inGamePosition;
        private readonly Repositioner _repositioner;
        private readonly HeightDenormalizer _heightDenormalizer;

        public GRingTripletProvider(MyRectangle inGamePosition, Repositioner repositioner,
            HeightDenormalizer heightDenormalizer)
        {
            this._inGamePosition = inGamePosition;
            _repositioner = repositioner;
            _heightDenormalizer = heightDenormalizer;
        }

        public MyTransformTriplet ProvideTriplet()
        {
            var debugTestDivider = 1; ///*240f;
            var upScale = _heightDenormalizer.DenormalizationMultiplier;

            var position = new Vector3(_inGamePosition.X / debugTestDivider, _heightDenormalizer.DenormalizationOffset,
                _inGamePosition.Y / debugTestDivider);
            position = _repositioner.Move(position);
            return new MyTransformTriplet(
                position,
                Vector3.zero,
                new Vector3(_inGamePosition.Width / debugTestDivider, upScale,
                    _inGamePosition.Height / debugTestDivider));
        }
    }
}