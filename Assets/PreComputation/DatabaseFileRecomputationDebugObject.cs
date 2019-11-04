using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Trees.Db;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.Placement;
using Assets.Utils;
using UnityEngine;

namespace Assets.PreComputation
{
    public class DatabaseFileRecomputationDebugObject : MonoBehaviour
    {
        public void Start()
        {
            var db = VegetationDatabaseFileUtils.LoadRawFromFile($@"C:\inz\dbs2\db_636481152000000000.json");

            Dictionary<VegetationSpeciesEnum, int> speciesDict = new Dictionary<VegetationSpeciesEnum, int>();
            var ranksDict = new Dictionary<VegetationLevelRank, int>();
            foreach (var entity in db)
            {
                var specie = entity.SpeciesEnum;
                if (!speciesDict.ContainsKey(specie))
                {
                    speciesDict[specie] = 1;
                }
                speciesDict[specie]++;

                var rank = entity.Rank;
                if (!ranksDict.ContainsKey(rank))
                {
                    ranksDict[rank] = 0;
                }
                ranksDict[rank]++;
            }
            Debug.Log("By species: " + StringUtils.ToString(speciesDict.Select(c => c.Key + "  " + c.Value)));
            Debug.Log("By rank: " + StringUtils.ToString(ranksDict.Select(c => c.Key + "  " + c.Value)));
        }

        public void Start2()
        {
            var db = VegetationDatabaseFileUtils.LoadRawFromFile(@"C:\inz\dbs2\db_636477696000000000.json");

            var treesPerFile = 1000;

            var buffer = new List<VegetationDatabaseFileUtils.VegetationSubjectJson>();
            int i = 0;
            foreach (var entity in db)
            {
                buffer.Add(entity);
                if (buffer.Count >= treesPerFile)
                {
                    VegetationDatabaseFileUtils.WriteRawToFile(buffer, $@"C:\inz\dbs3\db_{i}.json");
                    buffer.Clear();
                    i++;
                }
            }
        }
    }
}