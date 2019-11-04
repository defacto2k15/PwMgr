using UnityEngine;

namespace Assets.TerrainMat
{
    public class TextureCell
    {
        private ColorPack _color;
        private Vector4 _control;

        public TextureCell(ColorPack color, Vector4 control)
        {
            _color = color;
            _control = control;
        }

        public ColorPack Color
        {
            get { return _color; }
            set { _color = value; }
        }

        public Vector4 Control
        {
            get { return _control; }
            set { _control = value; }
        }
    }
}