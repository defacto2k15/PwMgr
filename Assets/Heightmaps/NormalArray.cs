using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Heightmaps
{
    public class NormalArray
    {
        private Vector3[,] _array;

        public NormalArray(Vector3[,] array)
        {
            this._array = array;
        }


        public Vector3[,] NormalsAsArray
        {
            get { return _array; }
        }

        public int Width
        {
            get { return _array.GetLength(0); }
        }

        public int WorkingWidth
        {
            get { return Width - 1; }
        }

        public int Height
        {
            get { return _array.GetLength(1); }
        }

        public int WorkingHeight
        {
            get { return Height - 1; }
        }
    }
}