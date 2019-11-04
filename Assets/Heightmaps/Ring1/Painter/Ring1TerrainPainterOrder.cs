using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.VisibilityTexture;

namespace Assets.Heightmaps.Ring1.Painter
{
    public class Ring1TerrainPainterOrder
    {
        public Dictionary<UInt32, Ring1GroundPieceCreationTemplate> NewlyCreatedElements =
            new Dictionary<uint, Ring1GroundPieceCreationTemplate>();

        public Dictionary<UInt32, bool> ActivationChanges = new Dictionary<uint, bool>();

        public bool Any
        {
            get { return NewlyCreatedElements.Any() || ActivationChanges.Any(); }
        }
    }
}