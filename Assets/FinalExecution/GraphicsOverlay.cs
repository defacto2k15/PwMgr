using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ETerrain;
using Assets.Scheduling;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class GraphicsOverlay : MonoBehaviour
    {
        public GameObject Traveller;
        public Camera Cam;
        public GlobalServicesProfileInfo ServicesProfileInfo;

        private float _deltaTime = 0.0f;
        private bool _overlayEnabled = true;
        private bool _travellerConnectedToCamera;
        private List<MovementBlockingProcess> _movementPossibilityDetails;

        public void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            if (Input.GetKeyDown(KeyCode.C))
            {
                _overlayEnabled = !_overlayEnabled;
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                _travellerConnectedToCamera = !_travellerConnectedToCamera;
                Debug.Log("IsTravellerConnected "+_travellerConnectedToCamera);
            }

        }

        private bool debugObce = false;
        public void OnGUI()
        {
            if (_overlayEnabled)
            {
                WriteFpsInformation();
                if (ServicesProfileInfo != null)
                {
                    WriteServicesInformation();
                }

                if (_movementPossibilityDetails != null)
                {
                    if (_movementPossibilityDetails.Count > 0)
                    {
                        DrawMovementPossibilityDetailsInfo();
                    }

                    if (_travellerConnectedToCamera )
                    {
                        if (_movementPossibilityDetails == null || !_movementPossibilityDetails.Any())
                        {
                            Traveller.transform.position = Cam.transform.position;
                            debugObce = true;
                        }
                        else
                        {
                            if (debugObce)
                            {
                                Cam.transform.position = Traveller.transform.position;
                            }
                        }
                    }
                }
            }
        }

        private void DrawMovementPossibilityDetailsInfo()
        {
            int w = Screen.width, h = Screen.height; 
            Rect rect = new Rect(10, 100, w , h );
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = h * 12 / 100;
            style.normal.textColor = new Color(0.7f, 0.0f, 0.0f, 1.0f);
            GUI.Label(rect, "MOVEMENT BLOCKED", style);

            int i = 0;
            foreach (var blockingDetail in _movementPossibilityDetails)
            {
                rect = new Rect(10, 150 + (30*i), w , h );
                style.fontSize = h * 5 / 100;
                style.normal.textColor = new Color(0.0f,0,0,1);
                GUI.Label(rect, $"Blocking process: {blockingDetail.ProcessName} Count {blockingDetail.BlockCount}", style);
                i++;
            }
        }

        private void WriteFpsInformation()
        {
            int w = Screen.width, h = Screen.height; // na bazie http://wiki.unity3d.com/index.php?title=FramesPerSecond

            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(10, 10, w * 2, h * 4 / 10f);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 6 / 100;
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

            int currentHeight = 60;
            foreach (var otService in ServicesProfileInfo.OtServicesProfileInfo)
            {
                var color = Color.green;
                if (otService.IsWorking)
                {
                    color = Color.magenta;
                }else if (otService.BlockedTasksCount > 0)
                {
                    color = Color.yellow;
                }else if (otService.ContinuingTasksCount > 0)
                {
                    color = Color.blue;
                }else if (otService.NewTaskCount > 0)
                {
                    color = Color.black;
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
                if (utService.WorkQueueSize > 0)
                {
                    color1 = Color.grey;
                }
                _cachedStringBuilder.Append($"{utService.Name} Queue size: {utService.WorkQueueSize}");

                GUIStyle style1 = new GUIStyle();
                style1.normal.textColor = color1;
                GUI.Label(new Rect(10, currentHeight, width, height), _cachedStringBuilder.ToString(), style1);
                currentHeight += height + 5;
                _cachedStringBuilder.Length = 0;
            }
        }

        public void SetMovementPossibilityDetails(List<MovementBlockingProcess> movementPossibilityDetails)
        {
            _movementPossibilityDetails = movementPossibilityDetails;
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

