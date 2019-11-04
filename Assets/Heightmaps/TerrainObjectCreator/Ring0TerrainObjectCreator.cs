using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.submaps;
using UnityEngine;

namespace Assets.Heightmaps.TerrainObjectCreator
{
    class Ring0TerrainObjectCreator
    {
        public List<GameObject> CreatingRing0TerrainObjects(List<Submap> ring0Submaps)
        {
            List<GameObject> createdObjects = new List<GameObject>();
            foreach (var submap in ring0Submaps)
            {
                var newObject = SubmapPlane.CreatePlaneObject(submap.Heightmap.HeightmapAsArray).GameObject;
                newObject.transform.localPosition =
                    new Vector3(submap.SubmapPosition.DownLeftX, 0, submap.SubmapPosition.DownLeftY);
                newObject.transform.localScale =
                    new Vector3(submap.SubmapPosition.Width, 1000, submap.SubmapPosition.Height);
                newObject.transform.eulerAngles = new Vector3(0, 0, 0);
                createdObjects.Add(newObject);
            }
            return createdObjects;
        }
    }
}