using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.submaps;
using Assets.Utils;

namespace Assets.Heightmaps.Preparment.MarginMerging
{
    class MarginMerger
    {
        public void MergeMargins(List<Submap> input)
        {
            var submapsWithMarginData = input.Select(c => new MarginDataOfSubmap(c)).ToList();
            SetSubmapMargins(submapsWithMarginData);
            SetSubmapApexPoint(submapsWithMarginData);
        }


        private void SetSubmapMargins(List<MarginDataOfSubmap> submaps)
        {
            foreach (var aSubmap in submaps)
            {
                if (aSubmap.SubmapPosition.DownLeftX != 0)
                {
                    var leftNeighbours =
                        getLeftNeighbours(aSubmap.SubmapPosition, submaps);
                    var ourLeftMargin = aSubmap.GetLeftMargin();
                    foreach (var aLeftNeighbour in leftNeighbours)
                    {
                        var marginAfterLod = ourLeftMargin.SetLod(aLeftNeighbour.LodFactor);
                        aLeftNeighbour.SetRightMargin(aLeftNeighbour.GetRightMargin()
                            .UpdateWherePossible(marginAfterLod));
                        if (aSubmap.LodFactor >= aLeftNeighbour.LodFactor) // we have bigger lod
                        {
                            // do nthin
                        }
                        else // neighbour has bigger lod
                        {
                            var unLoddedMargin = marginAfterLod.SetLod(aSubmap.LodFactor);
                            aSubmap.SetLeftMargin(unLoddedMargin); // we have to make our margin with less resolution
                        }
                    }
                }

                if (aSubmap.SubmapPosition.DownLeftY != 0)
                {
                    var downNeighbours =
                        getBottomNeighbours(aSubmap.SubmapPosition, submaps);
                    var ourDownMargin = aSubmap.GetDownMargin();
                    foreach (var neighbourPair in downNeighbours)
                    {
                        var marginAfterLod = ourDownMargin.SetLod(neighbourPair.LodFactor);
                        neighbourPair.SetTopMargin(neighbourPair.GetTopMargin().UpdateWherePossible(marginAfterLod));
                        if (aSubmap.LodFactor >= neighbourPair.LodFactor) // we have bigger lod
                        {
                            // do nthin
                        }
                        else // neighbour has bigger lod
                        {
                            var unLoddedMargin =
                                marginAfterLod.SetLod(aSubmap
                                    .LodFactor); //todo lodding destroys correction in touch points with arleady seamed margins
                            aSubmap.SetBottomMargin(unLoddedMargin); // we have to make our margin with less resolution
                        }
                    }
                }
            }
        }

        private IEnumerable<MarginDataOfSubmap> getBottomNeighbours(SubmapPosition position,
            List<MarginDataOfSubmap> heightmaps)
        {
            return (from heightmap in heightmaps
                where heightmap.SubmapPosition.DownLeftY + heightmap.SubmapPosition.Height == position.DownLeftY &&
                      MathHelp.SegmentsHaveCommonElement(heightmap.SubmapPosition.TopLeftPoint.X,
                          heightmap.SubmapPosition.TopRightPoint.X, position.DownLeftPoint.X, position.DownRightPoint.X)
                select heightmap).ToList();
        }

        private IEnumerable<MarginDataOfSubmap> getLeftNeighbours(SubmapPosition position,
            List<MarginDataOfSubmap> heightmaps)
        {
            return (from heightmap in heightmaps
                where heightmap.SubmapPosition.DownLeftX + heightmap.SubmapPosition.Width == position.DownLeftX &&
                      MathHelp.SegmentsHaveCommonElement(heightmap.SubmapPosition.DownRightPoint.Y,
                          heightmap.SubmapPosition.TopRightPoint.Y, position.DownLeftPoint.Y, position.TopLeftPoint.Y)
                select heightmap).ToList();
        }


        // Apex things

        private void SetSubmapApexPoint(List<MarginDataOfSubmap> heightmaps)
        {
            foreach (var aHeightmapPair in heightmaps)
            {
                if (aHeightmapPair.SubmapPosition.DownLeftX != 0)
                {
                    ApexPointData downLeftSubmapApexPoint =
                        getApexPointAt(aHeightmapPair.SubmapPosition.DownLeftPoint, heightmaps);
                    downLeftSubmapApexPoint.IntegrateApexPoint();
                }

                if (aHeightmapPair.SubmapPosition.DownLeftY != 0)
                {
                    var downLeftSubmapApexPoint =
                        getApexPointAt(aHeightmapPair.SubmapPosition.DownRightPoint, heightmaps);
                    downLeftSubmapApexPoint.IntegrateApexPoint();
                }
            }
        }

        private ApexPointData getApexPointAt(Point2D apexPoint, List<MarginDataOfSubmap> heightmaps)
        {
            var downLeftSubmap = from heightmap in heightmaps
                where heightmap.SubmapPosition.DownRightPoint.X == apexPoint.X
                      && (heightmap.SubmapPosition.DownRightPoint.Y) < apexPoint.Y &&
                      (heightmap.SubmapPosition.TopRightPoint.Y) >= apexPoint.Y
                select heightmap;
            var topLeftSubmap = from heightmap in heightmaps
                where heightmap.SubmapPosition.DownRightPoint.X == apexPoint.X
                      && (heightmap.SubmapPosition.DownRightPoint.Y) <= apexPoint.Y &&
                      (heightmap.SubmapPosition.TopRightPoint.Y) > apexPoint.Y
                select heightmap;

            var downRightSubmap = from heightmap in heightmaps
                where heightmap.SubmapPosition.DownLeftPoint.X == apexPoint.X
                      && (heightmap.SubmapPosition.DownLeftPoint.Y) < apexPoint.Y &&
                      (heightmap.SubmapPosition.TopLeftPoint.Y) >= apexPoint.Y
                select heightmap;
            var topRightSubmap = from heightmap in heightmaps
                where heightmap.SubmapPosition.DownLeftPoint.X == apexPoint.X
                      && (heightmap.SubmapPosition.DownLeftPoint.Y) <= apexPoint.Y &&
                      (heightmap.SubmapPosition.TopLeftPoint.Y) > apexPoint.Y
                select heightmap;
            return new ApexPointData(apexPoint, downLeftSubmap.FirstOrDefault(), downRightSubmap.FirstOrDefault(),
                topLeftSubmap.FirstOrDefault(), topRightSubmap.FirstOrDefault());
        }
    }
}