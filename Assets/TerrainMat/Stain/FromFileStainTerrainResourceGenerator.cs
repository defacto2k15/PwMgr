using System.Threading.Tasks;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;

namespace Assets.TerrainMat.Stain
{
    public class FromFileStainTerrainResourceGenerator : IStainTerrainResourceGenerator
    {
        private readonly string _path;
        private StainTerrainResourceFileManager _stainTerrainResourceFileManager;

        public FromFileStainTerrainResourceGenerator(string path, CommonExecutorUTProxy commonExecutor)
        {
            _path = path;
            _stainTerrainResourceFileManager = new StainTerrainResourceFileManager(path, commonExecutor);
        }

        public Task<StainTerrainResource> GenerateTerrainTextureDataAsync()
        {
            return _stainTerrainResourceFileManager.LoadResources();
        }
    }
}