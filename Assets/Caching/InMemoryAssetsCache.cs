using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Caching
{
    public interface IAssetsCache<TQuery, TAsset > where TAsset : class
    {
        bool IsInCache(TQuery query);

        Task<CacheQueryOutput<TAsset>> TryRetriveAsync(TQuery query);

        Task<InternalTokenizedAsset<TQuery,TAsset>> AddAssetAsync(int creationObligationToken, TQuery query, TAsset asset);

        Task RemoveAssetAsync(TQuery query);
    }

    public class InMemoryAssetsCache<TQuery, TAsset> : IAssetsCache<TQuery, TAsset> where TAsset: class
    {
        private InMemoryAssetsLevel2Cache<TQuery,TAsset> _level2Cache;
        private Dictionary<int, DetailElemensUnderCreationSemaphore> _creationObligationDictionary = new Dictionary<int, DetailElemensUnderCreationSemaphore>();
        private int _lastObligationId = 0;

        public InMemoryAssetsCache(InMemoryAssetsLevel2Cache<TQuery, TAsset> level2Cache)
        {
            _level2Cache = level2Cache;
        }

        public bool IsInCache(TQuery query)
        {
            return _level2Cache.IsInCache(query);
        }

        public async Task<CacheQueryOutput<TAsset>> TryRetriveAsync(TQuery query)
        {
            var elementUnderCreation = _creationObligationDictionary.Values.FirstOrDefault(c => c.Query.Equals(query));

            if (elementUnderCreation != null)
            {
                await elementUnderCreation.Semaphore.Await();
                var detailElementAfterWaiting = _level2Cache.TryRetrive(query);
                Preconditions.Assert(detailElementAfterWaiting!= null, "Impossible. Even after waiting, still no queryOutput");

                return new CacheQueryOutput<TAsset>()
                {
                    CreationObligationToken = null,
                    Asset = detailElementAfterWaiting
                };
            }

            var  detailElement = _level2Cache.TryRetrive(query);
            if ( detailElement != null)
            {
                return new CacheQueryOutput<TAsset>()
                {
                    CreationObligationToken = null,
                    Asset = detailElement
                };
            }

            //Debug.Log("T88 Adding creation obligation!");
            var obligationToken = _lastObligationId++;
            var semaphore = new DetailElemensUnderCreationSemaphore()
            {
                Semaphore = new TcsSemaphore(),
                Query = query
            };
            _creationObligationDictionary[obligationToken] = semaphore;

            return new CacheQueryOutput<TAsset>()
            {
                CreationObligationToken = obligationToken,
                Asset = null
            };
        }

        public async Task<InternalTokenizedAsset<TQuery,TAsset>> AddAssetAsync(int creationObligationToken, TQuery query, TAsset asset)
        {
            await _level2Cache.AddAsset(creationObligationToken, query, asset); 

            var semaphore = _creationObligationDictionary[creationObligationToken];
            _creationObligationDictionary.Remove(creationObligationToken);
            semaphore.Semaphore.Set();

            return new InternalTokenizedAsset<TQuery,TAsset>()
            {
                Asset = asset,
                Query = query
            };
        }

        public Task RemoveAssetAsync(TQuery query)
        {
            return _level2Cache.RemoveAssetElementAsync(query);
        }


        private class DetailElemensUnderCreationSemaphore
        {
            public TQuery Query;
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


    public class InMemoryAssetsLevel2Cache<TQuery, TAsset > where TAsset : class
    {
        private MemoryCachableAssetsActionsPerformer<TAsset> _entityActionsPerformer;
        private InMemoryCacheConfiguration _configuration;

        private Dictionary<TQuery, ReferenceCountedAsset> _activeTree;

        private List<TQuery> _nonReferencedElementsList = new List<TQuery>();
        private int _elementsLength = 0;

        public InMemoryAssetsLevel2Cache( InMemoryCacheConfiguration configuration, MemoryCachableAssetsActionsPerformer<TAsset>  entityActionsPerformer)
        {
            _configuration = configuration;
            _entityActionsPerformer = entityActionsPerformer;
            _activeTree = new Dictionary<TQuery, ReferenceCountedAsset>();
        }

        public bool IsInCache(TQuery queryRect)
        {
            return TryRetriveAssetFromTree(queryRect) != null;
        }

        public  TAsset TryRetrive(TQuery queryArea)
        {
            ReferenceCountedAsset foundElement = null;

            foundElement = TryRetriveAssetFromTree(queryArea);

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

        private ReferenceCountedAsset TryRetriveAssetFromTree(TQuery queryRect)
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


        public Task AddAsset(int creationObligationToken, TQuery quantisizedQueryArea, TAsset asset)
        {
            var activeTreeElement = TryRetriveAssetFromTree(quantisizedQueryArea);
            Preconditions.Assert(activeTreeElement == null,
                "There arleady is one detailElement of given description: qa: " + quantisizedQueryArea);

            var newElement = new ReferenceCountedAsset()
            {
                Element = asset,
                ReferenceCount = 1
            };
            AddElementToActiveTree(quantisizedQueryArea, newElement);
            return ClearNonReferencedElements();
        }

        private void AddElementToActiveTree(TQuery queryArea, ReferenceCountedAsset newElement)
        {
            _activeTree[queryArea] = newElement;
            _elementsLength += _entityActionsPerformer.CalculateMemoryUsage(newElement.Element);
        }

 
        public async Task RemoveAssetElementAsync(TQuery queryArea)
        {
            var foundElement =
                TryRetriveAssetFromTree(queryArea);
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
            ReferenceCountedAsset foundElement)
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

                var elementToRemove = TryRetriveAssetFromTree(tokenOfElementToRemove);
                Preconditions.Assert(elementToRemove != null, "Element we wish to delete is not in activeTree");
                Debug.Log("T77 Removing element from non-referenced!");
                await DeleteElement(tokenOfElementToRemove, elementToRemove);
            }
        }

        private class ReferenceCountedAsset
        {
            public TAsset Element;
            public int ReferenceCount;
        }
    }

    public class InMemoryCacheConfiguration
    {
        public long MaxTextureMemoryUsed = 1024 * 1024 * 512 * 2;
    }

    public class InternalTokenizedAsset<TQuery, TAsset>
    {
        public TAsset Asset;
        public TQuery Query;
    }

    public class CacheQueryOutput<TAsset>
    {
        public TAsset Asset;
        public int? CreationObligationToken;
    }

}