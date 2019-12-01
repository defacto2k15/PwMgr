using System.Collections.Generic;
using System.Linq;
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

        private IntVector2 _currentFieldMarker;

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
            _currentFieldMarker = CalculateFieldPositionMarker(travellerPositionInSectorSpace);

            foreach (var pair in ComputeSectorsInRectangle(_currentFieldMarker))
            {
                _listener.AddSegment(new SegmentInformation
                {
                    SegmentState = pair.Value,
                    SegmentAlignedPosition = pair.Key
                });
            }
        }

        private Dictionary<IntVector2, SegmentState> ComputeSectorsInRectangle(IntVector2 fieldMarker)
        {
            var activeSectorsCenter = new IntVector2(Mathf.FloorToInt(fieldMarker.X / 2f), Mathf.FloorToInt(fieldMarker.Y / 2f));
            var fieldActiveSectors = new IntRectangle( activeSectorsCenter.X-2, activeSectorsCenter.Y-2, 5, 5 );

            var outDict = new Dictionary<IntVector2, SegmentState>();
            for (int x = fieldActiveSectors.X; x < fieldActiveSectors.MaxX; x++)
            {
                for (int y = fieldActiveSectors.Y; y < fieldActiveSectors.MaxY; y++)
                {
                    var state = SegmentState.Active;
                    outDict[new IntVector2(x, y)] = state;
                }
            }

            var standbyInnerMarginOffset = new IntVector2(-1,-1);
            if (fieldMarker.X % 2 == 1) // todo, this is temporary
            {
                standbyInnerMarginOffset.X = 5;
            }
            if (fieldMarker.Y % 2 == 1) // todo, this is temporary
            {
                standbyInnerMarginOffset.Y = 5;
            }

            var rectDownLeftPoint = new IntVector2(fieldActiveSectors.DownLeftX, fieldActiveSectors.DownLeftY);
            for (int y = -1; y < 6; y++)
            {
                outDict.Add(rectDownLeftPoint + new IntVector2(standbyInnerMarginOffset.X, y), SegmentState.Standby);
            }
            for (int x = -1; x < 6; x++)
            {
                var segmentCoords = rectDownLeftPoint + new IntVector2(x, standbyInnerMarginOffset.Y);
                if (!outDict.ContainsKey(segmentCoords))
                {
                    outDict.Add(segmentCoords, SegmentState.Standby);
                }
            }


            return outDict;
        }

        private IntVector2 CalculateFieldPositionMarker(Vector2 travellerPositionInSectorSpace)
        {
            return IntVector2.FloorFromFloat(travellerPositionInSectorSpace*2);
        }

        public void Update(Vector2 travellerPosition)
        {
            var travellerPositionInSectorSpace = travellerPosition / _segmentWorldSpaceLength;
            var newMarker = CalculateFieldPositionMarker(travellerPositionInSectorSpace);
            // URUCHAMIANE DOPIERO JAK MUSIMY WYMIENIC SEKTORY!
            if (!newMarker.Equals(_currentFieldMarker))
            {
                var rectangleDelta = CalculateDelta(newMarker, _currentFieldMarker);
                _currentFieldMarker = newMarker;

                rectangleDelta.SectorsToRemove.ForEach(c => _listener.RemoveSegment(c));
                rectangleDelta.SectorsToCreate.ForEach(c => _listener.AddSegment(c));
                rectangleDelta.SectorsToChange.ForEach(c => _listener.SegmentStateChange(c));
            }
        }

        private SegmentFieldDelta CalculateDelta(IntVector2 newMarker, IntVector2 oldMarker)
        {
            var newFieldsSectors = ComputeSectorsInRectangle(newMarker);
            var oldFieldsSectors = ComputeSectorsInRectangle(oldMarker);

            var sectorsToCreate = new List<SegmentInformation>();
            var sectorsToRemove = new List<SegmentInformation>();
            var sectorsChanged = new List<SegmentInformation>();

            foreach (var oldPair in oldFieldsSectors)
            {
                if (!newFieldsSectors.ContainsKey(oldPair.Key))
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