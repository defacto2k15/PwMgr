using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    public static class RotationUtils
    {
        public static Vector3 AlignRotationToNormal(Vector3 normalX, float flatRotationInRadians)
        {
            var normal = normalX; //new Vector3(normalX.y, -normalX.x, normalX.z);
            var alignToNormalQuat = Quaternion.LookRotation(normal);
            var flatRotationQuat = Quaternion.AngleAxis(flatRotationInRadians * Mathf.Rad2Deg, Vector3.up);
            var f1 = (alignToNormalQuat * flatRotationQuat).eulerAngles;
            return new Vector3(f1.y, f1.z, f1.x);
        }

        public static Vector3 AlignRotationToNormalRotationNormalized(Vector3 normalX, float rotationNormalized)
        {
            var normal = new Vector3(normalX.y, -normalX.x, normalX.z);
            var alignToNormalQuat = Quaternion.LookRotation(normal);
            var flatRotationQuat = Quaternion.AngleAxis(rotationNormalized * 2 * Mathf.PI, Vector3.up);
            return (alignToNormalQuat * flatRotationQuat).eulerAngles;
        }
    }
}