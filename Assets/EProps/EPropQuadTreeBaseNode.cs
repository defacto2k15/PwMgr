using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using UnityEngine;

namespace Assets.EProps
{
    using LocaleBufferScopeIndexType = UInt32;
    using InScopeIndexType = UInt32;


    public class EPropQuadTreeBaseNode 
    {
        private MyQuantRectangle _rectangle;
        private readonly EPropLocaleBufferManager _localeBufferManager;
        private IEPropQuadTreeNode _node;

        public EPropQuadTreeBaseNode(MyQuantRectangle rectangle, EPropLocaleBufferManager localeBufferManager)
        {
            _rectangle = rectangle;
            _localeBufferManager = localeBufferManager;
            _node = new EPropQuadTreeLeaf(rectangle, localeBufferManager);
        }

        public EPropElevationId RegisterProp(Vector2 flatPosition)
        {
            return _node.RegisterProp(flatPosition);
        }

        public List<EPropElevationId> RegisterPropGroup(List<Vector2> positions, Vector2 center)
        {
            return _node.RegisterPropGroup(positions, center);
        }

        public List<EPropSectorSoleChange> RetriveAndClearUpdateOrders()
        {
            return _node.RetriveAndClearUpdateOrders();
        }

        public List<SectorWithStateAndRectangle> RetriveSectorsWithState(EPropHotAreaSelectorWithParameters selectorWithParameters)
        {
            return _node.RetriveSectorsWithState(selectorWithParameters);
        }

        public EPropQuadTreeDivisionResult ResolveDivision(EPropQuadTreeDivisionDecider decider)
        {
            var expectedNodeType = decider.WhatTypeShouldNodeBe(_rectangle.RealSpaceRectangle);
            var currentNodeType = _node.NodeType;

            var idChanges = new List<EPropIdChange>();
            var scopesToFree = new List<LocaleBufferScopeIndexType>();
            if (expectedNodeType == currentNodeType)
            {
                if (currentNodeType == EPropQuadTreeNodeType.Complex)
                {
                    var complexNode = _node as EPropQuadTreeComplex;
                    var complexNodeDivisionResult = complexNode.ResolveDivision(decider.ChildElementDecider());

                    idChanges.AddRange(complexNodeDivisionResult.IdChanges);
                    scopesToFree.AddRange(complexNodeDivisionResult.ScopesToFree);

                }
            }else {
                Dictionary<LocaleBufferScopeIndexType, EPropLocaleBufferScopeRegistry> scopes = _node.TakeAwayAllScopes();
                if (expectedNodeType == EPropQuadTreeNodeType.Leaf)
                {
                    var leafNode = new EPropQuadTreeLeaf(_rectangle, _localeBufferManager);
                    _node = leafNode;
                    leafNode.SetScopes(scopes);
                }
                else
                {
                    var flatPositionWithId = scopes.SelectMany(c =>
                    {
                        var allLocales = c.Value.RetriveAllLocales();
                        return allLocales.Select(k => new EPropElevationIdWithFlatPosition()
                        {
                            FlatPosition = k.FlatPosition,
                            Id = new EPropElevationId()
                            {
                                InScopeIndex = k.InScopeIndex,
                                LocaleBufferScopeIndex = c.Key
                            }
                        });
                    });
                    var complexNode = new EPropQuadTreeComplex(_rectangle, _localeBufferManager);
                    var complexNodeDivisionResult = complexNode.ResolveDivision(decider.ChildElementDecider());

                    idChanges.AddRange(complexNodeDivisionResult.IdChanges);
                    scopesToFree.AddRange(complexNodeDivisionResult.ScopesToFree);

                    _node = complexNode;
                    idChanges.AddRange(flatPositionWithId.Select(c =>
                        new EPropIdChange()
                        {
                            NewId = _node.RegisterProp(c.FlatPosition),
                            OldId = c.Id
                        }
                    ));

                    scopesToFree.AddRange(scopes.Keys);
                }
            }

            return new EPropQuadTreeDivisionResult()
            {
                IdChanges = idChanges,
                ScopesToFree = scopesToFree
            };
        }

        public Dictionary<LocaleBufferScopeIndexType, EPropLocaleBufferScopeRegistry> TakeAwayAllScopes()
        {
            return _node.TakeAwayAllScopes();
        }

        public DebugSectorInformation DebugQuerySectorStates(EPropHotAreaSelectorWithParameters selectorWithParameters, int depth)
        {
            return new DebugSectorInformation()
            {
                Depth = depth,
                Area = _rectangle,
                Children = new List<DebugSectorInformation>() {  _node.DebugQuerySectorStates(selectorWithParameters, depth+1)},
                SectorState = EPropSectorState.Cold
            };
        }

    }


    public enum EPropQuadTreeApex
    {
        TopLeft, TopRight, BottomLeft, BottomRight
    }

    public interface IEPropQuadTreeNode
    {
        EPropElevationId RegisterProp(Vector2 flatPosition);
        List<EPropElevationId> RegisterPropGroup(List<Vector2> positions, Vector2 center);
        List<EPropSectorSoleChange> RetriveAndClearUpdateOrders();
        List<SectorWithStateAndRectangle> RetriveSectorsWithState(EPropHotAreaSelectorWithParameters selectorWithParameters);
        EPropQuadTreeNodeType NodeType { get; }
        Dictionary<LocaleBufferScopeIndexType, EPropLocaleBufferScopeRegistry> TakeAwayAllScopes();
        DebugSectorInformation DebugQuerySectorStates(EPropHotAreaSelectorWithParameters selectorWithParameters, int depth);
    }

    public class EPropQuadTreeLeaf : IEPropQuadTreeNode
    {
        private EPropSector _leafSector;
        private MyQuantRectangle _rectangle;

        public EPropQuadTreeLeaf(MyQuantRectangle rectangle, EPropLocaleBufferManager localeBufferManager)
        {
            _rectangle = rectangle;
            _leafSector = new EPropSector(localeBufferManager);
        }

        public EPropElevationId RegisterProp(Vector2 flatPosition)
        {
            Preconditions.Assert(MyRectangle.IsInside(_rectangle.RealSpaceRectangle,flatPosition), $"Prop of position {flatPosition} is not inside rectangle of node {_rectangle}");
            return _leafSector.RegisterProp(flatPosition);
        }

        public List<EPropElevationId> RegisterPropGroup(List<Vector2> positions, Vector2 center)
        {
            Preconditions.Assert(MyRectangle.IsInside(_rectangle.RealSpaceRectangle,center), $"Prop group of center position {center} is not inside rectangle of node {_rectangle}");
            return _leafSector.RegisterPropGroup(positions);
        }

        public List<EPropSectorSoleChange> RetriveAndClearUpdateOrders()
        {
            return _leafSector.RetriveAndClearUpdateOrders();
        }

        public List<SectorWithStateAndRectangle> RetriveSectorsWithState(EPropHotAreaSelectorWithParameters selectorWithParameters)
        {
            var state = ComputeSectorState(selectorWithParameters);

            return new List<SectorWithStateAndRectangle>()
            {
                new SectorWithStateAndRectangle()
                {
                    Sector = _leafSector,
                    State = state,
                    Rectangle = _rectangle
                }
            };
        }

        private EPropSectorState ComputeSectorState(EPropHotAreaSelectorWithParameters selectorWithParameters)
        {
            var isSectorHot = !_leafSector.IsEmpty && selectorWithParameters.IsRectangleInAnyMergeArea(_rectangle.RealSpaceRectangle);
            EPropSectorState state;
            if (isSectorHot)
            {
                state = EPropSectorState.Hot;
            }
            else
            {
                state = EPropSectorState.Cold;
            }

            return state;
        }

        public EPropQuadTreeNodeType NodeType => EPropQuadTreeNodeType.Leaf;
        public Dictionary<LocaleBufferScopeIndexType, EPropLocaleBufferScopeRegistry> TakeAwayAllScopes()
        {
            return _leafSector.TakeAwayScopes();
        }

        public DebugSectorInformation DebugQuerySectorStates(EPropHotAreaSelectorWithParameters selectorWithParameters, int depth)
        {
            return new DebugSectorInformation()
            {
                Area = _rectangle,
                Children = new List<DebugSectorInformation>(),
                Depth = depth,
                SectorState = ComputeSectorState(selectorWithParameters)
            };
        }

        public void SetScopes(Dictionary<LocaleBufferScopeIndexType, EPropLocaleBufferScopeRegistry> scopes)
        {
            _leafSector.SetScopes(scopes);
        }
    }

    public class EPropQuadTreeComplex : IEPropQuadTreeNode
    {
        private Dictionary<EPropQuadTreeApex, EPropQuadTreeBaseNode> _subNodes;
        private MyQuantRectangle _rectangle;

        public EPropQuadTreeComplex(MyQuantRectangle rectangle, EPropLocaleBufferManager localeBufferManager)
        {
            _rectangle = rectangle;
            _subNodes = Enum.GetValues(typeof(EPropQuadTreeApex)).Cast<EPropQuadTreeApex>()
                .ToDictionary(c => c, c => new EPropQuadTreeBaseNode(CreateSubRectangle(c, _rectangle), localeBufferManager));
        }

        private MyQuantRectangle CreateSubRectangle(EPropQuadTreeApex apex, MyQuantRectangle rectangle)
        {
            if (apex == EPropQuadTreeApex.BottomLeft)
            {
                return rectangle.GetQuantBottomLeftRectangle();
            }
            else if (apex == EPropQuadTreeApex.BottomRight)
            {
                return rectangle.GetQuantBottomRightRectangle();
            }
            else if (apex == EPropQuadTreeApex.TopLeft)
            {
                return rectangle.GetQuantTopLeftRectangle();
            }
            else if (apex == EPropQuadTreeApex.TopRight)
            {
                return rectangle.GetQuantTopRightRectangle();
            }

            Preconditions.Fail("Unsupported apex " + apex);
            return rectangle;
        }

        public EPropElevationId RegisterProp(Vector2 flatPosition)
        {
            var apex = FindApex(flatPosition);
            return _subNodes[apex].RegisterProp(flatPosition);
        }

        public List<EPropElevationId> RegisterPropGroup(List<Vector2> positions, Vector2 center)
        {
            var apex = FindApex(center);
            return _subNodes[apex].RegisterPropGroup(positions, center);
        }

        private EPropQuadTreeApex FindApex(Vector2 position)
        {
            var realSpaceRect = _rectangle.RealSpaceRectangle;
            bool isRight = position.x > realSpaceRect.X + realSpaceRect.Width/2;
            bool isTop = position.y > realSpaceRect.Y + realSpaceRect.Height/2;

            if (isRight)
            {
                if (isTop)
                {
                    return EPropQuadTreeApex.TopRight;
                }
                else
                {
                    return EPropQuadTreeApex.BottomRight;
                }
            }
            else
            {
                if (isTop)
                {
                    return EPropQuadTreeApex.TopLeft;
                }
                else
                {
                    return EPropQuadTreeApex.BottomLeft;
                }
            }
        }

        public List<EPropSectorSoleChange> RetriveAndClearUpdateOrders()
        {
            return _subNodes.Values.SelectMany(c => c.RetriveAndClearUpdateOrders()).ToList();
        }

        public List<SectorWithStateAndRectangle> RetriveSectorsWithState(EPropHotAreaSelectorWithParameters selectorWithParameters)
        {
            return _subNodes.Values.SelectMany(c => c.RetriveSectorsWithState(selectorWithParameters)).ToList();
        }

        public EPropQuadTreeNodeType NodeType => EPropQuadTreeNodeType.Complex;

        public Dictionary<LocaleBufferScopeIndexType, EPropLocaleBufferScopeRegistry> TakeAwayAllScopes()
        {
            return _subNodes.SelectMany(c => c.Value.TakeAwayAllScopes()).ToDictionary(c => c.Key, c => c.Value);
        }

        public DebugSectorInformation DebugQuerySectorStates(EPropHotAreaSelectorWithParameters selectorWithParameters, int depth)
        {
            return new DebugSectorInformation()
            {
                Area = _rectangle,
                Depth = depth,
                SectorState = EPropSectorState.Cold,
                Children = _subNodes.Select(c => c.Value.DebugQuerySectorStates(selectorWithParameters,depth)).ToList()
            };
        }

        public  EPropQuadTreeDivisionResult ResolveDivision(EPropQuadTreeDivisionDecider decider)
        {
            var childResults = _subNodes.Values.Select(c => c.ResolveDivision(decider)).ToList();
            return new EPropQuadTreeDivisionResult()
            {
                ScopesToFree = childResults.SelectMany(c=>c.ScopesToFree).ToList(),
                IdChanges = childResults.SelectMany(c=>c.IdChanges).ToList()
            };
        }
    }

    public class EPropQuadTreeDivisionDecider
    {
        private Func<float, int> _requiredNodeDepthResolver;
        private Vector2 _travellerPosition;
        private int _currentDepth;

        public EPropQuadTreeDivisionDecider(Func<float, int> requiredNodeDepthResolver, Vector2 travellerPosition, int currentDepth)
        {
            _requiredNodeDepthResolver = requiredNodeDepthResolver;
            _travellerPosition = travellerPosition;
            _currentDepth = currentDepth;
        }

        public EPropQuadTreeNodeType WhatTypeShouldNodeBe(MyRectangle sectorRectangle)
        {
            var distance = sectorRectangle.Vertices.Select(c => VectorUtils.ManhattanDistance(c, _travellerPosition)).Min();
            var requredDepth = _requiredNodeDepthResolver(distance);
            if (_currentDepth < requredDepth)
            {
                return EPropQuadTreeNodeType.Complex;
            }
            else
            {
                return EPropQuadTreeNodeType.Leaf;
            }
        }

        public EPropQuadTreeDivisionDecider ChildElementDecider() => new EPropQuadTreeDivisionDecider(_requiredNodeDepthResolver,_travellerPosition, _currentDepth+1);
    }

    public enum EPropQuadTreeNodeType
    {
        Leaf, Complex
    }

    public class EPropQuadTreeDivisionResult
    {
        public List<EPropIdChange> IdChanges;
        public List<LocaleBufferScopeIndexType> ScopesToFree;
    }

    public class EPropIdChange
    {
        public EPropElevationId OldId;
        public EPropElevationId NewId;
    }

    public class EPropElevationIdWithFlatPosition
    {
        public EPropElevationId Id;
        public Vector2 FlatPosition;
    }
}