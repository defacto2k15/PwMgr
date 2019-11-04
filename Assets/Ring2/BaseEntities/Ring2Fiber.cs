namespace Assets.Ring2.BaseEntities
{
    public class Ring2Fiber
    {
        private int _index;
        private string _keyword;
        private bool _isFirm;

        private Ring2Fiber(int index, string keyword, bool isFirm)
        {
            _index = index;
            _keyword = keyword;
            _isFirm = isFirm;
        }

        public readonly static Ring2Fiber BaseGroundFiber = new Ring2Fiber(0, "BASE", true);
        public readonly static Ring2Fiber DrySandFiber = new Ring2Fiber(1, "DRY_SAND", false);
        public readonly static Ring2Fiber GrassyFieldFiber = new Ring2Fiber(2, "GRASS", false);
        public readonly static Ring2Fiber DottedTerrainFiber = new Ring2Fiber(3, "DOTS", false);

        public string FiberKeyword
        {
            get { return "OT_" + _keyword; }
        }

        public int Index
        {
            get { return _index; }
        }

        public bool IsFirm
        {
            get { return _isFirm; }
        }

        protected bool Equals(Ring2Fiber other)
        {
            return _index == other._index;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Ring2Fiber) obj);
        }

        public override int GetHashCode()
        {
            return _index;
        }
    }
}