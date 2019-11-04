using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.SpotUpdating
{
    public class RTAlignedTreeDebugObject : MonoBehaviour
    {
        private float _wholeAreaLength = 64f;

        public void Start()
        {
            var tree = new RTAlignedTree<DebItem, DebTerrain>();
            var terrainsList = new List<DebTerrain>()
            {
                new DebTerrain(0),
                new DebTerrain(1),
                new DebTerrain(2),
                new DebTerrain(3),
                new DebTerrain(4),
                new DebTerrain(5),
                new DebTerrain(6),
                new DebTerrain(7),
                new DebTerrain(8),
                new DebTerrain(9),
            };
            var itemsList = new List<DebItem>()
            {
                new DebItem(0),
                new DebItem(1),
                new DebItem(2),
                new DebItem(3),
                new DebItem(4),
                new DebItem(5),
                new DebItem(6),
                new DebItem(7),
                new DebItem(8),
            };

            tree.InsertTerrainRetriveItems(new RTAlignedTerrain<DebTerrain>()
            {
                Area = new RTAlignedArea(3, new IntVector2(0, 0), _wholeAreaLength),
                Terrain = terrainsList[0]
            });
            tree.InsertTerrainRetriveItems(new RTAlignedTerrain<DebTerrain>()
            {
                Area = new RTAlignedArea(3, new IntVector2(1, 0), _wholeAreaLength),
                Terrain = terrainsList[1]
            });
            tree.InsertTerrainRetriveItems(new RTAlignedTerrain<DebTerrain>()
            {
                Area = new RTAlignedArea(3, new IntVector2(2, 0), _wholeAreaLength),
                Terrain = terrainsList[2]
            });

            DebEq(tree, new Vector2(0.5f, 0.5f), itemsList[0], 0);
            DebEq(tree, new Vector2(4.5f, 4.5f), itemsList[1], 0);
            DebEq(tree, new Vector2(8+4.5f, 4.5f), itemsList[2], 1);
            DebEq(tree, new Vector2(8*2+4.5f, 4.5f), itemsList[3], 2);
            DebEq(tree, new Vector2(8*3+4.5f, 4.5f), itemsList[4], null);


            DebEq(tree, new Vector2(8*0+0.5f, 2*8+0.5f), itemsList[5], null);
            DebEq(tree, new Vector2(8*0+4.5f, 2*8+0.5f), itemsList[6], null);
            DebEq(tree, new Vector2(8*0+4.5f, 2*8+4.5f), itemsList[7], null);

            DebTerrEq(tree, 3, new IntVector2(0, 2), terrainsList[3], new List<DebItem>()
            {
                itemsList[5], itemsList[6], itemsList[7]
            });

            DebTerrEq(tree, 4, new IntVector2(1, 4), terrainsList[4], new List<DebItem>()
            {
                itemsList[6],
            });

            DebTerrEq(tree, 5, new IntVector2(0, 0), terrainsList[5], new List<DebItem>()
            {
                itemsList[0],
            });

            DebTerrEq(tree, 5, new IntVector2(10, 10), terrainsList[6], new List<DebItem>()
            {
            });
        }

        private void DebTerrEq(RTAlignedTree<DebItem, DebTerrain> tree, int lod, IntVector2 pos, DebTerrain terrains, List<DebItem> expectedDebItems)
        {
            var insertRes = tree.InsertTerrainRetriveItems(new RTAlignedTerrain<DebTerrain>()
            {
                Area = new RTAlignedArea(lod, pos, _wholeAreaLength),
                Terrain = terrains
            }).ToList();

            Preconditions.Assert(insertRes.Count == expectedDebItems.Count, $"I expected {expectedDebItems.Count} elements, got {insertRes.Count}");
            foreach (var item in expectedDebItems)
            {
                Preconditions.Assert(insertRes.Contains(item), $"E32 There is no {item.Id} that i expected");
            }
        }

        private void DebEq(RTAlignedTree<DebItem, DebTerrain> tree, Vector2 itemPos, DebItem items, int? expectedTerrainId)
        {
            var t = tree.InsertItemRetriveTerrain(new RTAlignedItem<DebItem>()
            {
                Position = new RTAlignedPosition(itemPos, _wholeAreaLength),
                Item = items
            });
            if (t == null && expectedTerrainId.HasValue)
            {
                Preconditions.Fail($"E1: inserting item {itemPos} returned null terrain, but expected {expectedTerrainId}");
            }else if (t != null && !expectedTerrainId.HasValue)
            {
                Preconditions.Fail($"E1: inserting item {itemPos} returned terrain {t.Id}, but expected null");
            }
            else
            {
                if (t == null)
                {
                    Preconditions.Assert((t == null && !expectedTerrainId.HasValue),
                        $"E3 Inserting item {itemPos} returnedTerrain null, expected {expectedTerrainId}");
                }
                else
                {
                    Preconditions.Assert( t.Id == expectedTerrainId.Value,
                        $"E3 Inserting item {itemPos} returnedTerrain {t.Id}, expected {expectedTerrainId}");
                }
            }
        }

        private class DebItem
        {
            private int _id;

            public DebItem(int id)
            {
                _id = id;
            }

            public int Id => _id;
        }

        private class DebTerrain
        {
            private int _id;

            public DebTerrain(int id)
            {
                _id = id;
            }

            public int Id => _id;
        }
    }

}
