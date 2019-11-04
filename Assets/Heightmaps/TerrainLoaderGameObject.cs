using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Assets;
using Assets.Heightmaps.Preparment;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.Ring1HeightArrayModifier;
using Assets.Heightmaps.submaps;
using Assets.MeshGeneration;
using UnityEngine;

namespace Assets.Heightmaps
{
    public class TerrainLoaderGameObject : MonoBehaviour
    {
        private Ring1Tree _ring1Tree;
        public Camera _camera;

        // Use this for initialization
        void Start()
        {
            string bilFilePath = @"C:\n49_e019_1arc_v3.bil";
            HeightmapFile heightmapFile = new HeightmapFile();
            const int filePixelWidth = 3601;
            //heightmapFile.LoadFile(bilFilePath, 3601);
            //heightmapFile.MirrorReflectHeightDataInXAxis();

            MyTotalHeightmap totalHeightmap = new MyTotalHeightmap();

            //const int subTerrainCount = 14;
            int minSubmapWidth = 256; //(int)Math.Floor((double)filePixelWidth/subTerrainCount)-1 ;
            List<SubmapPreparmentOrder> preparmentOrders = new List<SubmapPreparmentOrder>
            {
                newPreparmentOrder(0, 0, 4, 4, minSubmapWidth, 6, 0),
                newPreparmentOrder(0, 4, 4, 6, minSubmapWidth, 7, 0),
                newPreparmentOrder(4, 0, 6, 4, minSubmapWidth, 5, 0),
                newPreparmentOrder(0, 10, 4, 4, minSubmapWidth, 4, 0),

                newPreparmentOrder(4, 4, 2, 2, minSubmapWidth, 2 + 4, 0),
                newPreparmentOrder(4, 6, 2, 2, minSubmapWidth, 2 + 4, 0),

                newPreparmentOrder(4, 8, 2, 2, minSubmapWidth, 2 + 4, 0),
                newPreparmentOrder(6, 4, 2, 2, minSubmapWidth, 2 + 4, 0),
                newPreparmentOrder(6, 8, 2, 2, minSubmapWidth, 3, 0),
                newPreparmentOrder(8, 4, 2, 2, minSubmapWidth, 2 + 4, 0),

                newPreparmentOrder(8, 6, 2, 2, minSubmapWidth, 2 + 4, 0),

                //newPreparmentOrder(8,8,2,2,minSubmapWidth, 2+4, 1), was lithe this
                newPreparmentOrder(8, 8, 1, 1, minSubmapWidth, 3, 0),
                newPreparmentOrder(9, 8, 1, 1, minSubmapWidth, 3, 0),
                newPreparmentOrder(9, 8, 1, 1, minSubmapWidth, 3, 0),
                newPreparmentOrder(9, 9, 1, 1, minSubmapWidth, 0, 1),


                newPreparmentOrder(6, 6, 2, 2, minSubmapWidth, 2 + 4, 0),
                newPreparmentOrder(10, 4, 4, 6, minSubmapWidth, 3 + 2, 0),

                newPreparmentOrder(10, 10, 4, 4, minSubmapWidth, 4 + 1, 0),
                newPreparmentOrder(4, 10, 6, 4, minSubmapWidth, 3 + 4, 0),
                newPreparmentOrder(10, 0, 4, 4, minSubmapWidth, 3 + 4, 0),
            };

            SubmapPreparer submapPreparer = new SubmapPreparer();
            //var submaps = submapPreparer.PrepareSubmaps(heightmapFile.GlobalHeightArray, preparmentOrders);
            //totalHeightmap.LoadHeightmap(submaps.Ring0Submaps);


            Ring1HeightArrayCreator creator = new Ring1HeightArrayCreator();
            //var ring1Array = creator.CreateRing1HeightmapArray(submaps.Ring1Submaps[0].Heightmap);
            //SavingFileManager.SaveToFile("map.dat", ring1Array);
            var ring1Array = SavingFileManager.LoadFromFile("map.dat", 2048, 2048);

            //ring1Array = BasicHeightmapModifier.AddConstant(-0.5f, ring1Array);
            ring1Array = BasicHeightmapModifier.Multiply(1000, ring1Array);

            _ring1Tree = new Ring1Tree();

            var tempHeightmapInfo = new GlobalHeightmapInfo(2000, 1000, 80000, 80000);
            //_ring1Tree.CreateHeightmap(ring1Array, heightmapFile.GlobalHeightmapInfo);
            _ring1Tree.CreateHeightmap_TODO_DELETE(ring1Array);
        }

        private SubmapPreparmentOrder newPreparmentOrder(int x, int y, int width, int height, int distanceBase,
            int lodFactor, int ringNumber)
        {
            return new SubmapPreparmentOrder(
                new SubmapPosition(x * distanceBase, width * distanceBase, y * distanceBase, height * distanceBase),
                lodFactor, ringNumber);
        }

        public Ring1Tree Ring1Tree
        {
            get { return _ring1Tree; }
        }

        public void UpdateHeightmap()
        {
            _ring1Tree.UpdateLod(FovData.FromCamera(_camera));
        }
    }

    public class HeightmapWidth
    {
        private int smallerWidth;

        public HeightmapWidth(int smallerWidth)
        {
            this.smallerWidth = smallerWidth;
            // todo check if smallerWidth to potega 2
        }

        public int StandardWidth
        {
            get { return smallerWidth; }
        }

        public int UnityWidth
        {
            get { return smallerWidth + 1; }
        }
    }
}