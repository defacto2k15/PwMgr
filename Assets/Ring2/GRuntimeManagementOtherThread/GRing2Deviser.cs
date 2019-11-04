using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Repositioning;
using Assets.Ring2.Devising;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.ShaderUtils;
using Assets.Utils;
using GeoAPI.Geometries;
using UnityEngine;

namespace Assets.Ring2.GRuntimeManagementOtherThread
{
    public class GRing2Deviser
    {
        private Repositioner _repositioner = Repositioner.Default;

        public GRing2PatchDevised DevisePatch(Ring2Patch patch)
        {
            return new GRing2PatchDevised()
            {
                SliceArea = patch.SliceArea.ToUnityCoordPositions2D(),
                SliceInfos = patch.Slices.Select(slice => CreatePlate(slice, patch.SliceArea)).ToList()
            };
        }

        private UniformsWithKeywords CreatePlate(Ring2Slice slice, Envelope sliceArea)
        {
            sliceArea = _repositioner.Move(sliceArea);
            var pack = new UniformsPack();

            pack.SetUniform("_Palette",
                slice.SlicePalette.Palette.Select(c => new Vector4(c.r, c.g, c.b, c.a)).ToArray());
            pack.SetUniform("_Dimensions",
                new Vector4((float) sliceArea.MinX, (float) sliceArea.MinY, (float) sliceArea.CalculatedWidth(),
                    (float) sliceArea.CalculatedHeight()));
            pack.SetTexture("_ControlTex", slice.IntensityPattern.Texture); //todo texture garbage collector
            pack.SetUniform("_LayerPriorities", slice.LayerPriorities);

            return new UniformsWithKeywords()
            {
                Keywords = slice.Keywords,
                Uniforms = pack
            };
        }
    }
}