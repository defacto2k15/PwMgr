using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scheduling;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class GraphicsOverlay : MonoBehaviour
    {
        private float _deltaTime = 0.0f;

        public GlobalServicesProfileInfo ServicesProfileInfo;

        public void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        public void OnGUI()
        {
            WriteFpsInformation();
            if (ServicesProfileInfo != null)
            {
                WriteServicesInformation();
            }
        }

        private void WriteFpsInformation()
        {
            int w = Screen.width, h = Screen.height; // na bazie http://wiki.unity3d.com/index.php?title=FramesPerSecond

            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(10, 10, w * 2, h * 4 / 40);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 100;
            style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);

            float msec = _deltaTime * 1000.0f;
            float fps = 1.0f / _deltaTime;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

            GUI.Label(rect, text, style);

            //GraphManager.Graph.Plot("Frames per second", fps, Color.green, new Rect(0, Screen.height - 100, 500, 100));
        }

        private StringBuilder _cachedStringBuilder = new StringBuilder(1000);
        private void WriteServicesInformation()
        {
            int width = 500;
            int height = 10;

            int currentHeight = 40;
            foreach (var otService in ServicesProfileInfo.OtServicesProfileInfo)
            {
                var color = Color.green;
                if (otService.IsWorking)
                {
                    color = Color.magenta;
                }

                _cachedStringBuilder.Append($"{otService.Name}  New:{otService.NewTaskCount} Continuing:{otService.ContinuingTasksCount} Blocked:{otService.BlockedTasksCount}");

                GUIStyle style = new GUIStyle();
                style.normal.textColor = color;
                GUI.Label(new Rect(10, currentHeight, width, height), _cachedStringBuilder.ToString(), style);

                currentHeight += height + 5;
                _cachedStringBuilder.Length = 0;
            }

            currentHeight = currentHeight+40;
            foreach (var utService in ServicesProfileInfo.UtServicesProfileInfo.OrderBy(c => c.Name))
            {
                var color1 = Color.cyan;
                _cachedStringBuilder.Append($"{utService.Name} Queue size: {utService.WorkQueueSize}");

                GUIStyle style1 = new GUIStyle();
                style1.normal.textColor = color1;
                GUI.Label(new Rect(10, currentHeight, width, height), _cachedStringBuilder.ToString(), style1);
                currentHeight += height + 5;
                _cachedStringBuilder.Length = 0;
            }
        }
    }

    public class LoadingLabel
    {
        private List<string> _phases;
        private float _cycleSeconds;


        public LoadingLabel(List<string> phases, float cycleSeconds)
        {
            _phases = phases;
            _cycleSeconds = cycleSeconds;
        }

        public void OnGUI()
        {
            var timeInCycle = Mathf.Repeat(Time.time, _cycleSeconds);
            var index = Mathf.Min((int)(Mathf.FloorToInt(timeInCycle / _cycleSeconds * _phases.Count)), _phases.Count-1);
            
            Rect rect = new Rect(200,200,400,400);
            GUI.Label(rect, _phases[index]);
        }
    }
}

