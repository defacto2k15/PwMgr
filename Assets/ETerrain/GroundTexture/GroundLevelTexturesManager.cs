using System.Collections.Generic;
using System.Linq;
using Assets.Utils;

namespace Assets.ETerrain.Pyramid.Map
{
    public class GroundLevelTexturesManager
    {
        private SegmentSlotState[][] _segmentSlotStates;
        private IntVector2 _slotMapSize;

        public GroundLevelTexturesManager(IntVector2 slotMapSize)
        {
            _slotMapSize = slotMapSize;
            _segmentSlotStates = new SegmentSlotState[_slotMapSize.X][];
            for (var x = 0; x < _slotMapSize.X; x++)
            {
                _segmentSlotStates[x] = Enumerable.Range(0, _slotMapSize.Y).Select(_ => new SegmentSlotState(_slotMapSize))
                    .ToArray();
            }
        }

        public PlacementDetails Place(IntVector2 segmentAlignedPosition)
        {
            var initAlign = new ModuloPosition(_slotMapSize, segmentAlignedPosition);
            var thisSlotState = _segmentSlotStates[initAlign.X][initAlign.Y];
            thisSlotState.SetSegmentPosition(segmentAlignedPosition);

            var cornersToRegenerate = new List<SegmentCornerToModify>();
            var neighbours = SegmentNeighbourhoodDiregment.AllDiregments.ToDictionary(
                c => c,
                c => {
                    var neighbourSlotPos = initAlign.GetNeighbourPosition(c);
                    return _segmentSlotStates[neighbourSlotPos.X][neighbourSlotPos.Y];
                });

            IEnumerable<IntersegmentInGrid> intersegments = new List<IntersegmentInGrid>()
            {
                new IntersegmentInGrid(new Dictionary<SegmentNeighbourhoodCorner, SegmentSlotState>()
                {
                    {SegmentNeighbourhoodCorner.TopLeft, neighbours[SegmentNeighbourhoodDiregment.TopLeft]},
                    {SegmentNeighbourhoodCorner.TopRight, neighbours[SegmentNeighbourhoodDiregment.Top]},
                    {SegmentNeighbourhoodCorner.BottomLeft, neighbours[SegmentNeighbourhoodDiregment.Left]},
                    {SegmentNeighbourhoodCorner.BottomRight, thisSlotState}
                }, SegmentNeighbourhoodCorner.BottomRight),

                new IntersegmentInGrid(new Dictionary<SegmentNeighbourhoodCorner, SegmentSlotState>()
                {
                    {SegmentNeighbourhoodCorner.TopLeft, neighbours[SegmentNeighbourhoodDiregment.Top]},
                    {SegmentNeighbourhoodCorner.TopRight, neighbours[SegmentNeighbourhoodDiregment.TopRight]},
                    {SegmentNeighbourhoodCorner.BottomLeft, thisSlotState},
                    {SegmentNeighbourhoodCorner.BottomRight, neighbours[SegmentNeighbourhoodDiregment.Right]}
                }, SegmentNeighbourhoodCorner.BottomLeft),

                new IntersegmentInGrid(new Dictionary<SegmentNeighbourhoodCorner, SegmentSlotState>()
                {
                    {SegmentNeighbourhoodCorner.TopLeft, thisSlotState},
                    {SegmentNeighbourhoodCorner.TopRight, neighbours[SegmentNeighbourhoodDiregment.Right]},
                    {SegmentNeighbourhoodCorner.BottomLeft, neighbours[SegmentNeighbourhoodDiregment.Bottom]},
                    {SegmentNeighbourhoodCorner.BottomRight, neighbours[SegmentNeighbourhoodDiregment.BottomRight]}
                }, SegmentNeighbourhoodCorner.TopLeft),

                new IntersegmentInGrid(new Dictionary<SegmentNeighbourhoodCorner, SegmentSlotState>()
                {
                    {SegmentNeighbourhoodCorner.TopLeft, neighbours[SegmentNeighbourhoodDiregment.Left]},
                    {SegmentNeighbourhoodCorner.TopRight, thisSlotState},
                    {SegmentNeighbourhoodCorner.BottomLeft, neighbours[SegmentNeighbourhoodDiregment.BottomLeft]},
                    {SegmentNeighbourhoodCorner.BottomRight, neighbours[SegmentNeighbourhoodDiregment.Bottom]}
                }, SegmentNeighbourhoodCorner.TopRight),
            };
            foreach (IntersegmentInGrid intersegment in intersegments)
            {
                if (IsSegmentPresentInGrid(segmentAlignedPosition, intersegment,
                    SegmentNeighbourhoodCorner.BottomLeft) )// this segment allways has intersecting corner!!
                {
                    if (IsSegmentPresentInGrid(segmentAlignedPosition, intersegment,
                        SegmentNeighbourhoodCorner.BottomRight))
                    {
                        if (!SegmentHasPresentIntersectingCorner(intersegment, SegmentNeighbourhoodCorner.BottomRight))
                        {
                            AddSegmentsCornerWelding(intersegment, SegmentNeighbourhoodCorner.BottomRight,
                                cornersToRegenerate);
                        }
                    }

                    if (IsSegmentPresentInGrid(segmentAlignedPosition, intersegment,
                        SegmentNeighbourhoodCorner.TopLeft))
                    {
                        if (!SegmentHasPresentIntersectingCorner(intersegment, SegmentNeighbourhoodCorner.TopLeft))
                        {
                            AddSegmentsCornerWelding(intersegment, SegmentNeighbourhoodCorner.TopLeft,
                                cornersToRegenerate);
                        }
                    }

                    if (IsSegmentPresentInGrid(segmentAlignedPosition, intersegment,
                            SegmentNeighbourhoodCorner.BottomRight) &&
                        SegmentHasPresentIntersectingCorner(intersegment, SegmentNeighbourhoodCorner.BottomRight))
                    {
                        if (IsSegmentPresentInGrid(segmentAlignedPosition, intersegment,
                                SegmentNeighbourhoodCorner.TopLeft) &&
                            SegmentHasPresentIntersectingCorner(intersegment, SegmentNeighbourhoodCorner.TopLeft))
                        {
                            if (IsSegmentPresentInGrid(segmentAlignedPosition, intersegment,
                                SegmentNeighbourhoodCorner.TopRight))
                            {
                                if (!SegmentHasPresentIntersectingCorner(intersegment,
                                    SegmentNeighbourhoodCorner.TopRight))
                                {
                                    AddSegmentsCornerWelding(intersegment, SegmentNeighbourhoodCorner.TopRight,
                                        cornersToRegenerate);
                                }
                            }
                        }
                    }
                }
            }

            return new PlacementDetails()
            {
                CornersToModify = cornersToRegenerate,
                ModuledPositionInGrid = initAlign.ModuledPosition
            };
        }


        private void AddSegmentsCornerWelding(IntersegmentInGrid intersegment, SegmentNeighbourhoodCorner cornerInIntersegment, List<SegmentCornerToModify> cornersToRegenerate)
        {
            cornersToRegenerate.Add(new SegmentCornerToModify()
            {
                Corner = cornerInIntersegment.Opposite,
                ModuledPositionOfSegment = intersegment.GetSegment(cornerInIntersegment).ModuledSegmentPosition.ModuledPosition
            });
            intersegment.GetSegment(cornerInIntersegment).SetCornerWelded(cornerInIntersegment.Opposite);
        }

        private bool SegmentHasPresentIntersectingCorner(IntersegmentInGrid intersegment, SegmentNeighbourhoodCorner corner)
        {
            var segment = intersegment.GetSegment(corner);
            return segment.WasCornerWelded(corner.Opposite);
        }

        private bool IsSegmentPresentInGrid(IntVector2 segmentAlignedPosition, IntersegmentInGrid intersegment, SegmentNeighbourhoodCorner cornerInIntersegment)
        {
            return intersegment.IsNewSegment(cornerInIntersegment) ||
                   NeighbourHasAdjacentSegment(intersegment.GetSegment(cornerInIntersegment), segmentAlignedPosition, intersegment.GetDiregmentOf(cornerInIntersegment));
        }

        private bool NeighbourHasAdjacentSegment(SegmentSlotState neighbour, IntVector2 newSegmentPosition,
            SegmentNeighbourhoodDiregment diregment)
        {
            return neighbour.HasSegment && neighbour.SegmentPosition.Equals(newSegmentPosition + diregment.Movement);
        }


        public class SegmentSlotState
        {
            private IntVector2 _slotMapSize;
            private IntVector2? _segmentAlignedPosition = null;

            private Dictionary<SegmentNeighbourhoodCorner, bool> _cornerWasWelded =
                new Dictionary<SegmentNeighbourhoodCorner, bool>();

            public SegmentSlotState(IntVector2 slotMapSize)
            {
                _slotMapSize = slotMapSize;
                SetInitialCornerWeldingStates();
            }

            private void SetInitialCornerWeldingStates()
            {
                _cornerWasWelded =
                    new Dictionary<SegmentNeighbourhoodCorner, bool>()
                    {
                        {SegmentNeighbourhoodCorner.TopRight, true},
                        {SegmentNeighbourhoodCorner.BottomRight, false},
                        {SegmentNeighbourhoodCorner.BottomLeft, false},
                        {SegmentNeighbourhoodCorner.TopLeft, false},
                    };
            }

            public void SetSegmentPosition(IntVector2 segmentAlignedPosition)
            {
                _segmentAlignedPosition = segmentAlignedPosition;
                SetInitialCornerWeldingStates();
            }

            public bool HasSegment => _segmentAlignedPosition.HasValue;
            public IntVector2 SegmentPosition => _segmentAlignedPosition.Value;
            public ModuloPosition ModuledSegmentPosition => new ModuloPosition(_slotMapSize, SegmentPosition);

            public bool WasCornerWelded(SegmentNeighbourhoodCorner corner)
            {
                return _cornerWasWelded[corner];
            }

            public void SetCornerWelded(SegmentNeighbourhoodCorner corner)
            {
                _cornerWasWelded[corner] = true;
            }
        }

        public class IntersegmentInGrid
        {
            private Dictionary<SegmentNeighbourhoodCorner, SegmentSlotState> _segmentsInIntersegment;
            private SegmentNeighbourhoodCorner _cornerOfNewSegment;

            public IntersegmentInGrid(Dictionary<SegmentNeighbourhoodCorner, SegmentSlotState> segmentsInIntersegment, SegmentNeighbourhoodCorner cornerOfNewSegment)
            {
                _segmentsInIntersegment = segmentsInIntersegment;
                _cornerOfNewSegment = cornerOfNewSegment;
            }

            public SegmentSlotState GetSegment(SegmentNeighbourhoodCorner corner)
            {
                return _segmentsInIntersegment[corner];
            }

            public bool IsNewSegment(SegmentNeighbourhoodCorner queryCorner)
            {
                return queryCorner == _cornerOfNewSegment;
            }

            public SegmentNeighbourhoodDiregment GetDiregmentOf(SegmentNeighbourhoodCorner cornerInIntersegment)
            {
                Preconditions.Assert(_cornerOfNewSegment != cornerInIntersegment, "E193 This is corner of new segment!");
                if (_cornerOfNewSegment == SegmentNeighbourhoodCorner.BottomLeft)
                {
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.BottomLeft)
                    {
                        Preconditions.Fail("Unexpected");
                        return null;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.BottomRight)
                    {
                        return SegmentNeighbourhoodDiregment.Right;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.TopLeft)
                    {
                        return SegmentNeighbourhoodDiregment.Top;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.TopRight)
                    {
                        return SegmentNeighbourhoodDiregment.TopRight;
                    }
                }

                if (_cornerOfNewSegment == SegmentNeighbourhoodCorner.BottomRight)
                {
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.BottomRight)
                    {
                        Preconditions.Fail("Unexpected");
                        return null;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.BottomLeft)
                    {
                        return SegmentNeighbourhoodDiregment.Left;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.TopLeft)
                    {
                        return SegmentNeighbourhoodDiregment.TopLeft;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.TopRight)
                    {
                        return SegmentNeighbourhoodDiregment.Top;
                    }
                }

                if (_cornerOfNewSegment == SegmentNeighbourhoodCorner.TopRight)
                {
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.TopRight)
                    {
                        Preconditions.Fail("Unexpected");
                        return null;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.BottomRight)
                    {
                        return SegmentNeighbourhoodDiregment.Bottom;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.TopLeft)
                    {
                        return SegmentNeighbourhoodDiregment.Left;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.BottomLeft)
                    {
                        return SegmentNeighbourhoodDiregment.BottomLeft;
                    }
                }

                if (_cornerOfNewSegment == SegmentNeighbourhoodCorner.TopLeft)
                {
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.TopLeft)
                    {
                        Preconditions.Fail("Unexpected");
                        return null;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.BottomRight)
                    {
                        return SegmentNeighbourhoodDiregment.BottomRight;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.BottomLeft)
                    {
                        return SegmentNeighbourhoodDiregment.Bottom;
                    }
                    if (cornerInIntersegment == SegmentNeighbourhoodCorner.TopRight)
                    {
                        return SegmentNeighbourhoodDiregment.Right;
                    }
                }
                Preconditions.Fail("Not expected");
                return null;
            }
        }
    }
}