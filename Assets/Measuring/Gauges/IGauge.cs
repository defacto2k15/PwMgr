using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Measuring.Gauges;
using Assets.Utils.Textures;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Measuring
{
    public interface IGauge
    {
        IMeasurementResult TakeMeasurement(MeasurementScreenshotsSet inputSet);
    }

    public enum GaugeType
    {
        BlockSpecificationGauge, LinesLayoutGauge, LinesWidthGauge, StrokesPixelCountGauge
    }

    public class MeasurementScreenshotsSet
    {
        public LocalTexture HatchMainTexture;
        public LocalTexture HatchIdTexture;
        public LocalTexture WorldPosition1Texture;
        public LocalTexture WorldPosition2Texture;
        public Texture2D HatchMainTexture2D;

        private uint[,] _idArray;

        public uint[,] IdArray
        {
            get
            {
                if (_idArray == null)
                {
                    _idArray = GaugeUtils.GenerateIdArray(HatchIdTexture);
                }

                return _idArray;
            }
        }

        private bool[,] _hatchOccurenceArray;
        public bool[,] HatchOccurenceArray
        {
            get
            {
                if (_hatchOccurenceArray == null)
                {
                    _hatchOccurenceArray = GaugeUtils.GenerateHatchOccurenceArray(HatchMainTexture);
                }

                return _hatchOccurenceArray;
            }
        }
    }

    public interface IMeasurementResult
    {
        IMeasurementPOCO GeneratePoco();
        Texture2D GenerateIllustration();
        string GetResultName();
        string ToCsvString();
    }

    [Serializable]
    public class IMeasurementPOCO
    {
    }

    public class MeasurementRenderTargetsSet
    {
        public RenderTexture ArtisticMainTexture;
        public RenderTexture HatchMainTexture;
        public RenderTexture HatchIdTexture;
        public RenderTexture WorldPosition1Texture;
        public RenderTexture WorldPosition2Texture;
    }

}
