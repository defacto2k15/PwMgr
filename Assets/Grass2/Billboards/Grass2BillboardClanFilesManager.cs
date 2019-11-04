using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass2.Billboards
{
    public class Grass2BillboardClanFilesManager
    {
        public Grass2SingledBillboardClan Load(string rootPath, IntVector2 billboardImageSize)
        {
            var pathGenerator = new Grass2BillboardClanFilesManagerPathGenerator(rootPath);
            var infoFile =
                JsonUtility.FromJson<Grass2BillboardInfoJson>(File.ReadAllText(pathGenerator.CreateInfoFilePath()));

            var billboardsList = new List<DetailedGrass2SingleBillboard>();

            int i = 0;
            foreach (var bladesCount in infoFile.BladesCountList)
            {
                var tex = SavingFileManager.LoadPngTextureFromFile(pathGenerator.CreateBillboardFilePath(i),
                    billboardImageSize.X, billboardImageSize.Y, TextureFormat.ARGB32, true, true);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;

                billboardsList.Add(new DetailedGrass2SingleBillboard()
                {
                    BladesCount = bladesCount,
                    Texture = tex
                });
                i++;
            }

            return new Grass2SingledBillboardClan()
            {
                BillboardsList = billboardsList
            };
        }

        public void Save(string rootPath, Grass2SingledBillboardClan clan)
        {
            var billboardsList = clan.BillboardsList;
            var pathGenerator = new Grass2BillboardClanFilesManagerPathGenerator(rootPath);
            var infoFile = new Grass2BillboardInfoJson()
            {
                BladesCountList = billboardsList.Select(c => c.BladesCount).ToList()
            };
            File.WriteAllText(pathGenerator.CreateInfoFilePath(), JsonUtility.ToJson(infoFile));

            int i = 0;
            foreach (var billboard in billboardsList)
            {
                SavingFileManager.SaveTextureToPngFile(pathGenerator.CreateBillboardFilePath(i), billboard.Texture);
                i++;
            }
        }

        private class Grass2BillboardClanFilesManagerPathGenerator
        {
            private string _rootPath;

            public Grass2BillboardClanFilesManagerPathGenerator(string rootPath)
            {
                _rootPath = rootPath;
            }

            public string CreateInfoFilePath()
            {
                return _rootPath + "info.json";
            }

            public string CreateBillboardFilePath(int index)
            {
                return $"{_rootPath}billboard-{index}.png";
            }
        }

        [Serializable]
        public class Grass2BillboardInfoJson
        {
            public List<int> BladesCountList;
        }
    }
}