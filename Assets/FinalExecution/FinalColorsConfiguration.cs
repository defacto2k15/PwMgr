using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Utils;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class FinalColorsConfiguration
    {
        private ColorPaletteFile _colorPaletteFile;
        private ColorPaletteFileManager _colorPaletteFileManager;

        public FinalColorsConfiguration(ColorPaletteFileManagerConfiguration fileManagerConfiguration)
        {
            _colorPaletteFileManager = new ColorPaletteFileManager(fileManagerConfiguration);
        }

        public ColorPaletteFile ColorPaletteFile
        {
            get
            {
                if (_colorPaletteFile == null)
                {
                    _colorPaletteFile = _colorPaletteFileManager.LoadColorPaletteFile();
                }
                return _colorPaletteFile;
            }
        }

        public List<Color> SpruceColors => new List<Color>()
        {
            ColorUtils.FromHex("2f3c16"),
            ColorUtils.FromHex("3e4f1d"),
            ColorUtils.FromHex("263c16"),
        };
    }

    public class ColorPaletteFileManager
    {
        private ColorPaletteFileManagerConfiguration _configuration;

        public ColorPaletteFileManager(ColorPaletteFileManagerConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ColorPaletteFile LoadColorPaletteFile()
        {
            var texSize = _configuration.TextureSize;
            var tex = SavingFileManager.LoadPngTextureFromFile(_configuration.FilePath, texSize.X,
                texSize.Y, TextureFormat.ARGB32, true, false);

            var oneLineCount = Mathf.Min(_configuration.MaxOneLineColorCount, texSize.X);
            var outColorArrays = new List<List<Color>>();
            for (int y = 0; y < texSize.Y; y++)
            {
                var oneLineList = new List<Color>();
                for (int x = 0; x < oneLineCount; x++)
                {
                    var color = tex.GetPixel(x, (texSize.Y - 1) - y);
                    if (!color.Equals(_configuration.EmptyColor))
                    {
                        oneLineList.Add(color);
                    }
                }
                outColorArrays.Add(oneLineList);
            }
            GameObject.Destroy(tex);
            return new ColorPaletteFile(outColorArrays);
        }
    }

    public class ColorPaletteFileManagerConfiguration
    {
        public string FilePath;
        public IntVector2 TextureSize;
        public int MaxOneLineColorCount;
        public Color EmptyColor;
    }

    public class ColorPaletteFile
    {
        private readonly List<List<Color>> _colorArrays;

        public ColorPaletteFile(List<List<Color>> colorArrays)
        {
            _colorArrays = colorArrays;
        }


        public List<Color> RetriveList(ColorPaletteLines lineNo, int elementsCount)
        {
            var line = _colorArrays[(int) lineNo];
            return line.Take(elementsCount).ToList();
        }

        public List<Color> RetrivePack(ColorPaletteLines lineNo)
        {
            return _colorArrays[(int) lineNo];
        }
    }

    public enum ColorPaletteLines
    {
        Ring2_Habitat_Forrest_Ground = 0,
        Ring2_Habitat_Forrest_GrassyField = 1,
        Ring2_Habitat_Fell_Ground = 2,
        Ring2_Habitat_Fell_GrassyField = 3,
        Ring2_Habitat_Not_Specified_Dotted = 4,
        Ring2_Habitat_Not_Specified_Ground = 5,
        Ring2_Habitat_Grassland_GrassyField = 6,
        Ring2_Habitat_Grassland_Ground = 7,
        Ring2_Habitat_Meadow_DrySand = 8,


        Trees_Beech = 32,
        Trees_Spruce = 33,
        Trees_Cypress = 34,
        Trees_Bush1 = 35,
        Trees_Bush2 = 36,
        Trees_Pinus = 37,

        Road1 = 48,
        Road1_Dots = 49,
    }
}