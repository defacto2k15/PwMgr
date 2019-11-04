using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Preparment.MarginMerging
{
    class MarginPosition
    {
        private Point2D _startPoint;
        private Point2D _endPoint;

        public MarginPosition(Point2D startPoint, Point2D endPoint)
        {
            AssertPositionsAreCorrect(startPoint, endPoint);

            var toSortList = (new List<Point2D> {startPoint, endPoint}).OrderBy((a) => a.X).ThenBy((b) => b.Y).ToList();

            _endPoint = toSortList[1];
            _startPoint = toSortList[0];
        }

        public Point2D StartPoint
        {
            get { return _startPoint; }
        }

        public Point2D EndPoint
        {
            get { return _endPoint; }
        }

        public bool IsVertical
        {
            get { return StartPoint.X == EndPoint.X; }
        }

        public bool IsHorizontal
        {
            get { return StartPoint.Y == EndPoint.Y; }
        }

        private void AssertPositionsAreCorrect(Point2D endPos, Point2D startPos)
        {
            Preconditions.Assert(!Equals(startPos, endPos),
                string.Format("Start pos {0} and end pos {1} of margin can't be the same", startPos, endPos));
            Preconditions.Assert(startPos.X == endPos.X || startPos.Y == endPos.Y,
                string.Format("Start pos {0} and end pos {1} are not horizontal nor vertical", startPos, endPos));
        }

        public bool HaveCommonElementWith(MarginPosition position)
        {
            if (!((IsVertical && position.IsVertical) || (IsHorizontal && position.IsHorizontal)))
            {
                return false;
            }
            if (IsVertical)
            {
                if (StartPoint.X != position.StartPoint.X)
                {
                    return false;
                }
                else
                {
                    return MathHelp.SegmentsHaveCommonElement(StartPoint.Y, EndPoint.Y, position.StartPoint.Y,
                        position.EndPoint.Y);
                }
            }
            else
            {
                if (StartPoint.Y != position.StartPoint.Y)
                {
                    return false;
                }
                else
                {
                    return MathHelp.SegmentsHaveCommonElement(StartPoint.X, EndPoint.X, position.StartPoint.X,
                        position.EndPoint.X);
                }
            }
        }

        public MarginPosition GetCommonSegment(MarginPosition other)
        {
            Preconditions.Assert(HaveCommonElementWith(other),
                string.Format("Cant get common element of margins {0} and {1} - they have no common element ", this,
                    other));
            if (IsHorizontal)
            {
                return new MarginPosition(
                    new Point2D(Math.Max(StartPoint.X, other.StartPoint.X), StartPoint.Y),
                    new Point2D(Math.Min(EndPoint.X, other.EndPoint.X), EndPoint.Y));
            }
            else
            {
                return new MarginPosition(
                    new Point2D(StartPoint.X, Math.Max(StartPoint.Y, other.StartPoint.Y)),
                    new Point2D(EndPoint.X, Math.Min(EndPoint.Y, other.EndPoint.Y)));
            }
        }

        public override string ToString()
        {
            return string.Format("StartPoint: {0}, EndPoint: {1}", StartPoint, EndPoint);
        }

        public double InvLerp(Point2D point)
        {
            if (IsHorizontal)
            {
                return Mathf.InverseLerp(StartPoint.X, EndPoint.X, point.X);
            }
            else
            {
                return Mathf.InverseLerp(StartPoint.Y, EndPoint.Y, point.Y);
            }
        }
    }
}