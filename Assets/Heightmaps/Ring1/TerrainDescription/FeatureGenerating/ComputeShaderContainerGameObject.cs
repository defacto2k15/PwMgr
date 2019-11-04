using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating
{
    public class ComputeShaderContainerGameObject : MonoBehaviour
    {
        public ComputeShader ThermalErosionShaderField;
        public ComputeShader HydraulicErosionShaderField;
        public ComputeShader HeightTransferShaderField;
        public ComputeShader MeiErosionShaderField;
        public ComputeShader TweakedThermalErosionShaderField;
        public ComputeShader RenderTextureMovingShaderField;
        public ComputeShader HeightTransferShaderPlainField;
        public ComputeShader SpotResolvingComputeShaderField;
        public ComputeShader RoadEngravingComputeShaderField;
        public ComputeShader SingleToDuoBillboardShaderField;
        public ComputeShader HabitatToGrassTypeShaderField;
        public ComputeShader FloraDomainShaderField;
        public ComputeShader WeldRenderingShaderField;
        public ComputeShader NPRBarycentricBufferGeneratorField;

        public ComputeShader ThermalErosionShader
        {
            get
            {
                Preconditions.Assert(ThermalErosionShaderField != null, "ThermalErosionShaderFieldNotSet");
                return ThermalErosionShaderField;
            }
        }

        public ComputeShader HydraulicErosionShader
        {
            get
            {
                Preconditions.Assert(HydraulicErosionShaderField != null, "HydraulicErosionShaderFieldNotSet");
                return HydraulicErosionShaderField;
            }
        }

        public ComputeShader HeightTransferShader
        {
            get
            {
                Preconditions.Assert(HeightTransferShaderField != null, "HeightTransferShaderFieldNotSet");
                return HeightTransferShaderField;
            }
        }

        public ComputeShader HeightTransferShaderPlain
        {
            get
            {
                Preconditions.Assert(HeightTransferShaderPlainField != null, "HeightTransferShaderPlainFieldNotSet");
                return HeightTransferShaderPlainField;
            }
        }

        public ComputeShader MeiErosionShader
        {
            get
            {
                Preconditions.Assert(MeiErosionShaderField != null, "MeiErosionShaderFieldNotSet");
                return MeiErosionShaderField;
            }
        }

        public ComputeShader TweakedThermalErosionShader
        {
            get
            {
                Preconditions.Assert(TweakedThermalErosionShaderField != null,
                    "TweakedThermalErosionShaderFieldNotSet");
                return TweakedThermalErosionShaderField;
            }
        }

        public ComputeShader RenderTextureMovingShader
        {
            get
            {
                Preconditions.Assert(RenderTextureMovingShaderField != null, "RenderTextureMovingShaderField not set");
                return RenderTextureMovingShaderField;
            }
        }

        public ComputeShader SpotResolvingComputeShader
        {
            get
            {
                Preconditions.Assert(SpotResolvingComputeShaderField != null,
                    "SpotResolvingComputeShaderField not set");
                return SpotResolvingComputeShaderField;
            }
        }

        public ComputeShader RoadEngravingComputeShader
        {
            get
            {
                Preconditions.Assert(RoadEngravingComputeShaderField != null,
                    "RoadEngravingComputeShaderField not set");
                return RoadEngravingComputeShaderField;
            }
        }

        public ComputeShader SingleToDuoBillboardShader
        {
            get
            {
                Preconditions.Assert(SingleToDuoBillboardShaderField != null,
                    "SingleToDuoBillboardShaderField not set");
                return SingleToDuoBillboardShaderField;
            }
        }

        public ComputeShader HabitatToGrassTypeShader
        {
            get
            {
                Preconditions.Assert(HabitatToGrassTypeShaderField != null, "HabitatToGrassTypeShaderField not set");
                return HabitatToGrassTypeShaderField;
            }
        }

        public ComputeShader FloraDomainShader
        {
            get
            {
                Preconditions.Assert(FloraDomainShaderField != null, "FloraDomainShaderField not set");
                return FloraDomainShaderField;
            }
        }

        public ComputeShader WeldRenderingShader
        {
            get
            {
                Preconditions.Assert(WeldRenderingShaderField != null, "WeldRenderingShaderField not set");
                return WeldRenderingShaderField;
            }
        }

        public ComputeShader NPRBarycentricBufferGenerator
        {
            get
            {
                Preconditions.Assert(NPRBarycentricBufferGeneratorField != null, "WeldRenderingShaderField not set");
                return NPRBarycentricBufferGeneratorField;
            }
        }
    }
}