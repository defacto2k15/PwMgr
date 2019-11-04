using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;

namespace Assets.Heightmaps.Ring1.Painter
{
    public class Ring1PaintingOrderGrabber
    {
        private Ring1TerrainPainterOrder _order = new Ring1TerrainPainterOrder();
        private UInt32 _lastId = 0;

        public bool IsAnyOrder
        {
            get { return _order.Any; }
        }

        public UInt32 AddCreationOrder(Ring1GroundPieceCreationTemplate creationTemplate)
        {
            _order.NewlyCreatedElements[_lastId] = creationTemplate;
            return _lastId++;
        }

        public void SetActive(uint ring1TerrainId, bool isActive)
        {
            _order.ActivationChanges[ring1TerrainId] = isActive;
        }

        public Ring1TerrainPainterOrder RetriveOrderAndClear()
        {
            var toReturn = _order;
            _order = new Ring1TerrainPainterOrder();
            return toReturn;
        }
    }
}