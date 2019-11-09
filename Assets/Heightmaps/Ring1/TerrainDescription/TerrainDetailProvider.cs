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
        private TerrainDetailGenerator _generator;
        private TerrainDetailCornerMerger _cornerMerger;
        private TerrainDetailAlignmentCalculator _alignmentCalculator;

        public TerrainDetailProvider(
             TerrainDetailGenerator generator, TerrainDetailCornerMerger cornerMerger, TerrainDetailAlignmentCalculator alignmentCalculator)
        {
            _generator = generator;
            _cornerMerger = cornerMerger;
            _alignmentCalculator = alignmentCalculator;
        }

        public async Task<TerrainDetailElement> GenerateHeightDetailElementAsync(MyRectangle queryArea,
            TerrainCardinalResolution cardinalResolution, CornersMergeStatus requiredMerge)
        {
            Func<MyRectangle, TerrainCardinalResolution, TextureWithSize, Task<TextureWithSize>> cornerMergingFunc = null;
            if (_cornerMerger != null)
            {
                cornerMergingFunc = _cornerMerger.MergeHeightDetailCorners;
            }
            
            return await GenerateTerrainDetailElementAsync(queryArea, cardinalResolution, requiredMerge,
                cornerMergingFunc,
                _generator.GenerateHeightDetailElementAsync
            );
        }

        private async Task<TerrainDetailElement> GenerateTerrainDetailElementAsync(
            MyRectangle queryArea, TerrainCardinalResolution cardinalResolution, CornersMergeStatus requiredMerge,
            Func<MyRectangle, TerrainCardinalResolution, TextureWithSize, Task<TextureWithSize>> cornerMergingFunc,
            Func<MyRectangle, TerrainCardinalResolution, RequiredCornersMergeStatus, Task<TextureWithSize>> generateElementFunc
            )
        {

            var newTerrainArea = _alignmentCalculator.ComputeAlignedTerrainArea(queryArea, cardinalResolution);
            RetrivedTerrainDetailTexture texture = null;
            CornersMergeStatus outCornersMergeStatus;

            TextureWithSize outTexture = null;
            
                var baseTexture = await generateElementFunc(newTerrainArea, cardinalResolution, RequiredCornersMergeStatus.NOT_MERGED);
                if (requiredMerge == CornersMergeStatus.NOT_MERGED)
                {
                    outTexture = baseTexture;
                    outCornersMergeStatus = CornersMergeStatus.NOT_MERGED;
                }
                else
                {
                    outTexture = await cornerMergingFunc(queryArea, cardinalResolution, baseTexture);
                    outCornersMergeStatus = CornersMergeStatus.MERGED;
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
            TerrainCardinalResolution cardinalResolution, CornersMergeStatus requiredMerge)
        {
            return await GenerateTerrainDetailElementAsync(queryArea, cardinalResolution, requiredMerge,
                ((rectangle, resolution, textureWithSize) => TaskUtils.MyFromResult(textureWithSize)), // we do not have to merge normals - just generate normals from merged heightmap!
                _generator.GenerateNormalDetailElementAsync
            );
        }
    }

}