using Assets.Heightmaps.Ring1.valTypes;

namespace Assets.ETerrain.Pyramid.Shape
{
    public class HeightPyramidLocationParametersUpdaterConfiguration
    {
        public MyRectangle PyramidLevelWorldSize = new MyRectangle(-90*3, -90*3, 90*6, 90*6); // Size in World units in Level
        public float TransitionSingleStep = 5f; // After how many unity group will be moved
    }
}