using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
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
using OsmSharp.Osm.Data.Core.API;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class TerrainShapeDb
    {
        private TerrainDetailAlignmentCalculator _alignmentCalculator;
        private CachedTerrainDetailProvider _cachedTerrainDetailProvider;

        public TerrainShapeDb(CachedTerrainDetailProvider cachedTerrainDetailProvider, TerrainDetailAlignmentCalculator alignmentCalculator)
        {
            _cachedTerrainDetailProvider = cachedTerrainDetailProvider;
            _alignmentCalculator = alignmentCalculator;
        }

        public async Task<TerrainDescriptionOutput> QueryAsync(TerrainDescriptionQuery query)
        {
            Dictionary<TerrainDescriptionElementTypeEnum, TerrainDetailElementOutput> elementsDict =
                new Dictionary<TerrainDescriptionElementTypeEnum, TerrainDetailElementOutput>();
            var queryArea = query.QueryArea;
            foreach (var elementDetail in query.RequestedElementDetails)
            {
                elementDetail.Resolution = TerrainCardinalResolution.ToSingletonResolution(elementDetail.Resolution);
                AssertResolutionIsCompilant(queryArea, elementDetail.Resolution);
                var type = elementDetail.Type;
                TerrainDetailElementOutput element = null;
                if (type == TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY)
                {
                    element = await RetriveHeightArrayAsync(queryArea, elementDetail.Resolution, elementDetail.RequiredMergeStatus);
            Debug.Log("E651: " + query.QueryArea+ $" and outUV is {element.UvBase}");
                }
                else if (type == TerrainDescriptionElementTypeEnum.NORMAL_ARRAY)
                {
                    element = await RetriveNormalArrayAsync(queryArea, elementDetail.Resolution, elementDetail.RequiredMergeStatus);
                }
                else
                {
                    Preconditions.Fail("Tesselation map unsupported!!!");
                }
                elementsDict[type] = element;
            }

            return new TerrainDescriptionOutput(elementsDict);
        }

        public Task RemoveTerrainDetailElementAsync(TerrainDetailElementToken token)
        {
            return _cachedTerrainDetailProvider.RemoveTerrainDetailElementAsync(token);
        }

        private void AssertResolutionIsCompilant(MyRectangle queryArea,
            TerrainCardinalResolution elementDetailResolution)
        {
            _alignmentCalculator.AssertResolutionIsCompilant(queryArea, elementDetailResolution);
        }

        private async Task<TerrainDetailElementOutput> RetriveHeightArrayAsync(MyRectangle queryArea, TerrainCardinalResolution resolution, RequiredCornersMergeStatus requiredMerge)
        {
            var alignedArea = _alignmentCalculator.ComputeAlignedTerrainArea(queryArea, resolution);
            return GenerateOutput( await _cachedTerrainDetailProvider.GenerateHeightDetailElementAsync(alignedArea, resolution, requiredMerge), queryArea);
        }

        private async Task<TerrainDetailElementOutput> RetriveNormalArrayAsync(MyRectangle queryArea, TerrainCardinalResolution resolution, RequiredCornersMergeStatus requiredMerge) //todo
        {
            var alignedArea = _alignmentCalculator.ComputeAlignedTerrainArea(queryArea, resolution);
            return GenerateOutput( await _cachedTerrainDetailProvider.GenerateNormalDetailElementAsync(alignedArea, resolution, requiredMerge), queryArea);
        }

        private TerrainDetailElementOutput GenerateOutput(TokenizedTerrainDetailElement element,
            MyRectangle queryArea)
        {
            var elementArea = element.DetailElement.DetailArea;
            var uvBase = TerrainShapeUtils.ComputeUvOfSubElement(queryArea, elementArea);

            return new TerrainDetailElementOutput()
            {
                UvBase = uvBase,
                TokenizedElement = element
            };
        }
    }

    public class TerrainDetailElement
    {
        public TextureWithSize Texture;
        public MyRectangle DetailArea;
        public TerrainCardinalResolution Resolution;
        public CornersMergeStatus CornersMergeStatus;
    }

    public class TerrainDetailElementOutput
    {
        public TokenizedTerrainDetailElement TokenizedElement;
        public MyRectangle UvBase;
    }


    public class TerrainDescriptionOutput
    {
        private Dictionary<TerrainDescriptionElementTypeEnum, TerrainDetailElementOutput> _output;

        public TerrainDescriptionOutput(
            Dictionary<TerrainDescriptionElementTypeEnum, TerrainDetailElementOutput> output)
        {
            _output = output;
        }

        public TerrainDetailElementOutput GetElementOfType(TerrainDescriptionElementTypeEnum type)
        {
            return _output[type];
        }

        public bool HasElementOfType(TerrainDescriptionElementTypeEnum type)
        {
            return _output.ContainsKey(type);
        }
    }
}