using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using GeoAPI.Geometries;
using UnityEngine;

namespace Assets.Repositioning
{
    public class Repositioner
    {
        private readonly Vector2 _delta;

        public Repositioner(Vector2 delta)
        {
            _delta = delta;
        }

        public Envelope Move(Envelope sliceArea)
        {
            return new Envelope(sliceArea.MinX + _delta.x, sliceArea.MaxX + _delta.x, sliceArea.MinY + _delta.y,
                sliceArea.MaxY + _delta.y);
        }

        public static Repositioner Default => new Repositioner(-new Vector2(520 * 90, 580 * 90)); // 46800 52200
        public static Repositioner Identity => new Repositioner(Vector2.zero);

        public Vector2 Move(Vector2 pos)
        {
            return new Vector2(pos.x + _delta.x, pos.y + _delta.y);
        }

        public Vector3 Move(Vector3 pos)
        {
            return new Vector3(pos.x + _delta.x, pos.y, pos.z + _delta.y);
        }

        public MyRectangle Move(MyRectangle rect)
        {
            return new MyRectangle(rect.X + _delta.x, rect.Y + _delta.y, rect.Width, rect.Height);
        }

        public Vector2 InvMove(Vector2 pos)
        {
            return new Vector2(pos.x - _delta.x, pos.y - _delta.y);
        }

        public Vector3 InvMove(Vector3 pos)
        {
            return new Vector3(pos.x - _delta.x, pos.y, pos.z - _delta.y);
        }

        public MyRectangle InvMove(MyRectangle rect)
        {
            return new MyRectangle(rect.X - _delta.x, rect.Y - _delta.y, rect.Width, rect.Height);
        }

        public static Repositioner MergeRepositioners(Repositioner r1, Repositioner r2)
        {
            return new Repositioner(r1._delta+r2._delta);
        }
    }
}