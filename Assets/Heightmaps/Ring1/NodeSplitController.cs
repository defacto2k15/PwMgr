using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class NodeSplitController
    {
        private int _maximumLodLevel;
        private int _minimumLodLevel;
        private Dictionary<float, int> _precisionDistances;
        private UnityCoordsCalculator _coordsCalculator;
        private Vector3 _cameraPosition;

        public NodeSplitController(
            int maximumLodLevel,
            int minimumLodLevel,
            Dictionary<float, int> precisionDistances,
            UnityCoordsCalculator coordsCalculator)
        {
            this._maximumLodLevel = maximumLodLevel;
            this._minimumLodLevel = minimumLodLevel;
            this._precisionDistances = precisionDistances;
            this._coordsCalculator = coordsCalculator;

            Preconditions.Assert(maximumLodLevel > minimumLodLevel, "maximum lod level must be smaller than minimum!");
            //Debug.Log("E1: "+StringUtils.ToString(precisionDistances.Keys));
        }

        private bool IsMaximumLod(int lodLevel)
        {
            return lodLevel >= _maximumLodLevel;
        }

        private bool IsMinimumLod(int lodLevel)
        {
            return lodLevel >= _minimumLodLevel;
        }

        private bool IsPreciseEnough(Ring1Node node)
        {
            Vector2 center = _coordsCalculator.CalculateGlobalObjectPosition(node.Ring1Position).Center;
            float currentDistance = Vector2.Distance(Utils.VectorUtils.To2DPosition(_cameraPosition),
                center);

            int minLod = _precisionDistances.Values.Max();
            int appropiateQuadLod =
                _precisionDistances.OrderByDescending(a => a.Key)
                    .Where(c => c.Key < currentDistance)
                    .Select(c => c.Value)
                    .DefaultIfEmpty(minLod)
                    .First();
            var isPreciseEnough = node.QuadLodLevel >= appropiateQuadLod;
            //Debug.Log($"E1: Cen:{_cameraPosition} Test center:{center}, lod {node.QuadLodLevel} distance{currentDistance}, appropiate: {appropiateQuadLod}, isOk, {isPreciseEnough}");

            return isPreciseEnough;
        }

        public bool IsTerminalNode(Ring1Node node)
        {
            return (IsMinimumLod(node.QuadLodLevel)) && (IsPreciseEnough(node) || IsMaximumLod(node.QuadLodLevel));
        }

        public void SetCameraPosition(Vector3 position)
        {
            _cameraPosition = position;
        }

        public Vector3 CameraLastPosition => _cameraPosition;
    }
}