using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Filling.Szecsi
{
    public class NprSzecsiDebugGo : MonoBehaviour
    {
        public void Start()
        {
            var szecsiCycleGen = new SzecsiCyclesGenerator(2, 2);

            var cycle = szecsiCycleGen.GenerateCycle(123);

            var uniformCycle = szecsiCycleGen.GenerateUniformFromCycle(cycle);
            var cycleLength = cycle.Count*2;
            long max = (long) Math.Pow(2, cycleLength);
            uint mask = 0;

            for (int i = 0; i < cycleLength; i++)
            {
                uint v = (uint) (1 << i);
                mask = mask | v;
            }

            GetComponent<MeshRenderer>().material.SetInt("_UniformCycle",(int)uniformCycle);

            var uniformCyclesGroup = szecsiCycleGen.GenerateUniformGroupFromCycle(cycle,2).Select(c => (uint)c).ToList();

            var xCycle = uniformCyclesGroup[0];
            var yCycle = uniformCyclesGroup[1];

            for (int i = 0; i < 16; i++)
            {
                var posX = ((float) xCycle) / (float) (max);
                xCycle = ((xCycle << 1) | (xCycle >> (cycleLength-1)))  & mask;
                var posY = ((float) yCycle) / (float) (max);
                yCycle = ((yCycle << 1) | (yCycle >> (cycleLength-1)))  & mask;

                Debug.Log($"Pos is {posX},{posY}");
            }


            GetComponent<MeshRenderer>().material.SetInt("_UniformCycleX",(int)uniformCyclesGroup[0]);
            GetComponent<MeshRenderer>().material.SetInt("_UniformCycleY",(int)uniformCyclesGroup[1]);
            
            var buf = new ComputeBuffer(3,sizeof(int));
            buf.SetData(new List<uint>(){uniformCycle, xCycle, yCycle});
            GetComponent<MeshRenderer>().material.SetBuffer("_UniformCyclesBuf", buf);
        }
    }

    public class SzecsiCyclesGenerator
    {
        // N - length of period
        // N = K^4 then exacly one seed will fall in every cell of K^2 K^2. This depends on dimension
        private int _k; // length of snippet
        private int _dimensions;
        private System.Random _random;

        public SzecsiCyclesGenerator(int dimensions, int k)
        {
            _dimensions = dimensions;
            _k = k;
        }

        private int CrumbCount => (int) Math.Pow(2, _dimensions);
        private int N => (int) Math.Pow(CrumbCount, _k);

        public uint GenerateUniformFromCycle(List<int> cycle)
        {
            Preconditions.Assert(cycle.Count == 16, "Cycle must be of length 16, but is "+cycle.Count);
            uint uniform = 0;
            for (int i = 0; i < cycle.Count; i++)
            {
                uniform = uniform << 2;
                uniform = uniform | ((uint)cycle[i]);
            }

            return uniform;
        }

        public ulong[] GenerateUniformGroupFromCycle(List<int> cycle, uint dimensions)
        {
            Preconditions.Assert(cycle.Count<=64, "Cycle count must be <= 64 (ulong size)");
            ulong[] uniforms = new ulong[dimensions];
            for (int i = 0; i < cycle.Count; i++)
            {
                for (int d = 0; d < dimensions; d++)
                {
                    uniforms[d] = uniforms[d] << 1;
                    ulong bit = (((ulong) cycle[i]) >> d) & 1; // we take i-th bit counting from right
                    uniforms[d] = uniforms[d] | bit;
                }
            }

            // le trick
            int repetitionsCount = Mathf.FloorToInt(64 / cycle.Count);
            int baseSize = cycle.Count;
            for (int d = 0; d < dimensions; d++)
            {
                ulong repetitionBase = uniforms[d];
                for (int i = 1; i < repetitionsCount ; i++)
                {
                    uniforms[d] = uniforms[d] | (repetitionBase << i * baseSize);
                }
            }

            return uniforms;
        }

        public List<int> GenerateCycle(int seed)
        {
            _random = new System.Random(seed);
            var allSnippets = GenerateAllSnippets();
            var startSequence = allSnippets[0];
            return Uniform(allSnippets.Where(c => !c.Equals(startSequence)).ToList(), startSequence.CrumbsList);
        }


        private List<SzecsiSnippet> GenerateAllSnippets()
        {
            var snippets = new List<List<int>>();
            for (int j = 0; j < CrumbCount; j++)
            {
                snippets.Add(new List<int>() {j});
            }

            for (int i = 0; i < _k-1; i++)
            {
                var newSnippetsList = new List<List<int>>();
                for (int j = 0; j < CrumbCount; j++)
                {
                    newSnippetsList.AddRange(snippets.Select(c =>
                    {
                        var a = c.ToList();
                        a.Add(j);
                        return a;
                    }));
                }

                snippets = newSnippetsList;
            }

            return snippets.Select(c => new SzecsiSnippet(c)).ToList();
        }

        // Quaternary uniform cycle generation
        private List<int> Uniform(List<SzecsiSnippet> P, List<int> Q)
        {
            if (Q.Count != N) //sequence incomplete
            {
                var ro = GetRandomCrumb();
                for (int i = 0; i < CrumbCount; i++)
                {
                    var delta = (ro + i) % CrumbCount;
                    var p = FormSnippetEndingIn(Q,delta);
                    if (P.Contains(p))
                    {
                        var P_Prim = P.Where(c => !c.Equals(p)).ToList();
                        var Q_Prim = Q.ToList();
                        Q_Prim.Add(delta);

                        var Q_dash = Uniform(P_Prim, Q_Prim );
                        if (Q_dash.Any())
                        {
                            return Q_dash;
                        }
                    }
                }
                return new List<int>(); //nothing worked, fail branch
            }
            else //sequence complete
            {
                for (int i = 0; i < _k - 1; i++)
                {
                    var p = FormLoopSnippet(Q,i);
                    if (!P.Contains(p))
                    {
                        return new List<int>();
                    }

                    P = P.Where(c => !c.Equals(p)).ToList();
                }

                return Q;
            }
            Preconditions.Fail("No sequence can be returned");
            return null;
        }

        private SzecsiSnippet FormLoopSnippet(List<int> Q, int offset)
        {
            var l = new List<int>();
            for (int j = _k-1; j >= 0; j--)
            {
                l.Add( Q[(Q.Count-j  + offset) % Q.Count]);
            }

            return new SzecsiSnippet(l);
        }

        private SzecsiSnippet FormSnippetEndingIn(List<int> Q, int delta)
        {
            var l = new List<int>();
            for (int i = Q.Count - _k + 1; i < Q.Count; i++)
            {
                l.Add(Q[i]);
            }
            l.Add(delta);
            return new SzecsiSnippet(l);
        }

        private int GetRandomCrumb()
        {
            return _random.Next(CrumbCount);
        }


        public static PointsWithLastBits GeneratePointsFromUniformGroup(ulong[] uniformGroup, int dimension, int cycleLength)
        {
            var outPositions = new List<List<float>>();
            var outLastBits = new List<List<int>>();

            ulong max = UInt64.MaxValue;
            int longLength = 64;

            for (int i = 0; i < cycleLength; i++)
            {
                outPositions.Add(new List<float>());
                outLastBits.Add(new List<int>());

                for (int d = 0; d < dimension; d++)
                {
                    var pos = ((double) uniformGroup[d] / max);
                    int lastBit = (int) (uniformGroup[d] % 2);

                    outPositions[i].Add((float) pos);
                    outLastBits[i].Add(lastBit);

                    uniformGroup[d] = ((uniformGroup[d] << 1) | (uniformGroup[d] >> (longLength - 1)));
                }
            }

            return new PointsWithLastBits
            {
                Positions = outPositions,
                LastCycleBits = outLastBits
            };
        }
    }

    public class PointsWithLastBits
    {
        public List<List<float>> Positions;
        public List<List<int>> LastCycleBits;
    }

    public class SzecsiSnippet
    {
        public SzecsiSnippet(List<int> crumbsList)
        {
            _crumbsList = crumbsList;
        }

        private readonly List<int> _crumbsList;

        public List<int> CrumbsList => _crumbsList;

        protected bool Equals(SzecsiSnippet other)
        {
            return _crumbsList.SequenceEqual(other._crumbsList);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SzecsiSnippet) obj);
        }

        public override int GetHashCode()
        {
            return (_crumbsList != null ? _crumbsList.GetHashCode() : 0);
        }
    }
}
