using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Measuring;
using UnityEngine;

namespace Assets.NPR.Filling
{
    public interface INprRenderingPostProcessingDirector
    {
        void SetMeasurementRenderTargets (MeasurementRenderTargetsSet set);
        void SetAutonomicRendering( bool autonomicRendering);
        void OnPreRenderInternal();
        void OnRenderImageInternal( RenderTexture src, RenderTexture dest);
        void StartInternal();
    }
}
