using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.NPR.DataBuffers
{
    public enum MyShaderBufferType
    {
        Adjacency, Barycentric, EdgeAngle, PrincipalCurvature,  TrimeshPrincipalCurvature, InterpolatedNormals
    }

    public static class MyShaderBufferTypeUtils
    {
        private static Dictionary<MyShaderBufferType, MyShadeBufferTypeDetails> _details = new Dictionary<MyShaderBufferType, MyShadeBufferTypeDetails>()
        {
            {MyShaderBufferType.Adjacency, new MyShadeBufferTypeDetails() {StrideInFloats = 3, BufferFileSuffix = "Adjacency", InShaderBufferName = "_AdjacencyBuffer"} },
            {MyShaderBufferType.Barycentric, new MyShadeBufferTypeDetails() {StrideInFloats = 2, BufferFileSuffix = "Barycentric", InShaderBufferName = "_BarycentricCoordinatesBuffer"} },
            {MyShaderBufferType.EdgeAngle, new MyShadeBufferTypeDetails() {StrideInFloats = 1, BufferFileSuffix = "EdgeAngle", InShaderBufferName = "_EdgeAngleBuffer"} },
            {MyShaderBufferType.InterpolatedNormals, new MyShadeBufferTypeDetails() {StrideInFloats = 3, BufferFileSuffix = "InterpolatedNormals", InShaderBufferName = "_InterpolatedNormalsBuffer"} },
            {MyShaderBufferType.TrimeshPrincipalCurvature, new MyShadeBufferTypeDetails() {StrideInFloats = 12, BufferFileSuffix = "TrimeshPrincipalCurvature", InShaderBufferName = "_PrincipalCurvatureBuffer"} },
        };

        public static MyShadeBufferTypeDetails Details(this MyShaderBufferType type)
        {
            return _details[type];
        }
    }

    public class MyShadeBufferTypeDetails
    {
        public String InShaderBufferName;
        public String BufferFileSuffix;
        public int StrideInFloats;
    }
}
