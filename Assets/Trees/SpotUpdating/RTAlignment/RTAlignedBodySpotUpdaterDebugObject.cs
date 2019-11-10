using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Trees.SpotUpdating.RTAlignment
{
    public class RTAlignedBodySpotUpdaterDebugObject : MonoBehaviour, ISpotPositionChangesListener
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;
        private DesignBodySpotUpdaterProxy _proxy;
        private Dictionary<SpotId, DebugElem> _elemsDict = new Dictionary<SpotId, DebugElem>();
        private CommonExecutorUTProxy _commonExecutor = new CommonExecutorUTProxy();
        private bool _createDebugObjects = false;
        private int _instanceCountMultiplier = 10;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            DesignBodySpotChangeCalculator spotChangeCalculator =
                new DesignBodySpotChangeCalculator(ComputeShaderContainer, new UnityThreadComputeShaderExecutorObject(),
                   _commonExecutor, HeightDenormalizer.Identity);

            RTAlignedDesignBodySpotUpdaterConfiguration configuration = new RTAlignedDesignBodySpotUpdaterConfiguration()
            {
                WholeAreaLength = 6400,
                _topLod = 2
            };
            _proxy = new DesignBodySpotUpdaterProxy(
                new RTAlignedDesignBodySpotUpdater(spotChangeCalculator, configuration, this));

            AddHeightTerrain(0.2f, new MyRectangle(0, 0, 1600, 1600));
            AddSpots(new MyRectangle(0,0,1600,1600), 100*_instanceCountMultiplier );
            AddSpots(new MyRectangle(1600,0,1600,1600), 200*_instanceCountMultiplier );
            AddHeightTerrain(0.4f, new MyRectangle(1600, 0, 800, 800));
            AddHeightTerrain(0.6f, new MyRectangle(1600+800, 0, 800, 800));
            AddHeightTerrain(0.8f, new MyRectangle(1600, 800, 800, 800));
            AddHeightTerrain(0.9f, new MyRectangle(1600+800, 800, 800, 800));

            AddSpotGroup(new MyRectangle(0,    1600    ,800,800), 100*_instanceCountMultiplier );
            AddSpotGroup(new MyRectangle(0+800,1600    ,800,800), 100*_instanceCountMultiplier );
            AddSpotGroup(new MyRectangle(0+800,1600+800,800,800), 100*_instanceCountMultiplier );
            AddSpotGroup(new MyRectangle(0    ,1600+800,800,800), 100*_instanceCountMultiplier );

            AddHeightTerrain(0.45f, new MyRectangle(0, 1600, 1600, 1600));

        }


        public void Update()
        {
            if (Time.frameCount == 4)
            {
                _proxy.SynchronicUpdate();
            }
        }

        private int _terrainTextureId = 0;
        private void AddHeightTerrain(float height, MyRectangle area)
        {
            var heightArray = new float[12, 12];
            for (int x = 0; x < 12; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    heightArray[x, y] = height;
                }
            }
            var ha = new HeightmapArray(heightArray);
            var encodedHeightTex = HeightmapUtils.CreateTextureFromHeightmap(ha);

            var transformer = new TerrainTextureFormatTransformator(_commonExecutor);
            var plainHeightTex =
                transformer.EncodedHeightTextureToPlain(TextureWithSize.FromTex2D(encodedHeightTex));

            _proxy.UpdateBodiesSpots(new UpdatedTerrainTextures()
            {
                HeightTexture = plainHeightTex,
                NormalTexture = plainHeightTex,
                TerrainTextureId = new SpotUpdaterTerrainTextureId(_terrainTextureId++),
                TextureCoords = new MyRectangle(0, 0, 1, 1),
                TextureGlobalPosition = area
            });
        }

        private void AddSpots(MyRectangle generationArea, int amount)
        {
            List<Vector2> bodiesFlatPositions = new List<Vector2>();
            var atSide = Mathf.Sqrt(amount);
            var step = (generationArea.Width / atSide) - 0.5f;

            for (float x = generationArea.X + 0.5f; x < generationArea.MaxX - 0.5f; x += step)
            {
                for (float y = generationArea.Y + 0.5f; y < generationArea.MaxY - 0.5f; y += step)
                {
                    bodiesFlatPositions.Add(new Vector2(x,y));
                }
            }


            var spots = _proxy.RegisterDesignBodies(bodiesFlatPositions);

            if (_createDebugObjects)
            {
                for (int i = 0; i < spots.Count; i++)
                {
                    _elemsDict[spots[i]] = new DebugElem()
                    {
                        FlatPosition = bodiesFlatPositions[i],
                        CreatedObject = GameObject.CreatePrimitive(PrimitiveType.Cube)
                    };
                }
            }
        }

        private void AddSpotGroup(MyRectangle generationArea, int amount)
        {
            List<Vector2> bodiesFlatPositions = new List<Vector2>();
            var atSide = Mathf.Sqrt(amount);
            var step = (generationArea.Width / atSide) - 0.5f;

            for (float x = generationArea.X + 0.5f; x < generationArea.MaxX - 0.5f; x += step)
            {
                for (float y = generationArea.Y + 0.5f; y < generationArea.MaxY - 0.5f; y += step)
                {
                    bodiesFlatPositions.Add(new Vector2(x,y));
                }
            }


            var spotId = _proxy.RegisterBodiesGroup(bodiesFlatPositions);

            if (_createDebugObjects)
            {
                _elemsDict[spotId] = new DebugElem()
                {
                    GroupElems = bodiesFlatPositions.Select(c => new DebugElem()
                    {
                        CreatedObject = GameObject.CreatePrimitive(PrimitiveType.Capsule),
                        FlatPosition = c
                    }).ToList() 
                };
            }
        }

        private int _changeNo = 0;
        public void SpotsWereChanged(Dictionary<SpotId, DesignBodySpotModification> changedSpots)
        {
            if (_createDebugObjects)
            {
                var go = new GameObject(""+_changeNo++);
                foreach (var pair in changedSpots)
                {
                    var elem = _elemsDict[pair.Key];
                    var pos = new Vector3(elem.FlatPosition.x, pair.Value.SpotData.Height * 1000, elem.FlatPosition.y);
                    elem.CreatedObject.transform.localPosition = pos;
                    elem.CreatedObject.transform.localScale = new Vector3(10, 10, 10);
                    elem.CreatedObject.transform.SetParent(go.transform);
                }
            }
        }

        public void SpotGroupsWereChanged(Dictionary<SpotId, List< DesignBodySpotModification>> changedSpots)
        {
            if (_createDebugObjects)
            {
                foreach (var pair in changedSpots.Select(c => new
                {
                    elems = _elemsDict[c.Key].GroupElems,
                    spotDatas = c.Value
                }))
                {
                    var go = new GameObject(""+_changeNo++);
                    for (int i = 0; i < pair.elems.Count; i++)
                    {
                        var spot = pair.spotDatas[i];
                        var elem = pair.elems[i];
                        var pos = new Vector3(elem.FlatPosition.x, spot.SpotData.Height * 1000, elem.FlatPosition.y);
                        elem.CreatedObject.transform.localPosition = pos;
                        elem.CreatedObject.transform.localScale = new Vector3(10, 10, 10);
                        elem.CreatedObject.transform.SetParent(go.transform);
                    }
                }
            }
        }

        private class DebugElem
        {
            public Vector2 FlatPosition;
            public GameObject CreatedObject;

            public List<DebugElem> GroupElems;
        }
    }
}
