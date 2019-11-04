using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.TerrainMat;
using Assets.Trees.Db;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.Placement.BiomesMap;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Trees.Placement
{
    public class VegetationPlacerConfiguration
    {
        public VegetationPlacerConfiguration()
        {
            GenerationWindowSize = new Vector2(10, 10);
            MaxTriesPerMaxElementsFactor = 1.8f;
            StartOfForcingFactor = 1.6f;
            FailedConcievingFactorPerPropabilitiesFromEffectorsSum = 0.2f;
            FailedConcievingFactorPerPropabilitiesFromConstantBiomesSum = 0.4f;
            PerBiomeConstantAddingFactor = 0.1f;
            FailedConcievingConstantFactor = 0.1f;
        }

        public Vector2 GenerationWindowSize { get; set; }
        public float MaxTriesPerMaxElementsFactor { get; set; }
        public float StartOfForcingFactor { get; set; }
        public float FailedConcievingFactorPerPropabilitiesFromEffectorsSum { get; set; }
        public float FailedConcievingFactorPerPropabilitiesFromConstantBiomesSum { get; set; }
        public float PerBiomeConstantAddingFactor { get; set; }
        public float FailedConcievingConstantFactor { get; set; }
    }

    public class PositionWithResourcesSum
    {
        private float _resourcesSum;
        private Vector2 _position;

        public PositionWithResourcesSum(float resourcesSum, Vector2 position)
        {
            _resourcesSum = resourcesSum;
            _position = position;
        }

        public float ResourcesSum
        {
            get { return _resourcesSum; }
        }

        public Vector2 Position
        {
            get { return _position; }
        }
    }

    class VegetationPlacer
    {
        private readonly IVegetationSubjectsDatabase _database;

        public VegetationPlacer(IVegetationSubjectsDatabase database)
        {
            _database = database;
        }

        public void PlaceVegetation(
            GenerationArea areaToGenerate,
            VegetationPlacerConfiguration configuration,
            VegetationSpeciesIntensityMap intensityMap,
            IVegetationBiomesMap biomesMap,
            List<VegetationSpeciesCreationCharacteristics> creationCharacteristicses,
            ConcievingTerrainSpotInfoProvider spotInfoProvider,
            VegetationLevelRank levelRank)
        {
            var generationWindowSize = configuration.GenerationWindowSize;
            var generationWindowSizeArea = generationWindowSize.x * generationWindowSize.y;

            GenerationGrid generationGrid = new GenerationGrid(generationWindowSize, _database);
            generationGrid.StartAt(areaToGenerate.X, areaToGenerate.Y);

            int blockIndex = 0;
            int createdSubjectsCount = 0;
            int triesCount = 0;
            float fillRate = 0f;

            while (generationGrid.ActiveWindowPosition.X < areaToGenerate.EndX)
            {
                while (generationGrid.ActiveWindowPosition.Y < areaToGenerate.EndY)
                {
                    var tryIndex = 0;
                    var createdElements = 0;

                    var centerPoint = generationGrid.ActiveWindowPosition.CenterPoint;
                    var intensity = intensityMap.GetIntensityAt(centerPoint); //todo to rectangle
                    var levelComposition = biomesMap.RetriveBiomesAt(generationGrid.ActiveWindowPosition.Rectangle);
                    // to musi zależać od rank!!
                    var maxElementsPerCell =
                        (int) levelComposition.RetriveMaxElementsPerCell(intensity,
                            generationWindowSizeArea); //todo do sth with it
                    float maxTriesPerMaxElementsFactor = configuration.MaxTriesPerMaxElementsFactor;
                    var maxTriesPerCell = (int) (maxElementsPerCell * maxTriesPerMaxElementsFactor);


                    while (tryIndex < maxTriesPerCell && createdElements < maxElementsPerCell)
                    {
                        float triesLeft = tryIndex - maxTriesPerCell;
                        float elementsToCreateLeft = createdElements - maxElementsPerCell;

                        // jezeli triesLeft == elementToCreateLeft to forcingFactor == 1
                        // jezeli triesLeft == elementsToCreateLeft * 1.6 to ff == 0
                        float startOfForcingFactor = configuration.StartOfForcingFactor;
                        float ratio = triesLeft / elementsToCreateLeft;
                        // ratio == 1 -> ff == 1
                        // ratio = 1.6 -> ff == 0
                        var forcingFactor = Mathf.InverseLerp(startOfForcingFactor, 1.0f, ratio);

                        Vector2 candidatePos =
                            new Vector2(UnityEngine.Random.value * generationWindowSize.x,
                                UnityEngine.Random.value * generationWindowSize.y);
                        candidatePos += new Vector2(generationGrid.ActiveWindowPosition.X,
                            generationGrid.ActiveWindowPosition.Y);

                        IList<VegetationEffector> effectors = generationGrid.GetEffectorsAt(candidatePos);

                        VegetationSubject subject =
                            ConcieveAt(spotInfoProvider.Provide(candidatePos), effectors, forcingFactor,
                                levelComposition,
                                creationCharacteristicses, configuration);
                        if (subject != null)
                        {
                            _database.AddSubject(subject, levelRank);
                            generationGrid.AddNewSubject(subject);
                            createdElements++;
                            createdSubjectsCount++;
                        }
                        tryIndex++;
                        triesCount++;
                    }

                    blockIndex++;
                    fillRate += createdElements / (float) maxElementsPerCell;
                    generationGrid.MoveDown();
                }
                generationGrid.MoveRightUp();
            }

            Debug.Log("Made " + triesCount + " tries, created " + createdSubjectsCount + " subjects, that is " +
                      ((float) createdSubjectsCount / triesCount) * 100 + "% , the fill rate was " +
                      (fillRate / blockIndex) * 100 + "%");
        }

        public VegetationSubject ConcieveAt(
            ConcievingTerrainSpotInfo spotInfo,
            IList<VegetationEffector> effectors,
            float forcingCreation,
            VegetationBiomeLevelComposition composition,
            List<VegetationSpeciesCreationCharacteristics> creationCharacteristics, //todo to dictionary
            VegetationPlacerConfiguration configuration)
        {
            var candidatePos = spotInfo.Position;

            float size = UnityEngine.Random.value;
            var exclusionRadiusRange = composition.RetriveExclusionRadiusRange();
            float exclusionRadius = Mathf.Lerp(exclusionRadiusRange[0], exclusionRadiusRange[1], size);

            float resourcesSum = 1.0f; //todo jakaś mapa resourców
            foreach (var aEffector in effectors)
            {
                resourcesSum *= aEffector.GetResourceDeprivationAt(candidatePos, exclusionRadius);
            }
            if (resourcesSum < 0.0001) //no physical place
            {
                return null;
            }
            resourcesSum *= Mathf.Lerp(1, 5, forcingCreation);

            Dictionary<VegetationSpeciesEnum, float> propabilities = new Dictionary<VegetationSpeciesEnum, float>();

            foreach (var aEffector in effectors)
            {
                var propability = aEffector.GetSpeciesOccurencePropability(candidatePos, resourcesSum);
                if (!creationCharacteristics.Any(c => c.CurrentVegetationType == propability.VegetationType))
                {
                    continue;
                }

                if (!propabilities.ContainsKey(propability.VegetationType))
                {
                    propabilities[propability.VegetationType] = 0f;
                }

                propabilities[propability.VegetationType] += propability.Propability;
            }

            var biomePropabilities = composition.RetrivePropabilities().Where(c => creationCharacteristics
                .Any(r => r.CurrentVegetationType == c.VegetationType)).ToList();

            const float perBiomeSubjectTypeMultiplier = 0.5f;
            foreach (var propability in biomePropabilities)
            {
                if (propabilities.ContainsKey(propability.VegetationType))
                {
                    propabilities[propability.VegetationType] +=
                        propabilities[propability.VegetationType] * propability.Propability *
                        perBiomeSubjectTypeMultiplier;
                }
            }
            float propabilitiesFromEffectorsSum = propabilities.Sum(c => c.Value);
            float propabilitiesFromConstantBiomesSum = 0f;

            float perBiomeConstantAddingFactor = configuration.PerBiomeConstantAddingFactor;
            foreach (var propability in biomePropabilities)
            {
                if (!propabilities.ContainsKey(propability.VegetationType))
                {
                    propabilities[propability.VegetationType] = 0f;
                }
                var elementToAdd = propability.Propability * perBiomeConstantAddingFactor;
                propabilities[propability.VegetationType] += elementToAdd;
                propabilitiesFromConstantBiomesSum += elementToAdd;
            }

            /// Take care of PositionDependentPropability
            var keys = propabilities.Keys.ToList();
            foreach (var vegetationSpecieEnum in keys)
            {
                var characteristics =
                    creationCharacteristics.FirstOrDefault(c => c.CurrentVegetationType == vegetationSpecieEnum);
                if (characteristics?.SpotDependentConcievingPropability != null)
                {
                    var factor = characteristics.SpotDependentConcievingPropability.CalculatePropabilityFactor(
                        new ConcievingTerrainSpotInfo()
                        {
                            Position = candidatePos,
                            SlopeAngleInRadians = 1f
                        });
                    propabilities[vegetationSpecieEnum] *= factor;
                }
            }

            //normalization
            var propabilitiesSum = propabilities.Values.Sum();

            float failedConcievingFactorPerPropabilitiesFromEffectorsSum =
                configuration.FailedConcievingFactorPerPropabilitiesFromEffectorsSum;
            float failedConcievingFactorPerPropabilitiesFromConstantBiomesSum =
                configuration.FailedConcievingFactorPerPropabilitiesFromConstantBiomesSum;

            propabilitiesSum += propabilitiesFromEffectorsSum * failedConcievingFactorPerPropabilitiesFromEffectorsSum;
            propabilitiesSum += propabilitiesFromConstantBiomesSum *
                                failedConcievingFactorPerPropabilitiesFromConstantBiomesSum;
            propabilitiesSum += configuration.FailedConcievingConstantFactor;


            foreach (var key in keys)
            {
                propabilities[key] /= propabilitiesSum;
            }

            //finding weighted random
            var selectingValue = UnityEngine.Random.value;

            var sum = 0f;
            foreach (var pair in propabilities)
            {
                sum += pair.Value;
                if (sum >= selectingValue)
                {
                    var characteristicToReturn =
                        creationCharacteristics.FirstOrDefault(c => c.CurrentVegetationType == pair.Key);
                    Preconditions.Assert(characteristicToReturn != null,
                        "There is no characteristics for vegetation " + pair.Key);
                    return new VegetationSubject(candidatePos, size, characteristicToReturn);
                }
            }
            return null; // failed concieving happened!
        }
    }

    public class GenerationGrid
    {
        private readonly VegetationEffectorsDatabase _effectorsDatabase;
        private readonly IVegetationSubjectsDatabase _subjectsDatabase;
        private readonly Vector2 _generationWindowSize;
        private Vector2 _startPosition;

        private GenerationArea _activeWindowPosition;
        private int _topSubjectsId;
        private int _topLeftSubjectsId;
        private int _leftSubjectsId;
        private int _downLeftSubjectsId;

        private int _downSubjectsId;
        private int _downRightSubjectsId;
        private int _rightSubjectsId;
        private int _topRightSubjectsId;

        private int _currentSubjectsId;

        public GenerationGrid(Vector2 generationWindowSize, IVegetationSubjectsDatabase subjectsDatabase)
        {
            _generationWindowSize = generationWindowSize;
            _subjectsDatabase = subjectsDatabase;
            _effectorsDatabase = new VegetationEffectorsDatabase();
        }

        public GenerationArea ActiveWindowPosition
        {
            get { return _activeWindowPosition; }
        }

        public IList<VegetationEffector> GetEffectorsAt(Vector2 candidatePos)
        {
            return _effectorsDatabase.GetEffectorsAt(candidatePos);
        }

        public void StartAt(float x, float y)
        {
            _startPosition = new Vector2(x, y);
            _activeWindowPosition = new GenerationArea(x, y, _generationWindowSize.x, _generationWindowSize.y);
            SetNeighbourSubjects();
            _currentSubjectsId = _effectorsDatabase.StartNewCurrentEffectorsGroup();
        }

        public void MoveDown()
        {
            _activeWindowPosition = _activeWindowPosition.DownNeighbourArea;

            _effectorsDatabase.Remove(_topSubjectsId);
            _topSubjectsId = _currentSubjectsId;

            _effectorsDatabase.Remove(_topLeftSubjectsId);
            _topLeftSubjectsId = _leftSubjectsId;

            _leftSubjectsId = _downLeftSubjectsId;

            var downLeftSubjects = _subjectsDatabase.GetSubjectsAt(_activeWindowPosition.DownLeftNeighbourArea);
            _downLeftSubjectsId = _effectorsDatabase.AddSubjects(downLeftSubjects);

            _currentSubjectsId = _effectorsDatabase.SetNewCurrentEffectorsGroup(_downSubjectsId);

            var downSubjects = _subjectsDatabase.GetSubjectsAt(_activeWindowPosition.DownNeighbourArea);
            _downSubjectsId = _effectorsDatabase.AddSubjects(downSubjects);

            _effectorsDatabase.Remove(_topRightSubjectsId);
            _topRightSubjectsId = _rightSubjectsId;

            _rightSubjectsId = _downRightSubjectsId;

            var downRightSubjects = _subjectsDatabase.GetSubjectsAt(_activeWindowPosition.DownRightNeighbourArea);
            _downRightSubjectsId = _effectorsDatabase.AddSubjects(downRightSubjects);
        }

        public void MoveRightUp()
        {
            _effectorsDatabase.Remove(_currentSubjectsId);
            _effectorsDatabase.Remove(_downLeftSubjectsId);
            _effectorsDatabase.Remove(_leftSubjectsId);
            _effectorsDatabase.Remove(_topSubjectsId);
            _effectorsDatabase.Remove(_topLeftSubjectsId);

            _effectorsDatabase.Remove(_downSubjectsId);
            _effectorsDatabase.Remove(_downRightSubjectsId);
            _effectorsDatabase.Remove(_rightSubjectsId);
            _effectorsDatabase.Remove(_topRightSubjectsId);

            _activeWindowPosition = new GenerationArea(_activeWindowPosition.X + _activeWindowPosition.Width,
                _startPosition.y, _generationWindowSize.x, _generationWindowSize.y);
            SetNeighbourSubjects();
            _currentSubjectsId = _effectorsDatabase.StartNewCurrentEffectorsGroup();
        }

        private void SetNeighbourSubjects()
        {
            var topSubjects = _subjectsDatabase.GetSubjectsAt(_activeWindowPosition.TopNeighbourArea);
            _topSubjectsId = _effectorsDatabase.AddSubjects(topSubjects);

            var topLeftSubjects = _subjectsDatabase.GetSubjectsAt(_activeWindowPosition.TopLeftNeighbourArea);
            _topLeftSubjectsId = _effectorsDatabase.AddSubjects(topLeftSubjects);

            var leftSubjects = _subjectsDatabase.GetSubjectsAt(_activeWindowPosition.LeftNeighbourArea);
            _leftSubjectsId = _effectorsDatabase.AddSubjects(leftSubjects);

            var downLeftSubjects = _subjectsDatabase.GetSubjectsAt(_activeWindowPosition.DownLeftNeighbourArea);
            _downLeftSubjectsId = _effectorsDatabase.AddSubjects(downLeftSubjects);

            var downSubjects = _subjectsDatabase.GetSubjectsAt(_activeWindowPosition.DownNeighbourArea);
            _downSubjectsId = _effectorsDatabase.AddSubjects(downSubjects);

            var downRightSubjects = _subjectsDatabase.GetSubjectsAt(_activeWindowPosition.DownRightNeighbourArea);
            _downRightSubjectsId = _effectorsDatabase.AddSubjects(downRightSubjects);

            var rightSubjects = _subjectsDatabase.GetSubjectsAt(_activeWindowPosition.RightNeighbourArea);
            _rightSubjectsId = _effectorsDatabase.AddSubjects(rightSubjects);

            var topRightSubjects = _subjectsDatabase.GetSubjectsAt(_activeWindowPosition.TopRightNeighbourArea);
            _topRightSubjectsId = _effectorsDatabase.AddSubjects(topRightSubjects);
        }

        public void AddNewSubject(VegetationSubject subject)
        {
            _effectorsDatabase.AddCurrentGroupSubject(subject);
        }
    }

    public class VegetationEffectorsDatabase
    {
        private readonly Quadtree<VegetationEffector> _effectorsTree = new Quadtree<VegetationEffector>();
        private int _nextEffectorsGroupId = 0;
        private int _currentEffectorsGroupId = 0;

        private readonly Dictionary<int, List<VegetationEffector>> _effectorsDictionary =
            new Dictionary<int, List<VegetationEffector>>();

        public int AddSubjects(List<VegetationSubject> subjects)
        {
            _nextEffectorsGroupId++;
            var effectorsList = subjects.Select(c => c.CreateEffector()).ToList();
            _effectorsDictionary.Add(_nextEffectorsGroupId, effectorsList);
            effectorsList.ForEach(c => _effectorsTree.Insert(CalculateEnvelope(c), c));
            return _nextEffectorsGroupId;
        }

        public IList<VegetationEffector> GetEffectorsAt(Vector2 candidatePos)
        {
            return _effectorsTree.Query(MyNetTopologySuiteUtils.ToPointEnvelope(candidatePos));
        }

        public void Remove(int effectorsId)
        {
            Preconditions.Assert(_effectorsDictionary.ContainsKey(effectorsId),
                "There is no effectors of id " + effectorsId);

            foreach (var effector in _effectorsDictionary[effectorsId])
            {
                var removingSucceded = _effectorsTree.Remove(CalculateEnvelope(effector), effector);
                Preconditions.Assert(removingSucceded, "Cannot remove effector " + effector + " it cannot be found");
            }
            _effectorsDictionary.Remove(effectorsId);
        }

        private static Envelope CalculateEnvelope(VegetationEffector effector)
        {
            Vector2 center = effector.CenterPoint;
            float radius = effector.ExclusionRadius;

            return new Envelope(center.x - radius, center.x + radius, center.y - radius, center.y + radius);
        }

        public int StartNewCurrentEffectorsGroup()
        {
            _nextEffectorsGroupId++;
            _currentEffectorsGroupId = _nextEffectorsGroupId;
            _effectorsDictionary.Add(_currentEffectorsGroupId, new List<VegetationEffector>());
            return _nextEffectorsGroupId;
        }

        public void AddCurrentGroupSubject(VegetationSubject subject)
        {
            var vegetationEffector = subject.CreateEffector();
            _effectorsDictionary[_currentEffectorsGroupId].Add(vegetationEffector);
            _effectorsTree.Insert(CalculateEnvelope(vegetationEffector), vegetationEffector);
        }

        public int SetNewCurrentEffectorsGroup(int newId)
        {
            Preconditions.Assert(_effectorsDictionary.ContainsKey(newId), "There is no effectors of id " + newId);
            _currentEffectorsGroupId = newId;
            return newId;
        }
    }

    public class GenerationArea : Positions2D<float>
    {
        public GenerationArea(float x, float y, float width, float height) : base(x, y, width, height)
        {
        }

        public float EndX
        {
            get { return X + Width; }
        }

        public float EndY
        {
            get { return Y + Height; }
        }

        public GenerationArea TopNeighbourArea
        {
            get { return new GenerationArea(X, Y - Height, Width, Height); }
        }

        public GenerationArea TopLeftNeighbourArea
        {
            get { return new GenerationArea(X - Width, Y - Height, Width, Height); }
        }

        public GenerationArea LeftNeighbourArea
        {
            get { return new GenerationArea(X - Width, Y, Width, Height); }
        }

        public GenerationArea DownLeftNeighbourArea
        {
            get { return new GenerationArea(X - Width, Y + Height, Width, Height); }
        }

        public GenerationArea DownNeighbourArea
        {
            get { return new GenerationArea(X, Y + Height, Width, Height); }
        }

        public Vector2 CenterPoint
        {
            get { return new Vector2(X + Width / 2, Y + Height / 2); }
        }

        public GenerationArea DownRightNeighbourArea
        {
            get { return new GenerationArea(X + Width, Y + Height, Width, Height); }
        }

        public GenerationArea RightNeighbourArea
        {
            get { return new GenerationArea(X + Width, Height, Width, Height); }
        }

        public GenerationArea TopRightNeighbourArea
        {
            get { return new GenerationArea(X + Width, Y - Height, Width, Height); }
        }

        public bool IsIn(Vector2 position)
        {
            return X <= position.x && EndX >= position.x && Y <= position.y && EndY >= position.y;
        }

        public MyRectangle Rectangle => new MyRectangle(X, Y, Width, Height);

        public static GenerationArea FromMyRectangle(MyRectangle coords)
        {
            return new GenerationArea(coords.X, coords.Y, coords.Width, coords.Height);
        }
    }

    public class VegetationSpeciesIntensityMap
    {
        private readonly Texture2D _intensityTexture;
        private readonly PositionRemapper _positionRemapper;

        public VegetationSpeciesIntensityMap(Texture2D intensityTexture, PositionRemapper positionRemapper)
        {
            _intensityTexture = intensityTexture;
            _positionRemapper = positionRemapper;
        }

        public float GetIntensityAt(Vector2 candidatePos)
        {
            var mappedPos = _positionRemapper.GetRemappedPosition(candidatePos);
            if (mappedPos.x > 1.1f || mappedPos.y > 1.1f)
            {
                int rr = 2;
            }
            return _intensityTexture.GetPixelBilinear(mappedPos.x, mappedPos.y).r;
        }
    }

    public class PositionRemapper
    {
        private readonly GenerationArea _areaMapped;

        public PositionRemapper(GenerationArea areaMapped)
        {
            _areaMapped = areaMapped;
        }

        public Vector2 GetRemappedPosition(Vector2 input)
        {
            float xStart = input.x - _areaMapped.X;
            float yStart = input.y - _areaMapped.Y;

            float xPercent = xStart / _areaMapped.Width;
            float yPercent = yStart / _areaMapped.Height;
            return new Vector2(xPercent, yPercent);
        }
    }

    public class VegetationSubject
    {
        private readonly Vector2 _xzPosition;
        private readonly SizedVegetationSpeciesCreationCharacteristics _createCharacteristics;

        public VegetationSubject(Vector2 xzPosition, float size,
            VegetationSpeciesCreationCharacteristics createCharacteristics)
        {
            _createCharacteristics = new SizedVegetationSpeciesCreationCharacteristics(size, createCharacteristics);
            _xzPosition = xzPosition;
        }

        public SizedVegetationSpeciesCreationCharacteristics CreateCharacteristics
        {
            get { return _createCharacteristics; }
        }

        public VegetationEffector CreateEffector()
        {
            return new VegetationEffector(_xzPosition, _createCharacteristics); //todo size
        }

        public float ExclusionRadius
        {
            get { return _createCharacteristics.ExclusionRadius; }
        }

        public Vector2 XzPosition
        {
            get { return _xzPosition; }
        }
    }

    public class VegetationSpeciesCreationCharacteristics
    {
        public float ExclusionRadius { get; set; } = 1f;
        public float MaxCreationRadius { get; set; } = 5f;
        public float MaxSpeciesOccurenceDistance { get; set; } = 5f;
        public float InitialSpeciesOccurencePropability { get; set; } = 0.6f;

        public VegetationSpeciesEnum CurrentVegetationType { get; set; } = VegetationSpeciesEnum.Invalid;
        public Vector2 SizeMultiplierFactorRange { get; set; } = new Vector2(0.8f, 1.2f);
        public Vector2 PerResourceFactor { get; set; } = new Vector2(0.5f, 1.0f);

        public ISpotDependentConcievingPropability SpotDependentConcievingPropability { get; set; } = null;

        public VegetationSpeciesCreationCharacteristics ScaleSizeMultiplierFactorRange(float scale)
        {
            return new VegetationSpeciesCreationCharacteristics()
            {
                CurrentVegetationType = CurrentVegetationType,
                ExclusionRadius = ExclusionRadius,
                InitialSpeciesOccurencePropability = InitialSpeciesOccurencePropability,
                MaxCreationRadius = MaxCreationRadius,
                MaxSpeciesOccurenceDistance = MaxSpeciesOccurenceDistance,
                PerResourceFactor = PerResourceFactor,
                SizeMultiplierFactorRange = SizeMultiplierFactorRange * scale,
                SpotDependentConcievingPropability = SpotDependentConcievingPropability
            };
        }
    }

    public class SizedVegetationSpeciesCreationCharacteristics
    {
        private readonly float _rangedSize;
        private readonly VegetationSpeciesCreationCharacteristics _creationCharacteristics;

        public SizedVegetationSpeciesCreationCharacteristics(float size,
            VegetationSpeciesCreationCharacteristics creationCharacteristics)
        {
            float min = creationCharacteristics.SizeMultiplierFactorRange[0];
            float max = creationCharacteristics.SizeMultiplierFactorRange[1];

            _rangedSize = Mathf.Lerp(min, max, size);
            _creationCharacteristics = creationCharacteristics;
        }

        public float ExclusionRadius
        {
            get { return _creationCharacteristics.ExclusionRadius * _rangedSize; }
        }

        public float MaxCreationRadius
        {
            get { return _creationCharacteristics.MaxCreationRadius * _rangedSize; }
        }

        public float MaxSpeciesOccurenceDistance
        {
            get { return _creationCharacteristics.MaxSpeciesOccurenceDistance * _rangedSize; }
        }

        public float InitialSpeciesOccurencePropability
        {
            get { return _creationCharacteristics.InitialSpeciesOccurencePropability; }
        }

        public VegetationSpeciesEnum CurrentVegetationType
        {
            get { return _creationCharacteristics.CurrentVegetationType; }
        }

        public Vector2 PerResourceFactor
        {
            get { return _creationCharacteristics.PerResourceFactor * _rangedSize; }
        }

        public float RangedSize
        {
            get { return _rangedSize; }
        }
    }

    public class VegetationEffector
    {
        private readonly Vector2 _centerPoint;
        private readonly SizedVegetationSpeciesCreationCharacteristics _creationCharacteristics;

        public VegetationEffector(Vector2 centerPoint,
            SizedVegetationSpeciesCreationCharacteristics creationCharacteristics)
        {
            _centerPoint = centerPoint;
            _creationCharacteristics = creationCharacteristics;
        }

        public Vector2 CenterPoint
        {
            get { return _centerPoint; }
        }

        public float ExclusionRadius
        {
            get { return _creationCharacteristics.ExclusionRadius; }
        }

        public float GetResourceDeprivationAt(Vector2 candidatePos, float newElementExclusionRadius)
        {
            float distance = (candidatePos - _centerPoint).magnitude;
            if (distance < _creationCharacteristics.ExclusionRadius + newElementExclusionRadius)
            {
                return 0f;
            }
            return Mathf.Lerp(0, 1,
                Mathf.InverseLerp(0,
                    _creationCharacteristics.MaxCreationRadius - _creationCharacteristics.ExclusionRadius,
                    distance - _creationCharacteristics.ExclusionRadius));
        }

        public VegetationSpeciesOccurencePropability GetSpeciesOccurencePropability(Vector2 candidatePos,
            float resources)
        {
            float distance = (candidatePos - _centerPoint).magnitude;
            float propability = Mathf.Lerp(_creationCharacteristics.InitialSpeciesOccurencePropability, 0,
                Mathf.InverseLerp(0, _creationCharacteristics.MaxSpeciesOccurenceDistance, distance));

            Vector2 perResourceFactor = _creationCharacteristics.PerResourceFactor;
            float perResourceMultiplier = Mathf.InverseLerp(perResourceFactor[0], perResourceFactor[1], resources);
            propability *= perResourceMultiplier;

            return new VegetationSpeciesOccurencePropability(_creationCharacteristics.CurrentVegetationType,
                propability);
        }
    }

    public class VegetationSpeciesOccurencePropability
    {
        private readonly VegetationSpeciesEnum _vegetationType;
        private readonly float _propability; // values in 0-1

        public VegetationSpeciesOccurencePropability(VegetationSpeciesEnum vegetationType, float propability)
        {
            _vegetationType = vegetationType;
            _propability = propability;
        }

        public VegetationSpeciesEnum VegetationType
        {
            get { return _vegetationType; }
        }

        public float Propability
        {
            get { return _propability; }
        }

        public VegetationSpeciesOccurencePropability ScaleBy(float scale)
        {
            return new VegetationSpeciesOccurencePropability(_vegetationType, _propability * scale);
        }
    }

    public class VegetationBiomeLevel
    {
        public VegetationBiomeLevel(
            List<VegetationSpeciesOccurencePropability> biomeOccurencePropabilities,
            Vector2 exclusionRadiusRange,
            float maxElementsPerCellPerM2
        )
        {
            BiomeOccurencePropabilities = biomeOccurencePropabilities;
            ExclusionRadiusRange = exclusionRadiusRange;
            MaxElementsPerCellPerM2 = maxElementsPerCellPerM2;
        }

        public List<VegetationSpeciesOccurencePropability> BiomeOccurencePropabilities { get; }
        public Vector2 ExclusionRadiusRange { get; }
        public float MaxElementsPerCellPerM2 { get; }

        public float RetriveMaxElementsPerCellPerM2(float intensity)
        {
            return MaxElementsPerCellPerM2 * intensity; //todo, meybe more complicated function
        }
    }

    public class VegetationBiomeLevelComposition
    {
        private readonly List<BiomeLevelWithStrength> _allLevels;

        public VegetationBiomeLevelComposition(List<BiomeLevelWithStrength> allLevels)
        {
            _allLevels = allLevels;
        }

        public List<VegetationSpeciesOccurencePropability> RetrivePropabilities()
        {
            return _allLevels
                .SelectMany(c => c.BiomeLevel.BiomeOccurencePropabilities.Select(x => x.ScaleBy(c.Strength))).ToList();
        }

        public Vector2 RetriveExclusionRadiusRange()
        {
            return new Vector2(_allLevels.Max(c => c.BiomeLevel.ExclusionRadiusRange[0]),
                _allLevels.Max(c => c.BiomeLevel.ExclusionRadiusRange[1]));
        }

        public float RetriveMaxElementsPerCell(float intensity, float areaInM2)
        {
            var sumOfWeights = _allLevels.Sum(c => c.Strength);
            var weightedSumOfElements =
                _allLevels.Sum(pair => pair.Strength * pair.BiomeLevel.RetriveMaxElementsPerCellPerM2(intensity));
            return (weightedSumOfElements / sumOfWeights) * areaInM2;
        }
    }

    public class BiomeLevelWithStrength
    {
        private readonly float _strength;
        private readonly VegetationBiomeLevel _biomeLevel;

        public BiomeLevelWithStrength(VegetationBiomeLevel biomeLevel, float strength)
        {
            _biomeLevel = biomeLevel;
            _strength = strength;
        }

        public float Strength
        {
            get { return _strength; }
        }

        public VegetationBiomeLevel BiomeLevel
        {
            get { return _biomeLevel; }
        }
    }


    public enum VegetationLevelRank
    {
        Big,
        Medium,
        Small
    }

    public static class VegetationLevelRankUtils
    {
        public static List<VegetationLevelRank> RetriveSameOrBigger(this VegetationLevelRank rank)
        {
            if (rank == VegetationLevelRank.Small)
            {
                return new List<VegetationLevelRank>()
                {
                    VegetationLevelRank.Big,
                    VegetationLevelRank.Medium,
                    VegetationLevelRank.Small
                };
            }
            else if (rank == VegetationLevelRank.Medium)
            {
                return new List<VegetationLevelRank>()
                {
                    VegetationLevelRank.Big,
                    VegetationLevelRank.Medium,
                };
            }
            else if (rank == VegetationLevelRank.Big)
            {
                return new List<VegetationLevelRank>()
                {
                    VegetationLevelRank.Big
                };
            }
            else
            {
                Preconditions.Fail("Unsupported vegetationLevelRank: " + rank);
                return null;
            }
        }
    }

    public interface ISpotDependentConcievingPropability
    {
        float CalculatePropabilityFactor(ConcievingTerrainSpotInfo spotInfo);
    }

    public class ConcievingTerrainSpotInfo
    {
        public Vector2 Position;
        public float SlopeAngleInRadians;
    }

    public class SlopeDependentConcievingPropability : ISpotDependentConcievingPropability
    {
        private MarginedRange _slopeAcceptanceRangeInRadians;

        public SlopeDependentConcievingPropability(MarginedRange slopeAcceptanceRangeInRadians)
        {
            _slopeAcceptanceRangeInRadians = slopeAcceptanceRangeInRadians;
        }

        public float CalculatePropabilityFactor(ConcievingTerrainSpotInfo spotInfo)
        {
            return _slopeAcceptanceRangeInRadians.PresenceFactor(spotInfo.SlopeAngleInRadians);
        }
    }


    public class ConcievingTerrainSpotInfoProvider
    {
        public ConcievingTerrainSpotInfo Provide(Vector2 position)
        {
            return new ConcievingTerrainSpotInfo()
            {
                Position = position,
                SlopeAngleInRadians = 1.0f //todo!!
            };
        }
    }
}