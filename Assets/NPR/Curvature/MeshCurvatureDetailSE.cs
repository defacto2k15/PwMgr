using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.NPR.Curvature
{
    [Serializable]
    public class MeshCurvatureDetailSE : ScriptableObject
    {
        [SerializeField]
        public Vector3[] PrincipalDirection1;

        [SerializeField]
        public Vector3[] PrincipalDirection2;

        [SerializeField]
        public float[] PrincipalValue1;

        [SerializeField]
        public float[] PrincipalValue2;

        public static MeshCurvatureDetailSE CreateDetail(Vector3[] principalDirection1, Vector3[] principalDirection2, float[] principalValue1, float[] principalValue2)
        {
            MeshCurvatureDetailSE detail = ScriptableObject.CreateInstance<MeshCurvatureDetailSE>();
            detail.PrincipalDirection1 = principalDirection1;
            detail.PrincipalDirection2 = principalDirection2;
            detail.PrincipalValue1 = principalValue1;
            detail.PrincipalValue2 = principalValue2;
            return detail;
        }
    }
}
