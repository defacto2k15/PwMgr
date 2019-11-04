using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Painter;
using Assets.Utils;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.VisibilityTexture
{
    public class Ring1VisibilityTextureProcessorUTProxy : BaseUTTransformProxy<object, Ring1VisibilityTextureDelta>
    {
        private Ring1VisibilityTextureProcessor _processor;

        public Ring1VisibilityTextureProcessorUTProxy(Ring1VisibilityTextureProcessor processor)
        {
            _processor = processor;
        }

        public void AddOrder(Ring1VisibilityTextureDelta delta)
        {
            BaseUtAddOrder(delta);
        }


        protected override object ExecuteOrder(Ring1VisibilityTextureDelta delta)
        {
            _processor.ProcessDelta(delta);
            return null;
        }
    }

    public class Ring1VisibilityTextureProcessor
    {
        private Texture2D _visibilityTexture;

        public Ring1VisibilityTextureProcessor(Texture2D visibilityTexture)
        {
            _visibilityTexture = visibilityTexture;
        }

        public void ProcessDelta(Ring1VisibilityTextureDelta delta)
        {
            if (delta.Changes.Any())
            {
                foreach (var pair in delta.Changes)
                {
                    int rValue = 0;
                    if (pair.Value)
                    {
                        rValue = 1;
                    }

                    _visibilityTexture.SetPixel(pair.Key.X, pair.Key.Y, new Color(rValue, 0, 0));
                }
                _visibilityTexture.Apply();
            }
        }
    }

    public class Ring1VisibilityTextureDelta
    {
        private readonly Dictionary<IntVector2, bool> _changes;

        public Ring1VisibilityTextureDelta(Dictionary<IntVector2, bool> changes)
        {
            _changes = changes;
        }

        public Dictionary<IntVector2, bool> Changes => _changes;

        public bool AnyChange
        {
            get { return _changes.Any(); }
        }
    }
}