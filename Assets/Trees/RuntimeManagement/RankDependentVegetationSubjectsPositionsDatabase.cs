using System.Collections.Generic;
using System.Linq;
using Assets.Trees.Placement;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;

namespace Assets.Trees.RuntimeManagement
{
    public class RankDependentVegetationSubjectsPositionsDatabase : IVegetationSubjectsPositionsProvider
    {
        private readonly Dictionary<VegetationLevelRank, VegetationSubjectsPositionsDatabase> _databasesDict =
            new Dictionary<VegetationLevelRank, VegetationSubjectsPositionsDatabase>();

        public RankDependentVegetationSubjectsPositionsDatabase(
            Dictionary<VegetationLevelRank, Quadtree<VegetationSubjectEntity>> entitiesDict)
        {
            foreach (var pair in entitiesDict)
            {
                _databasesDict[pair.Key] = new VegetationSubjectsPositionsDatabase(pair.Value);
            }
        }

        public List<VegetationSubjectEntity> GetEntiesFrom(IGeometry area, VegetationDetailLevel level)
        {
            var ranksToTakeFrom = new List<VegetationLevelRank>();
            if (level == VegetationDetailLevel.BILLBOARD)
            {
                ranksToTakeFrom.Add(VegetationLevelRank.Big);
            }
            else if (level == VegetationDetailLevel.REDUCED)
            {
                ranksToTakeFrom.Add(VegetationLevelRank.Big);
                ranksToTakeFrom.Add(VegetationLevelRank.Medium);
            }
            else if (level == VegetationDetailLevel.FULL)
            {
                ranksToTakeFrom.Add(VegetationLevelRank.Big);
                ranksToTakeFrom.Add(VegetationLevelRank.Medium);
                ranksToTakeFrom.Add(VegetationLevelRank.Small);
            }
            else
            {
                Preconditions.Fail("Not supported vegetationDetailLevel " + level);
            }

            return ranksToTakeFrom.SelectMany(c => _databasesDict[c].GetEntiesFrom(area, level)).ToList();
        }
    }
}