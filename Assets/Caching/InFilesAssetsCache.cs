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
    public class InFilesAssetsCache : ILevel2AssetsCache<InternalTerrainDetailElementToken, TextureWithSize>  
    {
        private TerrainDetailFileManager _fileManager;
        private List<string> _filesOnDisk;

        public InFilesAssetsCache(TerrainDetailFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public async Task InitializeAsync()
        {
            _filesOnDisk = await _fileManager.RetriveAllTerrainDetailFilesListAsync();
        }

        public bool IsInCache(InternalTerrainDetailElementToken queryRect)
        {
            return _filesOnDisk.Contains(queryRect.ProvideFilename());
        }

        public async Task<TextureWithSize> TryRetrive(InternalTerrainDetailElementToken queryArea)
        {
            var filename = queryArea.ProvideFilename();
            if (!_filesOnDisk.Contains(filename))
            {
                return null;
            }
            if (queryArea.Type == TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY)
            {
                return (await _fileManager.RetriveHeightDetailElementAsync(filename));
            }else if (queryArea.Type == TerrainDescriptionElementTypeEnum.NORMAL_ARRAY)
            {
                return (await _fileManager.RetriveNormalDetailElementAsync(filename));
            }
            else
            {
                Preconditions.Fail("Not supported detailelement type "+queryArea.Type);
                return (null);
            }
        }

        public Task AddAsset( InternalTerrainDetailElementToken queryArea, TextureWithSize asset)
        {
            var filename = queryArea.ProvideFilename();
            Preconditions.Assert(!_filesOnDisk.Contains(filename), "There is arleady file "+filename+" listed in cache");
            _filesOnDisk.Add(filename);
            if (queryArea.Type == TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY)
            {
                return (_fileManager.SaveHeightDetailElementAsync(filename, asset));
            }else if (queryArea.Type == TerrainDescriptionElementTypeEnum.NORMAL_ARRAY)
            {
                return (_fileManager.SaveNormalDetailElementAsync(filename, asset));
            }
            else
            {
                Preconditions.Fail("Not supported detailelement type "+queryArea.Type);
                return (null);
            }
        }

        public Task RemoveAssetElementAsync(InternalTerrainDetailElementToken queryArea)
        {
            return TaskUtils.EmptyCompleted();
        }
    }
}
