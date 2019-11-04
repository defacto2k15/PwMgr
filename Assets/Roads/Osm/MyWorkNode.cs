using UnityEngine;

namespace Assets.Roads.Osm
{
    public class MyWorkNode
    {
        public MyWorkNode(long id, Vector2 position)
        {
            Id = id;
            Position = position;
        }

        public long Id;
        public Vector2 Position;

        protected bool Equals(MyWorkNode other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MyWorkNode) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}