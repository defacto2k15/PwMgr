using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.NPR.Filling.Szecsi;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Assets.NPR.Filling.MM
{
    public class MyMethodObjectDebugOC : MonoBehaviour
    {
        public bool DrawDebugAreas;
        public bool DrawDebugAngles;
        public bool DrawDebugLighting;
  
        public bool DrawDebugBalls = false;
        private RunOnceBox _runOnce;
        public void Start()
        {
            _runOnce = new RunOnceBox(() =>
            {
                var mat = GetComponent<MeshRenderer>().material;
                var mmPostProcessingDirector = FindObjectOfType<MyMethodPostProcessingDirectorOC>();
                AlignUniforms();
                //AddUniformCycleData(mat);
                AddUniformCycleData3D(mat, mmPostProcessingDirector.SeedPositionTexture3D);
                UpdateMaterialKeywords();
            },4);
        }

        private void AlignUniforms()
        {
            var mat = GetComponent<MeshRenderer>().material;
            var mmPostProcessingDirector = FindObjectOfType<MyMethodPostProcessingDirectorOC>();
            ShaderCustomizationUtils.FilterNonPresentUniforms(mat, mmPostProcessingDirector.MasterUniformsPack).SetUniformsToMaterial(mat);
        }

        public void OnValidate()
        {
            UpdateMaterialKeywords();
        }

        private void UpdateMaterialKeywords()
        {
            Material mat;
            if (Application.isEditor)
            {
                if (GetComponent<MeshRenderer>() != null)
                {
                    mat = GetComponent<MeshRenderer>().sharedMaterial;
                }
                else
                {
                    return;
                }
            }
            else
            {
                mat = GetComponent<MeshRenderer>().material;
            }

            mat.DisableKeyword("DEBUG_DRAW_AREAS");
            mat.DisableKeyword("DEBUG_DRAW_ANGLES");
            mat.DisableKeyword("DEBUG_DRAW_LIGHTING");
            if (DrawDebugAreas)
            {
                mat.EnableKeyword("DEBUG_DRAW_AREAS");
            }
            else if (DrawDebugAngles)
            {
                mat.EnableKeyword("DEBUG_DRAW_ANGLES");
            }
            else if (DrawDebugLighting)
            {
                mat.EnableKeyword("DEBUG_DRAW_LIGHTING");
            }
        }

        private void AddUniformCycleData(Material mat)
        {
            var szecsiCycleGen = new SzecsiCyclesGenerator(2, 2);

            var cycle = szecsiCycleGen.GenerateCycle(123);

            var uniformCycle = szecsiCycleGen.GenerateUniformFromCycle(cycle);

            var uniformGroup = szecsiCycleGen.GenerateUniformGroupFromCycle(cycle, 2);
            var uniformCyclesGroup = uniformGroup.Select(c => (uint)c).ToList();

            var xCycle = uniformCyclesGroup[0];
            var yCycle = uniformCyclesGroup[1];

            mat.SetInt("_UniformCycleX",(int)uniformCyclesGroup[0]);
            mat.SetInt("_UniformCycleY",(int)uniformCyclesGroup[1]);
            
            var buf = new ComputeBuffer(3,sizeof(int));
            buf.SetData(new List<uint>(){uniformCycle, xCycle, yCycle});
            mat.SetBuffer("_UniformCyclesBuf", buf);

            mat.SetTexture("_SeedPositionTex", GenerateSeedPositionTexture2D(uniformGroup));
        }

        private void AddUniformCycleData3D(Material mat, Texture3D seedPositionTexture3D)
        {
            if (DrawDebugBalls)
            {
                GenerateDebugBalls(seedPositionTexture3D);
            }

            mat.SetTexture("_SeedPositionTex3D", seedPositionTexture3D);
        }

        private Texture2D GenerateSeedPositionTexture2D( ulong[] uniformGroup)
        {
            var positionsWithBits = SzecsiCyclesGenerator.GeneratePointsFromUniformGroup(uniformGroup, 2, 16);

            var tex = new Texture2D(4, 4, TextureFormat.ARGB32, false) {wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point};
            for (int i = 0; i < 16; i++)
            {
                var pos = positionsWithBits.Positions[i];

                var seedBlockCoord = new IntVector2(
                        Mathf.FloorToInt(pos[0] * 4),
                        Mathf.FloorToInt(pos[1] * 4)
                    );

               Vector2 seedOffset = new Vector2(
                    pos[0]*4 - seedBlockCoord.X,
                    pos[1]*4 - seedBlockCoord.Y
                   );

                var bits = positionsWithBits.LastCycleBits[i];
               Vector2 cycleLastBits = new Vector2(bits[0], bits[1] );

               tex.SetPixel(seedBlockCoord.X, seedBlockCoord.Y, new Color(seedOffset.x, seedOffset.y, cycleLastBits.x, cycleLastBits.y));

            }
            tex.Apply();
            return tex;
        }

        private void GenerateDebugBalls(Texture3D tex)
        {
            var parent = new GameObject("debugBalls");

            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    for (int z = -1; z < 2; z++)
                    {
                        var bigBlockParent = GenerateOneBigBlockDebugBalls(new Vector3(x, y, z) * 1f, tex, 0.3f);
                        bigBlockParent.transform.SetParent(parent.transform);
                    }
                }
            }

            var bigParent = GenerateOneBigBlockDebugBalls(new Vector3(0, 0, 0), tex, 0.35f/2, true);
            bigParent.transform.localScale = new Vector3(2,2,2);

            bigParent = GenerateOneBigBlockDebugBalls(new Vector3(0, 0, -1), tex, 0.35f/2, true);
            bigParent.transform.localScale = new Vector3(2,2,2);
        }


        private GameObject GenerateOneBigBlockDebugBalls(Vector3 offset, Texture3D tex, float scale, bool colorOverride = false)
        {
            var parent = new GameObject($"bigBlock {offset.x} {offset.y} {offset.z}");

            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        var pix = tex.GetPixel(x, y, z);
                        var position = new Vector3(x, y, z) / 4.0f + (pix.ToVector3() / 4.0f) + offset;

                        var cycleInt = Mathf.RoundToInt(pix.a * 7); 
						int[] cycleLastBits = new int[3];
						cycleLastBits[2] = Mathf.FloorToInt( (cycleInt%8)/4.0f);
						cycleLastBits[1] = Mathf.FloorToInt( (cycleInt%4)/2.0f);
						cycleLastBits[0] = Mathf.FloorToInt( (cycleInt%2)/1.0f);

                        var ball = GenerateOneDebugBall(position, cycleLastBits, scale);
                        ball.name = $"{x} {y} {z}";
                        ball.transform.SetParent(parent.transform);

                        if (colorOverride)
                        {
                            ball.GetComponent<MeshRenderer>().material.color = Color.cyan;
                        }
                    }
                }
            }

            return parent;
        }

        private GameObject GenerateOneDebugBall(Vector3 position, int[] cycleLastBits, float scale)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.position = position;
            ball.transform.localScale = new Vector3(0.1f,0.1f,0.1f) * scale;

            return ball;
        }

        private RunEveryNthTimeBox _uniformsAlignmentBox;
        public void Update()
        {
            _runOnce.Update();
                var mat = GetComponent<MeshRenderer>().material;
            mat.SetMatrix("_CameraInverseProjection", FindObjectOfType<Camera>().projectionMatrix.inverse);

            if (_uniformsAlignmentBox == null)
            {
                _uniformsAlignmentBox = new RunEveryNthTimeBox(AlignUniforms, 30);
            }
            _uniformsAlignmentBox.Update();

        }
    }
}
