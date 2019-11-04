using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Trees.SpotUpdating.RTAlignment;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.SpotUpdating
{
    public class RTAlignedTree<V,T> where T : class where V : class
    {
        private Dictionary<RTAlignedArea, RTAlignedNode<V,T>> _topLevelNodes = new Dictionary<RTAlignedArea, RTAlignedNode<V,T>>();
        private int _topLodLevel;

        public RTAlignedTree(int topLodLevel=3)
        {
            _topLodLevel = topLodLevel;
        }

        public IEnumerable<V> InsertTerrainRetriveItems(RTAlignedTerrain<T> terrain)
        {
            var topLodArea = terrain.Area.RetriveParentArea(_topLodLevel);
            if (!_topLevelNodes.ContainsKey(topLodArea))
            {
                _topLevelNodes[topLodArea] = new RTAlignedNode<V,T>(topLodArea);
            }
            return _topLevelNodes[topLodArea].InsertTerrain(terrain);
        }

        public T InsertItemRetriveTerrain(RTAlignedItem<V> item)
        {
            var area = item.Position.ContainingAreaAt(_topLodLevel);
            if (!_topLevelNodes.ContainsKey(area))
            {
                _topLevelNodes.Add(area, new RTAlignedNode<V,T>(area));
            }
            return _topLevelNodes[area].InsertItem(item);
        }

        public IEnumerable<V> Query(RTAlignedArea area)
        {
            if (area.LodLevel == _topLodLevel)
            {
                if (_topLevelNodes.ContainsKey(area))
                {
                    return _topLevelNodes[area].GetAllItems();
                }
                else
                {
                    return new List<V>();
                }
            }
            else
            {
                var topLevelArea = area.RetriveParentArea(_topLodLevel);
                if (_topLevelNodes.ContainsKey(topLevelArea))
                {
                    return _topLevelNodes[topLevelArea].QueryItems(area);
                }
                else
                {
                    return new List<V>();
                }
            }
        }

        public void RemoveItem(RTAlignedItem<V> item)
        {
            var area = item.Position.ContainingAreaAt(_topLodLevel);
            _topLevelNodes[area].RemoveItem(item);
        }

        public void RemoveTerrain(RTAlignedTerrain<T> terrain)
        {
            var topLodArea = terrain.Area.RetriveParentArea(_topLodLevel);
            _topLevelNodes[topLodArea].RemoveTerrain(terrain);
        }
    }

    public class RTAlignedNode<V,T> where T : class where V : class
    {
        private Dictionary<RTAlignedArea, RTAlignedChildrenNode<V,T>> _childrenNodes = new Dictionary<RTAlignedArea, RTAlignedChildrenNode<V,T>>();
        private RTAlignedArea _area;
        private T _terrain;

        public RTAlignedNode(RTAlignedArea area)
        {
            _area = area;
            _area.ComputeQuadAreas().ForEach(c => _childrenNodes.Add(c, new RTAlignedChildrenNode<V,T>(c)));
        }

        public IEnumerable<V> GetAllItems()
        {
            return _childrenNodes.Values.SelectMany(c => c.ChildItems());
        }

        public IEnumerable<V> QueryItems(RTAlignedArea queryArea)
        {
            if (_area.Equals(queryArea))
            {
                return GetAllItems();
            }
            else
            {
                return _childrenNodes.First(c => c.Key.Contains(queryArea)).Value.ChildItems();
            }
        }

        public T InsertItem(RTAlignedItem<V> item)
        {
            var childArea = item.Position.ContainingAreaAt(_area.LodLevel + 1);
            var subTerrain = _childrenNodes[childArea].InsertItem(item);
            if (subTerrain == null)
            {
                if (_terrain == null)
                {
                    return null;
                }
                else
                {
                    return _terrain;
                }
            }
            else
            {
                return subTerrain;
            }
        }

        public IEnumerable<V> InsertTerrain(RTAlignedTerrain<T> terrain)
        {
            if (terrain.Area.LodLevel == _area.LodLevel)
            {
                Preconditions.Assert(terrain.Area.Equals(_area),
                    $"Areas do not match: orig {_area} new {terrain.Area}");
                _terrain = terrain.Terrain;
                return GetAllItems();
            }
            else
            {
                var subTerrainArea = terrain.Area.RetriveParentArea(_area.LodLevel + 1);
                return _childrenNodes[subTerrainArea].InsertTerrain(terrain);
            }
        }

        public void RemoveItem(RTAlignedItem<V> item)
        {
            var childArea = item.Position.ContainingAreaAt(_area.LodLevel + 1);
            _childrenNodes[childArea].RemoveItem(item);
        }

        public void RemoveTerrain(RTAlignedTerrain<T> terrain)
        {
            if (terrain.Area.LodLevel == _area.LodLevel)
            {
                Preconditions.Assert(_terrain!= null, $"E64 Terrain being removed at {_area} is null");
                Preconditions.Assert(_terrain == terrain.Terrain, "E98 removed terrain and terrain in node are not equal");
                _terrain = null;
            }
            else
            {
                var subTerrainArea = terrain.Area.RetriveParentArea(_area.LodLevel + 1);
                _childrenNodes[subTerrainArea].RemoveTerrain(terrain);
            }
        }
    }

    public class RTAlignedChildrenNode<V,T> where T : class where V : class
    {
        private RTAlignedArea _childArea;

        private List<RTAlignedItem<V>> _childrenItems;
        private RTAlignedNode<V,T> _childrenNode;

        public RTAlignedChildrenNode(RTAlignedArea childArea)
        {
            _childArea = childArea;
        }

        public IEnumerable<V> ChildItems()
        {
            Preconditions.Assert(_childrenItems == null || _childrenNode == null, "E59 both childitems and childnodes are not null");
            if (_childrenItems != null)
            {
                return _childrenItems.Select(c => c.Item);
            }
            else if (_childrenNode != null)
            {
                return _childrenNode.GetAllItems();
            }
            else
            {
                return Enumerable.Empty<V>();
            }
        }

        public T InsertItem(RTAlignedItem<V> item)
        {
            Preconditions.Assert(_childrenItems == null || _childrenNode == null, "E59 both childitems and childnodes are not null");
            if (_childrenNode != null)
            {
                return _childrenNode.InsertItem(item);
            }
            else
            {
                if (_childrenItems == null)
                {
                    _childrenItems = new List<RTAlignedItem<V>>();
                }
                _childrenItems.Add(item);
                return default(T);
            }
        }

        public void RemoveItem(RTAlignedItem<V> item)
        {
            if (_childrenNode != null)
            {
                _childrenNode.RemoveItem(item);
            }
            else
            {
                Preconditions.Assert(_childrenItems.Remove(item), "E44. Removing item failed");
            }
        }

        public IEnumerable<V> InsertTerrain(RTAlignedTerrain<T> terrain)
        {
            if (_childrenNode != null)
            {
                return _childrenNode.InsertTerrain(terrain);
            }
            else
            {
                _childrenNode = new RTAlignedNode<V, T>(_childArea);
                if (_childrenItems != null)
                {
                    _childrenItems.ForEach(c => _childrenNode.InsertItem(c));
                    _childrenItems = null;
                }
                return _childrenNode.InsertTerrain(terrain);
            }
        }

        public void RemoveTerrain(RTAlignedTerrain<T> terrain)
        {
            Preconditions.Assert(_childrenNode!= null, "E87 Cannot remove as there is null child node");
            _childrenNode.RemoveTerrain(terrain);
        }
    }

    public class RTAlignedArea
    {
        private int _lodLevel;
        private IntVector2 _pos;
        private float _wholeAreaLength;

        public RTAlignedArea(int lodLevel, IntVector2 pos, float wholeAreaLength)
        {
            _lodLevel = lodLevel;
            _pos = pos;
            _wholeAreaLength = wholeAreaLength;
        }

        public List<RTAlignedArea> ComputeQuadAreas()
        {
            return new List<RTAlignedArea>()
            {
                new RTAlignedArea(_lodLevel+1, _pos*2+new IntVector2(0,0), _wholeAreaLength),
                new RTAlignedArea(_lodLevel+1, _pos*2+new IntVector2(1,0), _wholeAreaLength),
                new RTAlignedArea(_lodLevel+1, _pos*2+new IntVector2(0,1), _wholeAreaLength),
                new RTAlignedArea(_lodLevel+1, _pos*2+new IntVector2(1,1), _wholeAreaLength),
            };
        }

        public RTAlignedArea RetriveParentArea(int topLodLevel)
        {
            Preconditions.Assert(_lodLevel >= topLodLevel,
                $"E251 Cannot get parent pos: this lod is {_lodLevel} parent is {topLodLevel}");
            var div = Mathf.Pow(2, _lodLevel- topLodLevel);
            var parentPos = new IntVector2(Mathf.FloorToInt(_pos.X /div), Mathf.FloorToInt(_pos.Y/div) );
            return new RTAlignedArea(topLodLevel, parentPos, _wholeAreaLength);
        }

        public int LodLevel => _lodLevel;

        public MyRectangle GlobalSize 
        {
            get
            {
                var loddedSideLength = _wholeAreaLength / Mathf.Pow(2, _lodLevel);
                return new MyRectangle(_pos.X * loddedSideLength, _pos.Y * loddedSideLength,
                    loddedSideLength, loddedSideLength);
            }
        }

        protected bool Equals(RTAlignedArea other)
        {
            return _lodLevel == other._lodLevel && _pos.Equals(other._pos) && _wholeAreaLength.Equals(other._wholeAreaLength);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RTAlignedArea) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _lodLevel;
                hashCode = (hashCode * 397) ^ _pos.GetHashCode();
                hashCode = (hashCode * 397) ^ _wholeAreaLength.GetHashCode();
                return hashCode;
            }
        }

        public bool Contains(RTAlignedArea queryArea)
        {
            if (queryArea.LodLevel < LodLevel)
            {
                return false;
            }
            var lodOffset = queryArea.LodLevel - LodLevel;
            var lodMult = Mathf.RoundToInt(Mathf.Pow(2, lodOffset));
            var thisStartPos = queryArea._pos * lodMult;

            var posDiff = queryArea.LocalLodPos - thisStartPos;
            if (posDiff.X < 0 || posDiff.Y < 0)
            {
                return false;
            }
            if (posDiff.X > lodMult || posDiff.Y > lodMult)
            {
                return false;
            }
            return true;
        }

        public IntVector2 LocalLodPos => _pos;

        public override string ToString()
        {
            return $"{nameof(_lodLevel)}: {_lodLevel}, {nameof(_pos)}: {_pos}, {nameof(_wholeAreaLength)}: {_wholeAreaLength}, {nameof(GlobalSize)}: {GlobalSize}";
        }
    }

    public class RTAlignedItem<V>
    {
        public RTAlignedPosition Position;
        public V Item;
    }

    public class RTAlignedTerrain<V>
    {
        public RTAlignedArea Area;
        public V Terrain;
    }

    public class RTAlignedPosition
    {
        private Vector2 _position;
        private float _wholeAreaLength;

        public RTAlignedPosition(Vector2 position, float wholeAreaLength)
        {
            _position = position;
            _wholeAreaLength = wholeAreaLength;
        }

        public RTAlignedArea ContainingAreaAt(int newLodLevel)
        {
            var loddedAreaSide = _wholeAreaLength / Mathf.Pow(2, newLodLevel);
            var alignedPosition = new IntVector2(Mathf.FloorToInt(_position.x / loddedAreaSide),
                Mathf.FloorToInt(_position.y / loddedAreaSide));
            return new RTAlignedArea(newLodLevel,alignedPosition,_wholeAreaLength);
        }

        protected bool Equals(RTAlignedPosition other)
        {
            return _position.Equals(other._position) && _wholeAreaLength.Equals(other._wholeAreaLength);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RTAlignedPosition) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_position.GetHashCode() * 397) ^ _wholeAreaLength.GetHashCode();
            }
        }
    }
}
