using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.ComputeShaders.Templating;
using Assets.Grass2.Billboards;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Textures;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Heightmaps.Welding
{
    public class HeightArrayWeldingDispatcher
    {
        private WeldMapLevel1Manager _mapLevel1Manager;

        private Dictionary<WeldOrientation, Dictionary<int, WeldLine>> _welds =
            new Dictionary<WeldOrientation, Dictionary<int, WeldLine>>();

        private Dictionary<int, WeldingTerrainEntity> _terrains = new Dictionary<int, WeldingTerrainEntity>();

        public HeightArrayWeldingDispatcher(WeldMapLevel1Manager mapLevel1Manager)
        {
            _mapLevel1Manager = mapLevel1Manager;
            foreach (var orientation in Enum.GetValues(typeof(WeldOrientation)).Cast<WeldOrientation>())
            {
                _welds[orientation] = new Dictionary<int, WeldLine>();
            }
        }

        public Task RegisterTerrain(WeldingInputTerrain inputTerrain)
        {
            _terrains[inputTerrain.WeldingInputTerrainId] = new WeldingTerrainEntity()
            {
                Uvs = new TerrainWeldUvs(),
                WeldModificationCallback = inputTerrain.WeldModificationCallback
            };

            var globalSubPosition = RectangleUtils.CalculateSubPosition(inputTerrain.DetailGlobalArea,
                inputTerrain.UvCoordsPositions2D);

            var weldPositions = new Dictionary<WeldSideType, WeldPosition>();
            weldPositions[WeldSideType.Bottom] = new WeldPosition()
            {
                ConstantAxisPosition = Mathf.RoundToInt(globalSubPosition.Y),
                Orientation = WeldOrientation.Horizontal,
                Range = new IntVector2(Mathf.RoundToInt(globalSubPosition.X), Mathf.RoundToInt(globalSubPosition.MaxX))
            };
            weldPositions[WeldSideType.Top] = new WeldPosition()
            {
                ConstantAxisPosition = Mathf.RoundToInt(globalSubPosition.MaxY),
                Orientation = WeldOrientation.Horizontal,
                Range = new IntVector2(Mathf.RoundToInt(globalSubPosition.X), Mathf.RoundToInt(globalSubPosition.MaxX))
            };
            weldPositions[WeldSideType.Left] = new WeldPosition()
            {
                ConstantAxisPosition = Mathf.RoundToInt(globalSubPosition.X),
                Orientation = WeldOrientation.Vertical,
                Range = new IntVector2(Mathf.RoundToInt(globalSubPosition.Y), Mathf.RoundToInt(globalSubPosition.MaxY))
            };
            weldPositions[WeldSideType.Right] = new WeldPosition()
            {
                ConstantAxisPosition = Mathf.RoundToInt(globalSubPosition.MaxX),
                Orientation = WeldOrientation.Vertical,
                Range = new IntVector2(Mathf.RoundToInt(globalSubPosition.Y), Mathf.RoundToInt(globalSubPosition.MaxY))
            };

            var orders = new List<WeldRegenerationOrder>();
            foreach (var weldPositionPair in weldPositions)
            {
                var weldPosition = weldPositionPair.Value;
                var orientation = weldPositionPair.Key.GetOrientation();
                var weldsDict = _welds[orientation];

                if (!weldsDict.ContainsKey(weldPosition.ConstantAxisPosition))
                {
                    weldsDict[weldPosition.ConstantAxisPosition] = new WeldLine();
                }

                var line = weldsDict[weldPosition.ConstantAxisPosition];
                orders.AddRange(line.RegisterWeld(weldPosition, weldPositionPair.Key, inputTerrain));
            }

            //var weldsUvs = TaskUtils.WhenAll(orders.Select(c => _mapLevel1Manager.Process(c))).Result;
            var weldsUvs = orders.Select(c => _mapLevel1Manager.Process(c)).ToList();

            foreach (var x in weldsUvs.SelectMany(c => c))
            {
                var terrainId = x.Key;
                var weldUv = x.Value;

                var terrain = _terrains[terrainId];
                terrain.Uvs.Merge(weldUv);
                terrain.WeldModificationCallback(terrain.Uvs);
            }

            return TaskUtils.EmptyCompleted();
        }

        public Task RemoveTerrain(int weldingTerrainId)
        {
            if (weldingTerrainId == 35)
            {
                int rr = 1231;
            }
            Preconditions.Assert(_terrains.ContainsKey(weldingTerrainId),
                $"E29 there is not terrain of id " + weldingTerrainId);
            _terrains.Remove(weldingTerrainId);

            foreach (var weld in _welds.SelectMany(c => c.Value.Values.ToList()))
            {
                weld.RemoveTerrain(weldingTerrainId);
            }

            _mapLevel1Manager.RemoveTerrain(weldingTerrainId);
            return TaskUtils.MyFromResultGenetic();
        }
    }

    public class WeldingTerrainEntity
    {
        public TerrainWeldUvs Uvs = new TerrainWeldUvs();
        public Action<TerrainWeldUvs> WeldModificationCallback;
    }

    public class WeldingInputTerrain
    {
        public TextureWithSize Texture;
        public MyRectangle DetailGlobalArea;
        public TerrainCardinalResolution Resolution;
        public int TerrainLod;
        public MyRectangle UvCoordsPositions2D;
        public Action<TerrainWeldUvs> WeldModificationCallback;

        public int WeldingInputTerrainId;

        public float GetSideLength()
        {
            return RectangleUtils.CalculateSubPosition(DetailGlobalArea, UvCoordsPositions2D).Width;
        }

        public override string ToString()
        {
            return
                $"{nameof(DetailGlobalArea)}: {DetailGlobalArea}, {nameof(Resolution)}: {Resolution}, {nameof(TerrainLod)}: {TerrainLod}, {nameof(UvCoordsPositions2D)}: {UvCoordsPositions2D}, {nameof(WeldingInputTerrainId)}: {WeldingInputTerrainId}";
        }
    }

    public class WeldSideSource
    {
        public WeldingInputTerrain Terrain;
        public WeldSideType SideType;

        public override string ToString()
        {
            return $"{nameof(Terrain)}: {Terrain}, {nameof(SideType)}: {SideType}";
        }
    }

    public enum WeldSideType
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public static class WeldSideTypeUtils
    {
        public static WeldOrientation GetOrientation(this WeldSideType type)
        {
            if (type == WeldSideType.Bottom || type == WeldSideType.Top)
            {
                return WeldOrientation.Horizontal;
            }
            else
            {
                return WeldOrientation.Vertical;
            }
        }

        public static bool IsDonorSide(this WeldSideType type)
        {
            if (type == WeldSideType.Left || type == WeldSideType.Bottom)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class Weld
    {
        public IntVector2 GlobalSizeRange;
        public WeldSideSource First;
        public WeldSideSource Second;
        public bool ThereAreBothSides => First != null && Second != null;


        public Weld Clone()
        {
            return new Weld()
            {
                First = First,
                Second = Second,
                GlobalSizeRange = GlobalSizeRange
            };
        }

        public Weld Subelement(int rangeX, int rangeY)
        {
            Preconditions.Assert(GlobalSizeRange.X <= rangeX && GlobalSizeRange.Y >= rangeY,
                $"E41: Ranges misaligned {GlobalSizeRange}, {rangeX}-{rangeY}");
            return new Weld()
            {
                First = First,
                Second = Second,
                GlobalSizeRange = new IntVector2(rangeX, rangeY)
            };
        }

        public void AddSide(WeldSideSource weldSideSource)
        {
            if (weldSideSource.SideType == WeldSideType.Bottom)
            {
                Preconditions.Assert(First == null,
                    $"E12 Bottom of weld is arleady taken. New {weldSideSource}, old {First}");
                First = weldSideSource;
            }
            else if (weldSideSource.SideType == WeldSideType.Left)
            {
                Preconditions.Assert(First == null,
                    $"E12 Left of weld is arleady taken. New {weldSideSource}, old {First}");
                First = weldSideSource;
            }
            else if (weldSideSource.SideType == WeldSideType.Top)
            {
                Preconditions.Assert(Second == null,
                    $"E12 Top of weld is arleady taken. New {weldSideSource}, old {Second}");
                Second = weldSideSource;
            }
            else if (weldSideSource.SideType == WeldSideType.Right)
            {
                Preconditions.Assert(Second == null,
                    $"E12 Right of weld is arleady taken. New {weldSideSource}, old {Second}");
                Second = weldSideSource;
            }
            else
            {
                Preconditions.Fail("Not supported sideType " + weldSideSource.SideType);
            }
        }
    }

    public class WeldPosition
    {
        public WeldOrientation Orientation;
        public int ConstantAxisPosition;
        public IntVector2 Range;
    }

    public enum WeldOrientation
    {
        Horizontal,
        Vertical
    }

    public static class WeldLineConstants
    {
        public static int MIN_RANGE = 0;
        public static int MAX_RANGE = 80640;
    }

    public class WeldLine
    {
        private List<Weld> _welds = new List<Weld>();

        public WeldLine()
        {
            _welds.Add(new Weld()
            {
                GlobalSizeRange = new IntVector2(WeldLineConstants.MIN_RANGE, WeldLineConstants.MAX_RANGE),
            });
        } //todo łącz przy kasowaniu!

        public List<WeldRegenerationOrder> RegisterWeld(WeldPosition weldPosition, WeldSideType sideType,
            WeldingInputTerrain inputTerrain)
        {
            var outList = new List<WeldRegenerationOrder>();
            var newWeldsList = new List<Weld>();
            foreach (var weld in _welds)
            {
                if (RangeIsInWeld(weld, weldPosition.Range))
                {
                    WeldSplicingResult spliced = SpliceWeld(weld, weldPosition.Range);
                    var newWeld = spliced.NewWeld;
                    newWeld.AddSide(new WeldSideSource()
                    {
                        SideType = sideType,
                        Terrain = inputTerrain
                    });

                    outList.Add(new WeldRegenerationOrder()
                    {
                        Weld = newWeld
                    });

                    newWeldsList.AddRange(spliced.RestOfWelds);
                    newWeldsList.Add(spliced.NewWeld);
                }
                else
                {
                    newWeldsList.Add(weld);
                }
            }

            _welds = newWeldsList.OrderBy(c => c.GlobalSizeRange.X).ToList();
            return outList.Where(c => c.Weld.ThereAreBothSides).ToList();
        }

        private bool RangeIsInWeld(Weld weld, IntVector2 range)
        {
            var oldRange = weld.GlobalSizeRange;
            if (oldRange.Equals(range))
            {
                return true;
            }
            else if (oldRange.X >= range.Y || oldRange.Y <= range.X) // not touching
            {
                return false;
            }
            else if (oldRange.X <= range.X && oldRange.Y >= range.Y) // new is inside old
            {
                return true;
            }
            else if (oldRange.X >= range.X && oldRange.Y <= range.Y) // old is inside new 
            {
                return true;
            }
            else if (oldRange.X <= WeldLineConstants.MIN_RANGE || oldRange.Y >= WeldLineConstants.MAX_RANGE)
            {
                return true;
            }
            else
            {
                Preconditions.Fail($"E43 misaligned ranges: old: {oldRange} new {range}");
                return false;
            }
        }

        private WeldSplicingResult SpliceWeld(Weld weld, IntVector2 range)
        {
            var oldRange = weld.GlobalSizeRange;
            if (oldRange.Equals(range))
            {
                return new WeldSplicingResult()
                {
                    NewWeld = weld.Clone(),
                    RestOfWelds = new List<Weld>()
                };
            }
            else if (oldRange.X >= range.Y || oldRange.Y <= range.X) // not touching
            {
                Preconditions.Fail("Welds are not touching!");
                return null;
            }
            else if (oldRange.X <= range.X && oldRange.Y >= range.Y) // new is inside old
            {
                if (oldRange.X == range.X)
                {
                    return new WeldSplicingResult()
                    {
                        NewWeld = weld.Subelement(range.X, range.Y),
                        RestOfWelds = new List<Weld>()
                        {
                            weld.Subelement(range.Y, oldRange.Y)
                        }
                    };
                }
                else if (oldRange.Y == range.Y)
                {
                    return new WeldSplicingResult()
                    {
                        NewWeld = weld.Subelement(range.X, range.Y),
                        RestOfWelds = new List<Weld>()
                        {
                            weld.Subelement(oldRange.X, range.X)
                        }
                    };
                }
                else
                {
                    return new WeldSplicingResult()
                    {
                        NewWeld = weld.Subelement(range.X, range.Y),
                        RestOfWelds = new List<Weld>()
                        {
                            weld.Subelement(oldRange.X, range.X),
                            weld.Subelement(range.Y, oldRange.Y)
                        }
                    };
                }
            }
            else if (oldRange.X >= range.X && oldRange.Y <= range.Y) // old is inside new 
            {
                return new WeldSplicingResult()
                {
                    NewWeld = weld.Clone(),
                    RestOfWelds = new List<Weld>()
                };
            }
            if (oldRange.X < range.X)
            {
                return new WeldSplicingResult()
                {
                    NewWeld = weld.Subelement(range.X, oldRange.Y),
                    RestOfWelds = new List<Weld>()
                    {
                        weld.Subelement(oldRange.X, range.X)
                    }
                };
            }
            else if (oldRange.Y > range.Y)
            {
                return new WeldSplicingResult()
                {
                    NewWeld = weld.Subelement(oldRange.X, range.Y),
                    RestOfWelds = new List<Weld>()
                    {
                        weld.Subelement(range.Y, oldRange.Y)
                    }
                };
            }
            else
            {
                // slight misalighment

                Preconditions.Fail($"E438 misaligned ranges: old: {oldRange} new {range}");
                return null;
            }
        }

        public void RemoveTerrain(int weldingTerrainId)
        {
            foreach (var weld in _welds)
            {
                if (weld.First != null && weld.First.Terrain.WeldingInputTerrainId == weldingTerrainId)
                {
                    weld.First = null;
                }
                if (weld.Second != null && weld.Second.Terrain.WeldingInputTerrainId == weldingTerrainId)
                {
                    weld.Second = null;
                }
            }

            var newList = new List<Weld>();
            var activeWeld = _welds[0];
            for (int i = 0; i < _welds.Count; i++)
            {
                if (i == _welds.Count - 1)
                {
                    newList.Add(activeWeld);
                }
                else
                {
                    var next = _welds[i + 1];
                    if (CanBeMerged(activeWeld, next))
                    {
                        var newWeld = new Weld()
                        {
                            First = activeWeld.First,
                            Second = activeWeld.Second,
                            GlobalSizeRange = new IntVector2(activeWeld.GlobalSizeRange.X, next.GlobalSizeRange.Y)
                        };
                        activeWeld = newWeld;
                    }
                    else
                    {
                        newList.Add(activeWeld);
                        activeWeld = next;
                    }
                }
            }
            _welds = newList;
        }

        private bool CanBeMerged(Weld w1, Weld w2)
        {
            return ((w1.First == null && w2.First == null) || (w1?.First?.Terrain.WeldingInputTerrainId == w2?.First?.Terrain.WeldingInputTerrainId)) &&
                   ((w1.Second== null && w2.Second== null) || ( w1?.Second?.Terrain.WeldingInputTerrainId == w2?.Second?.Terrain.WeldingInputTerrainId));
        }

        private class WeldSplicingResult
        {
            public Weld NewWeld;
            public List<Weld> RestOfWelds;
        }
    }

    public class WeldRegenerationOrder
    {
        public Weld Weld;

        public int GetBiggerTerrainIndex()
        {
            var firstLength = Weld.First.Terrain.GetSideLength();
            var secondLength = Weld.Second.Terrain.GetSideLength();
            if (firstLength > secondLength)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        public WeldSideSource GetTerrainOfIndex(int i)
        {
            if (i == 0)
            {
                return Weld.First;
            }
            else
            {
                return Weld.Second;
            }
        }
    }

    public class WeldMapLevel1Manager
    {
        // maping between terrain-Side and StripPosition

        private WeldMapLevel2Manager _level2Manager;

        // only leaders are here
        private Dictionary<TerrainSide, WeldStripPosition> _terrainSideToStripPositions =
            new Dictionary<TerrainSide, WeldStripPosition>();

        private Dictionary<TerrainSide, WeldStripPosition> _followerStripPositions =
            new Dictionary<TerrainSide, WeldStripPosition>();

        public WeldMapLevel1Manager(WeldMapLevel2Manager level2Manager)
        {
            _level2Manager = level2Manager;
        }

        public Dictionary<int, TerrainWeldUvs> Process(WeldRegenerationOrder order)
        {
            int biggerTerrainIndex = order.GetBiggerTerrainIndex();
            var biggerTerrain = order.GetTerrainOfIndex(biggerTerrainIndex);

            var biggerTerrainId = biggerTerrain.Terrain.WeldingInputTerrainId;
            TerrainSide biggerTerrainSide = new TerrainSide()
            {
                SideType = biggerTerrain.SideType,
                TerrainId = biggerTerrainId
            };

            var outWeldModificationsDict = new Dictionary<int, TerrainWeldUvs>();

            if (!_terrainSideToStripPositions.ContainsKey(biggerTerrainSide))
            {
                var registrationResult =
                    _level2Manager.RegisterStrip(CreateStripSide(biggerTerrain, new Vector2(0, 1)));

                outWeldModificationsDict[biggerTerrainId] = registrationResult.WeldUvs;
                _terrainSideToStripPositions[biggerTerrainSide] = registrationResult.StripPosition;
            }

            var weldPosition = _terrainSideToStripPositions[biggerTerrainSide];

            var smallerTerrain = order.GetTerrainOfIndex(1 - biggerTerrainIndex);
            var smallerSide = new TerrainSide()
            {
                SideType = smallerTerrain.SideType,
                TerrainId = smallerTerrain.Terrain.WeldingInputTerrainId
            };
            _followerStripPositions[smallerSide] = weldPosition;

            var followerSide = CreateStripSide(smallerTerrain,
                CalculateFollowerMarginUv(biggerTerrain, smallerTerrain));
            var followerWeldUv = _level2Manager.AddStipSegment(weldPosition, followerSide);
            outWeldModificationsDict[smallerTerrain.Terrain.WeldingInputTerrainId] = followerWeldUv;
            return outWeldModificationsDict;
        }

        public void RemoveTerrain(int terrainId)
        {
            foreach (var pair in _terrainSideToStripPositions.Where(c => c.Key.TerrainId == terrainId)
                .ToList()) // when given terrain is leader
            {
                _terrainSideToStripPositions.Remove(pair.Key);
                var wasRemoved = _level2Manager.RemoveWeld(pair.Value, terrainId);
                _followerStripPositions.Where(c => c.Value.Equals(pair.Value)).ToList()
                    .ForEach(c => _followerStripPositions.Remove(c.Key));

                Preconditions.Assert(wasRemoved, "leader was removed, but there was no removal of strip");
            }
            foreach (var pair in _followerStripPositions.Where(c => c.Key.TerrainId == terrainId)
                .ToList()) // when given terrain is follower
            {
                var wasRemoved = _level2Manager.RemoveWeld(pair.Value, terrainId);
                if (wasRemoved) //last follower - can removfe leader
                {
                    _terrainSideToStripPositions.Where(c => c.Value.Equals(pair.Value)).ToList()
                        .ForEach(c => _terrainSideToStripPositions.Remove(c.Key));
                }
                
                _followerStripPositions.Remove(pair.Key);
            }
        }

        private Vector2 CalculateFollowerMarginUv(WeldSideSource biggerTerrain, WeldSideSource smallerTerrain)
        {
            var biggerSideRange = new Vector2(0, 0);
            if (biggerTerrain.SideType.GetOrientation() == WeldOrientation.Horizontal)
            {
                biggerSideRange = RectangleUtils.CalculateSubPosition(biggerTerrain.Terrain.DetailGlobalArea,
                    biggerTerrain.Terrain.UvCoordsPositions2D).XRange;
            }
            else
            {
                biggerSideRange = RectangleUtils.CalculateSubPosition(biggerTerrain.Terrain.DetailGlobalArea,
                    biggerTerrain.Terrain.UvCoordsPositions2D).YRange;
            }

            var smallerSideRange = new Vector2(0, 0);
            if (smallerTerrain.SideType.GetOrientation() == WeldOrientation.Horizontal)
            {
                smallerSideRange = RectangleUtils.CalculateSubPosition(smallerTerrain.Terrain.DetailGlobalArea,
                    smallerTerrain.Terrain.UvCoordsPositions2D).XRange;
            }
            else
            {
                smallerSideRange = RectangleUtils.CalculateSubPosition(smallerTerrain.Terrain.DetailGlobalArea,
                    smallerTerrain.Terrain.UvCoordsPositions2D).YRange;
            }

            var uv = VectorUtils.CalculateSubelementUv(biggerSideRange, smallerSideRange);
            Preconditions.Assert(uv.IsNormalized(),
                $"E76 Margin uv is not normalized: {uv}, biggerSideRange:{biggerSideRange}, smallerSideRange {smallerSideRange}");
            return uv;
        }

        private static StripSide CreateStripSide(WeldSideSource weldingTerrain, Vector2 normalizedMarginOfUvTerrain)
        {
            return new StripSide()
            {
                HeightTexture = weldingTerrain.Terrain.Texture,
                HeightTextureUvs = weldingTerrain.Terrain.UvCoordsPositions2D,
                Lod = weldingTerrain.Terrain.TerrainLod,
                NormalizedMarginUvOfTerrain = normalizedMarginOfUvTerrain,
                Resolution = weldingTerrain.Terrain.Resolution,
                WeldSideType = weldingTerrain.SideType,
                TerrainId = weldingTerrain.Terrain.WeldingInputTerrainId
            };
        }

        private class TerrainSide
        {
            public int TerrainId;
            public WeldSideType SideType;

            protected bool Equals(TerrainSide other)
            {
                return TerrainId == other.TerrainId && SideType == other.SideType;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TerrainSide) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (TerrainId * 397) ^ (int) SideType;
                }
            }
        }
    }

    public class WeldMapLevel2Manager
    {
        private WeldingExecutor _weldingExecutor;
        private WeldMapLevel2ManagerConfiguration _configuration;
        private WeldMapColumn[] _stripColumns;

        public WeldMapLevel2Manager(WeldingExecutor weldingExecutor, WeldMapLevel2ManagerConfiguration configuration)
        {
            _configuration = configuration;
            _weldingExecutor = weldingExecutor;
            _stripColumns = new WeldMapColumn[configuration.ColumnsCount];
            for (int i = 0; i < configuration.ColumnsCount; i++)
            {
                _stripColumns[i] = new WeldMapColumn(configuration.StripsPerOneColumnCount);
            }
        }

        public StripRegistrationResult RegisterStrip(StripSide leader)
        {
            Preconditions.Assert(
                Math.Abs(leader.NormalizedMarginUvOfTerrain.x) < 0.00001f &&
                Math.Abs(leader.NormalizedMarginUvOfTerrain.y - 1) < 0.00001f,
                "Leader's marginUv must be 0-1, but is " + leader.NormalizedMarginUvOfTerrain);

            for (int i = 0; i < _stripColumns.Length; i++)
            {
                var column = _stripColumns[i];
                if (column.HasEmptyStrip)
                {
                    var stripInColumnIndex = column.AllocateStrip(leader);
                    return new StripRegistrationResult()
                    {
                        StripPosition =
                            new WeldStripPosition()
                            {
                                ColumnIndex = i,
                                StripInColumnIndex = stripInColumnIndex
                            },
                        WeldUvs = TerrainWeldUvs.CreateFrom(leader.WeldSideType,
                            CreateNormalizedWeldRange(
                                CalculateWeldRange(stripInColumnIndex, leader.NormalizedMarginUvOfTerrain), i))
                    };
                }
            }
            Preconditions.Fail("E22. There are no empty strips");
            return null;
        }

        public TerrainWeldUvs AddStipSegment(WeldStripPosition stripPosition, StripSide follower)
        {
            var strip = _stripColumns[stripPosition.ColumnIndex].GetSegment(stripPosition.StripInColumnIndex);
            strip.Followers.Add(follower);

            var leader = strip.Leader;
            var leaderSideInfo = CreateDrawingSideInfo(leader, follower.NormalizedMarginUvOfTerrain);
            var followerSideInfo = CreateDrawingSideInfo(follower, new Vector2(0, 1));

            var donorSide = leaderSideInfo;
            var acceptorSide = followerSideInfo;
            if (follower.WeldSideType.IsDonorSide())
            {
                donorSide = followerSideInfo;
                acceptorSide = leaderSideInfo;
            }

            var weldRange = CalculateWeldRange(stripPosition.StripInColumnIndex, follower.NormalizedMarginUvOfTerrain);
            WeldOnTextureInfo weldInfo = new WeldOnTextureInfo()
            {
                ColumnIndex = stripPosition.ColumnIndex,
                WeldRange = weldRange
            };

            acceptorSide.SamplingDistance = weldInfo.WeldRange.Length /
                                            (acceptorSide.FullLodSidePixelsRange.Length - 1);
            donorSide.SamplingDistance = weldInfo.WeldRange.Length / (donorSide.FullLodSidePixelsRange.Length - 1);

            _weldingExecutor.RenderWeld(new WeldTextureDrawingOrder()
            {
                FirstSideInfo = acceptorSide,
                SecondSideInfo = donorSide,
                WeldOnTextureInfo = weldInfo
            });

            return TerrainWeldUvs.CreateFrom(follower.WeldSideType,
                CreateNormalizedWeldRange(weldRange, stripPosition.ColumnIndex));
        }


        private Vector4 CreateNormalizedWeldRange(IntVector2 weldRange, int columnIndex)
        {
            return new Vector4(
                (float) columnIndex / _configuration.WeldTextureSize.X,
                (float) weldRange.X / _configuration.WeldTextureSize.Y,
                (float) weldRange.Y / _configuration.WeldTextureSize.Y);
        }

        private IntVector2 CalculateWeldRange(int stripPositionStripInColumnIndex, Vector2 uvOfMargin)
        {
            var start = _configuration.StripStride * stripPositionStripInColumnIndex;

            var fVec = VectorUtils.CalculateSubPosition(new Vector2(start, start + _configuration.StripLength),
                uvOfMargin);
            return new IntVector2(Mathf.RoundToInt(fVec.x), Mathf.RoundToInt(fVec.y));
        }

        private static WeldTextureDrawingSideInfo CreateDrawingSideInfo(StripSide leader,
            Vector2 normalizedMarginUvOfTerrain)
        {
            var texSize = new Vector2(leader.HeightTexture.Size.X - 1, leader.HeightTexture.Size.Y - 1);
            texSize = new Vector2(texSize.x / Mathf.Pow(2, leader.Lod), texSize.y / Mathf.Pow(2, leader.Lod));
            var texAreaUvd = RectangleUtils.CalculateSubPosition(new MyRectangle(0, 0, texSize.x, texSize.y),
                leader.HeightTextureUvs);

            var constantCoord = 0;
            if (leader.WeldSideType == WeldSideType.Right)
            {
                constantCoord = Mathf.RoundToInt(texAreaUvd.MaxX);
            }
            else if (leader.WeldSideType == WeldSideType.Left)
            {
                constantCoord = Mathf.RoundToInt(texAreaUvd.X);
            }
            else if (leader.WeldSideType == WeldSideType.Top)
            {
                constantCoord = Mathf.RoundToInt(texAreaUvd.MaxY);
            }
            else if (leader.WeldSideType == WeldSideType.Bottom)
            {
                constantCoord = Mathf.RoundToInt(texAreaUvd.Y);
            }

            Vector2 baseFullSideRange;
            if (leader.WeldSideType.GetOrientation() == WeldOrientation.Horizontal)
            {
                baseFullSideRange = new Vector2(texAreaUvd.X, texAreaUvd.MaxX);
            }
            else
            {
                baseFullSideRange = new Vector2(texAreaUvd.Y, texAreaUvd.MaxY);
            }
            baseFullSideRange = VectorUtils.CalculateSubPosition(baseFullSideRange, normalizedMarginUvOfTerrain);

            var leaderSideLength = Mathf.RoundToInt(texAreaUvd.Width);
            if (leaderSideLength % 5 == 1)
            {
                leaderSideLength--; //change 241 to 240 etc
            }
            //leaderSideLength /= Mathf.RoundToInt(Mathf.Pow(2, leader.Lod));

            int samplingDistance = 240 / leaderSideLength;


            WeldTextureDrawingSideInfo firstSideInfo = new WeldTextureDrawingSideInfo()
            {
                ConstantCoord = constantCoord,
                FullLodSidePixelsRange = new IntVector2(Mathf.RoundToInt(baseFullSideRange.x),
                    Mathf.RoundToInt(baseFullSideRange.y + 1)),
                HeightTexture = leader.HeightTexture,
                LodLevel = leader.Lod,
                SamplingDistance = samplingDistance,
                SideType = leader.WeldSideType
            };
            return firstSideInfo;
        }

        public bool RemoveWeld(WeldStripPosition position, int terrainId)
        {
            var column = _stripColumns[position.ColumnIndex];
            var strip = column.GetSegment(position.StripInColumnIndex);

            bool removeStrip = false;
            if (strip.Followers.Any(c => c.TerrainId == terrainId))
            {
                strip.Followers.RemoveAll(c => c.TerrainId == terrainId);
                if (!strip.Followers.Any())
                {
                    removeStrip = true; // no followers - strip can be removed!
                }
            }
            else if (strip.Leader.TerrainId == terrainId)
            {
                removeStrip = true;
            }

            if (removeStrip)
            {
                column.RemoveStrip(position.StripInColumnIndex);
                return true;
            }
            else
            {
                return false;
            }
        }
    }


    public class WeldMapLevel2ManagerConfiguration
    {
        public int ColumnsCount;
        public int OneWeldMaxHeight;
        public int StripsPerOneColumnCount;
        public int StripStride = 256;
        public int StripLength = 240;
        public IntVector2 WeldTextureSize;
    }

    public class WeldStripPosition
    {
        public int ColumnIndex;
        public int StripInColumnIndex;
    }

    public class WeldUpdateInfo
    {
        public int TerrainId;
        public TerrainWeldUvs WeldUvs;
    }

    public class WeldMapColumn
    {
        private WeldStrip[] _strips;

        public WeldMapColumn(int stripsCount)
        {
            _strips = new WeldStrip[stripsCount];
        }

        public bool HasEmptyStrip => Enumerable.Range(0, _strips.Length).Any(i => _strips[i] == null);

        public int AllocateStrip(StripSide leader)
        {
            Preconditions.Assert(HasEmptyStrip, "There is no empty strip in this column");
            for (int i = 0; i < _strips.Length; i++)
            {
                if (_strips[i] == null)
                {
                    _strips[i] = new WeldStrip()
                    {
                        Leader = leader,
                        Followers = new List<StripSide>()
                    };
                    return i;
                }
            }
            Preconditions.Fail("E46 Not expected");
            return -1;
        }

        public WeldStrip GetSegment(int index)
        {
            var strip = _strips[index];
            Preconditions.Assert(strip != null, $"E64 Strip of index {index} is not allocated");
            return strip;
        }

        public void RemoveStrip(int index)
        {
            _strips[index] = null;
        }
    }

    public class WeldStrip
    {
        public StripSide Leader;

        public List<StripSide> Followers;
        //public WeldStripPosition Position;
    }

    public class StripSide
    {
        public TextureWithSize HeightTexture;
        public MyRectangle HeightTextureUvs;
        public TerrainCardinalResolution Resolution;
        public int Lod;
        public int TerrainId;

        public WeldSideType WeldSideType;
        public Vector2 NormalizedMarginUvOfTerrain;
    }

    public class StripRegistrationResult
    {
        public WeldStripPosition StripPosition;
        public TerrainWeldUvs WeldUvs;
    }


    /// <summary>
    /// ///////////////////////////////////
    /// </summary>
    public class WeldingExecutor
    {
        private ComputeShaderContainerGameObject _computeShaderContainer;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;
        private Texture _weldTexture;

        public WeldingExecutor(ComputeShaderContainerGameObject computeShaderContainer,
            UnityThreadComputeShaderExecutorObject shaderExecutorObject, Texture weldTexture)
        {
            _computeShaderContainer = computeShaderContainer;
            _shaderExecutorObject = shaderExecutorObject;
            _weldTexture = weldTexture;
        }

        public async Task RenderWeld(WeldTextureDrawingOrder order)
        {
            var changedPixelsCount = order.WeldOnTextureInfo.WeldRange.Y - order.WeldOnTextureInfo.WeldRange.X;

            var parametersContainer = new ComputeShaderParametersContainer();

            MultistepComputeShader singleToDuoGrassBillboardShader =
                new MultistepComputeShader(_computeShaderContainer.WeldRenderingShader,
                    new IntVector2(changedPixelsCount, 1));

            var kernel = singleToDuoGrassBillboardShader.AddKernel("CSTerrainWelding_Main");
            var allKernels = new List<MyKernelHandle>()
            {
                kernel
            };
            var weldTextureInShader = parametersContainer.AddExistingComputeShaderTexture(_weldTexture);
            singleToDuoGrassBillboardShader.SetTexture("WeldTexture", weldTextureInShader, allKernels);

            var weldTextureChangesInfo = CalculateWeldTextureChangedPixelsShaderRange(order.WeldOnTextureInfo);
            var weldTextureChangesInfoInShader = parametersContainer.AddComputeBufferTemplate(
                new MyComputeBufferTemplate()
                {
                    BufferData = new WeldTexturePixelChangesInfo[]
                    {
                        weldTextureChangesInfo
                    },
                    Count = 1,
                    Stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WeldTexturePixelChangesInfo)),
                    Type = ComputeBufferType.Default
                });
            singleToDuoGrassBillboardShader.SetBuffer("WeldTextureChangesInfo", weldTextureChangesInfoInShader,
                allKernels);

            var samplingDistance =
                Mathf.RoundToInt(Mathf.Pow(2, Mathf.Max(order.FirstSideInfo.LodLevel, order.SecondSideInfo.LodLevel)));

            var firstSideChangedPixelsShaderRange = CalculateSideChangedPixelsShaderRange(order.FirstSideInfo,
                order.FirstSideInfo.SamplingDistance == 0 ? samplingDistance : order.FirstSideInfo.SamplingDistance);

            var secondSideChangedPixelsShaderRange = CalculateSideChangedPixelsShaderRange(order.SecondSideInfo,
                order.SecondSideInfo.SamplingDistance == 0 ? samplingDistance : order.SecondSideInfo.SamplingDistance);
            var terrainSideChangesInfoInShader = parametersContainer.AddComputeBufferTemplate(
                new MyComputeBufferTemplate()
                {
                    BufferData = new TerrainSidePixelChangesInfo[]
                    {
                        firstSideChangedPixelsShaderRange,
                        secondSideChangedPixelsShaderRange
                    },
                    Count = 2,
                    Stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(TerrainSidePixelChangesInfo)),
                    Type = ComputeBufferType.Default
                });
            singleToDuoGrassBillboardShader.SetBuffer("TerrainSideChangesInfos", terrainSideChangesInfoInShader,
                allKernels);

            singleToDuoGrassBillboardShader.SetTexture("FirstHeightTexture",
                parametersContainer.AddExistingComputeShaderTexture(order.FirstSideInfo.HeightTexture.Texture),
                allKernels);
            singleToDuoGrassBillboardShader.SetTexture("SecondHeightTexture",
                parametersContainer.AddExistingComputeShaderTexture(order.SecondSideInfo.HeightTexture.Texture),
                allKernels);


            ComputeBufferRequestedOutParameters outParameters = new ComputeBufferRequestedOutParameters();
            await _shaderExecutorObject.DispatchComputeShader(new ComputeShaderOrder()
            {
                ParametersContainer = parametersContainer,
                OutParameters = outParameters,
                WorkPacks = new List<ComputeShaderWorkPack>()
                {
                    new ComputeShaderWorkPack()
                    {
                        Shader = singleToDuoGrassBillboardShader,
                        DispatchLoops = new List<ComputeShaderDispatchLoop>()
                        {
                            new ComputeShaderDispatchLoop()
                            {
                                DispatchCount = 1,
                                KernelHandles = allKernels
                            }
                        }
                    },
                }
            });
        }

        private TerrainSidePixelChangesInfo CalculateSideChangedPixelsShaderRange(
            WeldTextureDrawingSideInfo orderFirstSideInfo, int samplingDistance)
        {
            return new TerrainSidePixelChangesInfo()
            {
                IsVertical = (orderFirstSideInfo.SideType.GetOrientation() == WeldOrientation.Vertical ? 1 : 0),
                ConstantCoord = orderFirstSideInfo.ConstantCoord,
                SamplingDistance = samplingDistance,
                Range1 = orderFirstSideInfo.FullLodSidePixelsRange.X,
                Range2 = orderFirstSideInfo.FullLodSidePixelsRange.Y,
                Lod = orderFirstSideInfo.LodLevel
            };
        }

        private WeldTexturePixelChangesInfo CalculateWeldTextureChangedPixelsShaderRange(
            WeldOnTextureInfo orderWeldOnTextureInfo)
        {
            return new WeldTexturePixelChangesInfo()
            {
                ConstantCoord = orderWeldOnTextureInfo.ColumnIndex,
                Range1 = orderWeldOnTextureInfo.WeldRange.X,
                Range2 = orderWeldOnTextureInfo.WeldRange.Y,
            };
        }

        public struct TerrainSidePixelChangesInfo
        {
            public int IsVertical;
            public int ConstantCoord;
            public int Range1;
            public int Range2;
            public int SamplingDistance;
            public int Lod;
        }

        public struct WeldTexturePixelChangesInfo
        {
            public int ConstantCoord;
            public int Range1;
            public int Range2;
        }
    }

    public class WeldTextureManagerConfiguration
    {
        public IntVector2 WeldTextureSize;
    }

    public class WeldTextureDrawingOrder
    {
        public WeldTextureDrawingSideInfo FirstSideInfo;
        public WeldTextureDrawingSideInfo SecondSideInfo;
        public WeldOnTextureInfo WeldOnTextureInfo;
    }

    public class WeldOnTextureInfo
    {
        public int ColumnIndex;
        public IntVector2 WeldRange;
    }

    public class WeldTextureDrawingSideInfo
    {
        public WeldSideType SideType;
        public TextureWithSize HeightTexture;
        public int LodLevel;
        public IntVector2 FullLodSidePixelsRange;
        public int ConstantCoord;
        public int SamplingDistance;
    }
}