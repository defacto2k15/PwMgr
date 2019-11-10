using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails
{
    public class DesignBodyLevel2Detail
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public UniformsPack UniformsPack;

        public DesignBodyLevel2Detail()
        {
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            Scale = Vector3.one;
            UniformsPack = null;
        }

        public DesignBodyLevel2Detail(Vector3 position, Quaternion rotation, Vector3 scale, UniformsPack uniformsPack)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
            UniformsPack = uniformsPack;
        }

        public MyTransformTriplet TransformTriplet
        {
            get { return new MyTransformTriplet(Position, Rotation, Scale); }
        }

        public void MergeWith(DesignBodyLevel2Detail other)
        {
            if (other == null)
            {
                return;
            }
            Position += other.Position;

            var thisQuaternionRotation = Rotation;
            var otherQuaternionRotation = (other.Rotation);
            var finalRotation = thisQuaternionRotation * otherQuaternionRotation;
            Rotation = finalRotation;
            Scale = VectorUtils.MemberwiseMultiply(Scale, other.Scale);
            if (UniformsPack != null)
            {
                if (other.UniformsPack != null)
                {
                    UniformsPack.MergeWith(other.UniformsPack);
                }
            }
            else
            {
                UniformsPack = other.UniformsPack; // may be dangerous, should have .Clone, but optimalisations lol
            }
        }

        public DesignBodyLevel2Detail MergeNewWith(DesignBodyLevel2Detail other)
        {
            var tempUniforms = UniformsPack;
            if (tempUniforms == null)
            {
                tempUniforms = new UniformsPack();
            }
            var newDetail = new DesignBodyLevel2Detail(Position, Rotation, Scale, tempUniforms.Clone());
            newDetail.MergeWith(other);
            return newDetail;
        }

        public DesignBodyLevel2Detail Clone()
        {
            if (UniformsPack != null)
            {
                return new DesignBodyLevel2Detail(Position, Rotation, Scale, UniformsPack.Clone());
            }
            else
            {
                return new DesignBodyLevel2Detail(Position, Rotation, Scale, null);
            }
        }

        public UniformsPack RetriveUniformsPack()
        {
            if (UniformsPack == null)
            {
                return new UniformsPack();
            }
            else
            {
                return UniformsPack;
            }
        }
    }
}