using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Trees.Generation
{
    public class BillboardTemplateGenerationOrder
    {
        private readonly int _billboardWidth;
        private readonly int _billboardsCount;
        private readonly GameObject _billboardingTarget;
        private readonly Action<BillboardTemplateGeneratorResult> _callback;
        private float _billboardMarginMultiplier;

        public BillboardTemplateGenerationOrder(
            int billboardWidth,
            int billboardsCount,
            GameObject billboardingTarget,
            Action<BillboardTemplateGeneratorResult> callback, float billboardMarginMultiplier)
        {
            _billboardWidth = billboardWidth;
            _billboardsCount = billboardsCount;
            _billboardingTarget = billboardingTarget;
            _callback = callback;
            _billboardMarginMultiplier = billboardMarginMultiplier;
        }

        public int BillboardWidth
        {
            get { return _billboardWidth; }
        }

        public int BillboardsCount
        {
            get { return _billboardsCount; }
        }

        public GameObject BillboardingTarget
        {
            get { return _billboardingTarget; }
        }

        public Action<BillboardTemplateGeneratorResult> Callback
        {
            get { return _callback; }
        }

        public float BillboardMarginMultiplier => _billboardMarginMultiplier;
    }
}