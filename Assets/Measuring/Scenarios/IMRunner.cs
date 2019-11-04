using System.Collections.Generic;
using Assets.NPR.Filling;

namespace Assets.Measuring.Scenarios
{
    public interface IMRunner
    {
        void Enable(MOneTestConfiguration configuration, INprRenderingPostProcessingDirector ppDirector, List<IMRunnerSupport> supports);
        bool TestFinished { get; }
    }
}