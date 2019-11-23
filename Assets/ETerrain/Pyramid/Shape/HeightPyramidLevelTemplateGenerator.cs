using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.ETerrain.Pyramid.Map;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.Pyramid.Shape
{
    public class HeightPyramidLevelTemplateGenerator
    {
        private HeightPyramidLevelShapeGenerationConfiguration _configuration;

        public HeightPyramidLevelTemplateGenerator(HeightPyramidLevelShapeGenerationConfiguration configuration)
        {
            _configuration = configuration;
        }

        public HeightPyramidLevelTemplate CreateGroup(Vector2 center, bool createCenterObject, Dictionary<int, Vector2> heightMergeRangePerRing)
        {
            List<HeightPyramidSegmentShapeTemplate> shapeTemplates = new List<HeightPyramidSegmentShapeTemplate>();
            Dictionary<int, HeightPyramidPerRingTemplate> perRingTemplates = new Dictionary<int, HeightPyramidPerRingTemplate>();

            var centerObjectLength = _configuration.CenterObjectLength;
            var tssMarginOffsetLength = _configuration.TransitionSingleStepPercent * centerObjectLength;
            if (createCenterObject)
            {
                var centerMergeWidth = _configuration.PerRingMergeWidths[0];
                var mergeStart = centerObjectLength * 0.5f;
                var centerTransitionRange = new Vector2(mergeStart - centerMergeWidth - tssMarginOffsetLength, mergeStart - tssMarginOffsetLength) /
                                            (centerObjectLength * 3f);
                shapeTemplates.Add(new HeightPyramidSegmentShapeTemplate()
                {
                    FlatPosition = new Vector2(-0.5f * centerObjectLength, -0.5f * centerObjectLength),
                    FlatSize = new Vector2(centerObjectLength, centerObjectLength),
                    RingIndex = 0
                });

                perRingTemplates[0] = new HeightPyramidPerRingTemplate()
                {
                    HeightMergeRange = centerTransitionRange,
                };
            }

            var ringCenters = new List<Vector2>()
            {
                new Vector2(0, 3), new Vector2(1, 3), new Vector2(2, 3), new Vector2(3, 3),
                new Vector2(0, 2), new Vector2(3, 2),
                new Vector2(0, 1), new Vector2(3, 1),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 0), new Vector2(3, 0),
            }.Select(c => (c) / 2f + new Vector2(-1, -1)).ToList();

            for (int ringIndex = 1; ringIndex < 3; ringIndex++)
            {
                Vector2 flatSize = Vector2.one * (centerObjectLength * 0.5f) * ringIndex;
                Vector2 transitionRange;
                var mergeWidth = _configuration.PerRingMergeWidths[ringIndex];
                var mergeStart = Mathf.Pow(2, ringIndex - 1) * _configuration.CenterObjectLength;

                if (ringIndex == 1) //todo parametrize and clear. Should not be if here
                {
                    transitionRange = new Vector2(mergeStart - mergeWidth - tssMarginOffsetLength, mergeStart - tssMarginOffsetLength) /
                                      (centerObjectLength * 3f);
                }
                else
                {
                    transitionRange = new Vector2(mergeStart - mergeWidth - tssMarginOffsetLength, mergeStart - tssMarginOffsetLength) /
                                      (centerObjectLength * 3f);
                }

                perRingTemplates[ringIndex] = new HeightPyramidPerRingTemplate()
                {
                    HeightMergeRange = transitionRange,
                };

                foreach (var aRingCenter in ringCenters)
                {
                    Vector2 flatCenter = aRingCenter * centerObjectLength * ringIndex;

                    shapeTemplates.Add(new HeightPyramidSegmentShapeTemplate()
                    {
                        FlatPosition = flatCenter,
                        FlatSize = flatSize,
                        RingIndex = ringIndex
                    });
                }
            }

            var levelFlatSize = new Vector2(4 * centerObjectLength, 4 * centerObjectLength); //todo

            return new HeightPyramidLevelTemplate()
            {
                FlatSize = levelFlatSize,
                Center = center,
                HasCenterObject = createCenterObject,
                PerRingTemplates = perRingTemplates,
                ShapeTemplates = shapeTemplates
            };
        }
    }

    public class HeightPyramidLevelTemplate
    {
        public List<HeightPyramidSegmentShapeTemplate> ShapeTemplates;
        public Dictionary<int, HeightPyramidPerRingTemplate> PerRingTemplates;
        public Vector2 Center;
        public Vector2 FlatSize;
        public bool HasCenterObject;
    }


    public class HeightPyramidSegmentShapeTemplate
    {
        public int RingIndex;
        public Vector2 FlatPosition;
        public Vector2 FlatSize;
    }

    public class HeightPyramidPerRingTemplate
    {
        public Vector2 HeightMergeRange;
    }


    public class HeightPyramidLevelShapeGenerationConfiguration
    {
        public MyRectangle PyramidLevelWorldSize = new MyRectangle(-3 * 90, -3 * 90, 6 * 90, 6 * 90);
        public float YScale = 100;
        public float CenterObjectLength = 90f;
        public float TransitionSingleStepPercent;
        public Dictionary<int, float> PerRingMergeWidths;

        public static int OneRingShapeObjectsCount = 4;
        public IntVector2 CenterObjectMeshVertexLength = new IntVector2(240, 240);
        public IntVector2 RingObjectMeshVertexLength = new IntVector2(60, 60);
    }

    public interface IPyramidShapeInstancer
    {
        HeightPyramidSegmentShapeGroup CreateGroup(HeightPyramidLevelTemplate pyramidLevelTemplate, HeightPyramidLevel level, GameObject pyramidRootGo);
    }

    public class MergedMeshesPyramidShapeInstancer : IPyramidShapeInstancer
    {
        private MeshGeneratorUTProxy _meshGenerator;
        private HeightPyramidLevelShapeGenerationConfiguration _configuration;

        public MergedMeshesPyramidShapeInstancer(MeshGeneratorUTProxy meshGenerator, HeightPyramidLevelShapeGenerationConfiguration configuration)
        {
            _meshGenerator = meshGenerator;
            _configuration = configuration;
        }

        public HeightPyramidSegmentShapeGroup CreateGroup(HeightPyramidLevelTemplate pyramidLevelTemplate, HeightPyramidLevel level, GameObject pyramidRootGo)
        {
            var parentGO = new GameObject("ETerrainParent " + level);
            var center = pyramidLevelTemplate.Center;
            parentGO.transform.localPosition = new Vector3(center.x, 0, center.y);

            var ringTemplateElementMesh = _meshGenerator.AddOrder(() =>
                    PlaneGenerator.CreateETerrainSegmentMesh(_configuration.RingObjectMeshVertexLength.X, _configuration.RingObjectMeshVertexLength.Y))
                .Result; //todo
            var centerMesh = _meshGenerator.AddOrder(() =>
                    PlaneGenerator.CreateETerrainSegmentMesh(_configuration.CenterObjectMeshVertexLength.X, _configuration.CenterObjectMeshVertexLength.Y))
                .Result; //todo

            var shapeMeshesToCombine = new List<CombineInstance>();

            foreach (var shapeTemplate in pyramidLevelTemplate.ShapeTemplates)
            {
                Mesh thisSegmentMesh = null;
                if (shapeTemplate.RingIndex == 0) //center
                {
                    thisSegmentMesh = centerMesh;
                }
                else
                {
                    thisSegmentMesh = ringTemplateElementMesh;
                }

                var trs = CreateTrsMatrixForShape(shapeTemplate);
                shapeMeshesToCombine.Add(new CombineInstance()
                {
                    transform = trs,
                    mesh = thisSegmentMesh
                });
            }

            var finalMesh = _meshGenerator.AddOrder(() =>
            {
                var m = MeshGenerationUtils.CreateMeshAsSum(shapeMeshesToCombine);
                MeshGenerationUtils.RecalculateUvAsInPlane(m);
                return m;
            }).Result; //TODO
            MeshGenerationUtils.SetYBoundsToInfinity(finalMesh);

            var mainObject = CreateShapeObject(finalMesh, "MainObject", center, pyramidLevelTemplate.FlatSize);
            mainObject.transform.SetParent(parentGO.transform);

            parentGO.transform.localScale = new Vector3(1, _configuration.YScale, 1);
            parentGO.transform.SetParent(pyramidRootGo.transform);
            return new HeightPyramidSegmentShapeGroup()
            {
                CentralShape = mainObject,
                ParentGameObject = parentGO,
            };
        }

        private Matrix4x4 CreateTrsMatrixForShape(HeightPyramidSegmentShapeTemplate template)
        {
            return Matrix4x4.TRS(
                new Vector3(template.FlatPosition.x, 0, template.FlatPosition.y),
                Quaternion.identity,
                new Vector3(template.FlatSize.x, 1, template.FlatSize.y)
            );
        }

        private GameObject CreateShapeObject(Mesh mesh, string name, Vector2 center, Vector2 flatSize)
        {
            var go = new GameObject(name);
            go.transform.localPosition = new Vector3(center.x, 0, center.y);
            go.transform.localScale = new Vector3(1, 1, 1);
            var renderer = go.AddComponent<MeshRenderer>();

            var segmentUvs = RectangleUtils.CalculateSubelementUv(_configuration.PyramidLevelWorldSize,
                new MyRectangle(center.x - flatSize.x / 2, center.y - flatSize.y / 2, flatSize.x, flatSize.y));

            renderer.material = new Material(Shader.Find("Custom/ETerrain/Ground"));
            renderer.material.SetVector("_SegmentCoords", segmentUvs.ToVector4());

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            return go;
        }
    }

    public class SeparateMeshPerTemplateShapeInstancer : IPyramidShapeInstancer
    {
        private MeshGeneratorUTProxy _meshGenerator;
        private HeightPyramidLevelShapeGenerationConfiguration _configuration;

        public SeparateMeshPerTemplateShapeInstancer(MeshGeneratorUTProxy meshGenerator, HeightPyramidLevelShapeGenerationConfiguration configuration)
        {
            _meshGenerator = meshGenerator;
            _configuration = configuration;
        }

        public HeightPyramidSegmentShapeGroup CreateGroup(HeightPyramidLevelTemplate pyramidLevelTemplate, HeightPyramidLevel level, GameObject pyramidRootGo)
        {
            var parentGO = new GameObject("ETerrainParent " + level);
            var center = pyramidLevelTemplate.Center;
            parentGO.transform.localPosition = new Vector3(center.x, 0, center.y);

            var objectsPerRing = pyramidLevelTemplate.PerRingTemplates.Keys.ToDictionary(c => c, c => new List<GameObject>());

            GameObject centerObject = null;

            var ring1ElementMesh =
                _meshGenerator.AddOrder(() => PlaneGenerator.CreateETerrainSegmentMesh(
                    _configuration.RingObjectMeshVertexLength.X, _configuration.RingObjectMeshVertexLength.Y)).Result; //todo
            ring1ElementMesh.RecalculateBounds();
            Debug.Log(ring1ElementMesh.bounds);
            MeshGenerationUtils.SetYBounds(ring1ElementMesh, 0f, 1f);
            Debug.Log(ring1ElementMesh.bounds);
            foreach (var shapeTemplate in pyramidLevelTemplate.ShapeTemplates)
            {
                if (shapeTemplate.RingIndex == 0) //center
                {
                    var centerMesh = _meshGenerator.AddOrder(() =>
                            PlaneGenerator.CreateETerrainSegmentMesh(_configuration.CenterObjectMeshVertexLength.X,
                                _configuration.CenterObjectMeshVertexLength.Y))
                        .Result; //todo
                    MeshGenerationUtils.SetYBounds(centerMesh, 0f, 1f);
                    centerObject = CreateShapeObject(centerMesh, shapeTemplate, "Center");

                    centerObject.transform.SetParent(parentGO.transform);
                }
                else
                {
                    var shape = CreateShapeObject(ring1ElementMesh, shapeTemplate, "Ring " + shapeTemplate.RingIndex);
                    shape.transform.SetParent(parentGO.transform);
                    objectsPerRing[shapeTemplate.RingIndex].Add(shape);
                }
            }

            parentGO.transform.localScale = new Vector3(1, _configuration.YScale, 1);
            parentGO.transform.SetParent(pyramidRootGo.transform);
            return new HeightPyramidSegmentShapeGroup()
            {
                CentralShape = centerObject,
                ParentGameObject = parentGO,
                ShapesPerRing = objectsPerRing
            };
        }

        private GameObject CreateShapeObject(Mesh mesh, HeightPyramidSegmentShapeTemplate shapeTemplate, string name)
        {
            var go = new GameObject(name);
            var flatStartPos = shapeTemplate.FlatPosition;
            go.transform.localPosition = new Vector3(flatStartPos.x, 0, flatStartPos.y);
            var flatSize = shapeTemplate.FlatSize;
            go.transform.localScale = new Vector3(flatSize.x, 1, flatSize.y);
            var renderer = go.AddComponent<MeshRenderer>();

            var segmentUvs = RectangleUtils.CalculateSubelementUv(_configuration.PyramidLevelWorldSize,
                new MyRectangle(flatStartPos.x, flatStartPos.y, flatSize.x, flatSize.y));

            renderer.material = new Material(Shader.Find("Custom/ETerrain/Ground"));
            renderer.material.SetVector("_SegmentCoords", segmentUvs.ToVector4());

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            return go;

        }
    }
}
