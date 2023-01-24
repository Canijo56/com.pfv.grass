using UnityEngine;

namespace PFV.Grass
{

    public class CircleGrassBrush : GrassBrush
    {
        [SerializeField]
        private float _radius = 1;
        [SerializeField]
        private uint _rings = 1;
        [SerializeField]
        private AnimationCurve _density = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField]
        private float _densityMultiplier = 1;
        [SerializeField]
        [Range(3, 30)]
        private uint _resolution = 1;
        [SerializeField]
        [Range(0, 1)]
        private float _rotate = 1;

        protected override void OnValidate()
        {
            if (_radius < 0.01f)
                _radius = 0.01f;
            if (_rings < 1)
                _rings = 1;
            if (_rings >= _resolution)
                _rings = _resolution - 1;
            base.OnValidate();
        }
        public override void Bake()
        {
            using (UndoUtils.RecordScope(this))
            {


                _patches = new GrassPatch[1];
                _patches[0].vertices = new GrassVertex[_rings * _resolution + 1];
                _patches[0].triangles = new GrassTriangle[_resolution + (2 * _resolution * (_rings - 1))];
                float angle = 2 * Mathf.PI / _resolution;
                _patches[0].vertices[0].position = transform.TransformPoint(Vector3.zero);
                _patches[0].vertices[0].normal = transform.TransformDirection(Vector3.up);
                _patches[0].vertices[1].density = _density.Evaluate(0) * _densityMultiplier;
                for (uint i = 0; i < _rings; i++)
                {
                    float tRing = 1;

                    if (_rings != 1)
                        tRing = (i + 1) / (float)_rings;
                    bool isOddRing = i % 2 == 1;
                    bool isFirstOdd = i == 1;
                    for (uint j = 0; j < _resolution; j++)
                    {
                        uint vertexIndex = i * _resolution + j + 1;
                        float vertexAngle = angle * j;
                        if (isOddRing)
                        {
                            vertexAngle -= _rotate * ((angle * .5f) + angle * ((i - 1) / 2));
                        }
                        else if (i > 0)
                        {
                            vertexAngle -= _rotate * (angle + angle * ((i - 1) / 2));

                        }
                        _patches[0].vertices[vertexIndex].position = transform.TransformPoint(new Vector3(tRing * _radius * Mathf.Cos(vertexAngle), 0, tRing * _radius * Mathf.Sin(vertexAngle)));
                        _patches[0].vertices[vertexIndex].normal = transform.TransformDirection(Vector3.up);
                        float distanceToCenter = Vector3.Distance(_patches[0].vertices[0].position, _patches[0].vertices[vertexIndex].position);
                        _patches[0].vertices[vertexIndex].density = _density.Evaluate(distanceToCenter / _radius) * _densityMultiplier;
                    }
                }
                // first ring
                for (uint i = 0; i < _resolution; i++)
                {
                    GrassTriangle tri = _patches[0].triangles[i];
                    tri.vertexA = 0;
                    tri.vertexB = i + 1;
                    tri.vertexC = ((i + 1) % _resolution) + 1;
                    _patches[0].triangles[i] = tri;
                }
                // all other rings
                for (uint i = 1; i < _rings; i++)
                {
                    for (uint j = 0; j < _resolution; j++)
                    {
                        uint triIndex = _resolution + (2 * (i - 1) * _resolution) + (j * 2);
                        uint vertexIndex = i * _resolution + j + 1;
                        bool isOdd = vertexIndex % 2 == 1;
                        GrassTriangle tri1 = _patches[0].triangles[triIndex];
                        tri1.vertexA = vertexIndex - _resolution;
                        tri1.vertexB = vertexIndex;
                        // if (isOdd)
                        // {
                        //     if (j == _resolution - 1)
                        //         tri1.vertexC = ((i - 1) * _resolution) + 1;
                        //     else
                        //         tri1.vertexC = vertexIndex - _resolution + 1;

                        // }
                        // else
                        {
                            if (j == _resolution - 1)
                                tri1.vertexC = i * _resolution + 1;
                            else tri1.vertexC = vertexIndex + 1;

                        }

                        _patches[0].triangles[triIndex] = tri1;

                        GrassTriangle tri2 = _patches[0].triangles[triIndex + 1];
                        // if (isOdd)
                        // {
                        //     if (j == _resolution - 1)
                        //     {
                        //         tri2.vertexA = ((i - 1) * _resolution) + 1;
                        //         tri2.vertexC = i * _resolution + 1;
                        //     }
                        //     else
                        //     {
                        //         tri2.vertexA = vertexIndex - _resolution + 1;
                        //         tri2.vertexC = vertexIndex + 1;
                        //     }

                        //     tri2.vertexB = vertexIndex;
                        // }
                        // else
                        {
                            tri2.vertexA = vertexIndex - _resolution;
                            if (j == _resolution - 1)
                            {
                                tri2.vertexB = i * _resolution + 1;
                                tri2.vertexC = ((i - 1) * _resolution) + 1;
                            }
                            else
                            {
                                tri2.vertexB = vertexIndex + 1;
                                tri2.vertexC = vertexIndex - _resolution + 1;
                            }
                        }

                        _patches[0].triangles[triIndex + 1] = tri2;
                    }
                }
            }
        }

    }
}