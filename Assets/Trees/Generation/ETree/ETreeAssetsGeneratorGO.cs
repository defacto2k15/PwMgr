using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.Generation.ETree
{
    public class ETreeAssetsGeneratorGO : MonoBehaviour
    {
#if  UNITY_EDITOR
        public List<ClanGenerationOrder> ClansToGenerate;
        public BillboardGenerator BillboardGenerator;
        public Material TreeMaterial;
        public int BillboardsCount = 8;
        public int BillboardWidth = 256;

        private void Start()
        {
            TreePrefabManager prefabManager = new TreePrefabManager();
            prefabManager.ClearGeneratedFiles();
            prefabManager.ClearCompletedGeneratedFiles();

            var pyramidGenerator = new ETreeClanGenerator(BillboardGenerator, TreeMaterial, new ETreeClanGeneratorConfiguration()
            {
                BillboardMarginMultiplier = 1,
                BillboardsCount = BillboardsCount,
                BillboardWidth = BillboardWidth
            });
            foreach (var order in ClansToGenerate)
            {
                pyramidGenerator.CreateClanPyramid(order.Meshes, (pyramidTemplate) => prefabManager.ESaveCompleteTreeClan(pyramidTemplate, order.ClanName));
            }
        }

        public static T FindObjectWithComponent<T>(String name)
        {
            GameObject generatorObject = GameObject.Find(name);
            Preconditions.Assert(generatorObject != null, "Cannot find object of name " + name);

            T component = generatorObject.GetComponent<T>();
            Preconditions.Assert(component != null,
                "Cannot find component of type " + typeof(T) + " in object of  name " + name);
            return component;
        }
#endif
    }

    [Serializable]
    public class ClanGenerationOrder
    {
        public List<Mesh> Meshes;
        public string ClanName;
    }
}
