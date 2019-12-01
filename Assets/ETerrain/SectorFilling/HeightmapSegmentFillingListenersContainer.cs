using System.Collections.Generic;
using Assets.ETerrain.Pyramid.Map;
using Assets.Utils;

namespace Assets.ETerrain.SectorFilling
{
    public class HeightmapSegmentFillingListenersContainer
    {
        private Dictionary<HeightPyramidLevel, UnityThreadCompoundSegmentFillingListener> _listenersDict= new Dictionary<HeightPyramidLevel, UnityThreadCompoundSegmentFillingListener>();

        public void AddListener(HeightPyramidLevel level, UnityThreadCompoundSegmentFillingListener listener)
        {
            Preconditions.Assert(!_listenersDict.ContainsKey(level),$"There aelaady is listener from level {level}");
            _listenersDict[level] = listener;
        }

        public Dictionary<HeightPyramidLevel, UnityThreadCompoundSegmentFillingListener> ListenersDict => _listenersDict;
    }
}