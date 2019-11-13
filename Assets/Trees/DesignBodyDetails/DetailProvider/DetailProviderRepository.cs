using System.Collections.Generic;
using System.Linq;
using Assets.Random;
using Assets.ShaderUtils;
using Assets.Trees.DesignBodyDetails.MyRandom;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails.DetailProvider
{
    public class DetailProviderRepository
    {
        public AbstractDesignBodyLevel2DetailProvider Common3DTreeDetailProvider(
            SingleDetailDisposition disposition = null)
        {
            return new Standard3DObjectDetailProvider(disposition);
        }

        /// <summary>
        /// ///BILLBOARDS!!!!!
        /// </summary>
        public AbstractDesignBodyLevel2DetailProvider CalculateBillboardDesignBodyTreeScaleDetailProvider(
            MyTransformTriplet myTransformTriplet,
            SingleDetailDisposition disposition = null)
        {
            return new BillboardAlignToNormalDetailProvider(myTransformTriplet, disposition);
        }

        public AbstractDesignBodyLevel2DetailProvider CreateColorUniformDetailProvider(
            SingleDetailDisposition dispositionPerDetailDisposition)
        {
            return new ColorUniformDetailProvider(dispositionPerDetailDisposition);
        }
    }

    public class Standard3DObjectDetailProvider : AbstractDesignBodyLevel2DetailProvider
    {
        private readonly SingleDetailDisposition _disposition;

        public Standard3DObjectDetailProvider(SingleDetailDisposition disposition)
        {
            if (disposition == null)
            {
                disposition = new SingleDetailDisposition();
            }
            _disposition = disposition;
        }

        protected override DesignBodyLevel2Detail GetDetailsFor(DesignBodyLevel1Detail level1Detail, MyRandomProvider randomGenerator)
        {
            //var spotNormal = level1Detail.SpotNormal;
            var spotNormal = new Vector3(0, 0, 1);
            spotNormal = new Vector3(-spotNormal.y, spotNormal.x, spotNormal.z);

            var rotation = randomGenerator.FloatValueRange(StringSeed.YRotation, 0, 2 * Mathf.PI);
            //var finalRotation = RotationUtils.AlignRotationToNormal(spotNormal, rotation); //todo set rotation from data!!
            Debug.Log(rotation);
            var finalRotation = new Vector3(0, rotation * Mathf.Rad2Deg, 0);

            var heightScale = randomGenerator.FloatValueRange(StringSeed.HeightScale, 2, 4); //TODO we should configure it

            var firstPosition = new Vector3(level1Detail.Pos2D.x,0, level1Detail.Pos2D.y);
            var finalPosition = DesignBodyUtils.MoveToFoundation(heightScale, 0, spotNormal, firstPosition);
            finalPosition = firstPosition;

            var scale = new Vector3(1, heightScale, 1);
            scale *= randomGenerator.FloatValueRange(StringSeed.BaseScale, 0.6f, 1.5f);

            scale = VectorUtils.MemberwiseMultiply(scale, _disposition.SizeMultiplier);
            scale *= level1Detail.Size;

            return new DesignBodyLevel2Detail()
            {
                Position = finalPosition,
                Rotation = Quaternion.Euler(finalRotation ),
                Scale = scale,
            };
        }
    }

    public class ColorUniformDetailProvider : AbstractDesignBodyLevel2DetailProvider
    {
        private readonly SingleDetailDisposition _disposition;

        public ColorUniformDetailProvider(SingleDetailDisposition disposition)
        {
            if (disposition == null)
            {
                _disposition = new SingleDetailDisposition();
            }
            _disposition = disposition;
        }

        protected override DesignBodyLevel2Detail GetDetailsFor(DesignBodyLevel1Detail level1Detail,
            MyRandomProvider randomGenerator)
        {
            UniformsPack pack = new UniformsPack();
            Color chosenColor = _disposition.Color;

            if (_disposition.ColorGroups != null && _disposition.ColorGroups.Any())
            {
                var count = _disposition.ColorGroups.Count;
                var index = randomGenerator.NextWithMax(StringSeed.ColorIndex, 0, count - 1);
                chosenColor = _disposition.ColorGroups[index];
            }

            chosenColor = JitterColor(chosenColor, randomGenerator);

            pack.SetUniform("_Color", chosenColor);
            return new DesignBodyLevel2Detail()
            {
                UniformsPack = pack
            };
        }

        private Color JitterColor(Color chosenColor, MyRandomProvider randomGenerator)
        {
            var hsv = ColorUtils.RgbToHsv(chosenColor);
            var jittered = new Vector3(
                hsv.x * randomGenerator.FloatValueRange(StringSeed.HSV_H, 0.9f, 1.1f),
                hsv.y * randomGenerator.FloatValueRange(StringSeed.HSV_S, 0.9f, 1.1f),
                hsv.z
            );
            return ColorUtils.HsvToRgb(jittered);
        }
    }


    public class BillboardAlignToNormalDetailProvider : AbstractDesignBodyLevel2DetailProvider
    {
        public static int Count = 0;

        private MyTransformTriplet _transformTriplet;
        private SingleDetailDisposition _disposition;

        public BillboardAlignToNormalDetailProvider(MyTransformTriplet transformTriplet,
            SingleDetailDisposition disposition = null)
        {
            if (disposition == null)
            {
                disposition = new SingleDetailDisposition();
            }
            _transformTriplet = transformTriplet;
            _disposition = disposition;
        }

        protected override DesignBodyLevel2Detail GetDetailsFor(DesignBodyLevel1Detail level1Detail,
            MyRandomProvider randomGenerator)
        {
            var heightScale = randomGenerator.FloatValueRange(StringSeed.HeightScale, 2, 4);

            var outScale = new Vector3(_transformTriplet.Scale.x, _transformTriplet.Scale.y * heightScale,
                _transformTriplet.Scale.z);
            outScale = VectorUtils.MemberwiseMultiply(outScale, _disposition.SizeMultiplier);
            outScale *= level1Detail.Size;

            //var spotNormal = level1Detail.SpotNormal;
            var spotNormal = new Vector3(0, 0, 1);

            var finalRotation =
                RotationUtils.AlignRotationToNormal(spotNormal, 0); //Mathf.PI/2); //todo set rotation from data!!

            float spotHeight= 0;
            var firstPosition = new Vector3(level1Detail.Pos2D.x, spotHeight, level1Detail.Pos2D.y);

            var billboardYPositionOffset = -outScale.y / 2f;
            spotNormal = new Vector3(spotNormal.y, spotNormal.z, spotNormal.x);
            var finalPosition =
                DesignBodyUtils.MoveToFoundation(outScale.y / 10, billboardYPositionOffset, spotNormal, firstPosition);

            var flatRotation = randomGenerator.FloatValueRange(StringSeed.YRotation, 0f, 360f);
            var uniformPack = new UniformsPack();
            uniformPack.SetUniform("_BaseYRotation", flatRotation);

            return new DesignBodyLevel2Detail()
            {
                Position = finalPosition,
                Rotation = Quaternion.Euler(finalRotation * Mathf.Deg2Rad),
                Scale = outScale,
                UniformsPack = uniformPack
            };
        }
    }

    public static class DesignBodyUtils
    {
        public static Vector3 MoveToFoundation(float heightScale, float heightOffset, Vector3 spotNormal,
            Vector3 oldPosition)
        {
            float foundationLength = heightScale * (1 - Vector3.Angle(spotNormal, new Vector3(0, 0, 1)) / 90) +
                                     heightOffset;
            var foundationVector = foundationLength * spotNormal;
            var finalPosition = oldPosition - foundationVector;
            return finalPosition;
        }
    }

    public class IdentityDesignBodyLevel2DetailProvider : AbstractDesignBodyLevel2DetailProvider
    {
        protected override DesignBodyLevel2Detail GetDetailsFor(DesignBodyLevel1Detail level1Detail,
            MyRandomProvider random)
        {
            return new DesignBodyLevel2Detail()
            {
                Rotation = Quaternion.identity,
                Scale = new Vector3(1, 1, 1)
            };
        }
    }
}