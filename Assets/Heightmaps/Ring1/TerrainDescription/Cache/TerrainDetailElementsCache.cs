using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.TerrainMat;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription.Cache
{
    public class TerrainDetailElementsCache
    {
        private CommonExecutorUTProxy _commonExecutor;
        private TerrainDetailElementCacheConfiguration _configuration;

        private Dictionary<TerrainDescriptionElementTypeEnum, Quadtree<ReferenceCountedTerrainDetailElement>>
            _activeTrees = new Dictionary<TerrainDescriptionElementTypeEnum, Quadtree<ReferenceCountedTerrainDetailElement>>();

        private List<InternalTerrainDetailElementToken> _nonReferencedElementsList = new List<InternalTerrainDetailElementToken>();
        private int _elementsLength = 0;

        private Dictionary<int, DetailElemensUnderCreationSemaphore> _creationObligationDictionary =
            new Dictionary<int, DetailElemensUnderCreationSemaphore>();

        private int _lastObligationId = 0;

        public TerrainDetailElementsCache(CommonExecutorUTProxy commonExecutor,
            TerrainDetailElementCacheConfiguration configuration)
        {
            _commonExecutor = commonExecutor;
            _configuration = configuration;
            foreach (TerrainDescriptionElementTypeEnum aEnum in Enum.GetValues(
                typeof(TerrainDescriptionElementTypeEnum)))
            {
                _activeTrees[aEnum] = new Quadtree<ReferenceCountedTerrainDetailElement>();
            }
        }

        public bool IsInCache(MyRectangle queryArea, TerrainCardinalResolution resolution,
            TerrainDescriptionElementTypeEnum type)
        {
            return TryRetriveTerrainDetailElement(queryArea, resolution, type) != null;
        }

        public async Task<CacheQueryOutput> TryRetriveAsync(MyRectangle queryArea, TerrainCardinalResolution resolution, TerrainDescriptionElementTypeEnum type)
        {
            ReferenceCountedTerrainDetailElement foundElement = null;

            var newToken = new InternalTerrainDetailElementToken(queryArea, resolution, type);
            var elementUnderCreation =
                _creationObligationDictionary.Values.FirstOrDefault(c => c.Token.Equals(newToken));
            if (elementUnderCreation != null)
            {
                await elementUnderCreation.Semaphore.Await();
                foundElement = TryRetriveTerrainDetailElement(queryArea, resolution, type);
                Preconditions.Assert(foundElement != null, "Cannot be. After waiting still no element!");
            }
            else
            {
                foundElement = TryRetriveTerrainDetailElement(queryArea, resolution, type);
            }

            if (foundElement != null)
            {
                if (foundElement.ReferenceCount == 0)
                {
                    var indexInNonReferenced = _nonReferencedElementsList
                        .Select((c, i) => new
                        {
                            Index = i,
                            Token = c
                        }).FirstOrDefault(c => c.Token.Equals(newToken));
                    if (indexInNonReferenced == null)
                    {
                        Debug.Log("In Queue");
                        foreach (var elem in _nonReferencedElementsList)
                        {
                            Debug.Log("Elem: " + elem);
                        }
                        Debug.Log("We were looking for: " + newToken);
                    }
                    Preconditions.Assert(indexInNonReferenced != null,
                        " There is no non referenced element's token in queue. ");

                    _nonReferencedElementsList.RemoveAt(indexInNonReferenced.Index);
                    //Debug.Log("T99 Removing element from non referenced List");
                }

                foundElement.ReferenceCount++;
                return new CacheQueryOutput()
                {
                    CreationObligationToken = null,
                    DetailElement =
                        new InternalTokenizedTerrainDetailElement()
                        {
                            DetailElement = foundElement.Element,
                            Token = newToken
                        }
                };
            }
            else
            {
                //Debug.Log("T88 Adding creation obligation!");
                var obligationToken = _lastObligationId++;
                var semaphore = new DetailElemensUnderCreationSemaphore()
                {
                    Semaphore = new TcsSemaphore(),
                    Token = newToken
                };
                _creationObligationDictionary[obligationToken] = semaphore;

                return new CacheQueryOutput()
                {
                    CreationObligationToken = obligationToken,
                    DetailElement = null
                };
            }
        }

        private ReferenceCountedTerrainDetailElement TryRetriveTerrainDetailElement(
            MyRectangle queryArea, TerrainCardinalResolution resolution,
            TerrainDescriptionElementTypeEnum type)
        {
            var env = queryArea.ToEnvelope();
            var firstFound = _activeTrees[type].Query(env);
            var geoEnvelope = MyNetTopologySuiteUtils.ToGeometryEnvelope(env);
            var intersecting = firstFound.Where(c => MyNetTopologySuiteUtils
                .ToGeometryEnvelope(c.Element.DetailArea.ToEnvelope()).Intersects(geoEnvelope)).ToList();
            //AssertNoPartialIntersection(geoEnvelope, intersecting.Select(c => c.Element).ToList());

            var ofGoodResolution = intersecting
                .Where(c => IsMeaningfulIntersection(geoEnvelope, c.Element) && c.Element.Resolution == resolution)
                .ToList();

            //var bestResolution = ofGoodResolution.OrderByDescending(c => c.Resolution.PixelsPerMeter);
            var foundElement = ofGoodResolution.FirstOrDefault();

            return foundElement;
        }

        public async Task<InternalTokenizedTerrainDetailElement> AddTerrainDetailElement(int creationObligationToken,
            TextureWithSize texture,
            MyRectangle queryArea, TerrainCardinalResolution resolution,
            TerrainDescriptionElementTypeEnum type)
        {
            var activeTreeElement = TryRetriveTerrainDetailElement(queryArea, resolution, type);
            Preconditions.Assert(activeTreeElement == null,
                "There arleady is one detailElement of given description:  res: " + resolution + " type: " + type +
                " qa: " + queryArea);


            var newElement = new ReferenceCountedTerrainDetailElement()
            {
                Element = new TerrainDetailElement()
                {
                    DetailArea = queryArea,
                    Resolution = resolution,
                    Texture = texture
                },
                ReferenceCount = 1
            };
            AddElementToActiveTree(queryArea, type, newElement);
            await ClearNonReferencedElements();

            DetailElemensUnderCreationSemaphore semaphore = null;
            semaphore = _creationObligationDictionary[creationObligationToken];
            _creationObligationDictionary.Remove(creationObligationToken);
            semaphore.Semaphore.Set();

            var token = new InternalTerrainDetailElementToken(queryArea, resolution, type);

            //Debug.Log("T96 adding detailElement: succeded: res: "+resolution + " type: " + type + " qa: " + queryArea);
            return new InternalTokenizedTerrainDetailElement()
            {
                DetailElement = new TerrainDetailElement()
                {
                    DetailArea = queryArea,
                    Resolution = resolution,
                    Texture = texture
                },
                Token = token
            };
        }

        private void AddElementToActiveTree(MyRectangle queryArea, TerrainDescriptionElementTypeEnum type,
            ReferenceCountedTerrainDetailElement newElement)
        {
            _activeTrees[type].Insert(queryArea.ToEnvelope(), newElement);
            var tex = newElement.Element.Texture;
            _elementsLength += ComputeSizeOfTexture(tex);
        }

        private int ComputeSizeOfTexture(TextureWithSize tex)
        {
            return tex.Size.X * tex.Size.Y * 4;
        }

        public async Task RemoveTerrainDetailElementAsync(InternalTerrainDetailElementToken token)
        {
            var foundElement =
                TryRetriveTerrainDetailElement(token.QueryArea, token.Resolution, token.Type);
            Preconditions.Assert(foundElement != null, "There is no element of given description");
            foundElement.ReferenceCount--;
            //Debug.Log("T97 removing detailElement: res: "+token.Resolution+" type: "+token.Type+" qa: "+token.QueryArea+" succeded: ");
            if (foundElement.ReferenceCount <= 0)
            {
                if (ThereIsPlaceForTexture(foundElement.Element.Texture))
                {
                    _nonReferencedElementsList.Add(token);
                    //Debug.Log("T97 min ref count. Adding to non-ref list");
                }
                else
                {
                    Debug.Log("T97 min ref count. Removing");
                    await DeleteElement(token, foundElement);
                }
            }
        }

        private bool ThereIsPlaceForTexture(TextureWithSize elementTexture)
        {
            var textureSize = ComputeSizeOfTexture(elementTexture);
            return _elementsLength + textureSize <= _configuration.MaxTextureMemoryUsed;
        }

        private async Task DeleteElement(InternalTerrainDetailElementToken token,
            ReferenceCountedTerrainDetailElement foundElement)
        {
            await _commonExecutor.AddAction(() =>
            {
                var tex0 = foundElement.Element.Texture;
                GameObject.Destroy(tex0.Texture);
            });
            var removalResult = _activeTrees[token.Type].Remove(token.QueryArea.ToEnvelope(), foundElement);
            Preconditions.Assert(removalResult, "Removing failed");
            var tex = foundElement.Element.Texture;
            _elementsLength -= ComputeSizeOfTexture(tex);
        }


        private async Task ClearNonReferencedElements()
        {
            while (_nonReferencedElementsList.Any() && _elementsLength > _configuration.MaxTextureMemoryUsed)
            {
                var tokenOfElementToRemove = _nonReferencedElementsList[0];
                _nonReferencedElementsList.RemoveAt(0);

                var elementToRemove = TryRetriveTerrainDetailElement(tokenOfElementToRemove.QueryArea,
                    tokenOfElementToRemove.Resolution, tokenOfElementToRemove.Type);
                Preconditions.Assert(elementToRemove != null, "Element we wish to delete is not in activeTree");
                Debug.Log("T77 Removing element from non-referenced!");
                await DeleteElement(tokenOfElementToRemove, elementToRemove);
            }
        }


        private bool IsMeaningfulIntersection(IGeometry queryArea, TerrainDetailElement element)
        {
            var elementEnv = MyNetTopologySuiteUtils.ToGeometryEnvelope(element.DetailArea.ToEnvelope());
            var intersectionArea = elementEnv.Intersection(queryArea).Area;
            var queryAreasArea = queryArea.Area;
            var elementAreasArea = elementEnv.Area;

            double TOLERANCE = 0.001;
            Preconditions.Assert(
                intersectionArea < TOLERANCE ||
                Math.Abs(intersectionArea - queryAreasArea) < TOLERANCE ||
                Math.Abs(intersectionArea - elementAreasArea) < TOLERANCE, "There is partial intersection");
            return intersectionArea > TOLERANCE;
        }

        private void AssertNoPartialIntersection(IGeometry queryArea, List<TerrainDetailElement> intersecting)
        {
            foreach (var element in intersecting)
            {
                var elementEnv = MyNetTopologySuiteUtils.ToGeometryEnvelope(element.DetailArea.ToEnvelope());
                var intersectionArea = elementEnv.Intersection(queryArea).Area;
                var queryAreasArea = queryArea.Area;
                var elementAreasArea = elementEnv.Area;

                double TOLERANCE = 0.001;
                Preconditions.Assert(
                    intersectionArea < TOLERANCE ||
                    Math.Abs(intersectionArea - queryAreasArea) < TOLERANCE ||
                    Math.Abs(intersectionArea - elementAreasArea) < TOLERANCE, "There is partial intersection");
            }
        }

        private class ReferenceCountedTerrainDetailElement
        {
            public TerrainDetailElement Element;
            public int ReferenceCount;
        }

        private class DetailElemensUnderCreationSemaphore
        {
            public InternalTerrainDetailElementToken Token;
            public TcsSemaphore Semaphore;
        }
    }

    public class TerrainDetailElementCacheConfiguration
    {
        public long MaxTextureMemoryUsed = 1024 * 1024 * 512 * 2;
    }

    public class InternalTokenizedTerrainDetailElement
    {
        public TerrainDetailElement DetailElement;
        public InternalTerrainDetailElementToken Token;
    }

    public class TokenizedTerrainDetailElement
    {
        public TerrainDetailElement DetailElement;
        public TerrainDetailElementToken Token;
    }

    public class CacheQueryOutput
    {
        public InternalTokenizedTerrainDetailElement DetailElement;
        public int? CreationObligationToken;
    }

    public class InternalTerrainDetailElementToken
    {
        public InternalTerrainDetailElementToken(MyRectangle queryArea, TerrainCardinalResolution resolution,
            TerrainDescriptionElementTypeEnum type)
        {
            QueryArea = queryArea;
            Resolution = resolution;
            Type = type;
        }

        public MyRectangle QueryArea;
        public TerrainCardinalResolution Resolution;
        public TerrainDescriptionElementTypeEnum Type;

        protected bool Equals(InternalTerrainDetailElementToken other)
        {
            return Equals(QueryArea, other.QueryArea) && Equals(Resolution, other.Resolution) && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InternalTerrainDetailElementToken) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (QueryArea != null ? QueryArea.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Resolution != null ? Resolution.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) Type;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(QueryArea)}: {QueryArea}, {nameof(Resolution)}: {Resolution}, {nameof(Type)}: {Type}";
        }
    }
}