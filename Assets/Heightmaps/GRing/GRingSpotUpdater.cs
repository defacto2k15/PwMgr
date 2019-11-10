using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.TerrainMat;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Heightmaps.GRing
{
    public class GRingSpotUpdater
    {
        //TODO2: Why it is even here? Who uses it? 
        // todo change it in future, more well -thought solutions 

        private Quadtree<GroundShapeInfoInTree> _groundTree = new Quadtree<GroundShapeInfoInTree>();
        private DesignBodySpotUpdaterProxy _designBodySpotUpdaterProxy;

        public GRingSpotUpdater(DesignBodySpotUpdaterProxy designBodySpotUpdaterProxy)
        {
            _designBodySpotUpdaterProxy = designBodySpotUpdaterProxy;
        }

        public GroundShapeToken AddArea(GroundShapeInfo info, UpdatedTerrainTextures textures)
        {
            return new GroundShapeToken(this, info, textures);
        }

        public void SetArea(GroundShapeInfo info, UpdatedTerrainTextures textures)
        {
            var overlappingElements = FindOverlappingGroundShapes(info);
            foreach (var element in overlappingElements)
            {
                if (element.GroundShapeInfo.Equals(info))
                {
                    // this is the same area! Dont do anything
                    return;
                }
                else
                {
                    _designBodySpotUpdaterProxy.RemoveTerrainTextures(element.TerrainId);
                    _groundTree.Remove(element.GlobalSubpositionEnvelope, element);
                }
            }
            var terrainTextureId =
                _designBodySpotUpdaterProxy.UpdateBodiesSpots(textures);
            var newElementInTree = new GroundShapeInfoInTree()
            {
                GroundShapeInfo = info,
                TerrainId = terrainTextureId,
            };
            _groundTree.Insert(newElementInTree.GlobalSubpositionEnvelope, newElementInTree);
        }

        private List<GroundShapeInfoInTree> FindOverlappingGroundShapes(GroundShapeInfo info)
        {
            var queryEnvelope = MyNetTopologySuiteUtils.ToEnvelope(info.GlobalSubposition);
            var overlappingElements =
                _groundTree.Query(queryEnvelope)
                    .Where(c => c.GlobalSubpositionEnvelope.Intersection(queryEnvelope).Area > 1)
                    .ToList();
            return overlappingElements;
        }

        private class GroundShapeInfoInTree
        {
            public SpotUpdaterTerrainTextureId TerrainId;
            public GroundShapeInfo GroundShapeInfo;

            public Envelope GlobalSubpositionEnvelope => MyNetTopologySuiteUtils.ToEnvelope(GroundShapeInfo
                .GlobalSubposition);
        }
    }

    public class GroundShapeInfo
    {
        public MyRectangle TextureCoords;
        public MyRectangle TextureGlobalPosition;
        public TerrainCardinalResolution HeightmapResolution;

        public MyRectangle GlobalSubposition => RectangleUtils.CalculateSubPosition(TextureGlobalPosition,
            TextureCoords);

        protected bool Equals(GroundShapeInfo other)
        {
            return Equals(TextureCoords, other.TextureCoords) &&
                   Equals(TextureGlobalPosition, other.TextureGlobalPosition) &&
                   Equals(HeightmapResolution, other.HeightmapResolution);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GroundShapeInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (TextureCoords != null ? TextureCoords.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TextureGlobalPosition != null ? TextureGlobalPosition.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (HeightmapResolution != null ? HeightmapResolution.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class GroundShapeToken : IGroundShapeToken
    {
        private GRingSpotUpdater _updater;
        private GroundShapeInfo _info;
        private UpdatedTerrainTextures _textures;

        public GroundShapeToken(GRingSpotUpdater updater, GroundShapeInfo info, UpdatedTerrainTextures textures)
        {
            _updater = updater;
            _info = info;
            _textures = textures;
        }

        public void GroundActive()
        {
            _updater.SetArea(_info, _textures);
        }
    }

    public interface IGroundShapeToken
    {
        void GroundActive();
    }

    public class DummyGroundShapeToken : IGroundShapeToken
    {
        public void GroundActive()
        {
        }
    }
}