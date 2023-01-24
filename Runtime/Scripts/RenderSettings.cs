using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PFV.Grass
{
    [System.Serializable]
    public class RenderSettings
    {
        [SerializeField]
        private bool _doShadows;
        public bool doShadows => _doShadows;
        [SerializeField]
        private bool _doCulling;
        public bool doCulling => _doCulling;
        [SerializeField]
        private Mesh _mesh;
        public Mesh mesh => _mesh;
        [SerializeField]
        private Material _material;
        public Material material => _material;
        [SerializeField]
        private ComputeShader _grassCulling;
        public ComputeShader grassCulling => _grassCulling;
        [SerializeField]
        private ComputeShader _collectVisibleTriangles;
        public ComputeShader collectVisibleTriangles => _collectVisibleTriangles;
        [SerializeField]
        private ComputeShader _generateBlades;
        public ComputeShader generateBlades => _generateBlades;
        [SerializeField]
        private ComputeShader _interpolateBladeData;
        public ComputeShader interpolateBladeData => _interpolateBladeData;
        [SerializeField]
        private int _vertexSimulatedHeight = 1;
        public int vertexSimulatedHeight => _vertexSimulatedHeight;
        [SerializeField]
        private int _maxBlades = 1000000;
        public int maxBlades => _maxBlades;
        [SerializeField]
        private int _bladesPerDensity = 15;
        public int bladesPerDensity => _bladesPerDensity;
        [SerializeField]
        private Vector3 _scale = Vector3.one;
        public Vector3 scale => _scale;
        [SerializeField]
        private Vector4 _lalala = Vector4.one;
        public Vector4 lalala => _lalala;
        [SerializeField]
        private RenderPassEvent _renderPassEvt = RenderPassEvent.AfterRenderingOpaques;
        public RenderPassEvent renderPassEvt => _renderPassEvt;
        [SerializeField]
        private RenderPassEvent _depthPrePassEvt = RenderPassEvent.AfterRenderingPrePasses;
        public RenderPassEvent depthPrePassEvt => _depthPrePassEvt;
        [SerializeField]
        private RenderPassEvent _cullingPassEvt = RenderPassEvent.BeforeRenderingPrePasses;
        public RenderPassEvent cullingPassEvt => _cullingPassEvt;

        public void Validate(RenderSharedData data)
        {
            if (_bladesPerDensity <= 0)
                _bladesPerDensity = 1;
            if (_maxBlades <= 0)
                _maxBlades = 1;
        }
    }

}