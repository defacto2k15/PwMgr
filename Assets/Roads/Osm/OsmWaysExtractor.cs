using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.TerrainMat;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using OsmSharp.Math.Geo;
using OsmSharp.Osm;
using OsmSharp.Osm.Xml.Streams;
using UnityEngine;

namespace Assets.Roads.Osm
{
    public class OsmWaysExtractor
    {
        private class WorkingPathPlanState
        {
            public List<MyWorkWay> Ways;
            public NodesSet HighwayNodesSet;
            public Dictionary<MyWorkNode, List<MyWorkWay>> NodeToWayDict;
        }

        private WorkingPathPlanState ExtractInitialPathPlan(string pathToOsmFile, GeoCoordsRect nodesFilter)
        {
            var nodesDict = new Dictionary<long, MyWorkNode>();
            //var ways = new List<MyWay>();
            var workWays = new List<MyWorkWay>();

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
                            if (nodesFilter.Contains(internalNode.Coordinate))
                            {
                                nodesDict[node.Id.Value] =
                                    new MyWorkNode(node.Id.Value, ToVector2(internalNode.Coordinate));
                                //Debug.Log($"T42: {internalNode.Coordinate.Latitude} {internalNode.Coordinate.Longitude}");
                            }
                        }
                    }
                    else if (node.Type == OsmGeoType.Way)
                    {
                        if (node.Tags != null)
                        {
                            if (node.Tags.ContainsKey("highway") && node.Tags["highway"].Equals("path"))
                            {
                                workWays.Add(new MyWorkWay()
                                {
                                    NodeIds = ((Way) node).Nodes
                                });
                            }
                        }
                    }
                }
            }

            //splitting ways that's nodes were filtered out
            var splitWorkWays = new List<MyWorkWay>();
            foreach (var workWay in workWays)
            {
                var nodesInThisWay = new List<long>();
                foreach (var node in workWay.NodeIds)
                {
                    if (nodesDict.ContainsKey(node))
                    {
                        nodesInThisWay.Add(node);
                    }
                    else
                    {
                        if (nodesInThisWay.Count > 1)
                        {
                            splitWorkWays.Add(new MyWorkWay()
                            {
                                NodeIds = nodesInThisWay.ToList()
                            });
                            nodesInThisWay.Clear();
                        }
                    }
                }

                if (nodesInThisWay.Count > 1)
                {
                    splitWorkWays.Add(new MyWorkWay()
                    {
                        NodeIds = nodesInThisWay.ToList()
                    });
                }
            }

            var highwayNodesSet = new NodesSet();
            var nodeToWayDict = new Dictionary<MyWorkNode, List<MyWorkWay>>();

            foreach (var highway in splitWorkWays)
            {
                var highwayNodes = highway.NodeIds.Select(c => nodesDict[c]).ToList();
                foreach (var aHighwayNode in highwayNodes)
                {
                    highwayNodesSet.Add(aHighwayNode);
                    if (!nodeToWayDict.ContainsKey(aHighwayNode))
                    {
                        nodeToWayDict[aHighwayNode] = new List<MyWorkWay>();
                    }
                    nodeToWayDict[aHighwayNode].Add(highway);
                }
            }
            return new WorkingPathPlanState()
            {
                HighwayNodesSet = highwayNodesSet,
                NodeToWayDict = nodeToWayDict,
                Ways = splitWorkWays
            };
        }


        public List<MyWay> ExtractWays(string pathToOsmFile, GeoCoordsRect nodesFilter)
        {
            var planState = ExtractInitialPathPlan(pathToOsmFile, nodesFilter);

            var nodesTree = CreateNodesTree(planState);

            RemoveAliases(planState, nodesTree);

            var newWays = SplitWays(planState);

            FindWayTerminals(newWays);

            return newWays;
        }

        private static void FindWayTerminals(List<MyWay> newWays)
        {
            var nodeToNewWay = new Dictionary<MyWorkNode, List<MyWay>>();
            foreach (var way in newWays)
            {
                foreach (var node in way.Nodes)
                {
                    if (!nodeToNewWay.ContainsKey(node))
                    {
                        nodeToNewWay.Add(node, new List<MyWay>());
                    }
                    nodeToNewWay[node].Add(way);
                }
            }

            foreach (var way in newWays)
            {
                way.StartWays = nodeToNewWay[way.Nodes.First()].Where(w => w != way).ToList();
                way.EndWays = nodeToNewWay[way.Nodes.Last()].Where(w => w != way).ToList();
            }
        }

        private static List<MyWay> SplitWays(WorkingPathPlanState planState)
        {
            var newWays = new List<MyWay>();
            foreach (var way in planState.Ways)
            {
                List<MyWorkNode> nodesBuffer = new List<MyWorkNode>();
                int nodeIdx = 0;
                int nodesCount = way.NodeIds.Count;
                foreach (var node in way.NodeIds.Select(c => planState.HighwayNodesSet.OfIndex(c)))
                {
                    nodesBuffer.Add(node);
                    if (nodeIdx == nodesCount - 1) //last
                    {
                        var newWay = new MyWay()
                        {
                            Nodes = nodesBuffer.ToList()
                        };
                        newWays.Add(newWay);
                    }
                    else if (nodeIdx != 0) //not first
                    {
                        var waysOfNode = planState.NodeToWayDict[node];
                        if (waysOfNode.Count > 1)
                        {
                            var newWay = new MyWay()
                            {
                                Nodes = nodesBuffer.ToList()
                            };
                            newWays.Add(newWay);
                            nodesBuffer = new List<MyWorkNode>()
                            {
                                node
                            };
                        }
                    }
                    nodeIdx++;
                }
            }
            return newWays;
        }

        private void RemoveAliases(WorkingPathPlanState planState, Quadtree<MyWorkNode> nodesTree)
        {
            var aliasingGroups = new List<List<MyWorkNode>>();
            foreach (var node in planState.HighwayNodesSet.Nodes)
            {
                var aliasingBoxLength = 0.001f;
                var positionOfNode = PositionOfNode(node);
                var bottomLeftBoxPoint = positionOfNode - new Vector2(aliasingBoxLength / 2, aliasingBoxLength / 2);

                var aliasingEnvelope =
                    MyNetTopologySuiteUtils.ToEnvelope(new MyRectangle(bottomLeftBoxPoint.x,
                        bottomLeftBoxPoint.y, aliasingBoxLength, aliasingBoxLength));

                var aliasedNodes = nodesTree.Query(aliasingEnvelope)
                    .Where(c => c.Id != node.Id)
                    .Where(c => NodeEnvelope(c).Intersects(aliasingEnvelope)).ToList();

                if (aliasedNodes.All(c => c.Id < node.Id))
                {
                    if (aliasedNodes.Any())
                    {
                        aliasingGroups.Add(aliasedNodes.Union(new List<MyWorkNode>() {node}).ToList());
                    }
                }
            }

            var lastAliasingId = 0;
            foreach (var alaisingGroup in aliasingGroups
                .Select(c =>
                    c.Where(k => planState.NodeToWayDict
                            .ContainsKey(k))
                        .ToList()))
            {
                var avgPosition = new Vector2(
                    alaisingGroup.Average(c => c.Position.x),
                    alaisingGroup.Average(c => c.Position.y)
                );
                var newNode = new MyWorkNode(lastAliasingId++, avgPosition);

                foreach (var aliasedNode in alaisingGroup)
                {
                    var itsWays = planState.NodeToWayDict[aliasedNode];
                    foreach (var way in itsWays)
                    {
                        var idx = way.NodeIds.IndexOf(aliasedNode.Id);
                        way.NodeIds[idx] = newNode.Id;
                    }
                }

                var unionWays = alaisingGroup.SelectMany(c => planState.NodeToWayDict[c]).Distinct().ToList();
                planState.NodeToWayDict[newNode] = unionWays;
                foreach (var aliasedNode in alaisingGroup)
                {
                    planState.NodeToWayDict.Remove(aliasedNode);
                    planState.HighwayNodesSet.Remove(aliasedNode);
                }
                planState.HighwayNodesSet.Add(newNode);
            }

            //CreateDebugAliasingObjects(aliasingGroups);
        }

        private Quadtree<MyWorkNode> CreateNodesTree(WorkingPathPlanState planState)
        {
            var nodesTree = new Quadtree<MyWorkNode>();
            foreach (var node in planState.HighwayNodesSet.Nodes)
            {
                var envelope = MyNetTopologySuiteUtils.ToPointEnvelope(PositionOfNode(node), 0.000001f);
                nodesTree.Insert(envelope, node);
            }
            return nodesTree;
        }

        private static void CreateDebugAliasingObjects(List<List<MyWorkNode>> aliasingGroups)
        {
            foreach (var aliasingGroup in aliasingGroups)
            {
                var parent = new GameObject("ALIASING: " + aliasingGroup.First());
                foreach (var aliasedNode in aliasingGroup)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = RoadDebugObject.ThreeDPositionOfNode(aliasedNode);
                    cube.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    cube.transform.SetParent(parent.transform);
                }
            }
        }

        private Vector2 ToVector2(GeoCoordinate coordinate)
        {
            return new Vector2((float) coordinate.Longitude, (float) coordinate.Latitude);
        }

        private Envelope NodeEnvelope(MyWorkNode workNode)
        {
            return MyNetTopologySuiteUtils.ToPointEnvelope(PositionOfNode(workNode));
        }

        private Vector2 PositionOfNode(MyWorkNode workNode)
        {
            return workNode.Position * 100;
        }
    }
}