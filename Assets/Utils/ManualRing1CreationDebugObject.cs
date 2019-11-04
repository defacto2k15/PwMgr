using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.FinalExecution;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.PreComputation;
using Assets.PreComputation.Configurations;
using Assets.Repositioning;
using Assets.Roads.Pathfinding;
using Assets.TerrainMat;
using Assets.TerrainMat.Stain;
using UnityEngine;

namespace Assets.Utils
{
    public class ManualRing1CreationDebugObject : MonoBehaviour
    {
        public void Start()
        {
            //SkrewTexture2(@"C:\inz\colorPlay\geoHand1.png", @"C:\inz\colorPlay\skew_geoHand2.png");
            SkrewTexture2(@"C:\inz\colorPlay\geoHand2.png", @"C:\inz\colorPlay\skew_geoHand2.png");

            //SkrewTexture2(@"C:\inz\colorPlay\geo2.png", @"C:\inz\colorPlay\skew_geo2.png");
            //ReadCoordinates2();
        }

        public void ReadCoordinates()
        {
            var configuration = new PrecomputationConfiguration(new FEConfiguration(new FilePathsConfiguration() ),
                new FilePathsConfiguration());

            var generationCoords = configuration.StainTerrainProviderConfiguration.StainTerrainCoords;

            var geoTrans = GeoCoordsToUnityTranslator.DefaultTranslator;

            var coord1 = geoTrans.TranslateToGeo(generationCoords.DownLeftPoint);
            var coord2 = geoTrans.TranslateToGeo(generationCoords.TopRightPoint);

            Debug.Log("T58 Obszar do generacji to " + coord1 + "   " + coord2);
        }

        public void ReadCoordinates2()
        {
            var repo = Repositioner.Default.InvMove(new Vector2(360, 135));

            Debug.Log("T58 Obszar centralny to " + repo);
        }

        public void SkrewTexture(string inputPath, string outputPath)
        {
            var textureSize = new IntVector2(1024, 1024);
            var inputTexture = SavingFileManager.LoadPngTextureFromFile(inputPath, textureSize.X, textureSize.Y,
                TextureFormat.ARGB32, true, false);

            var skewer = new Ring1CoordinatesSkewer(0.1f);

            var newTexture = new Texture2D(textureSize.X, textureSize.Y, TextureFormat.ARGB32, false, false);
            for (int x = 0; x < textureSize.X; x++)
            {
                for (int y = 0; y < textureSize.Y; y++)
                {
                    var oldColor = inputTexture.GetPixel(x, y);
                    var floatTextureSize = new MyRectangle(0, 0, textureSize.X, textureSize.Y);
                    var uvCoords = RectangleUtils.CalculateSubelementUv(floatTextureSize, new Vector2(x, y));

                    var skewedUv = skewer.SkewPoint(uvCoords);
                    var newPosition = RectangleUtils.CalculateSubPosition(floatTextureSize, skewedUv).ToIntVector();

                    newTexture.SetPixel(newPosition.X, newPosition.Y, oldColor);
                }
            }
        }

        public void SkrewTexture2(string inputPath, string outputPath)
        {
            var textureSize = new IntVector2(1024, 1024);
            var inputTexture = SavingFileManager.LoadPngTextureFromFile(inputPath, textureSize.X, textureSize.Y,
                TextureFormat.ARGB32, true, false);

            var skewer = new Ring1CoordinatesSkewer(0.1f);

            var newTexture = new Texture2D(textureSize.X, textureSize.Y, TextureFormat.ARGB32, false, false);
            for (int x = 0; x < textureSize.X; x++)
            {
                for (int y = 0; y < textureSize.Y; y++)
                {
                    var floatTextureSize = new MyRectangle(0, 0, textureSize.X, textureSize.Y);
                    var uvCoords = RectangleUtils.CalculateSubelementUv(floatTextureSize, new Vector2(x, y));

                    var unSkewedUv = skewer.SkewPoint(uvCoords);

                    var newPosition = RectangleUtils.CalculateSubPosition(floatTextureSize, unSkewedUv).ToIntVector();
                    var oldColor = inputTexture.GetPixel(newPosition.X, newPosition.Y);

                    newTexture.SetPixel(x, y, oldColor);
                }
            }

            newTexture.Apply();
            SavingFileManager.SaveTextureToPngFile(outputPath, newTexture);
        }
    }
}