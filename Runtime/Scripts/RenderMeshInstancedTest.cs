using UnityEngine;

[ExecuteAlways]
public class RenderMeshInstancedTest : MonoBehaviour
{

    [System.Serializable]
    public class Settings
    {
        [SerializeField]
        private Mesh _mesh;
        public Mesh mesh => _mesh;

        [SerializeField]
        private Material _material;
        public Material material => _material;
        [SerializeField]
        private int _instances = 1024;
        public int instances => _instances;

        [SerializeField]
        private int _seed = 0;
        public int seed => _seed;

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
    public class Positions
    {
        Matrix4x4[] _positions;
        public Matrix4x4[] array => _positions;
        public int length => _positions.Length;

        public Positions(Settings settings)
        {
            _positions = new Matrix4x4[settings.instances];
            Random.InitState(settings.seed);
            for (int i = 0; i < _positions.Length; i++)
            {
                Vector3 randomPos = new Vector3(Random.value * settings.areaSize, 0, Random.value * settings.areaSize);
                _positions[i] = Matrix4x4.TRS(randomPos, Quaternion.AngleAxis(Random.value * 360, Vector3.up), Vector3.one * settings.scale);
            }

        }
    }

    struct CustomInstanceData
    {
        public Matrix4x4 objectToWorld;
    }

    [SerializeField]
    private Settings _settings = new Settings();

    private Positions _positions;
    private CustomInstanceData[] _instanceData;
    private RenderParams _renderParams;

    private void OnValidate()
    {

        PrepareInstanceData();
        _renderParams = new RenderParams(_settings.material);
        _renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        _renderParams.receiveShadows = true;
    }
    private void OnEnable()
    {
        PrepareInstanceData();
        _renderParams = new RenderParams(_settings.material);
        _renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        _renderParams.receiveShadows = true;
    }

    private void PrepareInstanceData()
    {
        _positions = new Positions(_settings);
        _instanceData = new CustomInstanceData[_settings.instances];
        for (int i = 0; i < _settings.instances; ++i)
        {
            _instanceData[i].objectToWorld = _positions.array[i];
        }
    }

    public void Update()
    {
        if (!_settings.IsValid())
            return;
        if (_instanceData == null || _instanceData.Length != _settings.instances)
        {
            PrepareInstanceData();
        }
        Graphics.RenderMeshInstanced(_renderParams, _settings.mesh, 0, _instanceData, _settings.instances);
    }


}