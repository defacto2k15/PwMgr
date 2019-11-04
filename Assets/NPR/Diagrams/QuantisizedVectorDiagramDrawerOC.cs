using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.NPR.Diagrams
{
    public class QuantisizedVectorDiagramDrawerOC : MonoBehaviour
    {
        private List<PositionedVector> _vectorsToDraw;
        private bool _useQuantization = false;

        public void Start()
        {
            _vectorsToDraw = GenerateNonQuantisizedVectors();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                _useQuantization = !_useQuantization;
                if (_useQuantization)
                {
                    _vectorsToDraw = GenerateQuantisizedVectors();
                }
                else
                {
                    _vectorsToDraw = GenerateNonQuantisizedVectors();
                }
            }

            _vectorsToDraw.ForEach(c =>
            {
                if (_useQuantization)
                {
                    Debug.DrawRay(c.Position, c.Direction / 4f, GenerateColorFromDirection(c.Direction));
                }
                else
                {
                    Debug.DrawRay(c.Position, c.Direction / 4f);
                }

            });
        }

        private Color GenerateColorFromDirection(Vector3 dir)
        {
            var seed = Mathf.RoundToInt((dir.x + 1) * 20 + (dir.y + 1) * 200 + (dir.z + 1) * 20000);
            UnityEngine.Random.InitState(seed);
            return UnityEngine.Random.ColorHSV();
        }

        private List<PositionedVector> GenerateNonQuantisizedVectors()
        {
            var mesh = GetComponent<MeshFilter>().mesh;
            return Enumerable.Range(0, mesh.vertexCount).Select(i => new PositionedVector()
            {
                Direction = mesh.normals[i],
                Position = mesh.vertices[i]
            }).ToList();

        }

        private List<PositionedVector> GenerateQuantisizedVectors()
        {
            return GenerateNonQuantisizedVectors().Select(c => new PositionedVector()
            {
                Position = c.Position,
                Direction = Quantisize(c.Direction)
            }).ToList();

        }
			Vector2 getYawAndPitch(Vector3 v) {
				float at2 = Mathf.Atan2(v.x, v.z);
				return new Vector2(at2, Mathf.Asin(-v.y));
			}

			Vector3 yawAndPitchToVector(Vector2 yawAndPitch) {
				float alpha = yawAndPitch.x;
				float beta = yawAndPitch.y;
				Vector3 v = new Vector3(
					Mathf.Sin(alpha)*Mathf.Cos(beta), 
					-Mathf.Sin(beta),
					Mathf.Cos(alpha) * Mathf.Cos(beta)
				);
				return v;
			}

			Vector3 quantisizeNormalizedVectorWithOffsetToYAndP(Vector3 nn, int quantCount) {
				Vector2 yAndP = getYawAndPitch(nn); // X: <-PI,PI> Y: <-PI/2, PI/2>
				yAndP.y *= 2; //<-PI, PI>

				Vector2 a = yAndP / 3.14f; //<-1,1>
				a = (a / 2.0f) + new Vector2(0.5f,0.5f); // <0,1>
				a = a * quantCount; //<0,quantCount-1> == <0,q>
			    IntVector2 fa = new IntVector2(Mathf.FloorToInt(a.x), Mathf.FloorToInt(a.y));
			    fa = new IntVector2(fa.X % quantCount, fa.Y % quantCount);


				Vector2 qYAndP = fa.ToFloatVec()/(float)(quantCount); // <0,1>
				qYAndP = qYAndP * 2 * 3.14f; //<0,2*PI>
				qYAndP = qYAndP - new Vector2(3.14f, 3.14f); //<-PI,PI>

				qYAndP.y /= 2; //X: <-PI, PI> Y:<-PI/2, PI/2>

			    return qYAndP;
			}


        private Vector3 Quantisize(Vector3 orgDirection)
        {
            return yawAndPitchToVector(quantisizeNormalizedVectorWithOffsetToYAndP(orgDirection, 4));
        }

        class PositionedVector
        {
            public Vector3 Position;
            public Vector3 Direction;
        }
    }
}
