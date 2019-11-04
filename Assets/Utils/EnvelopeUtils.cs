using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using GeoAPI.Geometries;

namespace Assets.Utils
{
    public static class EnvelopeUtils
    {
        public static Envelope WidenEnvelope(Envelope input, float widthOffset)
        {
            float half = widthOffset / 2;
            return new Envelope(input.MinX - half, input.MaxX + half, input.MinY - half, input.MaxY + half);
        }

        public static MyRectangle ToMyRectangle(this Envelope envelope)
        {
            return new MyRectangle(
                (float) envelope.MinX,
                (float) envelope.MinY,
                (float) envelope.CalculatedWidth(),
                (float) envelope.CalculatedHeight()
            );
        }

        public static MyRectangle ToUnityCoordPositions2D(this Envelope envelope)
        {
            return ToMyRectangle(envelope);
        }

        public static Envelope ToEnvelope(this MyRectangle rect)
        {
            return new Envelope(rect.X, rect.X + rect.Width, rect.Y, rect.Y + rect.Height);
        }

        public static double CalculatedWidth(this Envelope envelope)
        {
            return envelope.MaxX - envelope.MinX;
        }

        public static double CalculatedHeight(this Envelope envelope)
        {
            return envelope.MaxY - envelope.MinY;
        }
    }
}