using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    class TransformUtils
    {
        public static Matrix4x4 GetLocalToWorldMatrix(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            return GetLocalToWorldMatrixWithEulerAngles(position, rotation * Mathf.Rad2Deg, scale);
        }

        public static Matrix4x4 GetLocalToWorldMatrix(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var matrix = new Matrix4x4();
            matrix.SetTRS(position, rotation, scale);
            return matrix;
        }

        public static Matrix4x4 GetLocalToWorldMatrixWithEulerAngles(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            var matrix = new Matrix4x4();
            matrix.SetTRS(position, Quaternion.Euler(rotation), scale);
            return matrix;
            //UtilsGameObject.SigletonObject.transform.position = position;
            //UtilsGameObject.SigletonObject.transform.localEulerAngles = rotation;
            //UtilsGameObject.SigletonObject.transform.localScale = scale;
            //return UtilsGameObject.SigletonObject.transform.localToWorldMatrix;
        }

        public static List<MyTransformTriplet> MakeParentChildTransformations(MyTransformTriplet parent,
            List<MyTransformTriplet> children)
        {
            parent.SetTransformTo(UtilsGameObject.SigletonObject.transform);
            SetChildrenTransform(children);
            return UtilsGameObject.GetChildren(children.Count)
                .Select(c => MyTransformTriplet.FromGlobalTransform(c.transform)).ToList();
        }

        private static void SetChildrenTransform(List<MyTransformTriplet> childrenTransform)
        {
            var childrenList = UtilsGameObject.GetChildren(childrenTransform.Count);
            for (int i = 0; i < childrenList.Count; i++)
            {
                childrenTransform[i].SetTransformTo(childrenList[i].transform);
            }
        }
    }
}