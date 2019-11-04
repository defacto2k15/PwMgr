using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Utils
{
    public class MyTransformTriplet
    {
        private Vector3 _position;
        private Vector3 _rotation;
        private Vector3 _scale;
        private Quaternion _quaternionRotation;
        private bool _quaternionSet = false;

        public MyTransformTriplet(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            _position = position;
            _rotation = rotation;
            _scale = scale;
        }

        public MyTransformTriplet(Vector3 position, Quaternion quaternionRotation, Vector3 scale)
        {
            _position = position;
            _scale = scale;
            _quaternionRotation = quaternionRotation;
            _quaternionSet = true;
        }

        public static MyTransformTriplet FromGlobalTransform(Transform transform)
        {
            return new MyTransformTriplet(transform.position, MyMathUtils.DegToRad(transform.eulerAngles),
                transform.lossyScale);
        }

        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public Vector3 Rotation
        {
            get
            {
                Preconditions.Assert(!_quaternionSet, "Rotation is in quaternion");
                return _rotation;
            }
            set { _rotation = value; }
        }

        public Vector3 ForcedEulerRotation
        {
            get
            {
                if (!_quaternionSet)
                {
                    return Rotation;
                }
                else
                {
                    return _quaternionRotation.eulerAngles;
                }
            }
        }

        public Quaternion QuaternionRotation
        {
            get
            {
                Preconditions.Assert(_quaternionSet, "Rotation is not in quaternion");
                return _quaternionRotation;
            }
        }

        public Vector3 Scale
        {
            get { return _scale; }
            set { _scale = value; }
        }

        public Vector2 XZPosition
        {
            get { return new Vector2(_position.x, _position.z); }
        }

        public void SetTransformTo(Transform trans)
        {
            trans.localScale = Scale;
            trans.localPosition = Position;

            if (_quaternionSet)
            {
                trans.rotation = _quaternionRotation;
            }
            else
            {
                trans.localEulerAngles = MyMathUtils.RadToDeg(Rotation);
            }
        }

        public bool QuaternionSet => _quaternionSet;

        public Matrix4x4 ToLocalToWorldMatrix()
        {
            if (QuaternionSet)
            {
                return TransformUtils.GetLocalToWorldMatrix(Position, _quaternionRotation, Scale);
            }
            else
            {
                return TransformUtils.GetLocalToWorldMatrix(Position, ForcedEulerRotation, Scale);
            }
        }

        public MyTransformTriplet Clone()
        {
            return new MyTransformTriplet(_position, ForcedEulerRotation, _scale);
        }

        public static MyTransformTriplet operator +(MyTransformTriplet first, MyTransformTriplet second)
        {
            var outPos = first.Position + second.Position;
            var outScale = new Vector3(
                first.Scale.x * second.Scale.x,
                first.Scale.y * second.Scale.y,
                first.Scale.z * second.Scale.z);

            var outRotation = first.ForcedEulerRotation + second.ForcedEulerRotation;
            return new MyTransformTriplet(outPos, outRotation, outScale);
        }

        public static MyTransformTriplet IdentityTransform =
            new MyTransformTriplet(Vector3.zero, Vector3.zero, Vector3.one);
    }
}