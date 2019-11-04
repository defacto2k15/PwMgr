using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Utils
{
    public static class MyAssetDatabase
    {
        public static void CreateAndSaveAsset(UnityEngine.Object objectToSave, string path)
        {
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(objectToSave, path);
            AssetDatabase.SaveAssets();
#else
            Preconditions.Fail("Use AssetDatabase only in Editor!");
#endif
        }

        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
            Preconditions.Fail("Use LoadAssetAtPath only in Editor!");
            return null;
#endif

        }
    }
}
