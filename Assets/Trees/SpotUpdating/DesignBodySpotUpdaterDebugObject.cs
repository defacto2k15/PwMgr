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
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Trees.SpotUpdating
{
    public class DesignBodySpotUpdaterDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;
        private DesignBodySpotUpdater _spotUpdater;
        private UnityThreadComputeShaderExecutorObject _shaderExecutorObject;
        private Dictionary<SpotId, GameObject> _gameObjects = new Dictionary<SpotId, GameObject>();
        private CommonExecutorUTProxy _commonExecutor;
        private GameObject _parentGameObject;
        private DummySpotPositionChangesListener _spotPositionChangesListener;

        public void Start2()
        {
        }

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            _commonExecutor = new CommonExecutorUTProxy();
            _shaderExecutorObject = new UnityThreadComputeShaderExecutorObject();
            _spotPositionChangesListener = new DummySpotPositionChangesListener(_gameObjects);
            DesignBodySpotChangeCalculator spotChangeCalculator =
                new DesignBodySpotChangeCalculator(ComputeShaderContainer, _shaderExecutorObject, _commonExecutor,
                    HeightDenormalizer.Identity);
            _spotUpdater = new DesignBodySpotUpdater(spotChangeCalculator, _spotPositionChangesListener);


            List<Vector2> designBodiesList = new List<Vector2>();
            for (int x = 1; x < 9; x++)
            {
                for (int y = 1; y < 9; y++)
                {
                    var flatPosition = new Vector2(x * 10, y * 10);
                    designBodiesList.Add(flatPosition);
                }
            }

            _parentGameObject = new GameObject("ParentDesignBodies");
            var spotIds = Enumerable.Range(0, designBodiesList.Count).Select(idx => new SpotId(idx)).ToList();
            var spotIdList = _spotUpdater.RegisterDesignBodiesAsync(designBodiesList
                .Select((b, idx) => new FlatPositionWithSpotId()
                {
                    FlatPosition = b,
                    SpotId = spotIds[idx]
                }).ToList());
            int i = 0;
            foreach (var id in spotIds)
            {
                var newGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _gameObjects[id] = newGameObject;
                newGameObject.transform.SetParent(_parentGameObject.transform);
                var flatPos = designBodiesList[i];
                newGameObject.transform.position = new Vector3(flatPos.x, 0, flatPos.y);
                i++;
            }

            var heightArray = new float[12, 12];
            for (int x = 0; x < 12; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    heightArray[x, y] = 0.1f; //Mathf.Repeat((y+x)/12f, 1f);
                }
            }
            var ha = new HeightmapArray(heightArray);
            var encodedHeightTex = HeightmapUtils.CreateTextureFromHeightmap(ha);

            var transformer = new TerrainTextureFormatTransformator(_commonExecutor);
            var plainHeightTex = transformer.EncodedHeightTextureToPlain(TextureWithSize.FromTex2D(encodedHeightTex));

            _spotUpdater.UpdateBodiesSpotsAsync(new UpdatedTerrainTextures()
            {
                HeightTexture = plainHeightTex,
                NormalTexture = plainHeightTex,
                TextureGlobalPosition = new MyRectangle(0, 0, 120, 120),
                TextureCoords = new MyRectangle(0, 0, 1, 1)
            }).Wait();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                var heightArray = new float[12, 12];
                for (int x = 0; x < 12; x++)
                {
                    for (int y = 0; y < 12; y++)
                    {
                        heightArray[x, y] = 0.8f;
                    }
                }
                var ha = new HeightmapArray(heightArray);
                var encodedHeightTex = HeightmapUtils.CreateTextureFromHeightmap(ha);

                var transformer = new TerrainTextureFormatTransformator(_commonExecutor);
                var plainHeightTex =
                    transformer.EncodedHeightTextureToPlain(TextureWithSize.FromTex2D(encodedHeightTex));

                _spotUpdater.UpdateBodiesSpotsAsync(new UpdatedTerrainTextures()
                {
                    HeightTexture = plainHeightTex,
                    NormalTexture = plainHeightTex,
                    TextureGlobalPosition = new MyRectangle(0, 0, 150, 150),
                    TextureCoords = new MyRectangle(0.25f, 0.25f, 0.3f, 0.3f)
                }).Wait();
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                var keys = _gameObjects.Keys.ToList();
                var idsToRemove = Enumerable.Range(0, 20).Select(i => keys[i]).ToList();
                _spotUpdater.ForgetDesignBodies(idsToRemove);

                var heightArray = new float[12, 12];
                for (int x = 0; x < 12; x++)
                {
                    for (int y = 0; y < 12; y++)
                    {
                        heightArray[x, y] = 0.5f;
                    }
                }
                var ha = new HeightmapArray(heightArray);
                var encodedHeightTex = HeightmapUtils.CreateTextureFromHeightmap(ha);

                var transformer = new TerrainTextureFormatTransformator(_commonExecutor);
                var plainHeightTex =
                    transformer.EncodedHeightTextureToPlain(TextureWithSize.FromTex2D(encodedHeightTex));

                _spotUpdater.UpdateBodiesSpotsAsync(new UpdatedTerrainTextures()
                {
                    HeightTexture = plainHeightTex,
                    NormalTexture = plainHeightTex,
                    TextureGlobalPosition = new MyRectangle(0, 0, 120, 120),
                    TextureCoords = new MyRectangle(0, 0, 1, 1)
                }).Wait();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                var designBodiesList = new List<Vector2>();
                for (int x = 10; x < 15; x++)
                {
                    for (int y = 0; y < 15; y++)
                    {
                        designBodiesList.Add(new Vector2(x * 10, y * 10));
                    }
                }

                var spotIds = Enumerable.Range(0, designBodiesList.Count).Select(idx => new SpotId(_lastSpotId++))
                    .ToList();
                _spotUpdater.RegisterDesignBodiesAsync(designBodiesList.Select(
                    (c, idx) => new FlatPositionWithSpotId()
                    {
                        FlatPosition = c,
                        SpotId = spotIds[idx]
                    }).ToList()).Wait();
                int i = 0;
                foreach (var id in spotIds)
                {
                    var newGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _gameObjects[id] = newGameObject;
                    newGameObject.transform.SetParent(_parentGameObject.transform);
                    var flatPos = designBodiesList[i];
                    newGameObject.transform.position = new Vector3(flatPos.x, 0, flatPos.y);
                    i++;
                }
                _spotPositionChangesListener.UpdateNotRecognizedSpots();
            }
        }

        private int _lastSpotId = 1000;

        public class DummySpotPositionChangesListener : ISpotPositionChangesListener
        {
            private readonly Dictionary<SpotId, GameObject> _gameObjects;
            private Dictionary<SpotId, SpotData> _notRecognizedSpots = new Dictionary<SpotId, SpotData>();

            public DummySpotPositionChangesListener(Dictionary<SpotId, GameObject> gameObjects)
            {
                _gameObjects = gameObjects;
            }

            public void SpotsWereChanged(Dictionary<SpotId, DesignBodySpotModification> changedSpots)
            {
                //throw new NotImplementedException();
                foreach (var pair in changedSpots)
                {
                    if (!_gameObjects.ContainsKey(pair.Key))
                    {
                        _notRecognizedSpots.Add(pair.Key, pair.Value.SpotData);
                    }
                    else
                    {
                        var transform = _gameObjects[pair.Key].transform;
                        var oldPosition = transform.position;
                        transform.position = new Vector3(oldPosition.x, pair.Value.SpotData.Height, oldPosition.z);
                    }
                }
            }

            public void SpotGroupsWereChanged(Dictionary<SpotId, List< DesignBodySpotModification>> changedSpots)
            {
                throw new NotImplementedException();
            }

            public void UpdateNotRecognizedSpots()
            {
                foreach (var pair in _notRecognizedSpots)
                {
                    var transform = _gameObjects[pair.Key].transform;
                    var oldPosition = transform.position;
                    transform.position = new Vector3(oldPosition.x, pair.Value.Height, oldPosition.z);
                }
                _notRecognizedSpots.Clear();
            }
        }
    }
}