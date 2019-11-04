using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Measuring;
using Assets.NPR.Filling.Szecsi;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.TextureRendering;
using MathNet.Numerics.Statistics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.NPR.Filling.MM
{
    public class MyMethodPostProcessingDirectorOC : MonoBehaviour, INprRenderingPostProcessingDirector
    {
        public Material StrokeSeedGridMapDebugRendererMaterial;
        public Material Stage1RenderingMaterial;
        public Material GeometryRenderingMaterial;
        public Material GeometryAlphaRenderingMaterial;

        private Material SelectedGeometryMaterial 
        {
            get
            {
                if (RenderHatchesWithAlpha)
                {
                    return GeometryAlphaRenderingMaterial;
                }
                else
                {
                    return GeometryRenderingMaterial;
                }
            }

        }

        public Material DebugGridRenderingMaterial;
        public bool UseDebugGridRenderingMaterial;

        public Texture2D StrokeTexture;
        public int ScreenCellHeightMultiplier = 6;
        public int ScreenCellBaseLength = 8;
        public int SsgmRotationSliceCount = 32;

        public int TierCount = 1;
        public int VectorQuantCount = 8;
        public float SeedSamplingMultiplier;

        public int Stage1RenderingResolutionMultiplier = 8;

        private RenderTexture _strokeSeedGridMap;

        private IntVector2 _ssgmBlockSize => new IntVector2(1,ScreenCellHeightMultiplier)*ScreenCellBaseLength;
        private IntVector2 _ssgmResolution;
        private IntVector2 _stage1RenderingSize;

        private RenderTexture _worldPositionRenderTarget;
        private RenderTexture _vectorsRenderTarget;

        private RenderTexture _dummyStage1Texture;

        private Texture3D _seedPositionTexture3D;
        private UniformsPack _masterUniformsPack;
        private bool _writeRenderTargetsToFile = false;
        private RenderTexture _artisticMainRenderTextureCopyForTesting;

        /////////////////////////////////// Geometry rendering
        
        public bool RenderHatchesWithGeometry;
        public bool RenderHatchesWithAlpha;
        private ComputeBuffer _geometryRenderingFragmentBuffer;
        private ComputeBuffer _geometryRenderingArgBuffer;

        public UniformsPack MasterUniformsPack => _masterUniformsPack;
        public Texture3D SeedPositionTexture3D => _seedPositionTexture3D;

        public MeasurementRenderTargetsSet MeasurementRenderTargets;
        public bool AutonomicRendering;

        public void Start()
        {
            if (AutonomicRendering)
            {
                StartInternal();
            }
        }

        public void StartInternal()
        {


            InitializeRenderTargets();
            InitializeMeasurementRenderTargets();

            var additionalMarginBlocksSize = 0;
            _ssgmResolution = new IntVector2(
                Mathf.CeilToInt(Screen.width / (float) _ssgmBlockSize.X) + additionalMarginBlocksSize,
                Mathf.CeilToInt(Screen.height / (float) _ssgmBlockSize.Y) + additionalMarginBlocksSize);

            _strokeSeedGridMap = new RenderTexture(_ssgmResolution.X, _ssgmResolution.Y, 0, RenderTextureFormat.ARGBFloat)
                //_strokeSeedGridMap = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat)
                {
                    useMipMap = false,
                    dimension = TextureDimension.Tex2DArray,
                    volumeDepth = SsgmRotationSliceCount * TierCount,
                    enableRandomWrite = true,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                }; //todo format change
            _strokeSeedGridMap.Create();

            Debug.Log("StartSize: " + (_ssgmResolution.X * _ssgmResolution.Y * SsgmRotationSliceCount * TierCount));

            _stage1RenderingSize = new IntVector2(Screen.width / Stage1RenderingResolutionMultiplier, Screen.height / Stage1RenderingResolutionMultiplier);
            _stage1RenderingSize = new IntVector2(Mathf.ClosestPowerOfTwo(_stage1RenderingSize.X), Mathf.ClosestPowerOfTwo(_stage1RenderingSize.Y));
            Debug.Log("Stage1 Rendering size is " + _stage1RenderingSize);

            _dummyStage1Texture = new RenderTexture(_stage1RenderingSize.X, _stage1RenderingSize.Y, 0, RenderTextureFormat.ARGB32);
            _dummyStage1Texture.filterMode = FilterMode.Trilinear;
            _dummyStage1Texture.useMipMap = true;
            _dummyStage1Texture.autoGenerateMips = true;
            _dummyStage1Texture.Create();

            _geometryRenderingFragmentBuffer =
                new ComputeBuffer(_stage1RenderingSize.X * _stage1RenderingSize.Y * TierCount, 4 * sizeof(int), ComputeBufferType.Append);
            _geometryRenderingArgBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);

            ComputeUniformCycleData3D();

            SetMaterialParameters(StrokeSeedGridMapDebugRendererMaterial);
            SetMaterialParameters(Stage1RenderingMaterial);
            SetMaterialParameters(SelectedGeometryMaterial);
            SetMaterialParameters(DebugGridRenderingMaterial);

            AlignUniforms();

            _artisticMainRenderTextureCopyForTesting = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _artisticMainRenderTextureCopyForTesting.filterMode = FilterMode.Point;
            _artisticMainRenderTextureCopyForTesting.Create();
        }

        public void SetMeasurementRenderTargets(MeasurementRenderTargetsSet set)
        {
            MeasurementRenderTargets = set;
        }

        public void SetAutonomicRendering(bool autonomicRendering)
        {
            AutonomicRendering = autonomicRendering;
        }

        private void InitializeMeasurementRenderTargets()
        {
            if (AutonomicRendering)
            {
                var artisticMainRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
                artisticMainRenderTexture.filterMode = FilterMode.Point;

                var hatchMainRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
                hatchMainRenderTexture.filterMode = FilterMode.Point;

                var idRenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                idRenderTexture.filterMode = FilterMode.Point;

                var worldPos1RenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                worldPos1RenderTexture.filterMode = FilterMode.Point;

                var worldPos2RenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                worldPos2RenderTexture.filterMode = FilterMode.Point;

                MeasurementRenderTargets = new MeasurementRenderTargetsSet()
                {
                    ArtisticMainTexture = artisticMainRenderTexture,
                    HatchMainTexture = hatchMainRenderTexture,
                    HatchIdTexture = idRenderTexture,
                    WorldPosition1Texture = worldPos1RenderTexture,
                    WorldPosition2Texture = worldPos2RenderTexture
                };
            }
        }


        private void SetObjectRenderingTargetBuffers()
        {
            var renderBuffers = new List<RenderTexture>
            {
                MeasurementRenderTargets.ArtisticMainTexture,
                _worldPositionRenderTarget,
                _vectorsRenderTarget,
                MeasurementRenderTargets.HatchMainTexture,
                MeasurementRenderTargets.HatchIdTexture,
                MeasurementRenderTargets.WorldPosition1Texture,
                MeasurementRenderTargets.WorldPosition2Texture
            }.Select(c => c.colorBuffer).ToArray();
            GetComponent<Camera>().SetTargetBuffers(renderBuffers, MeasurementRenderTargets.ArtisticMainTexture.depthBuffer);
        }

        private void AlignUniforms()
        {
            var debugRendererPack = ShaderCustomizationUtils.RetriveUniforms(StrokeSeedGridMapDebugRendererMaterial);
            var stage1Pack = ShaderCustomizationUtils.RetriveUniforms(Stage1RenderingMaterial);
            var geometryPack = ShaderCustomizationUtils.RetriveUniforms(SelectedGeometryMaterial);

            _masterUniformsPack = UniformsPack.MergeTwo(UniformsPack.MergeTwo(debugRendererPack, stage1Pack), geometryPack).WithoutDebugUniforms();

            new List<Material>() { StrokeSeedGridMapDebugRendererMaterial, Stage1RenderingMaterial,  SelectedGeometryMaterial}.ForEach(m =>
             {
                 ShaderCustomizationUtils.FilterNonPresentUniforms(m, _masterUniformsPack).SetUniformsToMaterial(m);
             });
        }

        public struct MMStage1ToRenderingFragment
        {
            Vector2 screenCoords;
            float strokeAngle;
            float sParam;
            int id;
        }

        private void SetMaterialParameters(Material mat)
        {
            mat.SetTexture("_StrokeSeedGridMap", _strokeSeedGridMap);
            mat.SetTexture("_StrokeTex", StrokeTexture);

            mat.SetInt("_RotationSlicesCount", SsgmRotationSliceCount);
            mat.SetVector("_BlockSize", new Vector4(_ssgmBlockSize.X, _ssgmBlockSize.Y, 0, 0));
            mat.SetVector("_BlockCount", new Vector4(_ssgmResolution.X, _ssgmResolution.Y, 0, 0));
            mat.SetFloat("_ScreenCellHeightMultiplier", ScreenCellHeightMultiplier );

            mat.SetVector("_Stage1RenderingSize", new Vector4(_stage1RenderingSize.X, _stage1RenderingSize.Y, 0, 0));

            mat.SetTexture("_MainTex", MeasurementRenderTargets.ArtisticMainTexture);
            mat.SetTexture("_ArtisticMainTex", MeasurementRenderTargets.ArtisticMainTexture);
            mat.SetTexture("_BaseMainTex", MeasurementRenderTargets.ArtisticMainTexture);
            mat.SetTexture("_WorldPositionTex", _worldPositionRenderTarget);
            mat.SetTexture("_VectorsTex", _vectorsRenderTarget);
            mat.SetTexture("_DummyStage1Texture", _dummyStage1Texture);

            mat.SetTexture("_SeedPositionTex3D", _seedPositionTexture3D);

            mat.SetInt("_TierCount", TierCount);
            mat.SetInt("_VectorQuantCount", VectorQuantCount);
            mat.SetFloat("_SeedSamplingMultiplier", SeedSamplingMultiplier);

            mat.SetBuffer("_GeometryRenderingFragmentBuffer", _geometryRenderingFragmentBuffer);
        }

        private void InitializeRenderTargets()
        {
            _worldPositionRenderTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat)
            {
                useMipMap = false, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            _worldPositionRenderTarget.Create();

            _vectorsRenderTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat)
            {
                useMipMap = false, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            _vectorsRenderTarget.Create();
        }


        private RunEveryNthTimeBox _uniformsAlignmentBox;

        public void Update()
        {
            if (_uniformsAlignmentBox == null)
            {
                _uniformsAlignmentBox = new RunEveryNthTimeBox(AlignUniforms, 30);
            }
            _uniformsAlignmentBox.Update();

            if (Input.GetKeyDown(KeyCode.B))
            {
                _writeRenderTargetsToFile = true;
            }


        }

        public List<Material> RenderingMaterials => new List<Material>()
        {
            StrokeSeedGridMapDebugRendererMaterial,
            Stage1RenderingMaterial,
            SelectedGeometryMaterial,
            DebugGridRenderingMaterial,
        };

        private void UpdateGeometryRenderBuffers()
        {
            int[] args = new int[]{0,1,0,0};
            _geometryRenderingArgBuffer.SetData(args);

            ComputeBuffer.CopyCount(_geometryRenderingFragmentBuffer, _geometryRenderingArgBuffer, 0);
            _geometryRenderingArgBuffer.GetData(args);
            Debug.Log($"A: {args[0]} {args[1]} {args[2]} {args[3]}");
            _geometryRenderingArgBuffer.SetCounterValue(0);
        }

        public void OnPreRender()
        {
            if (AutonomicRendering)
            {
                OnPreRenderInternal();
            }
        }

        public void OnPreRenderInternal()
        {
            ClearSSGM();
            ClearRenderTextures();
            SetObjectRenderingTargetBuffers();
            if (RenderHatchesWithGeometry)
            {
                Graphics.SetRandomWriteTarget(4, _strokeSeedGridMap);
            }
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (AutonomicRendering)
            {
                OnRenderImageInternal(src,dest);
                if (RenderHatchesWithGeometry)
                {
                    Graphics.ClearRandomWriteTargets();
                    if (UseDebugGridRenderingMaterial)
                    {
                        Graphics.Blit(src, dest, DebugGridRenderingMaterial);
                    }
                    else
                    {
                        Graphics.Blit(src, dest);
                    }
                }
                else
                {
                    if (UseDebugGridRenderingMaterial)
                    {
                        Graphics.Blit(dest, MeasurementRenderTargets.ArtisticMainTexture);
                        Graphics.Blit(MeasurementRenderTargets.ArtisticMainTexture, dest, DebugGridRenderingMaterial);
                    }
                }
            }
            else
            {
                Graphics.Blit(src, dest);
            }

            if (_writeRenderTargetsToFile)
            {
                _writeRenderTargetsToFile = !_writeRenderTargetsToFile;
                WriteRenderTargetsToFile();
            }
        }

        private void WriteRenderTargetsToFile()
        {
            var targetDirectory = @"C:\tmp\diagrams\mmRenderTargets\";
            Directory.CreateDirectory(targetDirectory);

            SavingFileManager.SaveTextureToPngFile(targetDirectory+"/artistic.png", UltraTextureRenderer.RenderTextureToTexture2D(MeasurementRenderTargets.ArtisticMainTexture));
            SavingFileManager.SaveTextureToPngFile(targetDirectory+"/worldPos.png", UltraTextureRenderer.RenderTextureToTexture2D(_worldPositionRenderTarget));
            SavingFileManager.SaveTextureToPngFile(targetDirectory+"/vectors.png", UltraTextureRenderer.RenderTextureToTexture2D(_vectorsRenderTarget));

            //var dummyTex2D = UltraTextureRenderer.RenderTextureToTexture2D(_dummyStage1Texture);
            //SavingFileManager.SaveTextureToPngFile(targetDirectory + "/dummy.png", dummyTex2D);
            //SavingFileManager.SaveTextureToPngFile(targetDirectory + "/dummyL1.png", dummyTex2D.RetriveMipmapAsTexture(1));
            //SavingFileManager.SaveTextureToPngFile(targetDirectory + "/dummyL1.png", dummyTex2D.RetriveMipmapAsTexture(2));
        }

        public void OnRenderImageInternal(RenderTexture src, RenderTexture dest)
        {
            if (RenderHatchesWithGeometry)
            {
                UpdateGeometryRenderBuffers();
            }
            
            var matrix4X4 = CalculateVPMatrix();

            Stage1RenderingMaterial.SetMatrix("_MyUnityMatrixVP", matrix4X4);
            StrokeSeedGridMapDebugRendererMaterial.SetMatrix("_MyUnityMatrixVP", matrix4X4);

            var oldActiveTexture = RenderTexture.active;
            if (!AutonomicRendering)
            {
                Graphics.Blit(MeasurementRenderTargets.ArtisticMainTexture, _artisticMainRenderTextureCopyForTesting);
            }

            Graphics.ClearRandomWriteTargets();
            Graphics.SetRandomWriteTarget(1, _strokeSeedGridMap);
            Graphics.SetRandomWriteTarget(2, _geometryRenderingFragmentBuffer);
            Graphics.Blit(null, _dummyStage1Texture, Stage1RenderingMaterial, 0);
            Graphics.ClearRandomWriteTargets();

            Graphics.SetRenderTarget(new RenderBuffer[]
            {
                MeasurementRenderTargets.ArtisticMainTexture.colorBuffer,
                MeasurementRenderTargets.HatchMainTexture.colorBuffer,
                MeasurementRenderTargets.HatchIdTexture.colorBuffer,
                MeasurementRenderTargets.WorldPosition1Texture.colorBuffer,
                MeasurementRenderTargets.WorldPosition2Texture.colorBuffer,
            }, MeasurementRenderTargets.ArtisticMainTexture.depthBuffer);

            if (RenderHatchesWithGeometry)
            {
                SelectedGeometryMaterial.SetPass(0);
                SelectedGeometryMaterial.SetBuffer("_GeometryRenderingFragmentBuffer", _geometryRenderingFragmentBuffer);

                Graphics.DrawProceduralIndirectNow(MeshTopology.Points, _geometryRenderingArgBuffer, 0);
            } 
            else
            {
                if (AutonomicRendering)
                {
                    Graphics.Blit(src, dest, StrokeSeedGridMapDebugRendererMaterial);
                }
                else
                {
                    StrokeSeedGridMapDebugRendererMaterial.SetTexture("_ArtisticMainTex", _artisticMainRenderTextureCopyForTesting);
                    Graphics.Blit(src,StrokeSeedGridMapDebugRendererMaterial);
                }
            }
            RenderTexture.active = oldActiveTexture;
        }

        private void ClearRenderTextures()
        {
            var renderTextures = new List<RenderTexture>
            {
                _worldPositionRenderTarget,
                _vectorsRenderTarget,
            }.ToList();
            var oldRenderTarget = RenderTexture.active;
            for (int i = 0; i < renderTextures.Count; i++)
            {
                RenderTexture.active = renderTextures[i];
                GL.Clear(true, true, Color.clear);
            }

            RenderTexture.active = oldRenderTarget;
        }

        private Matrix4x4 CalculateVPMatrix()
        {
            var camera = GetComponent<Camera>();
            var p = GL.GetGPUProjectionMatrix(camera.projectionMatrix,
                false); // Unity flips its 'Y' vector depending on if its in VR, Editor view or game view etc... (facepalm)
            var matrix4X4 = p * camera.worldToCameraMatrix;
            matrix4X4[1, 0] *= -1;
            matrix4X4[1, 1] *= -1;
            matrix4X4[1, 2] *= -1;
            matrix4X4[1, 3] *= -1;
            return matrix4X4;
        }

        private void ClearSSGM()
        {
            var frameBuffer = RenderTexture.active;
            for (int i = 0; i < _strokeSeedGridMap.volumeDepth; i++)
            {
                Graphics.SetRenderTarget(_strokeSeedGridMap, 0, CubemapFace.Unknown, i);
                GL.Clear(false, true, new Color(0.4f, 0, 0.8f, 0));
            }
            RenderTexture.active = frameBuffer;
        }

        void OnDestroy()
        {
            _strokeSeedGridMap?.Release();

            _geometryRenderingArgBuffer?.Release();
            _geometryRenderingFragmentBuffer?.Release();
        }

        private void ComputeUniformCycleData3D()
        {
            PointsWithLastBits bestDistribution;
            var distributionGenerator = new MMSeedPositionDistributorGenerator();
            bestDistribution = distributionGenerator.FindBestPointsDistributionBasedOn2D();

            var seedPositionsArray = MMSeedPositionTextureGenerationUtils.GenerateSeedPositionsArray3D(bestDistribution);
            _seedPositionTexture3D =  MMSeedPositionTextureGenerationUtils.GenerateSeedPositionTexture3DFromArray(seedPositionsArray);
        }
    }
}
