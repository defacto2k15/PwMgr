namespace Assets.Trees.RuntimeManagement.Management
{
    public class DetailFieldsTemplateOneLine
    {
        private VegetationDetailLevel _level;
        private float _minRadius;
        private float _maxRadius;

        public DetailFieldsTemplateOneLine(VegetationDetailLevel level, float minRadius, float maxRadius)
        {
            _level = level;
            _minRadius = minRadius;
            _maxRadius = maxRadius;
        }

        public VegetationDetailLevel Level
        {
            get { return _level; }
        }

        public float MinRadius
        {
            get { return _minRadius; }
        }

        public float MaxRadius
        {
            get { return _maxRadius; }
        }
    }
}