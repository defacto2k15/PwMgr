using Assets.Utils;
using UnityEngine;

namespace Assets.Grass.Lod
{
    class LodLevelResolver
    {
        private readonly int _maxLodLevel;
        private readonly int _singleLevelDistance;
        private readonly float _noChangeMargin;
        private readonly float _powCoeficient;

        public LodLevelResolver(int maxLodLevel, int singleLevelDistance, float noChangeMargin, float powCoeficient)
        {
            this._maxLodLevel = maxLodLevel;
            this._singleLevelDistance = singleLevelDistance;
            this._noChangeMargin = noChangeMargin;
            _powCoeficient = powCoeficient;
        }

        public int Resolve(Vector3 cameraPosition, Vector3 centerPosition, int oldLodLevel = -1)
        {
            var distance = Vector3.Distance(cameraPosition, centerPosition);
            var newLodFloat = Mathf.Pow(distance / _singleLevelDistance, _powCoeficient);
            var newLod = (int) Mathf.Floor(newLodFloat);

            if (oldLodLevel == -1)
            {
                return (int) Mathf.Min(newLod, _maxLodLevel);
            }
            else if (newLod == oldLodLevel)
            {
                return (int) Mathf.Min(newLod, _maxLodLevel);
                ;
            }
            else if (Mathf.Abs(oldLodLevel - newLodFloat) < _noChangeMargin)
            {
                return oldLodLevel;
            }
            else
            {
                return (int) Mathf.Min(newLod, _maxLodLevel);
                ;
            }
        }

        public int Resolve(Vector3 cameraPosition, MapAreaPosition mapAreaPosition, int oldLodLevel = -1)
        {
            return Resolve(cameraPosition, mapAreaPosition.Center, oldLodLevel);
        }
    }
}