using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;

namespace Assets.Heightmaps.Ring1.TerrainDescription.Cache
{
    public class CachedTerrainDetailProvider 
    {
        private TerrainDetailProvider _terrainDetailProvider;
        private Dictionary<CornersMergeStatus, Dictionary<TerrainDescriptionElementTypeEnum, IAssetsCache>> _terrainCaches;

        public CachedTerrainDetailProvider(TerrainDetailProvider provider, Func<IAssetsCache> terrainCacheGenerator)
        {
            _terrainCaches = EnumUtils.GetValues<CornersMergeStatus>().ToDictionary(
                mergeStatus => mergeStatus,
                mergeStatus => EnumUtils.GetValues<TerrainDescriptionElementTypeEnum>().ToDictionary(type => type, type => terrainCacheGenerator())
            );

            _terrainDetailProvider = provider;
        }

        public Task RemoveTerrainDetailElementAsync(TerrainDetailElementToken token)
        {
            return _terrainCaches[token.CornersMergeStatus][token.Type].RemoveTerrainDetailElementAsync(token.InternalToken);
        }

        public async Task<TokenizedTerrainDetailElement> GenerateHeightDetailElementAsync(MyRectangle alignedArea,
            TerrainCardinalResolution resolution, RequiredCornersMergeStatus requiredMerge)
        {
            return await GenerateElementAsync(alignedArea, resolution, requiredMerge, TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY, _terrainDetailProvider.GenerateHeightDetailElementAsync);
       }

        private async Task<TokenizedTerrainDetailElement> GenerateElementAsync(
            MyRectangle alignedArea, TerrainCardinalResolution resolution,
            RequiredCornersMergeStatus requiredMerge,
            TerrainDescriptionElementTypeEnum elementType,
            Func<MyRectangle, TerrainCardinalResolution, RequiredCornersMergeStatus, Task<TerrainDetailElement>> detailElementGenerator 
            )
        {
            CornersMergeStatus statusWeTarget;
            if (requiredMerge == RequiredCornersMergeStatus.NOT_IMPORTANT)
            {
                if (_terrainCaches[CornersMergeStatus.MERGED][elementType].IsInCache(alignedArea, resolution, elementType))
                {
                    statusWeTarget = CornersMergeStatus.MERGED;
                }
                else
                {
                    statusWeTarget = CornersMergeStatus.NOT_MERGED;
                }
            }
            else
            {
                statusWeTarget = CornersMergeStatus.MERGED;
            }

            var queryOutput = await _terrainCaches[statusWeTarget][elementType].TryRetriveAsync(alignedArea, resolution, elementType);

            if (queryOutput.DetailElement != null)
            {
                return new TokenizedTerrainDetailElement()
                {
                    DetailElement = queryOutput.DetailElement.DetailElement,
                    Token = new TerrainDetailElementToken(queryOutput.DetailElement.Token, statusWeTarget)
                };
            }
            else
            {
                var detailElement = await detailElementGenerator(alignedArea, resolution, requiredMerge);
                var tokenizedElement = await _terrainCaches[detailElement.CornersMergeStatus][elementType].AddTerrainDetailElement(
                    queryOutput.CreationObligationToken.Value, detailElement.Texture, detailElement.DetailArea, resolution, elementType);
                return new TokenizedTerrainDetailElement()
                {
                    DetailElement = tokenizedElement.DetailElement,
                    Token = new TerrainDetailElementToken(tokenizedElement.Token, detailElement.CornersMergeStatus)
                };
            }
        }

        public async Task<TokenizedTerrainDetailElement> GenerateNormalDetailElementAsync(MyRectangle alignedArea,
            TerrainCardinalResolution resolution, RequiredCornersMergeStatus requiredMerge)
        {
            return await GenerateElementAsync(alignedArea, resolution, requiredMerge, 
                TerrainDescriptionElementTypeEnum.NORMAL_ARRAY, _terrainDetailProvider.GenerateNormalDetailElementAsync);
        }
    }

    public class TerrainDetailElementToken
    {
        private InternalTerrainDetailElementToken _token;
        private CornersMergeStatus _mergeStatus;

        public TerrainDetailElementToken(InternalTerrainDetailElementToken token, CornersMergeStatus mergeStatus)
        {
            _token = token;
            _mergeStatus = mergeStatus;
        }

        public MyRectangle QueryArea => _token.QueryArea;
        public TerrainCardinalResolution Resolution => _token.Resolution;
        public TerrainDescriptionElementTypeEnum Type => _token.Type;
        public CornersMergeStatus CornersMergeStatus => _mergeStatus;
        public CornersMergeStatus MergeStatus => _mergeStatus;

        public InternalTerrainDetailElementToken InternalToken => _token;

        protected bool Equals(TerrainDetailElementToken other)
        {
            return Equals(_token, other._token) && _mergeStatus == other._mergeStatus;
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
                return ((_token != null ? _token.GetHashCode() : 0) * 397) ^ (int) _mergeStatus;
            }
        }
    }
}
