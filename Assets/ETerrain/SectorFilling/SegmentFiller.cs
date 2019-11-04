using System.Collections.Generic;
using Assets.Ring2;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.SectorFilling
{
    public class SegmentFiller
    {
        private IntVector2 _fieldSize;
        private IntVector2 _standbyMarginsSize;
        private readonly float _segmentWorldSpaceLength;
        private ISegmentFillingListener _listener;

        private IntRectangle _currentField;

        public SegmentFiller(IntVector2 fieldSize, IntVector2 standbyMarginsSize, float segmentWorldSpaceLength, ISegmentFillingListener listener)
        {
            _fieldSize = fieldSize;
            _listener = listener;
            _standbyMarginsSize = standbyMarginsSize;
            _segmentWorldSpaceLength = segmentWorldSpaceLength;
        }

        public void InitializeField(Vector2 travellerPosition)
        {
            var travellerPositionInSectorSpace = travellerPosition / _segmentWorldSpaceLength;
            _currentField = CalculateFieldRectangle(travellerPositionInSectorSpace);
            foreach (var pair in ComputeSectorsInRectangle(_currentField))
            {
                _listener.AddSegment(new SegmentInformation
                {
                    SegmentState = pair.Value,
                    SegmentAlignedPosition = pair.Key
                });
            }
        }

        private Dictionary<IntVector2, SegmentState> ComputeSectorsInRectangle(IntRectangle rectangle)
        {
            var outDict = new Dictionary<IntVector2, SegmentState>();
            for (int x = rectangle.X; x < rectangle.MaxX; x++)
            {
                for (int y = rectangle.Y; y < rectangle.MaxY; y++)
                {
                    var state = SegmentState.Active;
                    if (x < rectangle.X + _standbyMarginsSize.X || x >= rectangle.MaxX - _standbyMarginsSize.X ||
                        y < rectangle.Y + _standbyMarginsSize.Y || y >= rectangle.MaxY - _standbyMarginsSize.Y)
                    {
                        state = SegmentState.Standby;
                    }
                    outDict[new IntVector2(x, y)] = state;
                }
            }
            return outDict;
        }

        private IntRectangle CalculateFieldRectangle(Vector2 travellerPositionInSectorSpace)
        {
            var downLeftPointInSectorSpace =  travellerPositionInSectorSpace - new Vector2(_fieldSize.X / 2f, _fieldSize.Y / 2f);
            var alignedDownLeftPoint = IntVector2.FromFloat(downLeftPointInSectorSpace);
            return new IntRectangle(alignedDownLeftPoint.X, alignedDownLeftPoint.Y, _fieldSize.X, _fieldSize.Y);
        }

        public void Update(Vector2 travellerPosition)
        {
            var travellerPositionInSectorSpace = travellerPosition / _segmentWorldSpaceLength;
            var newField = CalculateFieldRectangle(travellerPositionInSectorSpace);
            // URUCHAMIANE DOPIERO JAK MUSIMY WYMIENIC SEKTORY!
            if (!newField.Equals(_currentField))
            {
                var rectangleDelta = CalculateDelta(newField, _currentField);
                _currentField = newField;

                rectangleDelta.SectorsToCreate.ForEach(c => _listener.AddSegment(c));
                rectangleDelta.SectorsToRemove.ForEach(c => _listener.RemoveSegment(c));
                rectangleDelta.SectorsToChange.ForEach(c => _listener.SegmentStateChange(c));
            }
        }

        private SegmentFieldDelta CalculateDelta(IntRectangle newField, IntRectangle oldField)
        {
            var oldFieldsSectors = ComputeSectorsInRectangle(oldField);
            var newFieldsSectors = ComputeSectorsInRectangle(newField);

            var sectorsToCreate = new List<SegmentInformation>();
            var sectorsToRemove = new List<SegmentInformation>();
            var sectorsChanged = new List<SegmentInformation>();

            foreach (var oldPair in oldFieldsSectors)
            {
                if (!newField.Contains(oldPair.Key))
                {
                    sectorsToRemove.Add(new SegmentInformation
                    {
                        SegmentState = oldPair.Value,
                        SegmentAlignedPosition = oldPair.Key
                    });
                }
                else
                {
                    var newState = newFieldsSectors[oldPair.Key];
                    if (newState != oldPair.Value)
                    {
                        sectorsChanged.Add(new SegmentInformation
                        {
                            SegmentState = newState,
                            SegmentAlignedPosition = oldPair.Key
                        });
                    }
                    newFieldsSectors.Remove(oldPair.Key);
                }
            }

            foreach (var pair in newFieldsSectors)
            {
                sectorsToCreate.Add(new SegmentInformation
                {
                    SegmentState = pair.Value,
                    SegmentAlignedPosition = pair.Key
                });
            }

            return new SegmentFieldDelta
            {
                SectorsToRemove = sectorsToRemove,
                SectorsToCreate = sectorsToCreate,
                SectorsToChange = sectorsChanged
            }; 
        }
    }
}