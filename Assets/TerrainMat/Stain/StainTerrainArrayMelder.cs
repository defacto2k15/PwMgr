using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.TerrainMat.Stain
{
    public class StainTerrainArrayMelder
    {
        public void AddJoints(List<ColorPack> paletteArray, int[,] paletteIndexArray, Vector4[,] controlArray)
        {
            var texturesSideLength = paletteIndexArray.GetLength(0);

            for (int x = 0; x < texturesSideLength; x++)
            {
                for (int y = 0; y < texturesSideLength; y++)
                {
                    TextureCell left = null;
                    TextureCell top = null;
                    TextureCell topLeft = null;


                    if (x > 0)
                    {
                        left = new TextureCell(
                            paletteArray[paletteIndexArray[x - 1, y]],
                            controlArray[x - 1, y]
                        );
                    }

                    if (y > 0)
                    {
                        top = new TextureCell(
                            paletteArray[paletteIndexArray[x, y - 1]],
                            controlArray[x, y - 1]
                        );
                    }
                    if (x > 0 && y > 0)
                    {
                        topLeft = new TextureCell(
                            paletteArray[paletteIndexArray[x - 1, y - 1]],
                            controlArray[x - 1, y - 1]
                        );
                    }

                    TextureCell current = new TextureCell(
                        paletteArray[paletteIndexArray[x, y]],
                        controlArray[x, y]
                    );

                    List<TextureCell> neighbourCells = new List<TextureCell>
                    {
                        left,
                        top,
                        topLeft
                    };

                    var notEqualNeighbours =
                        neighbourCells
                            .Select((c, idx) => new
                            {
                                cell = c,
                                index = idx
                            })
                            .Where(c => c.cell != null && !current.Color.Equals(c.cell.Color)).ToList();

                    if (notEqualNeighbours.Count == 0)
                    {
                        continue; // all right
                    }

                    var perCellCompilance =
                        notEqualNeighbours.Select(
                            (c) =>
                                new
                                {
                                    compilance = calculateCompilance(current.Color, current.Control, c.cell.Color,
                                        c.cell.Control),
                                    index = c.index
                                }).ToList();
                    var finalCompilance =
                        Enumerable.Range(0, 4)
                            .Select(c =>
                                perCellCompilance.Select(r => r.compilance[c])
                                    .All(r => r == true))
                            .ToList();

                    var finalNoMatchCount = finalCompilance.Count(c => c == false);
                    if (finalNoMatchCount == 0)
                    {
                        continue;
                    }
                    if (finalNoMatchCount <= 2) // small no match
                    {
                        var oldControl = current.Control;
                        for (int i = 0; i < 4; i++)
                        {
                            if (finalCompilance[i] == false)
                            {
                                oldControl[i] = 0.0f;
                            }
                        }
                        controlArray[x, y] = oldControl;
                    }
                    else // big no match
                    {
                        var colorPossibilities = new List<List<Color>>(4);
                        for (int i = 0; i < 4; i++)
                        {
                            colorPossibilities.Add(new List<Color>());
                            if (finalCompilance[i] == true)
                            {
                                colorPossibilities[i].Add(current.Color[i]);
                            }
                            else
                            {
                                colorPossibilities[i].AddRange(
                                    perCellCompilance.Where((c) => c.compilance[i] == false)
                                        // we select colours which are not compilant
                                        .Select(c => neighbourCells[c.index].Color[i])
                                    // we select non compilant colour
                                );
                                colorPossibilities[i] = colorPossibilities[i].Distinct().ToList();
                            }
                        }
                        var control = current.Control;
                        int newColorsToAdd = finalNoMatchCount - 2;
                        for (int i = 0; i < 4; i++)
                        {
                            if (colorPossibilities[i].Count > 1
                            ) // we will not matchi this channel, so as well we can use if to change color
                            {
                                control[i] = 0.0f;
                                colorPossibilities[i].Clear();
                                colorPossibilities[i].Add(current.Color[i]);
                                newColorsToAdd--;
                            }
                        }
                        if (newColorsToAdd > 0) // still some new colors to add
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                if (colorPossibilities[i].Count != 1 || colorPossibilities[i][0] != current.Color[i])
                                {
                                    control[i] = 0.0f;
                                    colorPossibilities[i].Clear();
                                    colorPossibilities[i].Add(current.Color[i]);
                                    newColorsToAdd--;
                                    if (newColorsToAdd <= 0)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        int newPaletteIndex = GetPaletteIndexFor(colorPossibilities, paletteArray);
                        paletteIndexArray[x, y] = newPaletteIndex;
                        controlArray[x, y] = control;
                    }
                }
            }
            //for (int x = 0; x < _texturesSideLength; x++)
            //{
            //    paletteIndexArray[x, 6] = 5;
            //}
        }

        bool[] calculateCompilance(ColorPack colorPack, Vector4 currentControl, ColorPack left, Vector4 leftControl)
        {
            bool[] colorCompilance = colorPack.SingleEquality(left);
            for (int i = 0; i < 4; i++)
            {
                if (Math.Abs(leftControl[i]) < 0.001 && Math.Abs(currentControl[i]) < 0.001)
                {
                    colorCompilance[i] = true;
                }
            }
            return colorCompilance;
        }

        private int GetPaletteIndexFor(List<List<Color>> colorPossibilities, List<ColorPack> paletteArray)
        {
            for (int i = 0; i < paletteArray.Count; i++)
            {
                ColorPack pack = paletteArray[i];
                bool packOk = true;
                for (int k = 0; k < 4; k++)
                {
                    Color color = pack[k];
                    if (!colorPossibilities[k].Contains(color))
                    {
                        packOk = false;
                        break;
                    }
                }
                if (packOk)
                {
                    return i;
                }
            }

            var array = new Color[4];
            for (int i = 0; i < 4; i++)
            {
                array[i] = colorPossibilities[i][0];
            }
            paletteArray.Add((new ColorPack(array)));
            return paletteArray.Count - 1;
        }
    }
}