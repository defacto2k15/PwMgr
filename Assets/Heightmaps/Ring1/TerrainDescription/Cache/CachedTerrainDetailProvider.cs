using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Caching;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.Utils;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription.Cache
{
    public class CachedTerrainDetailProvider 
    {
        private TerrainDetailProvider _terrainDetailProvider;
        private Dictionary<CornersMergeStatus, Dictionary<TerrainDescriptionElementTypeEnum, IAssetsCache<IntRectangle, TextureWithSize>>> _memoryTerrainCaches;

        public CachedTerrainDetailProvider(TerrainDetailProvider provider, Func<IAssetsCache<IntRectangle, TextureWithSize>> terrainCacheGenerator)
        {
            _memoryTerrainCaches = EnumUtils.GetValues<CornersMergeStatus>().ToDictionary(
                mergeStatus => mergeStatus,
                mergeStatus => EnumUtils.GetValues<TerrainDescriptionElementTypeEnum>().ToDictionary(type => type, type => terrainCacheGenerator())
            );

            _terrainDetailProvider = provider;
        }

        public Task RemoveTerrainDetailElementAsync(TerrainDetailElementToken token)
        {
            return _memoryTerrainCaches[token.CornersMergeStatus][token.Type].RemoveAssetAsync(GenerateQuantisizedQueryRectangle(token.QueryArea));
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
            Func<MyRectangle, TerrainCardinalResolution, CornersMergeStatus, Task<TerrainDetailElement>> detailElementGenerator 
            )
        {
            CornersMergeStatus statusWeTarget;
            if (requiredMerge == RequiredCornersMergeStatus.NOT_IMPORTANT)
            {
                if (_memoryTerrainCaches[CornersMergeStatus.MERGED][elementType].IsInCache(GenerateQuantisizedQueryRectangle(alignedArea)))
                {
                    statusWeTarget = CornersMergeStatus.MERGED;
                }
                else
                {
                    statusWeTarget = CornersMergeStatus.NOT_MERGED;
                }
            }
            else if (requiredMerge == RequiredCornersMergeStatus.NOT_MERGED)
            {
                statusWeTarget = CornersMergeStatus.NOT_MERGED;
            }
            else
            {
                statusWeTarget = CornersMergeStatus.MERGED;
            }

            var queryOutput = await _memoryTerrainCaches[statusWeTarget][elementType].TryRetriveAsync(GenerateQuantisizedQueryRectangle(alignedArea));

            if (queryOutput.Asset != null)
            {
                return new TokenizedTerrainDetailElement()
                {
                    DetailElement = new TerrainDetailElement()
                    {
                        Texture = queryOutput.Asset,
                        Resolution = resolution,
                        DetailArea = alignedArea,
                        CornersMergeStatus = statusWeTarget
                    },
                    Token = new TerrainDetailElementToken(alignedArea,resolution,elementType, statusWeTarget)
                };
            }
            else
            {
                var detailElement = await detailElementGenerator(alignedArea, resolution, statusWeTarget);
                var tokenizedElement = await _memoryTerrainCaches[statusWeTarget][elementType].AddAssetAsync(
                    queryOutput.CreationObligationToken.Value, GenerateQuantisizedQueryRectangle(alignedArea), detailElement.Texture);
                return new TokenizedTerrainDetailElement()
                {
                    DetailElement = new TerrainDetailElement()
                    {
                        Texture = tokenizedElement.Asset,
                        Resolution = resolution,
                        DetailArea = alignedArea,
                        CornersMergeStatus =  statusWeTarget
                    },
                    Token = new TerrainDetailElementToken(alignedArea,resolution,elementType, detailElement.CornersMergeStatus)
                };
            }
        }

        public async Task<TokenizedTerrainDetailElement> GenerateNormalDetailElementAsync(MyRectangle alignedArea,
            TerrainCardinalResolution resolution, RequiredCornersMergeStatus requiredMerge)
        {
            return await GenerateElementAsync(alignedArea, resolution, requiredMerge, 
                TerrainDescriptionElementTypeEnum.NORMAL_ARRAY, _terrainDetailProvider.GenerateNormalDetailElementAsync);
        }

        private IntRectangle GenerateQuantisizedQueryRectangle(MyRectangle rect)
        {
            var QueryRectangleQuantLength = 5; //TODO to configuration
            return new IntRectangle(
                Mathf.RoundToInt(rect.X /QueryRectangleQuantLength),
                Mathf.RoundToInt(rect.Y /QueryRectangleQuantLength),
                Mathf.RoundToInt(rect.Width /QueryRectangleQuantLength),
                Mathf.RoundToInt(rect.Height /QueryRectangleQuantLength)
            );
        }
    }


    public class TerrainDetailElementToken
    {
        private MyRectangle _queryArea;
        private TerrainCardinalResolution _resolution;
        private TerrainDescriptionElementTypeEnum _type;
        private CornersMergeStatus _mergeStatus;

        public TerrainDetailElementToken(MyRectangle queryArea, TerrainCardinalResolution resolution, TerrainDescriptionElementTypeEnum type, CornersMergeStatus mergeStatus)
        {
            _queryArea = queryArea;
            _resolution = resolution;
            _type = type;
            _mergeStatus = mergeStatus;
        }

        public MyRectangle QueryArea => _queryArea;
        public TerrainCardinalResolution Resolution => _resolution;
        public TerrainDescriptionElementTypeEnum Type =>_type;
        public CornersMergeStatus CornersMergeStatus => _mergeStatus;

        protected bool Equals(TerrainDetailElementToken other)
        {
            return Equals(_queryArea, other._queryArea) && Equals(_resolution, other._resolution) && _type == other._type && _mergeStatus == other._mergeStatus;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TerrainDetailElementToken) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (_queryArea != null ? _queryArea.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_resolution != null ? _resolution.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) _type;
                hashCode = (hashCode * 397) ^ (int) _mergeStatus;
                return hashCode;
            }
        }
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

    public class TokenizedTerrainDetailElement
    {
        public TerrainDetailElement DetailElement;
        public TerrainDetailElementToken Token;
    }

}
