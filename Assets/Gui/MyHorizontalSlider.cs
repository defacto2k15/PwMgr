using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Gui
{
    public class MyHorizontalSlider
    {
        private Vector2 _valuesRange;
        private Rect _position;

        private float _value;
        private string _name;

        public MyHorizontalSlider(Vector2 valuesRange, Rect position, string name)
        {
            _valuesRange = valuesRange;
            _position = position;
            _name = name;
            _value = _valuesRange[0];
        }

        public void Draw()
        {
            _value = GUI.HorizontalSlider(_position, _value, _valuesRange[0], _valuesRange[1]);
            GUI.Label(new Rect(_position.x + _position.width/2, _position.y + 10, 80, _position.width), _name );
            GUI.Label(new Rect(_position.x + _position.width+5, _position.y, 20, 20), _value.ToString() );
        }

        public float Value => _value;
    }
}
