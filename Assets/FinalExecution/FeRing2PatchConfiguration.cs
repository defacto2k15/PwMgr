using System.Collections.Generic;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Ring2.Stamping;

namespace Assets.FinalExecution
{
    public class FeRing2PatchConfiguration
    {
        private FEConfiguration _feConfiguration;

        public FeRing2PatchConfiguration(FEConfiguration feConfiguration)
        {
            _feConfiguration = feConfiguration;
        }

        public int Ring2IntensityPatternEnhancingSizeMultiplier => 12;

        public Ring2PlateStamperConfiguration Ring2PlateStamperConfiguration = new Ring2PlateStamperConfiguration()
        {
            PlateStampPixelsPerUnit = new Dictionary<int, float>()
            {
                {10, 3f * 2},
                {11, 3f * 2 * 2},
                {12, 3f * 4 * 2},
                {13, 3f * 8 * 2},
                {14, 3f * 8 * 2},
            }
        };

        public Dictionary<int, float> Ring2PatchesOverseerConfiguration_IntensityPatternPixelsPerUnit = new Dictionary<int, float>() //TODO it is ugly
        {
            {10, 1 / 9f},
            {11, 1 / 3f},
            {12, 1f},
            {13, 3f},
            {14, 3f},
        };

        public Ring2PatchesOverseerConfiguration Ring2PatchesOverseerConfiguration =>
            new Ring2PatchesOverseerConfiguration()
            {
                IntensityPatternPixelsPerUnit = Ring2PatchesOverseerConfiguration_IntensityPatternPixelsPerUnit,
                PatchSize = _feConfiguration.Ring2PatchSize
            };


    }
}