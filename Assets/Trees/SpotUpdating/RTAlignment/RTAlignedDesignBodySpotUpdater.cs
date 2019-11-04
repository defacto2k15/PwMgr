using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Trees.SpotUpdating.RTAlignment
{
    public class RTAlignedDesignBodySpotUpdater : IDesignBodySpotUpdater
    {
        private RTAlignedTree<ItemInTree, TerrainInTree> _tree;
        private Dictionary<SpotId, RTAlignedItem<ItemInTree>> _spotToItem = new Dictionary<SpotId, RTAlignedItem<ItemInTree>>();
        private Dictionary<SpotUpdaterTerrainTextureId, RTAlignedTerrain<TerrainInTree>> _terrainToItem =
            new Dictionary<SpotUpdaterTerrainTextureId, RTAlignedTerrain<TerrainInTree>>();

        private ISpotPositionChangesListener _changesListener;
        private DesignBodySpotChangeCalculator _spotChangeCalculator;
        private RTAlignedDesignBodySpotUpdaterConfiguration _configuration;

        public RTAlignedDesignBodySpotUpdater(
            DesignBodySpotChangeCalculator spotChangeCalculator,
            RTAlignedDesignBodySpotUpdaterConfiguration configuration,
            ISpotPositionChangesListener changesListener = null)
        {
            _changesListener = changesListener;
            _spotChangeCalculator = spotChangeCalculator;
            _configuration = configuration;
            _tree = new RTAlignedTree<ItemInTree, TerrainInTree>(configuration._topLod);
        }

        public void SetChangesListener(ISpotPositionChangesListener listener)
        {
            _changesListener = listener;
        }

        public async Task RegisterDesignBodiesAsync(List<FlatPositionWithSpotId> bodiesWithIds)
        {
            var newTerrainToPointsDict = new Dictionary<UpdatedTerrainTextures, List<FlatPositionWithSpotId>>();

            foreach (var body in bodiesWithIds)
            {
                var rtAlignedItem = new RTAlignedItem<ItemInTree>()
                {
                    Item = new ItemInTree()
                    {
                        FlatPosition = body
                    },
                    Position = GetAlignedPosition(body.FlatPosition)
                };
                _spotToItem[body.SpotId] = rtAlignedItem;
                var terrainInTree =  _tree.InsertItemRetriveTerrain(rtAlignedItem);

                if (terrainInTree != null)
                {
                    var terrainTexture = terrainInTree.TerrainTexture;
                    if (!newTerrainToPointsDict.ContainsKey(terrainTexture))
                    {
                        newTerrainToPointsDict[terrainTexture] = new List<FlatPositionWithSpotId>();
                    }
                    newTerrainToPointsDict[terrainTexture].Add(body);
                }
            }

            Dictionary<SpotId, SpotData> changedSpots = new Dictionary<SpotId, SpotData>();
            foreach (var pair in newTerrainToPointsDict)
            {
                var flatPositions = pair.Value.Select(c => c.FlatPosition).ToList();
                if (!flatPositions.Any())
                {
                    continue;
                }
                var spotDatas = await _spotChangeCalculator.CalculateChangeAsync(pair.Key, flatPositions);
                for (int i = 0; i < spotDatas.Count; i++)
                {
                    changedSpots[pair.Value[i].SpotId] = spotDatas[i];
                }
            }

            _changesListener.SpotsWereChanged(changedSpots);

        }

        public async Task RegisterDesignBodiesGroupAsync(SpotId id, List<Vector2> bodiesPositions)
        {
            if (!bodiesPositions.Any())
            {
                return;
            }

            var rtAlignedItem = new RTAlignedItem<ItemInTree>()
            {
                Item = new ItemInTree()
                {
                    Group = new DesignBodyGroup()
                    {
                        Id = id,
                        BodiesPositions = bodiesPositions
                    }
                },
                Position = GetAlignedPosition(bodiesPositions[0])
            };
            _spotToItem[id] = rtAlignedItem;
            var terrainInTree = _tree.InsertItemRetriveTerrain(rtAlignedItem);

            if (terrainInTree == null)
            {
                return;
            }

            var spotDatas = await _spotChangeCalculator.CalculateChangeAsync(terrainInTree.TerrainTexture, bodiesPositions);

            _changesListener.SpotGroupsWereChanged(new Dictionary<SpotId, List<SpotData>>()
            {
                {id, spotDatas }
            });
        }

        public async Task UpdateBodiesSpotsAsync(UpdatedTerrainTextures newHeightTexture)
        {
            var rtAlignedTerrain = new RTAlignedTerrain<TerrainInTree>()
            {
                Area = GetAlignedArea(newHeightTexture.UsedGlobalArea()),
                Terrain = new TerrainInTree()
                {
                    TerrainTexture = newHeightTexture
                }
            };
            _terrainToItem[newHeightTexture.Id] = rtAlignedTerrain;

            var movedItems = _tree.InsertTerrainRetriveItems(rtAlignedTerrain).ToList();

            var positions = new List<Vector2>();
            var spotIds = new List<SpotId>();
            foreach (var itemInTree in movedItems.ToList().Where(c => c.FlatPosition != null ))
            {
                positions.Add(itemInTree.FlatPosition.FlatPosition);
                spotIds.Add(itemInTree.FlatPosition.SpotId);
            }
            if (positions.Any())
            {
                var newSpots =await _spotChangeCalculator.CalculateChangeAsync(newHeightTexture, positions);
                Dictionary<SpotId, SpotData> singleSpotsDict = new Dictionary<SpotId, SpotData>();
                for (int i = 0; i < newSpots.Count; i++)
                {
                    singleSpotsDict[spotIds[i]] = newSpots[i];
                }
                _changesListener.SpotsWereChanged(singleSpotsDict);
            }

            Dictionary<SpotId, List<SpotData>> changedSpots = new Dictionary<SpotId, List<SpotData>>();
            // GROUPS!!!
            foreach (var itemInTree in movedItems.ToList().Where(c => c.Group!= null ))
            {
                if (!itemInTree.Group.BodiesPositions.Any())
                {
                    continue;
                }
                spotIds.Add(itemInTree.Group.Id);

                var spotDatas =
                    await _spotChangeCalculator.CalculateChangeAsync(newHeightTexture, itemInTree.Group.BodiesPositions);
                changedSpots[itemInTree.Group.Id] = spotDatas;
            }
            _changesListener.SpotGroupsWereChanged(changedSpots);
        }

        private RTAlignedArea GetAlignedArea(MyRectangle  terrainArea)
        {
            var lod = Mathf.RoundToInt(Mathf.Log(_configuration.WholeAreaLength / terrainArea.Width, 2));
            var pos = new IntVector2(
                    Mathf.FloorToInt( (terrainArea.X / _configuration.WholeAreaLength) * Mathf.Pow(2, lod) ),
                    Mathf.FloorToInt( (terrainArea.Y / _configuration.WholeAreaLength) * Mathf.Pow(2, lod) )
                );
            return new RTAlignedArea(lod,pos,_configuration.WholeAreaLength);
        }

        private RTAlignedPosition GetAlignedPosition(Vector2 flatPos)
        {
            return  new RTAlignedPosition(flatPos, _configuration.WholeAreaLength );
        }

        public void ForgetDesignBodies(List<SpotId> bodiesToRemove)
        {
            foreach (var id in bodiesToRemove)
            {
                if (!_spotToItem.ContainsKey(id))
                {
                    Preconditions.Fail("E831 removing arleady removed item: " + id);
                }
                else
                {
                    _tree.RemoveItem(_spotToItem[id]);
                    _spotToItem.Remove(id);
                }
            }
        }

        public void RemoveTerrainTextures(SpotUpdaterTerrainTextureId id)
        {
            _tree.RemoveTerrain(_terrainToItem[id]);
            _terrainToItem.Remove(id);
        }

        private class TerrainInTree
        {
            public UpdatedTerrainTextures TerrainTexture;
        }

        private class ItemInTree
        {
            public FlatPositionWithSpotId FlatPosition;
            public DesignBodyGroup Group;
        }

        private class DesignBodyGroup
        {
            public SpotId Id;
            public List<Vector2> BodiesPositions;
        }
    }

    public class RTAlignedDesignBodySpotUpdaterConfiguration
    {
        public float WholeAreaLength;
        public int _topLod = 3;
    }
}
