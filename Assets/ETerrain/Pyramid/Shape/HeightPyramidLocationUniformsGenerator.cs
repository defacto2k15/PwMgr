using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Shape
{
    public class HeightPyramidLocationUniformsGenerator
    {
        private readonly HeightPyramidLocationParametersUpdaterConfiguration _heightPyramidLocationParametersUpdaterConfiguration;

        public HeightPyramidLocationUniformsGenerator(HeightPyramidLocationParametersUpdaterConfiguration heightPyramidLocationParametersUpdaterConfiguration)
        {
            _heightPyramidLocationParametersUpdaterConfiguration = heightPyramidLocationParametersUpdaterConfiguration;
        }

        public LocationParametersUniforms GenerateUniforms(HeightPyramidGroupTransition transition)
        {
            var transitionSingleStep = _heightPyramidLocationParametersUpdaterConfiguration.TransitionSingleStep;
            return new LocationParametersUniforms()
            {
                PyramidCenterWorldSpace = transition.AlignedPyramidCenter.ToFloatVec() * transitionSingleStep
            };
        }
    }

    public class HeightPyramidGroupMover
    {
        public void MoveGroup(HeightPyramidSegmentShapeGroup group, HeightPyramidGroupTransition transition)
        {
            if (transition.PyramidShouldMove)
            {
                group.MoveBy(transition.MoveDelta);
            }
        }
    }

    public class HeightPyramidGroupTransitionResolver
    {
        private HeightPyramidSegmentShapeGroup _heightPyramidSegmentShapeGroup;
        private readonly HeightPyramidLocationParametersUpdaterConfiguration _heightPyramidLocationParametersUpdaterConfiguration;

        private IntVector2 _alignedPyramidCenter = new IntVector2(0,0);

        public  HeightPyramidGroupTransitionResolver (HeightPyramidSegmentShapeGroup heightPyramidSegmentShapeGroup, HeightPyramidLocationParametersUpdaterConfiguration heightPyramidLocationParametersUpdaterConfiguration)
        {
            _heightPyramidSegmentShapeGroup = heightPyramidSegmentShapeGroup;
            _heightPyramidLocationParametersUpdaterConfiguration = heightPyramidLocationParametersUpdaterConfiguration;
        }

        public HeightPyramidGroupTransition ResolveTransition(Vector2 travellerPosition)
        {
            var transitionSingleStep = _heightPyramidLocationParametersUpdaterConfiguration.TransitionSingleStep;

            var currentTransitionAlignment = new IntVector2(
                Mathf.RoundToInt( travellerPosition.x / transitionSingleStep),
                Mathf.RoundToInt( travellerPosition.y / transitionSingleStep));
            if (!currentTransitionAlignment.Equals(_alignedPyramidCenter))
            {
                var delta = (currentTransitionAlignment - _alignedPyramidCenter).ToFloatVec() *
                            transitionSingleStep;
                
                _alignedPyramidCenter = currentTransitionAlignment;
                return new HeightPyramidGroupTransition()
                {
                    AlignedPyramidCenter = _alignedPyramidCenter,
                    MoveDelta = delta,
                    PyramidShouldMove = true
                };
            }
            else
            {
                return new HeightPyramidGroupTransition()
                {
                    MoveDelta = Vector2.zero,
                    AlignedPyramidCenter = _alignedPyramidCenter,
                    PyramidShouldMove = false
                };
            }
        }
    }

    public class HeightPyramidGroupTransition
    {
        public bool PyramidShouldMove;
        public IntVector2 AlignedPyramidCenter;
        public Vector2 MoveDelta;
    }

    public class LocationParametersUniforms
    {
        public Vector2 PyramidCenterWorldSpace;
    }
}