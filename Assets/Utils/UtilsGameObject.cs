using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    class UtilsGameObject : MonoBehaviour
    {
        private static GameObject _sigletonObject;

        private void OnStart()
        {
            if (_sigletonObject == null)
            {
                _sigletonObject = new GameObject("singletonUtilsGameObject");
            }
        }

        public static GameObject SigletonObject
        {
            get
            {
                if (_sigletonObject == null)
                {
                    _sigletonObject = new GameObject("singletonUtilsGameObject");
                }
                return _sigletonObject;
            }
        }

        private static readonly List<GameObject> children = new List<GameObject>();

        public static List<GameObject> GetChildren(int count)
        {
            if (children.Count < count)
            {
                var newElementsCount = count - children.Count;
                for (int i = 0; i < newElementsCount; i++)
                {
                    var newChild = new GameObject("Child");
                    newChild.transform.parent = UtilsGameObject.SigletonObject.transform;
                    children.Add(newChild);
                }
            }
            return children.Take(count).ToList();
        }
    }
}