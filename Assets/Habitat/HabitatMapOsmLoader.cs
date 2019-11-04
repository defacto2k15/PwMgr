using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Roads.Osm;
using Assets.TerrainMat;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Valid;
using OsmSharp.Collections.Tags;
using OsmSharp.Osm;
using OsmSharp.Osm.Xml.Streams;
using UnityEngine;

namespace Assets.Habitat
{
    public class HabitatMapOsmLoader
    {
        public List<HabitatField> Load(string pathToOsmFile)
        {
            var nodesDict = new Dictionary<long, MyWorkNode>();
            var waysDict = new Dictionary<long, MyWorkWay>();
            var relations = new List<MyWorkRelation>();

            using (var fileStream = new FileInfo(pathToOsmFile).OpenRead())
            {
                var source = new XmlOsmStreamSource(fileStream);

                foreach (var node in source)
                {
                    if (node.Type == OsmGeoType.Node)
                    {
                        if (node.Id.HasValue)
                        {
                            var internalNode = (Node) node;
                            //if (nodesFilter.Contains(internalNode.Coordinate))
                            //{
                            nodesDict[node.Id.Value] =
                                new MyWorkNode(node.Id.Value, internalNode.Coordinate.ToVector2());
                            //}
                        }
                    }
                    else if (node.Type == OsmGeoType.Way)
                    {
                        var way = ((Way) node);
                        if (way.Nodes != null)
                        {
                            waysDict[way.Id.Value] = (new MyWorkWay()
                            {
                                Tags = way.Tags,
                                NodeIds = way.Nodes
                            });
                        }
                    }
                    else if (node.Type == OsmGeoType.Relation)
                    {
                        var relation = (Relation) node;
                        if (RetriveTypeFromTags(relation.Tags).HasValue)
                        {
                            relations.Add(new MyWorkRelation()
                            {
                                InnerWayIds = relation.Members
                                    .Where(c => c.MemberType.Value == OsmGeoType.Way)
                                    .Where(c => c.MemberRole.Equals("inner"))
                                    .Select(c => c.MemberId.Value).ToList(),
                                OuterWayIds = relation.Members
                                    .Where(c => c.MemberType.Value == OsmGeoType.Way)
                                    .Where(c => c.MemberRole.Equals("outer"))
                                    .Select(c => c.MemberId.Value).ToList(),
                                Tags = relation.Tags
                            });
                        }
                    }
                }
            }

            //remove not present ways in relation
            foreach (var relation in relations)
            {
                relation.InnerWayIds = relation.InnerWayIds.Where(c => waysDict.ContainsKey(c)).ToList();
                relation.OuterWayIds = relation.OuterWayIds.Where(c => waysDict.ContainsKey(c)).ToList();
            }


            var habitatFields = new List<HabitatField>();
            var wayIdsToRemove = new List<long>();
            foreach (var aRelation in relations)
            {
                var outerInnerDict = new Dictionary<RingDetail, List<RingDetail>>();

                var outerRings = aRelation.OuterWayIds.Where(i => waysDict.ContainsKey(i))
                    .Select(i => new
                    {
                        way = waysDict[i],
                        id = i
                    }).ToList().Select(i => new RingDetail()
                    {
                        Ring = ToLinearRing(i.way.NodeIds.Select(k => nodesDict[k].Position).ToList()),
                        Type = RetriveTypeFromTags(i.way.Tags),
                        Id = i.id
                    }).Where(c => c.Ring != null).ToList();

                var innerHoles = aRelation.InnerWayIds.Select(
                    w => new RingDetail()
                    {
                        Ring = ToLinearRing(waysDict[w].NodeIds.Select(c => nodesDict[c].Position).ToList()),
                        Type = RetriveTypeFromTags(waysDict[w].Tags),
                        Id = w
                    }).Where(c => c.Ring != null).ToList();


                if (outerRings.Count == 1)
                {
                    outerInnerDict[outerRings.First()] = innerHoles;
                }
                else
                {
                    var outerRingPolygons = outerRings.Select(c => new RingDetailWithPolygon()
                    {
                        Polygon = new Polygon(c.Ring),
                        RingDetail = c
                    });

                    var innerRingPolygons = innerHoles.Select(c => new RingDetailWithPolygon()
                    {
                        Polygon = new Polygon(c.Ring),
                        RingDetail = c
                    }).ToList();

                    foreach (var aOuterRing in outerRingPolygons)
                    {
                        var ringsInOuter = innerRingPolygons.Select((c, i) =>
                            new
                            {
                                isCovered = aOuterRing.Polygon.Covers(c.Polygon),
                                index = i,
                                detail = c
                            }).ToList();

                        outerInnerDict[aOuterRing.RingDetail] = ringsInOuter
                            .Where(c => c.isCovered)
                            .Select(c => c.detail.RingDetail).ToList();

                        foreach (var index in ringsInOuter.Select(c => c.index).Reverse())
                        {
                            innerRingPolygons.RemoveAt(index);
                        }
                    }
                    Preconditions.Assert(!innerRingPolygons.Any(), "There is still unused inner polygons");
                }

                foreach (var pair in outerInnerDict)
                {
                    var polygon = new Polygon(pair.Key.Ring, pair.Value.Select(c => c.Ring).ToArray());
                    habitatFields.Add(new HabitatField()
                    {
                        Type = RetriveTypeFromTags(aRelation.Tags).Value,
                        Geometry = polygon
                    });

                    foreach (var hole in pair.Value)
                    {
                        var type = hole.Type;
                        if (!type.HasValue)
                        {
                            type = HabitatType.NotSpecified;
                        }

                        habitatFields.Add(new HabitatField()
                        {
                            Type = type.Value,
                            Geometry = new Polygon(hole.Ring)
                        });
                        wayIdsToRemove.Add(hole.Id);
                    }
                }
            }
            wayIdsToRemove.ForEach(i => waysDict.Remove(i));

            foreach (var aWay in waysDict.Values)
            {
                var type = RetriveTypeFromTags(aWay.Tags);
                if (type.HasValue)
                {
                    var ring = ToLinearRing(aWay.NodeIds.Select(i => nodesDict[i].Position).ToList());
                    if (ring == null)
                    {
                        continue;
                    }
                    habitatFields.Add(new HabitatField()
                    {
                        Type = type.Value,
                        Geometry = new Polygon(ring)
                    });
                }
            }

            return habitatFields;
        }


        private class RingDetail
        {
            public ILinearRing Ring;
            public HabitatType? Type;
            public long Id;
        }

        private class RingDetailWithPolygon
        {
            public RingDetail RingDetail;
            public Polygon Polygon;
        }
//19.64311408996582 49.603153228759766
//        1914206496
//        180998242

//19.71388099829122, 49.648193645648
// 19.628429388931949, 49.617853376339596
        private ILinearRing ToLinearRing(List<Vector2> nodes)
        {
            var nodesAsCoordinates = nodes.Select(c => MyNetTopologySuiteUtils.ToCoordinate(c)).ToList();
            if (!nodesAsCoordinates.First().Equals(nodesAsCoordinates.Last()))
            {
                var repairedRing = nodesAsCoordinates.ToList();
                repairedRing.Add(nodesAsCoordinates.First());
                var ring = new LinearRing(repairedRing.ToArray());
                if (!ring.IsValid)
                {
                    //simple connection failed. Propably should do crectangle merging
                    var ringRect = ring.EnvelopeInternal;
                    Debug.Log("Pushing your luck. Lets hope that it is his only, 2415555 relation!");
                    //todo make better invalid circular way  repairing algorithm. Like find which two sides
                    // of enveloping rectangle would be shorted in repairing road 
                    var newRingPoint = ringRect.ToMyRectangle().TopRightPoint;
                    repairedRing[repairedRing.Count - 1] = new Coordinate(newRingPoint.x, newRingPoint.y);
                    repairedRing.Add(nodesAsCoordinates.First());

                    ring = new LinearRing(repairedRing.ToArray());
                    if (!ring.IsValid)
                    {
                        var validOp = new IsValidOp(ring);
                        Debug.Log("Validation error is: " + validOp.ValidationError);
                        Debug.Log("R1 Ring is " + ring.ToString());
                        return null;

                        //var text = ring.ToText();
                        //File.WriteAllText(@"C:\inz\wrongRing.wkt", text);
                    }
                    Preconditions.Assert(ring.IsValid, "Ring still invalid. What to do?");
                }
                return ring;
            }
            else
            {
                return new LinearRing(nodesAsCoordinates.ToArray());
            }
        }


        private bool ContainsTag(TagsCollectionBase tags, string key, string value)
        {
            return tags != null && tags.ContainsKey(key) && tags[key].Equals(value);
        }

        private HabitatType? RetriveTypeFromTags(TagsCollectionBase tags)
        {
            if (ContainsTag(tags, "landuse", "forest"))
            {
                return HabitatType.Forest;
            }
            else if (ContainsTag(tags, "landuse", "meadow"))
            {
                return HabitatType.Meadow;
            }
            else if (ContainsTag(tags, "natural", "scrub"))
            {
                return HabitatType.Scrub;
            }
            else if (ContainsTag(tags, "natural", "grassland"))
            {
                return HabitatType.Grassland;
            }
            else if (ContainsTag(tags, "natural", "fell"))
            {
                return HabitatType.Fell;
            }
            else
            {
                return null;
            }
        }
    }
}