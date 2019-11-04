using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Preparment.Simplyfying;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    public class HeightmapBundleGenerator
    {
        private HeightmapSimplyfyer simplyfyer = new HeightmapSimplyfyer();
        private NormalArrayGenerator _normalArrayGenerator = new NormalArrayGenerator();

        public HeightmapBundle GenerateBundle(HeightmapArray inputArray, int mipCount)
        {
            List<OneLevelHeightmapPack> packList = new List<OneLevelHeightmapPack>();

            var timer = new Stopwatch();

            timer.Start();
            packList.Add(new OneLevelHeightmapPack(
                heightmapArray: inputArray,
                heightmapTexture: HeightmapUtils.CreateTextureFromHeightmap(inputArray),
                normalTexture: GenerateNormalTexture(inputArray)));
            timer.Stop();
            UnityEngine.Debug.Log("T78 executed bundle " + 1 + " took " + timer.ElapsedMilliseconds);
            timer.Reset();

            for (int i = 1; i < mipCount; i++)
            {
                timer.Start();

                var divisor = ((int) Mathf.Pow(2, i));
                var currentWidth = inputArray.Width / divisor;
                var currentHeight = inputArray.Height / divisor;
                var simplifiedMap = simplyfyer.SimplyfyHeightmapNoMargins(inputArray, currentWidth, currentHeight);
                var heightmapAsTexture = HeightmapUtils.CreateTextureFromHeightmap(simplifiedMap);
                var normalTexture = GenerateNormalTexture(simplifiedMap);
                packList.Add(new OneLevelHeightmapPack(simplifiedMap, heightmapAsTexture, normalTexture));

                timer.Stop();
                UnityEngine.Debug.Log("T78 executed bundle " + i + " took " + timer.ElapsedMilliseconds);
                timer.Reset();
            }

            return new HeightmapBundle(packList, inputArray.Width);
        }

        private Texture2D GenerateNormalTexture(HeightmapArray array)
        {
            var normalArray = _normalArrayGenerator.GenerateNormalArray(array, 10f);
            var normalAsTexture = NormalUtils.CreateTextureFromNormalArray(normalArray);
            return normalAsTexture;
        }
    }
}