using System.Collections.Generic;
using System.Linq;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass.Lod
{
    internal class LodGroup
    {
        private readonly List<LodEntitySplat> _lodEntitySplats;
        private int? _lodLevel;
        private readonly MapAreaPosition _position;

        public LodGroup(List<LodEntitySplat> lodEntitySplats, MapAreaPosition position)
        {
            this._lodEntitySplats = lodEntitySplats;
            _position = position;
        }

        public LodGroup(List<LodEntitySplat> lodEntitySplats, MapAreaPosition position, int lodLevel) : this(
            lodEntitySplats, position)
        {
            this._lodLevel = lodLevel;
        }

        public int LodLevel
        {
            get { return _lodLevel.Value; }
        }

        public MapAreaPosition Position
        {
            get { return _position; }
        }

        public LodGroup UpdateLod(int newLod)
        {
            if (_position.DownLeft == new Vector3(80.0f, 0, 70.0f))
            {
                Debug.Log(string.Format("W11 LodGroup of position {0} and lodLevel {1} changing lod to {2}",
                    _position.ToString(), _lodLevel, newLod));
            }
            List<LodEntitySplat> newSplats = new List<LodEntitySplat>();
            foreach (var lodEntitySplat in _lodEntitySplats)
            {
                newSplats.Add(lodEntitySplat.UpdateLod(newLod));
            }
            return new LodGroup(newSplats, _position, newLod);
        }

        public void Remove()
        {
            foreach (var lodEntitySplat in _lodEntitySplats)
            {
                lodEntitySplat.Remove();
            }
        }

        public void InitializeGroup(int lodLevel)
        {
            this._lodLevel = lodLevel;
            foreach (var lodEntitySplat in _lodEntitySplats)
            {
                lodEntitySplat.Initialize(lodLevel);
            }
        }

        public void Enable()
        {
            foreach (var lodEntitySplat in _lodEntitySplats)
            {
                lodEntitySplat.Enable();
            }
        }
    }
}