using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Diagrams
{
    public class TamMipmapsComparisionDiagramGO : MonoBehaviour
    {
        public int TamLevel;
        public string TargetDirectory;
        public Texture2DArray Tam;
        public int MipmapLevelCount = 5;

        public void Start()
        {
            Directory.CreateDirectory(TargetDirectory);
            var standardMipmappedTexture = GenerateMipmappedTexture();

            for (int i = 0; i < MipmapLevelCount; i++)
            {
                var  mp1 = RetriveMipmapFromTam(i);
                var  mp2 = RetriveMipmapFromTex(standardMipmappedTexture, i);
                DiagramsGenerationUtils.SaveImage(mp1 , i, 0, TargetDirectory);
                DiagramsGenerationUtils.SaveImage(mp2 , i, 1, TargetDirectory);
                DiagramsGenerationUtils.CreateDebugPlane(mp1, i, 0);
                DiagramsGenerationUtils.CreateDebugPlane(mp2, i, 1);
            }

        }

        private Texture2D RetriveMipmapFromTex(Texture2D tex, int mipmapLevel)
        {
            var size = new IntVector2((int) (tex.width / Mathf.Pow(2, mipmapLevel)), (int) (tex.height / Mathf.Pow(2, mipmapLevel)));
            var tx = new Texture2D(size.X, size.Y, tex.format, false);
            tx.SetPixels32(  tex.GetPixels32( mipmapLevel) );
            tx.Apply(false);
            return tx;
        }

        private Texture2D RetriveMipmapFromTam(int mipmapLevel)
        {
            var size = new IntVector2((int) (Tam.width / Mathf.Pow(2, mipmapLevel)), (int) (Tam.height / Mathf.Pow(2, mipmapLevel)));
            var tx = new Texture2D(size.X, size.Y, Tam.format, false);
            tx.SetPixels32(  Tam.GetPixels32(TamLevel, mipmapLevel) );
            tx.Apply(false);
            return tx;
        }

        private Texture2D GenerateMipmappedTexture()
        {
            var nt = new Texture2D(Tam.width, Tam.height, Tam.format, true);
            nt.SetPixels32(Tam.GetPixels32(TamLevel));
            nt.Apply(true);
            return nt;
        }

    }

    public static class DiagramsGenerationUtils
    {
        public static void SaveImage(Texture2D tex, int px, int py, string targetDirectory)
        {
            SavingFileManager.SaveTextureToPngFile(targetDirectory+$"tex{px}.{py}.png", tex);
        }

        public static void CreateDebugPlane(Texture2D tex, int px, int py)
        {
            var ob = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ob.transform.localPosition = new Vector3(px,0,py)*12;
            ob.name = $"Img.{px}.{py}";
            ob.GetComponent<MeshRenderer>().material.SetTexture("_MainTex",tex);
        }
    }
}
