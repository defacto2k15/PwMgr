using System;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.Generation
{
    public class TreeGeneratorDebug : MonoBehaviour
    {
#if  UNITY_EDITOR
        public Tree TreeInstance;

        private void Start()
        {
            TreePrefabManager prefabManager = new TreePrefabManager();
            prefabManager.ClearGeneratedFiles();
            prefabManager.ClearCompletedGeneratedFiles();

            var pyramidGenerator = new TreeClanGenerator(
                FindObjectWithComponent<BillboardGenerator>(BillboardGenerator.Name),
                prefabManager);
            pyramidGenerator.CreateClanPyramid(TreeInstance,
                (pyramidTemplate) => { prefabManager.SaveCompleteTreeClan(pyramidTemplate, "clan1"); }, 1);
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
}