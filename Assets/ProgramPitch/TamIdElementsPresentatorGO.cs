using System.Collections.Generic;
using Assets.NPRResources.TonalArtMap;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.ProgramPitch
{
    public class  TamIdElementsPresentatorGO: MonoBehaviour
    {
        public string TemporaryImagesPath = @"C:\mgr\tmp\tamid1\";

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var tonesCount = 5;
            var levelsCount = 5;
            var layersCount = 1;

            var tamTones = TAMTone.CreateList(tonesCount, new Dictionary<int, TAMStrokeOrientation>()
            {
                {0,TAMStrokeOrientation.Horizontal},
                {3,TAMStrokeOrientation.Vertical },
                {5,TAMStrokeOrientation.Both }
            });
            var tamMipmapLevels = TAMMipmapLevel.CreateList(levelsCount);

            var fileManager = new TamIdPackFileManager();

            var pack = fileManager.Load(TemporaryImagesPath, tamTones, tamMipmapLevels, layersCount);
            DrawDebugPlates(pack);
        }

        public static void DrawDebugPlates(TamIdSoleImagesPack pack)
        {
            var toneIndex = 0;
            foreach (var toneColumn in pack.Columns)
            {
                var levelIndex = 0;
                foreach (var levelPair in toneColumn.Value)
                {
                    var layerIndex = 0;
                    foreach (var layer in levelPair.Value)
                    {
                        CreateTexturedPlate(layer, 11f * new Vector2(toneIndex, levelIndex), "Tone " + toneIndex + " Level " + levelIndex+" Layer "+layerIndex);
                        layerIndex++;
                    }

                    levelIndex++;
                }

                toneIndex++;
            }
        }

        public static void SetMaterialRenderingModeToAlphablend(Material m)
        {
            m.SetFloat("_Mode", 2);
            m.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
        }

        public static void CreateTexturedPlate(Texture tex, Vector2 position, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.GetComponent<MeshRenderer>().material.mainTexture = tex;
            SetMaterialRenderingModeToAlphablend(go.GetComponent<MeshRenderer>().material);
            go.transform.position = new Vector3(position.x, 0, position.y);
            go.name = name;
        }
    }

}
