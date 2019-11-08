using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.TerrainMat;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;
using Object = System.Object;

namespace Assets.Heightmaps.Ring1.TerrainDescription.Cache
{
    public interface IAssetsCache
    {
        bool IsInCache(MyRectangle queryArea, TerrainCardinalResolution resolution,
            TerrainDescriptionElementTypeEnum type);

        Task<CacheQueryOutput> TryRetriveAsync(MyRectangle queryArea, TerrainCardinalResolution resolution, TerrainDescriptionElementTypeEnum type);

        Task<InternalTokenizedTerrainDetailElement> AddTerrainDetailElement(int creationObligationToken,
            TextureWithSize texture,
            MyRectangle queryArea, TerrainCardinalResolution resolution,
            TerrainDescriptionElementTypeEnum type);

        Task RemoveTerrainDetailElementAsync(InternalTerrainDetailElementToken token);
    }

    public class TerrainDetailElementsCache:IAssetsCache// ObligationProvidingTerrainDetailElementsCache : IAssetsCache
    {
        private TerrainDetailElementsLevel2Cache<IntRectangle,TextureWithSize> _level2Cache;
        private TerrainDetailElementCacheConfiguration _configuration;
        private Dictionary<int, DetailElemensUnderCreationSemaphore> _creationObligationDictionary = new Dictionary<int, DetailElemensUnderCreationSemaphore>();
        private int _lastObligationId = 0;

        public TerrainDetailElementsCache(CommonExecutorUTProxy commonExecutor, TerrainDetailElementCacheConfiguration configuration)
        {
            _configuration = configuration;
            _level2Cache = new TerrainDetailElementsLevel2Cache<IntRectangle, TextureWithSize>(configuration,
                new TextureWithSizeActionsPerformer(commonExecutor));
        }

        public bool IsInCache(MyRectangle queryArea, TerrainCardinalResolution resolution, TerrainDescriptionElementTypeEnum type)
        {
            return _level2Cache.IsInCache(GenerateQuantisizedQueryRectangle(queryArea));
        }

        public async Task<CacheQueryOutput> TryRetriveAsync(MyRectangle queryArea, TerrainCardinalResolution resolution, TerrainDescriptionElementTypeEnum type)
        {
            var newToken = new InternalTerrainDetailElementToken(queryArea, resolution, type);

            var quantisizedQueryRect = GenerateQuantisizedQueryRectangle(queryArea);
            var elementUnderCreation = _creationObligationDictionary.Values.FirstOrDefault(c => c.Token.Equals(newToken));

            if (elementUnderCreation != null)
            {
                await elementUnderCreation.Semaphore.Await();
                var detailElementAfterWaiting = _level2Cache.TryRetrive(quantisizedQueryRect);
                Preconditions.Assert(detailElementAfterWaiting!= null, "Impossible. Even after waiting, still no queryOutput");

                return new CacheQueryOutput()
                {
                    CreationObligationToken = null,
                    DetailElement =
                        new InternalTokenizedTerrainDetailElement()
                        {
                            DetailElement = new TerrainDetailElement()
                            {
                                Texture = detailElementAfterWaiting,
                                Resolution = resolution,
                                DetailArea = queryArea
                            },
                            Token = newToken
                        }
                };
            }

            var  detailElement = _level2Cache.TryRetrive(quantisizedQueryRect);
            if ( detailElement != null)
            {
                return new CacheQueryOutput()
                {
                    CreationObligationToken = null,
                    DetailElement =
                        new InternalTokenizedTerrainDetailElement()
                        {
                            DetailElement = new TerrainDetailElement()
                            {
                                Texture = detailElement,
                                Resolution = resolution,
                                DetailArea = queryArea
                            },
                            Token = newToken
                        }
                };
            }

            //Debug.Log("T88 Adding creation obligation!");
            var obligationToken = _lastObligationId++;
            var semaphore = new DetailElemensUnderCreationSemaphore()
            {
                Semaphore = new TcsSemaphore(),
                Token = newToken
            };
            _creationObligationDictionary[obligationToken] = semaphore;

            return new CacheQueryOutput()
            {
                CreationObligationToken = obligationToken,
                DetailElement = null
            };
        }

        public async Task<InternalTokenizedTerrainDetailElement> AddTerrainDetailElement(int creationObligationToken, TextureWithSize texture, MyRectangle queryArea, TerrainCardinalResolution resolution,
            TerrainDescriptionElementTypeEnum type)
        {
            await _level2Cache.AddTerrainDetailElement(creationObligationToken, GenerateQuantisizedQueryRectangle(queryArea), texture);

            var semaphore = _creationObligationDictionary[creationObligationToken];
            _creationObligationDictionary.Remove(creationObligationToken);
            semaphore.Semaphore.Set();

            return new InternalTokenizedTerrainDetailElement()
            {
                DetailElement = new TerrainDetailElement()
                {
                    Texture = texture,
                    Resolution = resolution,
                    DetailArea = queryArea,
                },
                Token = new InternalTerrainDetailElementToken(queryArea,resolution,type)
            };
        }

        public Task RemoveTerrainDetailElementAsync(InternalTerrainDetailElementToken token)
        {
            return _level2Cache.RemoveTerrainDetailElementAsync(GenerateQuantisizedQueryRectangle(token.QueryArea));
        }

        private IntRectangle GenerateQuantisizedQueryRectangle(MyRectangle rect)
        {
            return new IntRectangle(
                Mathf.RoundToInt(rect.X / _configuration.QueryRectangleQuantLength),
                Mathf.RoundToInt(rect.Y / _configuration.QueryRectangleQuantLength),
                Mathf.RoundToInt(rect.Width / _configuration.QueryRectangleQuantLength),
                Mathf.RoundToInt(rect.Height / _configuration.QueryRectangleQuantLength)
            );
        }

        private class DetailElemensUnderCreationSemaphore
        {
            public InternalTerrainDetailElementToken Token;
            public TcsSemaphore Semaphore;
        }
    }


    public interface MemoryCachableAssetsActionsPerformer<TEntity>
    {
        int CalculateMemoryUsage(TEntity entity);
        Task DestroyAsset(TEntity entity);
    }

    public class TextureWithSizeActionsPerformer : MemoryCachableAssetsActionsPerformer<TextureWithSize>
    {
        private CommonExecutorUTProxy _commonExecutor;

        public TextureWithSizeActionsPerformer(CommonExecutorUTProxy commonExecutor)
        {
            _commonExecutor = commonExecutor;
        }

        public int CalculateMemoryUsage(TextureWithSize tex)
        {
            return tex.Size.X * tex.Size.Y * 4;
        }

        public Task DestroyAsset(TextureWithSize entity)
        {
            return _commonExecutor.AddAction(() =>
            {
                GameObject.Destroy(entity.Texture);
            });
        }
    }


    public class TerrainDetailElementsLevel2Cache<TQuery, TAsset > where TAsset : class
    {
        private MemoryCachableAssetsActionsPerformer<TAsset> _entityActionsPerformer;
        private TerrainDetailElementCacheConfiguration _configuration;

        private Dictionary<TQuery, ReferenceCountedTerrainDetailElement> _activeTree;

        private List<TQuery> _nonReferencedElementsList = new List<TQuery>();
        private int _elementsLength = 0;

        public TerrainDetailElementsLevel2Cache( TerrainDetailElementCacheConfiguration configuration, MemoryCachableAssetsActionsPerformer<TAsset>  entityActionsPerformer)
        {
            _configuration = configuration;
            _entityActionsPerformer = entityActionsPerformer;
            _activeTree = new Dictionary<TQuery, ReferenceCountedTerrainDetailElement>();
        }

        public bool IsInCache(TQuery queryRect)
        {
            return TryRetriveTerrainDetailElement(queryRect) != null;
        }

        public  TAsset TryRetrive(TQuery queryArea)
        {
            ReferenceCountedTerrainDetailElement foundElement = null;

            foundElement = TryRetriveTerrainDetailElement(queryArea);

            if (foundElement != null)
            {
                if (foundElement.ReferenceCount == 0)
                {
                    var indexInNonReferenced = _nonReferencedElementsList
                        .Select((c, i) => new
                        {
                            Index = i,
                            AssetWithQueryArea = c
                        }).FirstOrDefault(c => c.AssetWithQueryArea.Equals(queryArea));
                    if (indexInNonReferenced == null)
                    {
                        Debug.Log("In Queue");
                        foreach (var elem in _nonReferencedElementsList)
                        {
                            Debug.Log("Elem: " + elem);
                        }
                        Debug.Log("We were looking for: " +queryArea);
                    }
                    Preconditions.Assert(indexInNonReferenced != null,
                        " There is no non referenced element's token in queue. ");

                    _nonReferencedElementsList.RemoveAt(indexInNonReferenced.Index);
                    //Debug.Log("T99 Removing element from non referenced List");
                }

                foundElement.ReferenceCount++;
                return foundElement.Element;
            }
            else
            {
                return null;
            }
        }

        private ReferenceCountedTerrainDetailElement TryRetriveTerrainDetailElement(TQuery queryRect)
        {
            if (_activeTree.ContainsKey(queryRect))
            {
                return _activeTree[queryRect];
            }
            else
            {
                return null;
            }
        }


        public Task AddTerrainDetailElement(int creationObligationToken, TQuery quantisizedQueryArea, TAsset asset)
        {
            var activeTreeElement = TryRetriveTerrainDetailElement(quantisizedQueryArea);
            Preconditions.Assert(activeTreeElement == null,
                "There arleady is one detailElement of given description: qa: " + quantisizedQueryArea);

            var newElement = new ReferenceCountedTerrainDetailElement()
            {
                Element = asset,
                ReferenceCount = 1
            };
            AddElementToActiveTree(quantisizedQueryArea, newElement);
            return ClearNonReferencedElements();
        }

        private void AddElementToActiveTree(TQuery queryArea, ReferenceCountedTerrainDetailElement newElement)
        {
            _activeTree[queryArea] = newElement;
            _elementsLength += _entityActionsPerformer.CalculateMemoryUsage(newElement.Element);
        }

 
        public async Task RemoveTerrainDetailElementAsync(TQuery queryArea)
        {
            var foundElement =
                TryRetriveTerrainDetailElement(queryArea);
            Preconditions.Assert(foundElement != null, "There is no element of given description");
            foundElement.ReferenceCount--;
            if (foundElement.ReferenceCount <= 0)
            {
                if (ThereIsPlaceForNewAsset(foundElement.Element))
                {
                    _nonReferencedElementsList.Add(queryArea);
                    //Debug.Log("T97 min ref count. Adding to non-ref list");
                }
                else
                {
                    Debug.Log("T97 min ref count. Removing");
                    await DeleteElement(queryArea, foundElement);
                }
            }
        }

        private bool ThereIsPlaceForNewAsset(TAsset asset)
        {
            var textureSize = _entityActionsPerformer.CalculateMemoryUsage(asset);
            return _elementsLength + textureSize <= _configuration.MaxTextureMemoryUsed;
        }

        private async Task DeleteElement(TQuery queryArea,
            ReferenceCountedTerrainDetailElement foundElement)
        {
            await _entityActionsPerformer.DestroyAsset(foundElement.Element);
            var removalResult = _activeTree.Remove(queryArea);
            Preconditions.Assert(removalResult, "Removing failed");
            _elementsLength -= _entityActionsPerformer.CalculateMemoryUsage(foundElement.Element);
        }


        private async Task ClearNonReferencedElements()
        {
            while (_nonReferencedElementsList.Any() && _elementsLength > _configuration.MaxTextureMemoryUsed)
            {
                var tokenOfElementToRemove = _nonReferencedElementsList[0];
                _nonReferencedElementsList.RemoveAt(0);

                var elementToRemove = TryRetriveTerrainDetailElement(tokenOfElementToRemove);
                Preconditions.Assert(elementToRemove != null, "Element we wish to delete is not in activeTree");
                Debug.Log("T77 Removing element from non-referenced!");
                await DeleteElement(tokenOfElementToRemove, elementToRemove);
            }
        }

        private class ReferenceCountedTerrainDetailElement
        {
            public TAsset Element;
            public int ReferenceCount;
        }
    }

    public class TerrainDetailElementCacheConfiguration
    {
        public long MaxTextureMemoryUsed = 1024 * 1024 * 512 * 2;
        public int QueryRectangleQuantLength = 3;
    }

    public class InternalTokenizedTerrainDetailElement
    {
        public TerrainDetailElement DetailElement;
        public InternalTerrainDetailElementToken Token;
    }

    public class TokenizedTerrainDetailElement
    {
        public TerrainDetailElement DetailElement;
        public TerrainDetailElementToken Token;
    }

    public class CacheQueryOutput
    {
        public InternalTokenizedTerrainDetailElement DetailElement;
        public int? CreationObligationToken;
    }

    public class InternalTerrainDetailElementToken
    {
        public InternalTerrainDetailElementToken(MyRectangle queryArea, TerrainCardinalResolution resolution,
            TerrainDescriptionElementTypeEnum type)
        {
            QueryArea = queryArea;
            Resolution = resolution;
            Type = type;
        }

        public MyRectangle QueryArea;
        public TerrainCardinalResolution Resolution;
        public TerrainDescriptionElementTypeEnum Type;

        protected bool Equals(InternalTerrainDetailElementToken other)
        {
            return Equals(QueryArea, other.QueryArea) && Equals(Resolution, other.Resolution) && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InternalTerrainDetailElementToken) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (QueryArea != null ? QueryArea.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Resolution != null ? Resolution.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) Type;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(QueryArea)}: {QueryArea}, {nameof(Resolution)}: {Resolution}, {nameof(Type)}: {Type}";
        }
    }
}