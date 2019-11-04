using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Random.Fields
{
    public class RandomFieldNature
    {
        public static RandomFieldNature FractalSimpleValueNoise3 = new RandomFieldNature("FractalSimpleValueNoise3");

        private string _NatureKeyword;

        private RandomFieldNature(string natureKeyword)
        {
            _NatureKeyword = natureKeyword;
        }

        public string NatureKeyword => _NatureKeyword;
    }
}