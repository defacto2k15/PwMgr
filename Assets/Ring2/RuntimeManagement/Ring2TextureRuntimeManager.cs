//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using Assets.Heightmaps.Ring1.valTypes;
//using Assets.Ring2.RuntimeManagementOtherThread;
//using UnityEngine;

//namespace Assets.Ring2.RuntimeManagement
//{
//    public class Ring2TextureRuntimeManager
//    {
//        private VisibleRing2PatchesContainer _visiblePatchesContainer;
//        private Ring2TextureRuntimeManagerConfiguration _configuration;
//        private Ring2PatchesOverseerProxy _patchesOverseer;

//        public Ring2TextureRuntimeManager(VisibleRing2PatchesContainer visiblePatchesContainer,
//            Ring2TextureRuntimeManagerConfiguration configuration, Ring2PatchesOverseerProxy patchesOverseer)
//        {
//            _visiblePatchesContainer = visiblePatchesContainer;
//            _configuration = configuration;
//            _patchesOverseer = patchesOverseer;
//        }

//        public void Start(Vector2 position)
//        {
//            Vector2 creationRectangleSize = _configuration.CreationRectangleSize;
//            var creationRectangle = new MyRectangle(0, 0, creationRectangleSize.x, creationRectangleSize.y);
//            creationRectangle = creationRectangle.CenterAt(position);

//            List<MyRectangle> patchesToCreateDiameters = CalculatePatchToCreateDiameners(creationRectangle,
//                _configuration.PatchSize);

//            AddPatches(patchesToCreateDiameters);
//        }

//        public void Update(Vector2 position)
//        {
//            Vector2 creationRectangleSize = _configuration.CreationRectangleSize;
//            var creationRectangle = new MyRectangle(0, 0, creationRectangleSize.x, creationRectangleSize.y);
//            creationRectangle = creationRectangle.CenterAt(position);

//            List<MyRectangle> patchesToCreateDiameters = CalculatePatchToCreateDiameners(creationRectangle,
//                _configuration.PatchSize); //todo better algorithm

//            var notRepeatedDiameters =
//                patchesToCreateDiameters.Where(c => !_visiblePatchesContainer.ContainsPatchAt(c)).ToList();
//            AddPatches(notRepeatedDiameters);

//            Vector2 removalRectangleSize = _configuration.RemovalRectangleSize;
//            var removalRectangle = new MyRectangle(0, 0, removalRectangleSize.x, removalRectangleSize.y);
//            removalRectangle.CenterAt(position);

//            List<Ring2PatchDiametersWithId> patchesToRemove =
//                _visiblePatchesContainer.RetriveAndRemovePatchesOutsideOf(removalRectangle);
//            _patchesOverseer.Remove(patchesToRemove);
//        }

//        private void AddPatches(List<MyRectangle> patchesToCreateDiameters)
//        {
//            List<Ring2PatchDiametersWithId> createdPatchesIds = _patchesOverseer.Create(patchesToCreateDiameters);
//            _visiblePatchesContainer.Add(createdPatchesIds);
//        }

//        private List<MyRectangle> CalculatePatchToCreateDiameners(MyRectangle creationRectangle,
//            Vector2 patchSize)
//        {
//            List<MyRectangle> patchSizesToCreate = new List<MyRectangle>();

//            for (float x = creationRectangle.X + patchSize.x / 2;
//                x < creationRectangle.MaxX + patchSize.x / 2;
//                x += patchSize.x)
//            {
//                for (float y = creationRectangle.Y + patchSize.y / 2;
//                    y < creationRectangle.MaxY + patchSize.y / 2;
//                    y += patchSize.y)
//                {
//                    patchSizesToCreate.Add(new MyRectangle(x - patchSize.x / 2, y - patchSize.y / 2, patchSize.x,
//                        patchSize.y));
//                }
//            }
//            return patchSizesToCreate;
//        }
//    }

//    public class VisibleRing2PatchesContainer
//    {
//        private Dictionary<Vector2AsKey, Ring2PatchDiametersWithId> _visiblePatches =
//            new Dictionary<Vector2AsKey, Ring2PatchDiametersWithId>();

//        public bool ContainsPatchAt(MyRectangle rect)
//        {
//            return _visiblePatches.ContainsKey(new Vector2AsKey(rect.Center));
//        }

//        public void Add(List<Ring2PatchDiametersWithId> createdPatchesIds)
//        {
//            foreach (var diameter in createdPatchesIds)
//            {
//                _visiblePatches[new Vector2AsKey(diameter.Diameters.Center)] = diameter;
//            }
//        }

//        public List<Ring2PatchDiametersWithId> RetriveAndRemovePatchesOutsideOf(MyRectangle rectangle)
//        {
//            List<KeyValuePair<Vector2AsKey, Ring2PatchDiametersWithId>> elementsToRemove =
//                new List<KeyValuePair<Vector2AsKey, Ring2PatchDiametersWithId>>();
//            foreach (var pair in _visiblePatches)
//            {
//                var centerPoint = pair.Value.Diameters.Center;
//                if (!rectangle.Contains(centerPoint))
//                {
//                    elementsToRemove.Add(pair);
//                }
//            }

//            foreach (var pair in elementsToRemove)
//            {
//                _visiblePatches.Remove(pair.Key);
//            }

//            return elementsToRemove.Select(c => c.Value).ToList();
//        }

//        private class Vector2AsKey
//        {
//            public float X;
//            public float Y;

//            public Vector2AsKey(float x, float y)
//            {
//                X = x;
//                Y = y;
//            }

//            public Vector2AsKey(Vector2 v)
//            {
//                X = v.x;
//                Y = v.y;
//            }

//            protected bool Equals(Vector2AsKey other)
//            {
//                return X.Equals(other.X) && Y.Equals(other.Y);
//            }

//            public override bool Equals(object obj)
//            {
//                if (ReferenceEquals(null, obj)) return false;
//                if (ReferenceEquals(this, obj)) return true;
//                if (obj.GetType() != this.GetType()) return false;
//                return Equals((Vector2AsKey) obj);
//            }

//            public override int GetHashCode()
//            {
//                unchecked
//                {
//                    return (X.GetHashCode() * 397) ^ Y.GetHashCode();
//                }
//            }
//        }
//    }

//    public class Ring2TextureRuntimeManagerConfiguration
//    {
//        public Vector2 CreationRectangleSize { get; set; }
//        public Vector2 PatchSize { get; set; }
//        public Vector2 RemovalRectangleSize { get; set; }
//    }

//    public class Ring2PatchDiametersWithId
//    {
//        public int Id;
//        public MyRectangle Diameters;
//    }
//}