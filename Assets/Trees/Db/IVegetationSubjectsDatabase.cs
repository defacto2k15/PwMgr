using System.Collections.Generic;
using Assets.Trees.Placement;
using NetTopologySuite.Index.Quadtree;

namespace Assets.Trees.Db
{
    public interface IVegetationSubjectsDatabase
    {
        void AddSubject(VegetationSubject subject, VegetationLevelRank levelRank);
        List<VegetationSubject> GetSubjectsAt(GenerationArea area);

        Dictionary<VegetationLevelRank, Quadtree<VegetationSubject>> Subjects { get; }
    }
}