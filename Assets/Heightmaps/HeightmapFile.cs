using System;
using System.IO;
using System.Linq;

namespace Assets.Heightmaps
{
    class HeightmapFile
    {
        public void LoadFile(string filePath, int inMapWidth)
        {
            _mapWidth = inMapWidth;
            const int bytesPerPixel = 2;

            byte[] fileByteArray = File.ReadAllBytes(filePath);
            if (fileByteArray.Length != _mapWidth * _mapWidth * bytesPerPixel)
            {
                throw new ArgumentException("Ilosc bajtow w pliku jest zla");
            }
            ushort[] fileHeightArray = new ushort[fileByteArray.Length / 2];

            Buffer.BlockCopy(fileByteArray, 0, fileHeightArray, 0, fileByteArray.Length);

            FileMaxValue = fileHeightArray.Max();
            FileMinValue = fileHeightArray.Min();

            _heightData = new float[_mapWidth, _mapWidth];
            for (int i = 0; i < _mapWidth; i++)
            {
                for (int j = 0; j < _mapWidth; j++)
                {
                    _heightData[i, j] = ((float) fileHeightArray[i * _mapWidth + j] - (float) FileMinValue) /
                                        (float) Delta;
                    //heightData[i, j] = i/3601f*20 + j/3601f;
                }
            }
        }

        public void MirrorReflectHeightDataInXAxis()
        {
            float[] buffer = new float[_mapWidth];
            for (int i = 0; i < _heightData.GetLength(0); i++)
            {
                System.Buffer.BlockCopy(_heightData, i * _heightData.GetLength(1) * sizeof(float), buffer, 0,
                    _heightData.GetLength(0) * sizeof(float));
                Array.Reverse(buffer);
                System.Buffer.BlockCopy(buffer, 0, _heightData, i * _heightData.GetLength(1) * sizeof(float),
                    _heightData.GetLength(0) * sizeof(float));
            }
        }

        private int _mapWidth;
        private float[,] _heightData;

        public HeightmapArray Heightmap
        {
            get { return new HeightmapArray(_heightData); }
        }

        public int MapWidth
        {
            get { return _mapWidth; }
        }

        public GlobalHeightmapInfo GlobalHeightmapInfo
        {
            get { return new GlobalHeightmapInfo(FileMaxValue, FileMinValue, 80000, 80000); }
        }

        private ushort FileMaxValue { get; set; }
        private ushort FileMinValue { get; set; }

        private ushort Delta
        {
            get { return (ushort) (FileMaxValue - FileMinValue); }
        }

        public HeightmapArray GlobalHeightArray
        {
            get { return new HeightmapArray(_heightData); }
        }
    }
}