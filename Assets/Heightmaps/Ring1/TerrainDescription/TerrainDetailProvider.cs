using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Erosion;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class TerrainDetailProvider
    {
        private TerrainDetailProviderConfiguration _configuration;
        private TerrainDetailFileManager _fileManager;
        private TerrainDetailGenerator _generator;
        private TerrainDetailCornerMerger _cornerMerger;
        private TerrainDetailAlignmentCalculator _alignmentCalculator;

        public TerrainDetailProvider(TerrainDetailProviderConfiguration configuration,
            TerrainDetailFileManager fileManager, TerrainDetailGenerator generator, TerrainDetailCornerMerger cornerMerger, TerrainDetailAlignmentCalculator alignmentCalculator)
        {
            _configuration = configuration;
            _fileManager = fileManager;
            _generator = generator;
            _cornerMerger = cornerMerger;
            _alignmentCalculator = alignmentCalculator;
        }

        public async Task<TerrainDetailElement> GenerateHeightDetailElementAsync(MyRectangle queryArea,
            TerrainCardinalResolution cardinalResolution, RequiredCornersMergeStatus requiredMerge)
        {
            Func<MyRectangle, TerrainCardinalResolution, TextureWithSize, Task<TextureWithSize>> cornerMergingFunc = null;
            if (_cornerMerger != null)
            {
                cornerMergingFunc = _cornerMerger.MergeHeightDetailCorners;
            }
            
            return await GenerateTerrainDetailElementAsync(queryArea, cardinalResolution, requiredMerge,
                _fileManager.TryRetriveHeightDetailElementAsync,
                _fileManager.SaveHeightDetailElementAsync,
                cornerMergingFunc,
                _generator.GenerateHeightDetailElementAsync
            );
        }

        private async Task<TerrainDetailElement> GenerateTerrainDetailElementAsync(
            MyRectangle queryArea, TerrainCardinalResolution cardinalResolution, RequiredCornersMergeStatus requiredMerge,
            Func<MyRectangle, TerrainCardinalResolution, Task<RetrivedTerrainDetailTexture>> retriveTextureFunc,
            Func<Texture, MyRectangle, TerrainCardinalResolution, CornersMergeStatus, Task> saveTextureFunc,
            Func<MyRectangle, TerrainCardinalResolution, TextureWithSize, Task<TextureWithSize>> cornerMergingFunc,
            Func<MyRectangle, TerrainCardinalResolution, RequiredCornersMergeStatus, Task<TextureWithSize>> generateElementFunc
            )
        {
            if (!_configuration.MergeTerrainDetail)
            {
                if (requiredMerge == RequiredCornersMergeStatus.MERGED)
                {
                    Preconditions.Fail("W915 Merging terrain in disabled and detail element is required merged");
                    requiredMerge = RequiredCornersMergeStatus.NOT_IMPORTANT;
                }
                
            }

            var newTerrainArea = _alignmentCalculator.ComputeAlignedTerrainArea(queryArea, cardinalResolution);
            RetrivedTerrainDetailTexture texture = null;
            CornersMergeStatus outCornersMergeStatus;
            if (_configuration.UseTextureLoadingFromDisk)
            {
                texture = await retriveTextureFunc(newTerrainArea, cardinalResolution);
            }

            TextureWithSize outTexture = null;
            if (texture != null)
            {
                if (requiredMerge == RequiredCornersMergeStatus.NOT_IMPORTANT)
                {
                    // we have texture, lets return it
                    outTexture = texture.TextureWithSize;
                    outCornersMergeStatus = texture.CornersMergeStatus;
                }
                else
                {
                    if (texture.CornersMergeStatus == CornersMergeStatus.MERGED)
                    {
                        // it is merged arleady, lets return
                        outTexture = texture.TextureWithSize;
                        outCornersMergeStatus = texture.CornersMergeStatus;
                    }
                    else
                    {
                        // we want it merged, but it is not merged. 
                        outTexture = await cornerMergingFunc(queryArea, cardinalResolution, texture.TextureWithSize);
                        outCornersMergeStatus = CornersMergeStatus.MERGED;
                        if (_configuration.UseTextureSavingToDisk)
                        {
                            await saveTextureFunc(outTexture.Texture, newTerrainArea,
                                cardinalResolution, CornersMergeStatus.MERGED);
                        }
                    }
                }
            }
            else
            {
                var baseTexture = await generateElementFunc(newTerrainArea, cardinalResolution, requiredMerge);
                if (requiredMerge == RequiredCornersMergeStatus.NOT_IMPORTANT)
                {
                    outTexture = baseTexture;
                    outCornersMergeStatus = CornersMergeStatus.NOT_MERGED;
                    if (_configuration.UseTextureSavingToDisk)
                    {
                        await saveTextureFunc(outTexture.Texture, newTerrainArea, cardinalResolution, CornersMergeStatus.NOT_MERGED);
                    }
                }
                else
                {
                    outTexture = await cornerMergingFunc(queryArea, cardinalResolution, baseTexture);
                    outCornersMergeStatus = CornersMergeStatus.MERGED;
                    if (_configuration.UseTextureSavingToDisk)
                    {
                        await saveTextureFunc(outTexture.Texture, newTerrainArea, cardinalResolution, CornersMergeStatus.MERGED);
                    }
                }
            }

            return new TerrainDetailElement()
            {
                DetailArea = newTerrainArea,
                Resolution = cardinalResolution,
                Texture = outTexture,
                CornersMergeStatus =  outCornersMergeStatus
            };
        }

        public async Task<TerrainDetailElement> GenerateNormalDetailElementAsync(MyRectangle queryArea,
            TerrainCardinalResolution cardinalResolution, RequiredCornersMergeStatus requiredMerge)
        {
            return await GenerateTerrainDetailElementAsync(queryArea, cardinalResolution, requiredMerge,
                _fileManager.TryRetriveNormalDetailElementAsync,
                _fileManager.SaveNormalDetailElementAsync,
                ((rectangle, resolution, textureWithSize) => TaskUtils.MyFromResult(textureWithSize)), // we do not have to merge normals - just generate normals from merged heightmap!
                _generator.GenerateNormalDetailElementAsync
            );
        }
    }

    public class TerrainDetailProviderConfiguration
    {
        public bool UseTextureSavingToDisk;
        public bool UseTextureLoadingFromDisk;
        public bool MergeTerrainDetail = false;
    }
}