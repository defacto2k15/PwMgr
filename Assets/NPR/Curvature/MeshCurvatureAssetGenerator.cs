using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Curvature
{
    public class MeshCurvatureAssetGenerator
    {
        public static void CreateAndSave(UnaliasedMesh mesh, string path, int radius = 5, bool useKring = true)
        {
            MeshCurvatureDetailGenerator detailGenerator = new MeshCurvatureDetailGenerator();
            var detail = detailGenerator.Generate(mesh, radius, useKring);
            MyAssetDatabase.CreateAndSaveAsset(detail, path);
        }

        public static MeshCurvatureDetailSE Generate(UnaliasedMesh mesh, int radius = 5, bool useKring = true)
        {
            MeshCurvatureDetailGenerator detailGenerator = new MeshCurvatureDetailGenerator();
            return detailGenerator.Generate(mesh,radius, useKring);
        }
    }
}