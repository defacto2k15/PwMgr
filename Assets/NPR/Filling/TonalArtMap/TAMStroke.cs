using UnityEngine;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMStroke
    {
        private readonly Vector2 _center;
        private readonly float _length;
        private readonly float _rotation;
        private readonly float _height;
        private readonly int _id;

        public TAMStroke(Vector2 center, float height, float length, float rotation, int id)
        {
            _center = center;
            _height = height;
            _length = length;
            _rotation = rotation;
            _id = id;
        }

        public Vector2 Center => _center;

        public float Length => _length;
        public float Height => _height;
        public float Rotation => _rotation;

        public int Id => _id;
    }
}