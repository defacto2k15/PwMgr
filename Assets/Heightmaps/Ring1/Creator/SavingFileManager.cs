using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Creator
{
    public class SavingFileManager
    {
        public static void SaveToFile(String path, HeightmapArray array)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                for (int x = 0; x < array.Width; x++)
                {
                    for (int y = 0; y < array.Height; y++)
                    {
                        writer.Write(array.HeightmapAsArray[x, y]);
                    }
                }
            }
        }

        public static HeightmapArray LoadFromFile(String path, int width, int height)
        {
            var array = new float[width, height];
            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        array[x, y] = reader.ReadSingle();
                    }
                }
            }
            return new HeightmapArray(array);
        }

        public static void SaveTextureToFile(string path, Texture2D texture)
        {
            var wholeBytes = texture.GetRawTextureData();
            File.WriteAllBytes(path, wholeBytes);
        }

        public static void SaveTextureToPngFile(string path, Texture2D texture)
        {
            var wholeBytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, wholeBytes);
        }

        public static Task SaveTextureToPngFileAsync(string path, Texture2D texture)
        {
            var wholeBytes = texture.EncodeToPNG();
            return AsyncFileUtils.WriteAllBytesAsync(path, wholeBytes);
        }

        public static Texture2D LoadTextureFromFile(string path, int width, int height, TextureFormat format,
            bool apply = true, bool generateMipmaps = false)
        {
            var newTexture = new Texture2D(width, height, format, generateMipmaps, false);
            try
            {
                var allBytes = File.ReadAllBytes(path);
                Preconditions.Assert(newTexture.LoadImage(allBytes), "Loading image from " + path + " failed");
                newTexture.LoadRawTextureData(allBytes);
            }
            catch (Exception e)
            {
                File.WriteAllText(@"C:\misc\inf.txt", e.ToString());
                Debug.LogError("E56 Err is " + e);
                Debug.LogError("E56 Size is " + width + " " + height + " " + format);
                throw;
            }
            if (apply)
            {
                newTexture.Apply();
            }
            return newTexture;
        }

        public static Texture2D LoadPngTextureFromFile(string path, int width, int height, TextureFormat format,
            bool apply = true, bool generateMipmaps = false)
        {
            MyProfiler.BeginSample("Load png texture from file");
            var newTexture = new Texture2D(width, height, format, generateMipmaps);
            newTexture.LoadImage(File.ReadAllBytes(path));
            if (apply)
            {
                newTexture.Apply();
            }
            MyProfiler.EndSample();

            Preconditions.Assert(width == newTexture.width,
                "Loaded texture width is not equal to expected one " + newTexture.width);
            Preconditions.Assert(height == newTexture.height,
                "Loaded texture height is not equal to expected one " + newTexture.height);
            return newTexture;
        }

        public static Texture2D LoadPngTextureFromFile(string path, bool apply = true, bool generateMipmaps = false)
        {
            MyProfiler.BeginSample("Load png texture from file2");
            var newTexture = new Texture2D(2,2);
            newTexture.LoadImage(File.ReadAllBytes(path));
            if (apply)
            {
                newTexture.Apply();
            }
            MyProfiler.EndSample();
            return newTexture;
        }

        public static async Task<Texture2D> LoadPngTextureFromFileAsync(string path, int width, int height,
            TextureFormat format, bool apply = true, bool generateMipmaps = false)
        {
            var newTexture = new Texture2D(width, height, format, generateMipmaps);
            newTexture.LoadImage(await AsyncFileUtils.ReadAllBytesAsync(path));
            if (apply)
            {
                newTexture.Apply();
            }

            Preconditions.Assert(width == newTexture.width,
                "Loaded texture width is not equal to expected one " + newTexture.width);
            Preconditions.Assert(height == newTexture.height,
                "Loaded texture height is not equal to expected one " + newTexture.height);
            return newTexture;
        }

        public static void SaveHeightmapBundleToFiles(String path, HeightmapBundle bundle)
        {
            int i = 0;
            foreach (var onePack in bundle.PackList)
            {
                SaveTextureToFile(path + "-HeightmapTexture-" + i, onePack.HeightmapTexture);
                SaveTextureToFile(path + "-NormalTexture-" + i, onePack.NormalTexture);
                SaveToFile(path + "-HeightmapArray-" + i, onePack.HeightmapArray);
                i++;
            }
        }

        public static HeightmapBundle LoadHeightmapBundlesFromFiles(String path, int packsCount, int startWidth)
        {
            List<OneLevelHeightmapPack> packs = new List<OneLevelHeightmapPack>();
            int currentWidth = startWidth;
            for (int i = 0; i < packsCount; i++)
            {
                packs.Add(new OneLevelHeightmapPack(
                    heightmapArray: LoadFromFile(path + "-HeightmapArray-" + i, currentWidth, currentWidth),
                    heightmapTexture: LoadTextureFromFile(path + "-HeightmapTexture-" + i, currentWidth, currentWidth,
                        TextureFormat.RFloat),
                    normalTexture: LoadTextureFromFile(path + "-NormalTexture-" + i, currentWidth, currentWidth,
                        TextureFormat.RGB24)
                ));
                currentWidth /= 2;
            }
            return new HeightmapBundle(packs, startWidth);
        }
    }
}