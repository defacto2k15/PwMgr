using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Textures;

namespace Assets.Caching
{
    public class InFilesAssetsCache<TQuery, TAsset> : ILevel2AssetsCache<TQuery,TAsset> where TQuery : IFromQueryFilenameProvider where TAsset : class
    {
        private IAssetCachingFileManager<TQuery, TAsset> _fileManager;
        private List<string> _filesOnDisk;

        public InFilesAssetsCache(IAssetCachingFileManager<TQuery, TAsset> fileManager)
        {
            _fileManager = fileManager;
        }

        public async Task InitializeAsync()
        {
            _filesOnDisk = await _fileManager.RetriveAllAssetFilenamesAsync();
        }

        public bool IsInCache(TQuery query)
        {
            return _filesOnDisk.Contains(query.ProvideFilename());
        }

        public Task<TAsset> TryRetriveAsync(TQuery query)
        {
            var filename = query.ProvideFilename();
            if (!_filesOnDisk.Contains(filename))
            {
                return null;
            }

            return _fileManager.RetriveAssetAsync(filename, query);
        }

        public async Task<bool> AddAssetAsync( TQuery query, TAsset asset)
        {
            var filename = query.ProvideFilename();
            Preconditions.Assert(!_filesOnDisk.Contains(filename), "There is arleady file "+filename+" listed in cache");
            _filesOnDisk.Add(filename);
            await _fileManager.SaveAssetAsync(filename, query, asset);
            return true;
        }

        public Task RemoveAssetElementAsync(TQuery queryArea)
        {
            return TaskUtils.EmptyCompleted();
        }
    }

    public interface IAssetCachingFileManager<TQuery, TAsset>
    {
        Task<TAsset> RetriveAssetAsync(string filename, TQuery query);
        Task SaveAssetAsync(string filename, TQuery query, TAsset asset);
        Task<List<string>> RetriveAllAssetFilenamesAsync();
    }
}
