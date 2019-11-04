using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Threading;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing
{
    public class GpuInstancingDebugObject : MonoBehaviour
    {
        //private GpuInstancingVegetationSubjectContainer _container;
        //private Queue<GpuInstanceId> allInstancesStack  = new Queue<GpuInstanceId>( 100*100);

        private Queue<GpuBucketedInstanceId> globalAllInstancesStack = new Queue<GpuBucketedInstanceId>(100 * 100);
        private GlobalGpuInstancingContainer _globalContainer = new GlobalGpuInstancingContainer();
        private int _capsuleBucketId;
        private int _cubeBucketId;

        //public void StartSingle()
        //{
        //    var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        //    var capsuleMesh = capsule.GetComponent<MeshFilter>().mesh;

        //    var instancingMaterial = new Material(Shader.Find("Custom/TestPlainInstancingMaterial"));

        //    _container = new GpuInstancingVegetationSubjectContainer(
        //            new GpuInstancerCommonData(capsuleMesh, instancingMaterial, new UniformsPack()),
        //            new GpuInstancingUniformsArrayTemplate( new List<GpuInstancingUniformTemplate>()
        //            {
        //                new GpuInstancingUniformTemplate("_Color", GpuInstancingUniformType.Vector4),
        //                new GpuInstancingUniformTemplate("_DbgValue", GpuInstancingUniformType.Float),
        //            })
        //        );
        //    var cyclicJobExecutor = new CyclicJobExecutingThread();
        //    cyclicJobExecutor.AddJob(_container.RetriveCyclicJob());
        //    cyclicJobExecutor.Start();

        //    var sw = new Stopwatch();
        //    sw.Start();
        //    int addedCount = 0;
        //    for (int x = 0; x < 1000; x += 10)
        //    {
        //        for (int y = 0; y < 1000; y += 10)
        //        {
        //            AddInstance(x,y);
        //            addedCount++;
        //        }
        //    }
        //    _container.FinishUpdateBatch();
        //    sw.Stop();
        //    UnityEngine.Debug.Log("T63 it took " + sw.ElapsedTicks / addedCount+" on average");
        //    UnityEngine.Debug.Log("T63 it took " + sw.ElapsedMilliseconds+"ms on sum");
        //}

        public void Start() //startGlobal
        {
            TaskUtils.SetGlobalMultithreading(false);

            var mesh1 = GameObject.CreatePrimitive(PrimitiveType.Capsule).GetComponent<MeshFilter>().mesh;
            var mesh2 = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().mesh;

            var instancingMaterial = new Material(Shader.Find("Custom/TestPlainInstancingMaterial"));
            instancingMaterial.enableInstancing = true;

            var container1 = new GpuInstancingVegetationSubjectContainer(
                new GpuInstancerCommonData(mesh1, instancingMaterial, new UniformsPack()),
                new GpuInstancingUniformsArrayTemplate(new List<GpuInstancingUniformTemplate>()
                {
                    new GpuInstancingUniformTemplate("_Color", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_DbgValue", GpuInstancingUniformType.Float),
                })
            );
            var container2 = new GpuInstancingVegetationSubjectContainer(
                new GpuInstancerCommonData(mesh2, instancingMaterial, new UniformsPack()),
                new GpuInstancingUniformsArrayTemplate(new List<GpuInstancingUniformTemplate>()
                {
                    new GpuInstancingUniformTemplate("_Color", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_DbgValue", GpuInstancingUniformType.Float),
                })
            );

            _capsuleBucketId = _globalContainer.CreateBucket(container1);
            _cubeBucketId = _globalContainer.CreateBucket(container2);

            int addedCount = 0;
            for (int x = 0; x < 1000; x += 10)
            {
                for (int y = 0; y < 1000; y += 10)
                {
                    AddInstanceGlobal(x, y, addedCount);
                    addedCount++;
                }
            }
            _globalContainer.StartThread();
            _globalContainer.FinishUpdateBatch();
        }

        private int lastInstanceIdx = 100 * 100;

        private int frameNo = 0;
        //public void UpdateR()
        //{
        //    _container.DrawFrame();
        //    if (frameNo == 10)
        //    {
        //        for (int i = 0; i < 50; i++)
        //        {
        //            _container.RemoveInstance(allInstancesStack.Dequeue());
        //        }
        //        _container.FinishUpdateBatch();
        //    }
        //    frameNo++;
        //}

        //public void UpdateSingle()
        //{

        //    _container.DrawFrame();
        //    if (Time.frameCount%5 != 0)
        //    {
        //        return;
        //    }
        //    var perUpdateAddedCount = UnityEngine.Random.Range(15, 25) / 4;
        //    var perUpdateRemovedCount = UnityEngine.Random.Range(25, 35) / 4;
        //    for (int i = 0; i < Math.Min(perUpdateRemovedCount, allInstancesStack.Count); i++)
        //    {
        //        _container.RemoveInstance(allInstancesStack.Dequeue());
        //    }
        //    for (int i = 0; i < perUpdateAddedCount; i++)
        //    {
        //        var x = (lastInstanceIdx / 100) % 20000;
        //        var y = lastInstanceIdx % 100;
        //        AddInstance(x * 10, y * 10);
        //        lastInstanceIdx++;
        //    }
        //    _container.FinishUpdateBatch();
        //}

        public void Update()
        {
            _globalContainer.DrawFrame();
            if (Time.frameCount % 5 != 0)
            {
                return;
            }
            var perUpdateAddedCount = UnityEngine.Random.Range(15, 25) / 4;
            var perUpdateRemovedCount = UnityEngine.Random.Range(25, 35) / 4;
            for (int i = 0; i < Math.Min(perUpdateRemovedCount, globalAllInstancesStack.Count); i++)
            {
                _globalContainer.RemoveInstance(globalAllInstancesStack.Dequeue());
            }
            for (int i = 0; i < perUpdateAddedCount; i++)
            {
                var x = (lastInstanceIdx / 100) % 20000;
                var y = lastInstanceIdx % 100;
                AddInstanceGlobal(x * 10, y * 10, lastInstanceIdx);
                lastInstanceIdx++;
            }
            _globalContainer.FinishUpdateBatch();
        }

        //private void AddInstance(int x, int y)
        //{
        //    var uniformsPack = new UniformsPack();
        //    float r = ((float) (x%100))/100f;
        //    float g = ((float) (y%100))/100f;
        //    float b = ((float) ((x+y)%100))/100f;
        //    float scalar = ((x + 2*y)%200)/200f;

        //    uniformsPack.SetUniform("_Color", new Vector4( r,g,b,1.0f));
        //    uniformsPack.SetUniform("_DbgValue", scalar);
        //    var id = _container.AddInstance(TransformUtils.GetLocalToWorldMatrix(new Vector3(x, 0, y), Vector3.zero,
        //        Vector3.one), uniformsPack);
        //    allInstancesStack.Enqueue( id);
        //}

        private void AddInstanceGlobal(int x, int y, int idx)
        {
            var uniformsPack = new UniformsPack();
            float r = ((float) (x % 100)) / 100f;
            float g = ((float) (y % 100)) / 100f;
            float b = ((float) ((x + y) % 100)) / 100f;
            float scalar = ((x + 2 * y) % 200) / 200f;

            uniformsPack.SetUniform("_Color", new Vector4(r, g, b, 1.0f));
            uniformsPack.SetUniform("_DbgValue", scalar);

            var matrix = TransformUtils.GetLocalToWorldMatrix(new Vector3(x, 0, y), UnityEngine.Vector3.zero,
                UnityEngine.Vector3.one);

            var bucketId = _cubeBucketId;
            if (idx % 2 == 0)
            {
                bucketId = _capsuleBucketId;
            }
            var id = _globalContainer.AddInstance(bucketId, matrix, uniformsPack);

            globalAllInstancesStack.Enqueue(id);
        }
    }
}