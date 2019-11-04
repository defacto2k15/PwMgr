using System;
using Assets.Utils;

namespace Assets.Heightmaps.Preparment.MarginMerging
{
    class HeightmapMarginWithInfo
    {
        private readonly HeightmapMargin _heightmapMargin;
        private readonly MarginPosition _position;
        private readonly int _lodFactor;

        public HeightmapMarginWithInfo(HeightmapMargin heightmapMargin, MarginPosition position, int lodFactor)
        {
            _heightmapMargin = heightmapMargin;
            _position = position;
            _lodFactor = lodFactor;
        }

        public HeightmapMargin HeightmapMargin
        {
            get { return _heightmapMargin; }
        }

        public MarginPosition Position
        {
            get { return _position; }
        }

        public int LodFactor
        {
            get { return _lodFactor; }
        }

        public HeightmapMarginWithInfo SetLod(int newLodFactor)
        {
            if (newLodFactor == LodFactor)
            {
                return this;
            }
            else
            {
                var newLength = (int) Math.Round(Math.Pow(2, LodFactor - newLodFactor) * HeightmapWorkingLength);
                return new HeightmapMarginWithInfo(_heightmapMargin.SetLength(newLength), Position, newLodFactor);
            }
        }

        public HeightmapMarginWithInfo UpdateWherePossible(HeightmapMarginWithInfo newMargin)
        {
            //Preconditions.Assert( _heightmapMargin.Length == newMargin.HeightmapLength, todo change not to length but to lods
            //    string.Format("Current margin length is {0} != new margin length == {1} ", _heightmapMargin.Length, newMargin.HeightmapLength));
            Preconditions.Assert(
                (newMargin.Position.IsHorizontal && Position.IsHorizontal) ||
                (newMargin.Position.IsVertical && Position.IsVertical),
                string.Format("Current and new margins are one vertical one horizontal: Old {0} new {1}",
                    _heightmapMargin, newMargin));

            bool haveCommonElements = Position.HaveCommonElementWith(newMargin.Position);
            Preconditions.Assert(haveCommonElements,
                string.Format("Current {0} and new {1} margin dont have common elements", HeightmapMargin, newMargin));

            MarginPosition commonSegment = Position.GetCommonSegment(newMargin.Position);

            var ourStartPercent = Position.InvLerp(commonSegment.StartPoint);
            var ourStartOffset = (int) Math.Round((double) HeightmapWorkingLength * ourStartPercent);

            var ourEndPercent = Position.InvLerp(commonSegment.EndPoint);
            var ourEndOffset = (int) Math.Round((double) HeightmapWorkingLength * ourEndPercent);

            var theirStartPercent = newMargin.Position.InvLerp(commonSegment.StartPoint);
            var theirStartOffset = (int) Math.Round((double) newMargin.HeightmapWorkingLength * theirStartPercent);

            var theirEndPercent = newMargin.Position.InvLerp(commonSegment.EndPoint);
            var theirEndOffset = (int) Math.Round((double) newMargin.HeightmapWorkingLength * theirEndPercent);

            return new HeightmapMarginWithInfo(
                SetMarginSubElement(ourStartOffset, ourEndOffset, theirStartOffset, theirEndOffset, newMargin),
                Position, LodFactor);
        }

        private HeightmapMargin SetMarginSubElement(int ourStartOffset, int ourEndOffset, int theirStartOffset,
            int theirEndOffset, HeightmapMarginWithInfo newMargin)
        {
            Preconditions.Assert(
                (ourEndOffset - ourStartOffset) == (theirEndOffset - theirStartOffset),
                String.Format(
                    "Cant set subelement. Offset lengths are not equal. Our start {0} end {1} their start {2} end {3} ",
                    ourStartOffset, ourEndOffset, theirStartOffset, theirEndOffset));
            var length = ourEndOffset - ourStartOffset;
            var newValues = new float[_heightmapMargin.Length];
            Array.Copy(_heightmapMargin.MarginValues, newValues, newValues.Length);
            for (int i = 0; i < length; i++)
            {
                newValues[ourStartOffset + i] = newMargin.HeightmapMargin.MarginValues[theirStartOffset + i];
            }
            return new HeightmapMargin(newValues);
        }


        public int HeightmapLength
        {
            get { return _heightmapMargin.Length; }
        }

        public int HeightmapWorkingLength
        {
            get { return _heightmapMargin.WorkingLength; }
        }
    }
}