using System;
using System.Collections.Concurrent;
using System.Linq;
using Assets.Utils;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Painter
{
    public class RingTerrainPainterUTProxy : BaseUTTransformProxy<object, Ring1TerrainPainterOrder>
    {
        private RingTerrainPainter _painter;

        public RingTerrainPainterUTProxy(RingTerrainPainter painter)
        {
            _painter = painter;
        }

        public void AddOrder(Ring1TerrainPainterOrder order)
        {
            BaseUtAddOrder(order);
        }

        protected override object ExecuteOrder(Ring1TerrainPainterOrder order)
        {
            _painter.ProcessOrder(order);
            return null;
        }
    }
}