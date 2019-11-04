using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Random;
using Assets.ShaderUtils;
using Assets.Trees.DesignBodyDetails.BucketsContainer;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.DesignBodyDetails.MyRandom;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails
{
    public class SpecificTreeForger : IDesignBodyPortrayalForger
    {
        private DesignBodyInstanceBucketsContainer _instanceBucketsContainer;
        private DesignBodyRepresentationContainer _representationsContainer;

        public SpecificTreeForger(DesignBodyInstanceBucketsContainer instanceBucketsContainer, DesignBodyRepresentationContainer representationsContainer)
        {
            _instanceBucketsContainer = instanceBucketsContainer;
            _representationsContainer = representationsContainer;
        }

        public RepresentationCombinationInstanceId Forge( DesignBodyLevel1DetailWithSpotModification level1DetailWithSpotModification)
        {
            var (qualifier, representationIdx, level2DetailsCombination) = CalculateInfoForAddingInstance(level1DetailWithSpotModification.Level1Detail);
            if (level1DetailWithSpotModification.SpotModification != null)
            {
                DesignBodyLevel2Detail additionalLevel2Details = level1DetailWithSpotModification.SpotModification.GenerateLevel2Details();
                level2DetailsCombination = level2DetailsCombination.MergeWithAdditionalDetails(additionalLevel2Details);
            }

            return _instanceBucketsContainer.AddInstance(qualifier, representationIdx, level2DetailsCombination);
        }

        private (DesignBodyRepresentationQualifier qualifier, int representationIdx, DesignBodyLevel2DetailContainerForCombination level2Detail)
            CalculateInfoForAddingInstance(DesignBodyLevel1Detail level1Detail)
        {
            var qualifier = new DesignBodyRepresentationQualifier(level1Detail.SpeciesEnum, level1Detail.DetailLevel);
            MyRandomProvider randomProvider = new MyRandomProvider(level1Detail.Seed, true);
            var representationCount = _representationsContainer.RetriveCombinationCountFor(qualifier);
            var representationIdx = randomProvider.NextWithMax(StringSeed.CombinationIdx, 0, representationCount - 1);

            var detailProviderCombination = _representationsContainer.RetriveDetailProviderFor(qualifier, representationIdx);
            var level2DetailsCombination = detailProviderCombination.SpecificGenerator.Generate(level1Detail, randomProvider);
            return (qualifier, representationIdx, level2DetailsCombination);
        }

        public void Modify(RepresentationCombinationInstanceId combinationInstanceId, DesignBodyLevel1DetailWithSpotModification level1DetailWithSpotModification)
        {
            var (qualifier, representationIdx, level2DetailsCombination) = CalculateInfoForAddingInstance(level1DetailWithSpotModification.Level1Detail);
            if (level1DetailWithSpotModification.SpotModification != null)
            {
                DesignBodyLevel2Detail additionalLevel2Details = level1DetailWithSpotModification.SpotModification.GenerateLevel2Details();
                level2DetailsCombination = level2DetailsCombination.MergeWithAdditionalDetails(additionalLevel2Details);
            }
            _instanceBucketsContainer.ModifyInstance(combinationInstanceId, qualifier, representationIdx, level2DetailsCombination);
        }

        public void Remove(RepresentationCombinationInstanceId instanceId)
        {
            _instanceBucketsContainer.RemoveInstance(instanceId);
        }
    }


    public interface ISpecificDesignBodyLevel2DetailsGenerator
    {
        DesignBodyLevel2DetailContainerForCombination Generate(DesignBodyLevel1Detail level1Detail, MyRandomProvider randomProvider);
    }

    public class BillboardSpecificDesignBodyLevel2DetailsGenerator : ISpecificDesignBodyLevel2DetailsGenerator
    {
        private SingleDetailDisposition _disposition;
        private Vector3 _scale;

        public BillboardSpecificDesignBodyLevel2DetailsGenerator(SingleDetailDisposition disposition, Vector3 scale)
        {
            _disposition = disposition;
            if (_disposition == null)
            {
                _disposition = new SingleDetailDisposition()
                {
                    Color = new Vector4(0,0.5f,0),
                    SizeMultiplier = Vector3.one
                };
            }
            _scale = scale;
        }

        public DesignBodyLevel2DetailContainerForCombination Generate(DesignBodyLevel1Detail level1Detail,
            MyRandomProvider randomGenerator)
        {
            var heightScale = randomGenerator.FloatValueRange(StringSeed.HeightScale, 2, 4);

            var outScale = new Vector3(
                _scale.x * _disposition.SizeMultiplier.x *level1Detail.Size,
                _scale.y * _disposition.SizeMultiplier.y * level1Detail.Size * heightScale,
                _scale.z * _disposition.SizeMultiplier.z * level1Detail.Size );

            var spotNormal = new Vector3(0, 0, 1);

            var finalRotation =
                RotationUtils.AlignRotationToNormal(spotNormal, 0); //Mathf.PI/2); //todo set rotation from data!!

            var firstPosition = new Vector3(level1Detail.Pos2D.x, 0, level1Detail.Pos2D.y);

            var billboardYPositionOffset = -outScale.y / 2f;
            spotNormal = new Vector3(spotNormal.y, spotNormal.z, spotNormal.x);
            var finalPosition =
                DesignBodyUtils.MoveToFoundation(outScale.y / 10, billboardYPositionOffset, spotNormal, firstPosition);

            var flatRotation = randomGenerator.FloatValueRange(StringSeed.YRotation, 0f, 360f);
            var uniformPack = new UniformsPack();
            uniformPack.SetUniform("_BaseYRotation", flatRotation);
            uniformPack.SetUniform("_Color", GenerateColor(randomGenerator));

            return new DesignBodyLevel2DetailContainerForCombination(new List<DesignBodyLevel2Detail>()
            {
                new DesignBodyLevel2Detail()
                {
                    Position = finalPosition,
                    Rotation = Quaternion.Euler(finalRotation * Mathf.Deg2Rad),
                    Scale = outScale,
                    UniformsPack = uniformPack
                }
            });
        }

        private Color GenerateColor( MyRandomProvider randomGenerator)
        {
            Color chosenColor = _disposition.Color;

            if (_disposition.ColorGroups != null && _disposition.ColorGroups.Any())
            {
                var count = _disposition.ColorGroups.Count;
                var index = randomGenerator.NextWithMax(StringSeed.ColorIndex, 0, count - 1);
                chosenColor = _disposition.ColorGroups[index];
            }

            return JitterColor(chosenColor, randomGenerator);
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
}
