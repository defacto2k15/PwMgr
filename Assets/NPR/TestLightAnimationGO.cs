using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Measuring.Scenarios;
using UnityEngine;
using UnityEngine.Playables;

namespace Assets.NPR
{
    public class TestLightAnimationGO : MonoBehaviour
    {
        public int SequenceFramesCount;
        public PlayableDirector Director;

        public void Update()
        {
            int requestedFrame = Time.frameCount % SequenceFramesCount;
            MeasurementUtils.SetAnimationToMeasurement(Director, SequenceFramesCount, requestedFrame);
        }
    }
}
