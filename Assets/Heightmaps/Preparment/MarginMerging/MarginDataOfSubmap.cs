using System;
using Assets.Heightmaps.submaps;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Preparment.MarginMerging
{
    internal class MarginDataOfSubmap
    {
        Submap _submap;

        public MarginDataOfSubmap(Submap submap)
        {
            this._submap = submap;
        }

        public Submap Submap
        {
            get { return _submap; }
        }

        public SubmapPosition SubmapPosition
        {
            get { return _submap.SubmapPosition; }
        }

        public int LodFactor
        {
            get { return _submap.LodFactor; }
        }

        public HeightmapMarginWithInfo GetDownMargin()
        {
            return new HeightmapMarginWithInfo(_submap.Heightmap.GetDownMargin(),
                new MarginPosition(_submap.SubmapPosition.DownLeftPoint, _submap.SubmapPosition.DownRightPoint),
                _submap.LodFactor);
        }

        public HeightmapMarginWithInfo GetTopMargin()
        {
            return new HeightmapMarginWithInfo(_submap.Heightmap.GetTopMargin(),
                new MarginPosition(_submap.SubmapPosition.TopLeftPoint, _submap.SubmapPosition.TopRightPoint),
                _submap.LodFactor);
        }

        public HeightmapMarginWithInfo GetLeftMargin()
        {
            return new HeightmapMarginWithInfo(_submap.Heightmap.GetLeftMargin(),
                new MarginPosition(_submap.SubmapPosition.DownLeftPoint, _submap.SubmapPosition.TopLeftPoint),
                _submap.LodFactor);
        }

        public HeightmapMarginWithInfo GetRightMargin()
        {
            return new HeightmapMarginWithInfo(_submap.Heightmap.GetRightMargin(),
                new MarginPosition(_submap.SubmapPosition.DownRightPoint, _submap.SubmapPosition.TopRightPoint),
                _submap.LodFactor);
        }

        public void SetRightMargin(HeightmapMarginWithInfo margin)
        {
            Preconditions.Assert(margin.Position.IsVertical,
                string.Format("Right margin {0} cant be set as is not vertical", margin));
            _submap.Heightmap.SetRightMargin(margin.HeightmapMargin);
        }

        public void SetLeftMargin(HeightmapMarginWithInfo margin)
        {
            Preconditions.Assert(margin.Position.IsVertical,
                string.Format("Left margin {0} cant be set as is not vertical", margin));
            _submap.Heightmap.SetLeftMargin(margin.HeightmapMargin);
        }

        public void SetTopMargin(HeightmapMarginWithInfo margin)
        {
            Preconditions.Assert(margin.Position.IsHorizontal,
                string.Format("Top margin {0} cant be set as is not horizontal", margin));
            _submap.Heightmap.SetTopMargin(margin.HeightmapMargin);
        }

        public void SetBottomMargin(HeightmapMarginWithInfo margin)
        {
            Preconditions.Assert(margin.Position.IsHorizontal,
                string.Format("Bottom margin {0} cant be set as is not horizontal", margin));
            _submap.Heightmap.SetBottomMargin(margin.HeightmapMargin);
        }

        public float GetApexHeight(Point2D apexPoint)
        {
            if (Equals(apexPoint, _submap.SubmapPosition.DownLeftPoint))
            {
                return _submap.Heightmap.GetHeight(0, 0);
            }
            else if (Equals(apexPoint, _submap.SubmapPosition.DownRightPoint))
            {
                return _submap.Heightmap.GetHeight(_submap.Heightmap.WorkingWidth, 0);
            }
            else if (Equals(apexPoint, _submap.SubmapPosition.TopLeftPoint))
            {
                return _submap.Heightmap.GetHeight(0, _submap.Heightmap.WorkingHeight);
            }
            else if (Equals(apexPoint, _submap.SubmapPosition.TopRightPoint))
            {
                return _submap.Heightmap.GetHeight(_submap.Heightmap.WorkingWidth, _submap.Heightmap.WorkingHeight);
            }
            else
            {
                Preconditions.Fail(string.Format("Point {0} is not apex point", apexPoint));
                return -22; //not used
            }
        }

        public void SetApexHeight(Point2D apexPoint, float value, int lod)
        {
            Preconditions.Assert(lod >= _submap.LodFactor, "Cant set apex height. Lod factor is too small");
            var pixelSize = (int) Math.Pow(2, lod - _submap.LodFactor);
            if (Equals(apexPoint, _submap.SubmapPosition.DownLeftPoint))
            {
                _submap.Heightmap.SetDownLeftApexMarginHeight(value, pixelSize);
            }
            else if (Equals(apexPoint, _submap.SubmapPosition.DownRightPoint))
            {
                _submap.Heightmap.SetDownRightApexMarginHeight(value, pixelSize);
            }
            else if (Equals(apexPoint, _submap.SubmapPosition.TopLeftPoint))
            {
                _submap.Heightmap.SetTopLeftApexMarginHeight(value, pixelSize);
            }
            else if (Equals(apexPoint, _submap.SubmapPosition.TopRightPoint))
            {
                _submap.Heightmap.SetTopRightApexMarginHeight(value, pixelSize);
            }
            else
            {
                Preconditions.Fail(string.Format("Point {0} is not apex point", apexPoint));
            }
        }

        public float GetHeight(Point2D apexPoint)
        {
            Preconditions.Assert(_submap.SubmapPosition.IsPointPartOfSubmap(apexPoint),
                String.Format("Point {0} is not part of submap ", apexPoint));
            int globalOffsetX = apexPoint.X - _submap.SubmapPosition.DownLeftX;
            int globalOffsetY = apexPoint.Y - _submap.SubmapPosition.DownLeftY;
            int offsetX = (int) (_submap.Heightmap.WorkingWidth *
                                 Mathf.InverseLerp(0, _submap.SubmapPosition.Width, globalOffsetX));
            int offsetY = (int) (_submap.Heightmap.WorkingHeight *
                                 Mathf.InverseLerp(0, _submap.SubmapPosition.Height, globalOffsetY));
            return _submap.Heightmap.GetHeight(offsetX, offsetY);
        }
    }
}