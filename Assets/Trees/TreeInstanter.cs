using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Assets.Utils;
using Object = UnityEngine.Object;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Assets.Trees
{
    public class TreeInstanter : MonoBehaviour
    {
#if UNITY_EDITOR
        public Tree InstantiateTreePrefab(Tree tree, Vector3 position, Quaternion rotation)
        {
            var outObject = PrefabUtility.InstantiatePrefab(tree) as Tree;
            var go = outObject.gameObject;
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            return outObject;
        }
#else
        public Tree InstantiateTreePrefab(Tree tree, Vector3 position, Quaternion rotation)
        {
            Preconditions.Fail("NOT Supported not in editor");
            return null;
        }
#endif
    }
}