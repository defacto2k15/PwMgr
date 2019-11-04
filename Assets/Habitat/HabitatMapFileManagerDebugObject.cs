using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Pathfinding;
using UnityEngine;

namespace Assets.Habitat
{
    public class HabitatMapFileManagerDebugObject : MonoBehaviour
    {
        public void Start()
        {
            var loader = new HabitatMapOsmLoader();
            var fields = loader.Load(@"C:\inz\osm\map.osm");

            var translator = new HabitatFieldPositionTranslator(GeoCoordsToUnityTranslator.DefaultTranslator);
            fields = fields.Select(c => translator.Translate(c)).ToList();

            var map = HabitatMap.Create(
                new MyRectangle(62 * 720, 67 * 720, 8 * 720, 8 * 720),
                new Vector2(90 * 8, 90 * 8),
                fields,
                HabitatType.NotSpecified,
                HabitatTypePriorityResolver.Default);

            var fileManager = new HabitatMapFileManager();
            fileManager.SaveHabitatMap($@"C:\inz\habitating3\", map);

            var remap = fileManager.LoadHabitatMap($@"C:\inz\habitating3\");
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    var tree = remap.QueryMap(
                        new MyRectangle(62 * 720 + x * 720, 67 * 720 + y * 720, 720, 720));

                    var parentObject = new GameObject($"x:{x} y:{y}");
                    tree.QueryAll()
                        .ForEach(c => HabitatMapOsmLoaderDebugObject.CreateDebugHabitatField(c.Field, 0.01f,
                            parentObject));
                }
            }
        }
    }
}