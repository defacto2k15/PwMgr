using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Grass2.IntenstityDb
{
    public class HabitatTexturesGeneratorDebugObject : MonoBehaviour
    {
        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var queryingArea = new MyRectangle(526 * 90, 582 * 90, 90, 90);

            var generator = new HabitatTexturesGenerator(
                new HabitatMapDbProxy(new HabitatMapDb(new HabitatMapDb.HabitatMapDbInitializationInfo()
                {
                    RootSerializationPath = @"C:\inz\habitating2\"
                })), new HabitatTexturesGenerator.HabitatTexturesGeneratorConfiguration()
                {
                    HabitatMargin = 10,
                    HabitatSamplingUnit = 1
                }, new TextureConcieverUTProxy());

            var outDict = generator.GenerateHabitatTextures(queryingArea, new IntVector2(90, 90)).Result;
            outDict.Values.Select((c, i) =>
            {
                GenerateDebugTextureObject(c, i);
                return 0;
            }).ToList();
        }

        private int once = 0;

        private void GenerateDebugTextureObject(Texture2D texture, int i)
        {
            //SavingFileManager.SaveTextureToPngFile($@"C:\inz2\habiTex{once++}.png", texture);
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.transform.localPosition = new Vector3(i * 2, 0, 0);

            go.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", texture);
        }
    }
}