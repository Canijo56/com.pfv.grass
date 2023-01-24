using UnityEngine;

[ExecuteAlways]
public class RenderMeshIndirectTest : MonoBehaviour
{

    [System.Serializable]
     class Settings
    {
        [SerializeField]
        private Mesh _mesh;
        public Mesh mesh => _mesh;

        [SerializeField]
        private Material _material;
        public Material material => _material;
        [SerializeField]
        private uint _instances = 1024;
        public uint instances => _instances;
        [SerializeField]
        private int _seed = 0;
        public int seed => _seed;
        [SerializeField]
        private int _commandCount = 1;
        public int commandCount => _commandCount;
        [SerializeField]
        private int _areaSize = 10;
        public int areaSize => _areaSize;
        [SerializeField]
        private float _scale;
        public float scale => _scale;

        public bool IsValid()
        {
            return _mesh && material && _instances > 0;
        }
    }

    [System.Serializable]
    class Positions
    {
        Matrix4x4[] _positions;
        public ComputeBuffer buffer { get; set; }
        public Matrix4x4[] array => _positions;
        public int length => _positions.Length;

        public Positions(Settings settings)
        {
            _positions = new Matrix4x4[settings.instances];
            Random.InitState(settings.seed);

            int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Matrix4x4));
            for (int i = 0; i < _positions.Length; i++)
            {
                Vector3 randomPos = new Vector3(Random.value * settings.areaSize, 0, Random.value * settings.areaSize);

                _positions[i] = Matrix4x4.TRS(randomPos, Quaternion.AngleAxis(Random.value * 360, Vector3.up), Vector3.one * settings.scale);
            }
            buffer = new ComputeBuffer((int)settings.instances, stride, ComputeBufferType.Structured);
            buffer.SetData(array);
        }
    }
    [SerializeField]
    private Settings _settings = new Settings();
    private Positions _positions;

    private GraphicsBuffer _graphicsBuffer;
    private GraphicsBuffer.IndirectDrawIndexedArgs[] _commandData;
    private RenderParams _renderParams;
    [SerializeField]
    private bool _dual;

    private void OnValidate()
    {
        Init();
    }
    private void OnEnable()
    {
        Init();
    }
    private void Init()
    {
        ReleaseBuffer();
        _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _settings.commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        _positions = new Positions(_settings);
        _commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[_settings.commandCount];
        _renderParams = new RenderParams(_settings.material);
        _renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        _renderParams.receiveShadows = true;
        _renderParams.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds for better FOV culling
        _renderParams.matProps = new MaterialPropertyBlock();
        _renderParams.matProps.SetBuffer("_positionsBuffer", _positions.buffer);
    }


    public void Update()
    {
        if (!_settings.IsValid())
            return;
        if (_commandData == null || _commandData.Length != _settings.commandCount)
        {
            ReleaseBuffer();
            _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _settings.commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            _commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[_settings.commandCount];
        }

        _renderParams.matProps.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
        if (_dual)
        {

            for (int i = 0; i < _settings.commandCount; i++)
            {
                _commandData[i].indexCountPerInstance = _settings.mesh.GetIndexCount(0);
                _commandData[i].instanceCount = _settings.instances / 2;
            }
            _graphicsBuffer.SetData(_commandData);
            Graphics.RenderMeshIndirect(_renderParams, _settings.mesh, _graphicsBuffer, _settings.commandCount);
            Graphics.RenderMeshIndirect(_renderParams, _settings.mesh, _graphicsBuffer, _settings.commandCount);
        }
        else
        {
            for (int i = 0; i < _settings.commandCount; i++)
            {
                _commandData[i].indexCountPerInstance = _settings.mesh.GetIndexCount(0);
                _commandData[i].instanceCount = _settings.instances;
            }
            _graphicsBuffer.SetData(_commandData);
            Graphics.RenderMeshIndirect(_renderParams, _settings.mesh, _graphicsBuffer, _settings.commandCount);
        }

    }


    // Cleanup any allocated resources that were created during the execution of this render pass.
    public void OnDisable()
    {
        ReleaseBuffer();
    }

    private void ReleaseBuffer()
    {
        _graphicsBuffer?.Release();
        if (_positions != null)
        {
            _positions.buffer?.Release();
            _positions.buffer = null;
        }
        _graphicsBuffer = null;
    }
}