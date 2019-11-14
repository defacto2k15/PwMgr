using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Caching
{
    public interface IAssetsCache<TQuery, TAsset > where TAsset : class
    {
        Task InitializeAsync();

        bool IsInCache(TQuery query);

        Task<CacheQueryOutput<TAsset>> TryRetriveAsync(TQuery query);

        Task<bool> AddAssetAsync(int creationObligationToken, TQuery query, TAsset asset);

        Task RemoveAssetAsync(TQuery query);
    }

    public class InMemoryAssetsCache<TQuery, TAsset> : IAssetsCache<TQuery, TAsset> where TAsset: class where TQuery : IFromQueryFilenameProvider
    {
        private ILevel2AssetsCache<TQuery,TAsset> _level2Cache;
        private Dictionary<int, DetailElemensUnderCreationSemaphore> _creationObligationDictionary = new Dictionary<int, DetailElemensUnderCreationSemaphore>();
        private int _lastObligationId = 0;

        public InMemoryAssetsCache(ILevel2AssetsCache<TQuery, TAsset> level2Cache)
        {
            _level2Cache = level2Cache;
        }

        public Task InitializeAsync()
        {
            return _level2Cache.InitializeAsync();
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

                var elementAfterWaiting = await detailElementAfterWaiting;
                Preconditions.Assert(elementAfterWaiting != null,"E65 after waiting at semaphore returned element is still null");
                return new CacheQueryOutput<TAsset>()
                {
                    CreationObligationToken = null,
                    Asset = elementAfterWaiting
                };
            }

            var  detailElement = _level2Cache.TryRetrive(query);
            if ( detailElement != null)
            {
                var awaitedElement = await  detailElement;
                if (awaitedElement != null)
                {
                    Preconditions.Assert(awaitedElement != null, "E66 Ever after waiting at for level2, element is still null");
                    return new CacheQueryOutput<TAsset>()
                    {
                        CreationObligationToken = null,
                        Asset = awaitedElement
                    };
                }
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

        public async Task<bool> AddAssetAsync(int creationObligationToken, TQuery query, TAsset asset)
        {
            var wasAddedToCache = await _level2Cache.AddAsset( query, asset); 

            // TODO different behaviour should be done if element was not added to cache( cache was full)
            var semaphore = _creationObligationDictionary[creationObligationToken];
            _creationObligationDictionary.Remove(creationObligationToken);
            semaphore.Semaphore.Set();
            return wasAddedToCache;
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

    public interface IFromQueryFilenameProvider
    {
        string ProvideFilename();
    }

    public interface ILevel2AssetsCache<TQuery, TAsset> where TQuery : IFromQueryFilenameProvider where TAsset : class
    {
        Task InitializeAsync();
        bool IsInCache(TQuery query);
        Task<TAsset> TryRetrive(TQuery queryArea);
        Task<bool> AddAsset( TQuery query, TAsset asset);
        Task RemoveAssetElementAsync(TQuery queryArea);
    }

    public class TwoStorageOverseeingLevel2Cache<TQuery, TAsset> : ILevel2AssetsCache<TQuery, TAsset> where TQuery : IFromQueryFilenameProvider where TAsset : class
    {
        private InFilesAssetsCache<TQuery, TAsset> _inFilesCache;
        private InMemoryAssetsLevel2Cache<TQuery,TAsset> _inMemoryCache;
        private bool _saveToFiles;

        public TwoStorageOverseeingLevel2Cache(InFilesAssetsCache<TQuery, TAsset> inFilesCache, InMemoryAssetsLevel2Cache<TQuery, TAsset> inMemoryCache, bool saveToFiles)
        {
            _inFilesCache = inFilesCache;
            _inMemoryCache = inMemoryCache;
            _saveToFiles = saveToFiles;
        }

        public async Task InitializeAsync()
        {
            await _inFilesCache.InitializeAsync();
            await _inMemoryCache.InitializeAsync();
        }

        public bool IsInCache(TQuery query)
        {
            return _inFilesCache.IsInCache(query) || _inMemoryCache.IsInCache(query);
        }

        public async Task<TAsset> TryRetrive(TQuery queryArea)
        {
            if (_inMemoryCache.IsInCache(queryArea))
            {
                return await _inMemoryCache.TryRetrive(queryArea);
            }
            else
            {
                if (_inFilesCache.IsInCache(queryArea))
                {
                    var found = await _inFilesCache.TryRetrive(queryArea);
                    await _inMemoryCache.AddAsset(queryArea, found);
                    return found;
                }
            }

            return null;
        }

        public async Task<bool> AddAsset( TQuery query,  TAsset asset)
        {
            var addedToMemoryCache = await _inMemoryCache.AddAsset(query, asset);
            if (!_inFilesCache.IsInCache(query) && _saveToFiles)
            {
                await _inFilesCache.AddAsset(query, asset);
            }

            return addedToMemoryCache;
        }

        public Task RemoveAssetElementAsync(TQuery queryArea)
        {
            return _inMemoryCache.RemoveAssetElementAsync(queryArea);
        }
    }

    public class InMemoryAssetsLevel2Cache<TQuery, TAsset > : ILevel2AssetsCache<TQuery, TAsset> where TAsset : class where TQuery : IFromQueryFilenameProvider
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

        public Task InitializeAsync()
        {
            return TaskUtils.EmptyCompleted();
        }

        public bool IsInCache(TQuery query)
        {
            return TryRetriveAssetFromTree(query) != null;
        }

        public Task<TAsset> TryRetrive(TQuery queryArea)
        {
            LogUsedMemory();
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
                return TaskUtils.MyFromResult(foundElement.Element);
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


        public async Task<bool> AddAsset(TQuery query, TAsset asset)
        {
            LogUsedMemory();
            var activeTreeElement = TryRetriveAssetFromTree(query);
            Preconditions.Assert(activeTreeElement == null,
                "There arleady is one detailElement of given description: qa: " + query);

            var newElement = new ReferenceCountedAsset()
            {
                Element = asset,
                ReferenceCount = 1
            };
            await ClearNonReferencedElements();
            if (ThereIsPlaceForNewAsset(asset))
            {
                AddElementToActiveTree(query, newElement);
                return true;
            }
            else
            {
                return false;
            }

        }

        private void AddElementToActiveTree(TQuery queryArea, ReferenceCountedAsset newElement)
        {
            _activeTree[queryArea] = newElement;
            _elementsLength += _entityActionsPerformer.CalculateMemoryUsage(newElement.Element);
        }

        public async Task RemoveAssetElementAsync(TQuery queryArea)
        {
            LogUsedMemory();
            var foundElement = TryRetriveAssetFromTree(queryArea);
            Preconditions.Assert(foundElement != null, "There is no element of given description");
            foundElement.ReferenceCount--;
            if (foundElement.ReferenceCount <= 0)
            {
                if (ThereIsPlaceForNewAsset(foundElement.Element))
                {
                    _nonReferencedElementsList.Add(queryArea);
                    //Debug.Log($"T97 min ref count. Adding to non-ref list. CurrentSize {_elementsLength} max size {_configuration.MaxTextureMemoryUsed}.");
                }
                else
                {
                    //Debug.Log($"T98 min ref count. CurrentSize {_elementsLength} max size {_configuration.MaxTextureMemoryUsed}. Removing");
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

        private void LogUsedMemory()
        {
            var usedPercent = (_elementsLength / ((float) _configuration.MaxTextureMemoryUsed))*100;
            //Debug.Log($"T84 Memory used: {usedPercent}%");
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

    public class TokenizedAsset<TQuery, TAsset>
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