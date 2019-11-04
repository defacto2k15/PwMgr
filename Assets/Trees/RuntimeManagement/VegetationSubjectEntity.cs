using Assets.Trees.DesignBodyDetails;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement
{
    public class VegetationSubjectEntity
    {
        private DesignBodyLevel0Detail _detail;
        private int _id;

        private static int _lastId = 0;

        public VegetationSubjectEntity(DesignBodyLevel0Detail detail)
        {
            _detail = detail;
            _id = _lastId++;
        }

        public VegetationSubjectEntity(VegetationSubjectEntity oldEntity, int newId)
        {
            _id = newId;
            _detail = oldEntity._detail;
        }

        public Vector2 Position2D
        {
            get { return _detail.Pos2D; }
        }

        public DesignBodyLevel0Detail Detail
        {
            get { return _detail; }
        }

        public int Id
        {
            get { return _id; }
        }

        public override string ToString()
        {
            return $"{nameof(_detail)}: {_detail}, {nameof(_id)}: {_id}";
        }
    }
}