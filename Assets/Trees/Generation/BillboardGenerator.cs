using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Trees.Generation
{
    public class BillboardGenerator : MonoBehaviour
    {
        private Queue<BillboardTemplateGenerationOrder> _orders = new Queue<BillboardTemplateGenerationOrder>();
        public static String Name = "BillboardGeneratorGameObject";

        private void Awake()
        {
            this.name = Name;
        }

        private IEnumerator Start()
        {
            while (true)
            {
                if (_orders.Count == 0)
                {
                    yield return new WaitForEndOfFrame();
                }
                else
                {
                    yield return FufillOrder(_orders.Dequeue());
                }
            }
        }

        public void AddOrder(BillboardTemplateGenerationOrder generationOrder)
        {
            _orders.Enqueue(generationOrder);
        }

        private IEnumerator FufillOrder(BillboardTemplateGenerationOrder generationOrder)
        {
            int billboardWidth = generationOrder.BillboardWidth;
            int billboardHeight = (int) (billboardWidth / Mathf.Sqrt(2));

            var oldCameraSetting = SetUpCamera(billboardWidth, billboardHeight);

            var oldAngles = generationOrder.BillboardingTarget.transform.eulerAngles;
            generationOrder.BillboardingTarget.transform.eulerAngles = new Vector3(0, 0, 0);

            Bounds originalBounds = generationOrder.BillboardingTarget.GetComponent<Renderer>().bounds;
            var originalBoundsSize = new Vector3(originalBounds.size.x, originalBounds.size.y, originalBounds.size.z);

            var oldScale = generationOrder.BillboardingTarget.transform.localScale;

            var marginMultiplier = generationOrder.BillboardMarginMultiplier;
            generationOrder.BillboardingTarget.transform.localScale = new Vector3(
                                                                          oldScale.x / originalBoundsSize.x,
                                                                          oldScale.y / originalBoundsSize.y,
                                                                          oldScale.z / originalBoundsSize.z) /
                                                                      marginMultiplier;

            generationOrder.BillboardingTarget.SetActive(true);
            var oldPosition = generationOrder.BillboardingTarget.transform.localPosition;
            Debug.Log("T11 OldPosition " + oldPosition);
            generationOrder.BillboardingTarget.transform.localPosition = new Vector3(0, -0.5f, 0);

            int billboardsCount = generationOrder.BillboardsCount;

            List<Texture2D> outList = new List<Texture2D>();
            var whiteBackgroundTex = new Texture2D(billboardWidth, billboardHeight, TextureFormat.ARGB32, false);
            var blackBackgroundTex = new Texture2D(billboardWidth, billboardHeight, TextureFormat.ARGB32, false);
            Camera.main.rect = new Rect(0, 0, (float) billboardWidth / Screen.width,
                (float) billboardHeight / Screen.height);
            for (var i = 0; i < billboardsCount; i++)
            {
                var angle = (360f / billboardsCount) * i;
                // move object to 0,0,0
                generationOrder.BillboardingTarget.transform.eulerAngles = new Vector3(0, angle, 0);

                Camera.main.backgroundColor = Color.white;
                yield return new WaitForEndOfFrame();
                whiteBackgroundTex.ReadPixels(new Rect(0, 0, billboardWidth, billboardHeight), 0, 0);
                whiteBackgroundTex.Apply();

                Camera.main.backgroundColor = Color.black;
                yield return new WaitForEndOfFrame();
                blackBackgroundTex.ReadPixels(new Rect(0, 0, billboardWidth, billboardHeight), 0, 0);
                blackBackgroundTex.Apply();

                var tex = new Texture2D(billboardWidth, billboardHeight, TextureFormat.ARGB32, false);
                for (var x = 0; x < tex.width; x++)
                {
                    for (var y = 0; y < tex.height; y++)
                    {
                        var blackTexPixel = blackBackgroundTex.GetPixel(x, y);
                        var whiteTexPixelRed = whiteBackgroundTex.GetPixel(x, y).r;
                        var blackTexPixelRed = blackTexPixel.r;
                        var alpha = 1.0 - (whiteTexPixelRed - blackTexPixelRed);

                        blackTexPixel = blackTexPixel / (float) alpha;
                        tex.SetPixel(x, y, new Color(blackTexPixel.r, blackTexPixel.g, blackTexPixel.b, (float) alpha));
                    }
                }
                tex.Apply();
                outList.Add(tex);
            }

            generationOrder.BillboardingTarget.transform.localScale = oldScale;
            generationOrder.BillboardingTarget.transform.eulerAngles = oldAngles;
            generationOrder.BillboardingTarget.transform.localPosition = oldPosition;

            generationOrder.Callback(new BillboardTemplateGeneratorResult(outList, originalBoundsSize));
            ResetCamera(oldCameraSetting);
            yield return new WaitForEndOfFrame();
        }


        private CameraSetting SetUpCamera(int billboardWidth, int billboardHeight)
        {
            var setting = new CameraSetting();
            Camera cam = Camera.main;

            setting.ortographic = cam.orthographic;
            setting.rect = cam.rect;
            setting.transform_eulerAngles = cam.transform.eulerAngles;
            setting.nearClipPlane = cam.nearClipPlane;
            setting.farClipPlane = cam.farClipPlane;
            setting.orthographicSize = cam.orthographicSize;
            setting.transform_position = cam.transform.position;
            setting.backgroundColor = cam.backgroundColor;

            cam.orthographic = true;

            var camRectSize
                = new Vector2(
                    billboardWidth / (float) Screen.width,
                    billboardHeight / (float) Screen.height);

            cam.rect = new Rect(0, 0, camRectSize.x, camRectSize.y);
            cam.transform.eulerAngles = Vector3.zero;
            cam.nearClipPlane = 0f;
            cam.farClipPlane = 2f;
            cam.orthographicSize = 1f / 2;
            cam.transform.position = new Vector3(0, 0, -Mathf.Sqrt(2));
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = new Color(0,0,0,0);

            return setting;
        }

        private void ResetCamera(CameraSetting oldCameraSetting)
        {
            Camera cam = Camera.main;
            cam.orthographic = oldCameraSetting.ortographic;
            cam.rect = oldCameraSetting.rect;
            cam.transform.eulerAngles = oldCameraSetting.transform_eulerAngles;
            cam.transform.position = oldCameraSetting.transform_position;
            cam.nearClipPlane = oldCameraSetting.nearClipPlane;
            cam.farClipPlane = oldCameraSetting.farClipPlane;
            cam.orthographicSize = oldCameraSetting.orthographicSize;
            cam.backgroundColor = oldCameraSetting.backgroundColor;
        }
    }

    public class CameraSetting
    {
        public bool ortographic;
        public Rect rect;
        public Vector3 transform_eulerAngles;
        public float nearClipPlane;
        public float farClipPlane;
        public float orthographicSize;
        public Vector3 transform_position;
        public Color backgroundColor;
    }
}