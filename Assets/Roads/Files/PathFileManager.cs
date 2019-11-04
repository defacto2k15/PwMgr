using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Roads.Pathfinding.Fitting;
using Assets.Utils;
using Assets.Utils.MT;
using NetTopologySuite.IO;

namespace Assets.Roads.Files
{
    public class PathFileManager
    {
        public void SavePaths(string rootFilePath, List<PathQuantisized> geoPath)
        {
            var writer = new WKTWriter();
            int i = 0;
            foreach (var aGeoPath in geoPath)
            {
                var uniqueFilePath = rootFilePath + $"/path-{i}.wkt";
                //using (var fileStream = new FileStream(uniqueFilePath, FileMode.Create, FileAccess.Write))
                //{
                var payload = writer.WriteFormatted(aGeoPath.Line);
                File.WriteAllText(uniqueFilePath, payload);
                i++;
                //}
            }
        }

        public List<PathQuantisized> LoadPaths(string rootFilePath)
        {
            var files = System.IO.Directory.GetFiles(rootFilePath, "*.wkt");
            var reader = new WKTReader();
            //var reader = new WKBReader();

            List<PathQuantisized> outList = new List<PathQuantisized>();
            foreach (var filePath in files)
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    outList.Add(new PathQuantisized(reader.Read(fileStream)));
                }
            }

            return outList;
        }

        public async Task<List<PathQuantisized>> LoadPathsAsync(string rootFilePath)
        {
            var files = System.IO.Directory.GetFiles(rootFilePath, "*.wkt");
            var reader = new WKTReader();

            List<PathQuantisized> outList = new List<PathQuantisized>();

            var filePayloads = await TaskUtils.WhenAll(files.Select(file => AsyncFileUtils.ReadAllBytesAsync(file)));
            foreach (var payload in filePayloads)
            {
                var payloadAsString = System.Text.Encoding.UTF8.GetString(payload);
                outList.Add(new PathQuantisized(reader.Read(payloadAsString)));
            }

            return outList;
        }
    }
}