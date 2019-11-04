using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer
{
    public class DummyVegetationSubjectInstanceContainer : IVegetationSubjectInstancingContainerChangeListener
    {
        private MyThreadSafeQueue<VegetationSubjectsInstancingOrder> _ordersQueue
            = new MyThreadSafeQueue<VegetationSubjectsInstancingOrder>();

        private VegetationSubjectsInstancingOrder _currentOrder = null;
        private Dictionary<int, GameObject> _gameObjects = new Dictionary<int, GameObject>();

        public void AddInstancingOrder(VegetationDetailLevel level, List<VegetationSubjectEntity> gainedEntities,
            List<VegetationSubjectEntity> lostEntities)
        {
            AddOrder(new VegetationSubjectsInstancingOrder(
                new Queue<VegetationSubjectEntity>(gainedEntities),
                new Queue<VegetationSubjectEntity>(lostEntities),
                level
            ));
        }

        private void AddOrder(VegetationSubjectsInstancingOrder order)
        {
            _ordersQueue.Enqueue(order);
        }

        public void Update(int msLeft)
        {
            if (msLeft < 0)
            {
                return;
            }
            var sw = new Stopwatch();
            sw.Start();

            if (_currentOrder == null)
            {
                if (_ordersQueue.IsEmpty())
                {
                    return;
                }
                else
                {
                    _currentOrder = _ordersQueue.Dequeue();
                }
            }
            var elementsToCreate = _currentOrder.CreationList;
            while (elementsToCreate.Any())
            {
                var elementToProcess = elementsToCreate.Dequeue();
                AddInstance(elementToProcess, _currentOrder.Level);
                if (TimeIsUp(sw, msLeft))
                {
                    return;
                }
            }

            var elementsToRemove = _currentOrder.RemovalList;
            while (elementsToRemove.Any())
            {
                var elementToProcess = elementsToRemove.Dequeue();
                RemoveInstance(elementToProcess, _currentOrder.Level);
                if (TimeIsUp(sw, msLeft))
                {
                    return;
                }
            }
            _currentOrder = null;
        }

        private bool TimeIsUp(Stopwatch sw, int msLeft)
        {
            return sw.ElapsedMilliseconds > msLeft;
        }

        private void AddInstance(VegetationSubjectEntity entity, VegetationDetailLevel level)
        {
            var newObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var entityId = entity.Id;

            newObject.transform.position = new Vector3(entity.Position2D.x, 0, entity.Position2D.y);
            var position = newObject.transform.position;
            newObject.transform.localScale = new Vector3(5, 5, 5);

            newObject.name = "Entity level " + level + " pos x " + position.x + " pos y " + position.y;

            var color = new Color(1, 1, 1);
            if (level == VegetationDetailLevel.BILLBOARD)
            {
                color = Color.red;
                newObject.transform.position += new Vector3(0.5f, 0, 0.5f);
            }
            else if (level == VegetationDetailLevel.FULL)
            {
                color = Color.blue;
                newObject.transform.position += new Vector3(-0.5f, 0, -0.5f);
            }
            else if (level == VegetationDetailLevel.REDUCED)
            {
                color = Color.green;
            }

            newObject.GetComponent<MeshRenderer>().material.color = color;

            _gameObjects[entityId] = newObject;
        }

        private void RemoveInstance(VegetationSubjectEntity entity, VegetationDetailLevel level)
        {
            var go = _gameObjects[entity.Id];
            GameObject.Destroy(go);
            bool res = _gameObjects.Remove(entity.Id);
            if (!res)
            {
                Debug.Log("T16 remocing object failed of if " + entity.Id);
            }
        }
    }
}