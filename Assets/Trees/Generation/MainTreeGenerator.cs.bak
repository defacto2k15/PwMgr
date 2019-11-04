using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Trees.Generation
{
    public class MainTreeGenerator
    {
#if UNITY_EDITOR
        private BillboardGenerator _billboardGeneratorObject;

        public MainTreeGenerator(BillboardGenerator billboardGeneratorObject)
        {
            _billboardGeneratorObject = billboardGeneratorObject;
        }

        public void Generate(Tree treeInstance, string clanName, float marginMultiplier)
        {
            var treeFileManager = new TreeFileManager(new TreeFileManagerConfiguration());

            TreePrefabManager prefabManager = new TreePrefabManager();
            prefabManager.ClearGeneratedFiles();

            var pyramidGenerator = new TreeClanGenerator(_billboardGeneratorObject, prefabManager);
            pyramidGenerator.CreateClanPyramid(treeInstance,
                (pyramidTemplate) => { treeFileManager.SaveCompleteTreeClan(pyramidTemplate, clanName); }, 5,
                marginMultiplier);
        }
#endif
    }
}