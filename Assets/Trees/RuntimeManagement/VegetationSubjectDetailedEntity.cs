using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement
{
    public class VegetationSubjectDetailedEntity
    {
        private int _id;
        private MyTransformTriplet _fullTransform;
        private Vector3 TODO_NORMAL_VECTOR;

        public VegetationSubjectDetailedEntity(int id, MyTransformTriplet fullTransform, Vector3 todoNormalVector)
        {
            _id = id;
            _fullTransform = fullTransform;
            TODO_NORMAL_VECTOR = todoNormalVector;
        }

        public int Id
        {
            get { return _id; }
        }

        public MyTransformTriplet FullTransform
        {
            get { return _fullTransform; }
        }

        public Vector3 TodoNormalVector
        {
            get { return TODO_NORMAL_VECTOR; }
        }
    }
}