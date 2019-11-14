using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2;
using Assets.Ring2.BaseEntities;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging
{
    public class TerrainDetailCornerMerger
    {
        private BaseTerrainDetailProvider _terrainDetailProvider;
        private readonly LateAssignFactory<BaseTerrainDetailProvider> _terrainDetailProviderFactory;
        private TerrainDetailAlignmentCalculator _alignmentCalculator;
        private UTTextureRendererProxy _renderer;
        private TextureConcieverUTProxy _textureConciever;
        private TerrainDetailCornerMergerConfiguration _configuration;

        private RenderTexture _scratchTexture;

        public TerrainDetailCornerMerger(LateAssignFactory<BaseTerrainDetailProvider> terrainDetailProviderFactory,
            TerrainDetailAlignmentCalculator alignmentCalculator, UTTextureRendererProxy renderer, TextureConcieverUTProxy textureConciever, TerrainDetailCornerMergerConfiguration configuration)
        {
            _terrainDetailProviderFactory = terrainDetailProviderFactory;
            _alignmentCalculator = alignmentCalculator;
            _renderer = renderer;
            _textureConciever = textureConciever;
            _configuration = configuration;
        }

        private void RetriveTerrainDetailProvider()
        {
            _terrainDetailProvider = _terrainDetailProviderFactory.Retrive();
        }

        public async Task<TextureWithSize> MergeHeightDetailCorners(MyRectangle queryArea,
            TerrainCardinalResolution cardinalResolution, TextureWithSize baseTexture)
        {
            RetriveTerrainDetailProvider();

            var griddedTerrainDetail = _alignmentCalculator.GetGriddedTerrainArea(queryArea, cardinalResolution);
            var sourceTerrainDetailTasks = new List<TerrainDetailNeighbourhoodDirections>()
            {
                TerrainDetailNeighbourhoodDirections.Left,
                TerrainDetailNeighbourhoodDirections.BottomLeft,
                TerrainDetailNeighbourhoodDirections.Bottom,
            }.Select(direction => new
            {
                direction,
                alignedPosition = _alignmentCalculator.GetAlignedTerrainArea(griddedTerrainDetail + direction.Movement, cardinalResolution)
            })
            .Where( p => _alignmentCalculator.AreaInMap(p.alignedPosition))
            .ToDictionary(
                p => p.direction,
                p => _terrainDetailProvider.RetriveTerrainDetailAsync(
                        TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY, p.alignedPosition, cardinalResolution, RequiredCornersMergeStatus.NOT_IMPORTANT)
            );

            var sourceTerrainDetails = new Dictionary<TerrainDetailNeighbourhoodDirections, TokenizedTerrainDetailElement>();
            foreach (var pair in sourceTerrainDetailTasks)
            {
                sourceTerrainDetails.Add(pair.Key, (await pair.Value).TokenizedElement);
            }

            var activeTerrainDetails = new TerrainDetailElement()
            {
                CornersMergeStatus = CornersMergeStatus.NOT_MERGED,
                DetailArea = new MyRectangle(0,0,1,1),
                Resolution = cardinalResolution,
                Texture = baseTexture
            };

            var outputTexture = await CreateOutputTexture(baseTexture);

            await MergeDetailObject(new Dictionary<TerrainDetailCorner, TerrainDetailElement>()
            {
                {TerrainDetailCorner.BottomLeft, activeTerrainDetails},
            }, baseTexture, outputTexture, TerrainDetailCorner.TopRight);

            if (sourceTerrainDetails.ContainsKey(TerrainDetailNeighbourhoodDirections.Left)) // do not merge if one of merging elements was out of map boundaries
            {
                await MergeDetailObject(new Dictionary<TerrainDetailCorner, TerrainDetailElement>()
                {
                    {TerrainDetailCorner.BottomRight, activeTerrainDetails},
                    {TerrainDetailCorner.BottomLeft, sourceTerrainDetails[TerrainDetailNeighbourhoodDirections.Left].DetailElement},
                }, baseTexture, outputTexture, TerrainDetailCorner.TopLeft);
            }


            if (sourceTerrainDetails.ContainsKey(TerrainDetailNeighbourhoodDirections.Left) &&
                sourceTerrainDetails.ContainsKey(TerrainDetailNeighbourhoodDirections.BottomLeft) &&
                sourceTerrainDetails.ContainsKey(TerrainDetailNeighbourhoodDirections.Bottom) ) 
            {
                await MergeDetailObject(new Dictionary<TerrainDetailCorner, TerrainDetailElement>()
                {
                    {TerrainDetailCorner.TopRight, activeTerrainDetails},
                    {TerrainDetailCorner.TopLeft, sourceTerrainDetails[TerrainDetailNeighbourhoodDirections.Left].DetailElement},
                    {TerrainDetailCorner.BottomLeft, sourceTerrainDetails[TerrainDetailNeighbourhoodDirections.BottomLeft].DetailElement},
                    {TerrainDetailCorner.BottomRight, sourceTerrainDetails[TerrainDetailNeighbourhoodDirections.Bottom].DetailElement},
                }, baseTexture, outputTexture, TerrainDetailCorner.BottomLeft);
            }

            if (sourceTerrainDetails.ContainsKey(TerrainDetailNeighbourhoodDirections.Bottom) )
            {
                await MergeDetailObject(new Dictionary<TerrainDetailCorner, TerrainDetailElement>()
                {
                    {TerrainDetailCorner.TopLeft, activeTerrainDetails},
                    {TerrainDetailCorner.BottomLeft, sourceTerrainDetails[TerrainDetailNeighbourhoodDirections.Bottom].DetailElement},
                }, baseTexture, outputTexture, TerrainDetailCorner.BottomRight);
            }

            await TaskUtils.WhenAll(sourceTerrainDetails.Values.Select(c => c.Token)
                .Select(c => _terrainDetailProvider.RemoveTerrainDetailAsync(c)));
            return outputTexture;
        }

        private async Task<TextureWithSize> CreateOutputTexture(TextureWithSize baseTexture)
        {
            var newTextureTemplate = new MyRenderTextureTemplate(baseTexture.Size.X, baseTexture.Size.Y, RenderTextureFormat.RFloat, true, FilterMode.Point);
            newTextureTemplate.wrapMode = TextureWrapMode.Clamp;
            newTextureTemplate.SourceTexture = baseTexture.Texture;
            var newTexture = await _textureConciever.ConcieveRenderTextureAsync(newTextureTemplate);
            return new TextureWithSize()
            {
                Size = baseTexture.Size,
                Texture = newTexture
            };
        }

        private async Task MergeDetailObject(Dictionary<TerrainDetailCorner, TerrainDetailElement> sourceTextures,
            TextureWithSize sourceTexture, TextureWithSize outTexture, TerrainDetailCorner activeCorner)
        {
            var scratchTextureSize = 121;
            if (_scratchTexture == null)
            {
                _scratchTexture = await _textureConciever.ConcieveRenderTextureAsync(new MyRenderTextureTemplate(scratchTextureSize, scratchTextureSize, RenderTextureFormat.RFloat, false, FilterMode.Point));
            }

            var uniforms = new UniformsPack();
            var cornersPresent = new Vector4();
            var cornersMerged = new Vector4();
            int activeCornerIndex = 0;

            var i = 0;
            foreach (var corner in TerrainDetailCorner.OrderedDirections)
            {
                if (corner == activeCorner)
                {
                    activeCornerIndex = i;
                }
                if (sourceTextures.ContainsKey(corner))
                {
                    cornersPresent[i] = 10;
                    if (sourceTextures[corner].CornersMergeStatus == CornersMergeStatus.MERGED)
                    {
                        cornersMerged[i] = 10;
                    }
                    uniforms.SetTexture("_Corner" + GetCornerTexUniformName(corner)+"Tex", sourceTextures[corner].Texture.Texture);
                }
                i++;
            }

            uniforms.SetUniform("_ActiveCornerIndex", activeCornerIndex);
            uniforms.SetUniform("_CornersMerged", cornersMerged);
            uniforms.SetTexture("_ScratchTex", _scratchTexture);
            uniforms.SetUniform("_MergeMargin", _configuration.MergeMarginSize); // todo

            await _renderer.AddOrder(new TextureRenderingTemplate()
            {
                CanMultistep = false,
                CreateTexture2D = false,
                RenderTextureToModify = _scratchTexture,
                ShaderName = "Custom/TerrainDetailMerger/MergeIntoScratch",
                UniformPack = uniforms,
                RenderingRectangle = new IntRectangle(0, 0, scratchTextureSize, scratchTextureSize),
                RenderTargetSize = new IntVector2(scratchTextureSize, scratchTextureSize),
            });


            IntRectangle renderingRectangle;
            if (activeCorner == TerrainDetailCorner.BottomLeft)
            {
                renderingRectangle = new IntRectangle(0,0,121,121);
            }
            else if (activeCorner == TerrainDetailCorner.BottomRight)
            {
                renderingRectangle = new IntRectangle(120,0,121,121);
            }
            else if (activeCorner == TerrainDetailCorner.TopLeft)
            {
                renderingRectangle = new IntRectangle(0,120,121,121);
            }
            else if (activeCorner == TerrainDetailCorner.TopRight)
            {
                renderingRectangle = new IntRectangle(120, 120, 121, 121);
            }
            else
            {
                Preconditions.Fail("Unsupported activeCorner "+activeCorner);
                renderingRectangle = new IntRectangle(0,0,1,1);
            }

            await _renderer.AddOrder(new TextureRenderingTemplate()
            {
                CanMultistep = false,
                CreateTexture2D = false,
                RenderTextureToModify = (RenderTexture)outTexture.Texture,
                ShaderName = "Custom/TerrainDetailMerger/ScratchToActive",
                UniformPack = uniforms,
                RenderingRectangle = renderingRectangle,
                RenderTargetSize = new IntVector2(241, 241),
            });
        }

        private string GetCornerTexUniformName(TerrainDetailCorner corner)
        {
            if (corner == TerrainDetailCorner.BottomLeft)
            {
                return "BottomLeft";
            }else
            if (corner == TerrainDetailCorner.BottomRight)
            {
                return "BottomRight";
            }else if (corner == TerrainDetailCorner.TopLeft)
            {
                return "TopLeft";
            }else if (corner == TerrainDetailCorner.TopRight)
            {
                return "TopRight";
            }
            else
            {
                Preconditions.Fail("Unsupported corner "+corner);
                return null;
            }
        }
    }

    public class TerrainDetailCornerMergerConfiguration
    {
        public float MergeMarginSize=0.2f;

    }
}