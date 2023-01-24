using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;
using Unity.Mathematics;
using UnityEngine.Profiling;
using System.Runtime.InteropServices;

public class GrassRenderFeature : ScriptableRendererFeature
{
    [SerializeField]
    private bool _doShadows;
    [SerializeField]
    private bool _doCulling;

    [SerializeField]
    private Settings _settings;

    [NonSerialized]
    private SharedData _sharedData;

    // Passes
    private CullingPass _cullingPass;
    private GrassRenderPass _grassPass;
    // private GrassShadowCasterPass _shadowCasterPass;

    /// <inheritdoc/>
    public override void Create()
    {
        _sharedData?.Dispose();
        _sharedData = new SharedData(_settings);
        _grassPass = new GrassRenderPass(_sharedData);
        _cullingPass = new CullingPass(_sharedData);
        // _shadowCasterPass = new GrassShadowCasterPass(_sharedData);
        _grassPass.renderPassEvent = _settings.renderPassEvt;
        _cullingPass.renderPassEvent = _settings.cullingPassEvt;
        // _shadowPass.renderPassEvent = _settings.shadowPassEvt;
    }
    protected override void Dispose(bool disposing)
    {
        _sharedData?.Dispose();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_doCulling)
        {
            renderer.EnqueuePass(_cullingPass);
        }

        renderer.EnqueuePass(_grassPass);

        if (_doShadows && renderer is UniversalRenderer universalRenderer)
        {
            // universalRenderer.EnqueueShadowCasterPass(_shadowCasterPass);
        }
    }
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
        private ComputeShader _cullingCompute;
        public ComputeShader cullingCompute => _cullingCompute;
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
        private Vector3 _scale = Vector3.one;
        public Vector3 scale => _scale;
        [SerializeField]
        private RenderPassEvent _renderPassEvt = RenderPassEvent.AfterRenderingOpaques;
        public RenderPassEvent renderPassEvt => _renderPassEvt;
        [SerializeField]
        private RenderPassEvent _cullingPassEvt = RenderPassEvent.BeforeRendering;
        public RenderPassEvent cullingPassEvt => _cullingPassEvt;

        public bool IsValid()
        {
            return _mesh && material && _instances > 0;
        }

    }
    struct AABBData
    {
        public Vector3 boundsCenter;
        public Vector3 boundsExtents;
    };
    class SharedData : IDisposable
    {
        private Buffers _buffers;
        public Buffers buffers => _buffers;
        private Arrays _arrays;
        public Arrays arrays => _arrays;
        private Settings _settings;
        public Settings settings => _settings;

        public SharedData(Settings settings)
        {
            _settings = settings;
            _arrays = new Arrays(this);
            _buffers = new Buffers(this);
        }


        public void Dispose()
        {
            _buffers?.Dispose();
            _arrays?.Dispose();
        }
    }
    class Buffers : IDisposable
    {

        private ComputeBuffer _indirectArgsBuffer;
        public ComputeBuffer indirectArgsBuffer => _indirectArgsBuffer;

        private ComputeBuffer _shadowIndirectArgsBuffer;
        public ComputeBuffer shadowIndirectArgsBuffer => _shadowIndirectArgsBuffer;

        private ComputeBuffer _visibleIndicesAppendBuffer;
        public ComputeBuffer visibleIndicesAppendBuffer => _visibleIndicesAppendBuffer;

        private ComputeBuffer _grassInstancesBuffer;
        public ComputeBuffer grassInstancesBuffer => _grassInstancesBuffer;

        // private ComputeBuffer _visibleIndicesBuffer;
        // public ComputeBuffer visibleIndicesBuffer => _visibleIndicesBuffer;

        private ComputeBuffer _positions;
        public ComputeBuffer positions => _positions;

        private ComputeBuffer _AABBs;
        public ComputeBuffer AABBs => _AABBs;

        private uint[] _args;

        public Buffers(SharedData sharedData)
        {
            // Indirect args
            _args = new uint[5] { 0, 0, 0, 0, 0 };
            _args[0] = (uint)sharedData.settings.mesh.GetIndexCount(0);
            _args[1] = (uint)sharedData.settings.instances;
            _args[2] = (uint)sharedData.settings.mesh.GetIndexStart(0);
            _args[3] = (uint)sharedData.settings.mesh.GetBaseVertex(0);

            _indirectArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            _indirectArgsBuffer.SetData(_args);

            _shadowIndirectArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            _shadowIndirectArgsBuffer.SetData(_args);

            _positions = new ComputeBuffer((int)(sharedData.settings.instances), Marshal.SizeOf(typeof(Matrix4x4)), ComputeBufferType.Structured);
            _positions.SetData(sharedData.arrays.positions);

            _AABBs = new ComputeBuffer((int)(sharedData.settings.instances), Marshal.SizeOf(typeof(AABBData)), ComputeBufferType.Structured);
            _AABBs.SetData(sharedData.arrays.aabbs);

            _visibleIndicesAppendBuffer = new ComputeBuffer((int)(sharedData.settings.instances), sizeof(uint), ComputeBufferType.Append);
            _grassInstancesBuffer = new ComputeBuffer((int)sharedData.settings.instances, Marshal.SizeOf<PFV.Grass.GrassBladeInstanceData>());

            _grassInstancesBuffer.SetData(sharedData.arrays.positions.Select(p => new PFV.Grass.GrassBladeInstanceData() { positionWS = p.GetPosition() }).ToArray()); ;
            // _visibleIndicesAppendBuffer = new ComputeBuffer((int)(sharedData.settings.instances), sizeof(uint), ComputeBufferType.Structured);
        }

        public void Dispose()
        {
            _indirectArgsBuffer?.Dispose();
            _indirectArgsBuffer = null;
            _shadowIndirectArgsBuffer?.Dispose();
            _shadowIndirectArgsBuffer = null;
            _positions?.Dispose();
            _positions = null;
            _visibleIndicesAppendBuffer?.Dispose();
            _visibleIndicesAppendBuffer = null;
            // _visibleIndicesBuffer?.Dispose();
            // _visibleIndicesBuffer = null;
            _grassInstancesBuffer?.Dispose();
            _grassInstancesBuffer = null;
            _AABBs?.Dispose();
            _AABBs = null;
        }
    }

    class Arrays : IDisposable
    {
        Matrix4x4[] _positions;
        AABBData[] _aabbs;
        public Matrix4x4[] positions => _positions;
        public AABBData[] aabbs => _aabbs;

        public Arrays(SharedData sharedData)
        {
            _positions = new Matrix4x4[sharedData.settings.instances];
            _aabbs = new AABBData[sharedData.settings.instances];
            Random.InitState(sharedData.settings.seed);
            int areaSize = sharedData.settings.areaSize;
            Vector3 scale = sharedData.settings.scale;
            for (int i = 0; i < _positions.Length; i++)
            {
                Vector3 randomPos = new Vector3(Random.value * areaSize, 0, Random.value * areaSize);
                _positions[i] = Matrix4x4.TRS(
                    randomPos,
                    Quaternion.AngleAxis(Random.value * 360, Vector3.up),
                    scale);
                Bounds bounds = new Bounds(randomPos, Vector3.one * .01f);
                _aabbs[i] = new AABBData()
                {
                    boundsCenter = bounds.center,
                    boundsExtents = bounds.extents
                };
            }
        }


        public void Dispose()
        {
            _positions = null;
        }
    }
    static class ShaderVariableIDs
    {
        public static int grassInstanceDataBufferID = Shader.PropertyToID("_GrassInstanceData");
        public static int drawIndirectArgs => Shader.PropertyToID("_DrawIndirectArgs");
        public static int visibleInstancesBufferID = Shader.PropertyToID("_VisibleInstances");
        public static int cameraFrustrumID => Shader.PropertyToID("_CameraFrustrumMatrix");
        public static int positionsBufferID => Shader.PropertyToID("_InstancePositions");
        public static int aabbsBufferID => Shader.PropertyToID("_InstanceAABBs");
        public static int objectToWorldID => Shader.PropertyToID("_ObjectToWorld");
        public static int totalInstancesID => Shader.PropertyToID("_TotalInstances");
    }
    static class CSKernelNames
    {
        public const string gpuCulling = "CS_GPUCulling";
    }
    class CullingPass : ScriptableRenderPass
    {
        private SharedData _data;

        private int _kernelID;
        private bool _isSetup;
        private ComputeShader _cullingCS;

        private static Vector3Int threadGroups = new Vector3Int(128, 1, 1);

        private const int MAX_CS_THREAD_GROUPS = 65535;

        public CullingPass(SharedData sharedData)
        {
            _data = sharedData;
            Setup();
        }

        private bool Setup()
        {

            _isSetup = false;
            if (!_data.settings.cullingCompute)
                return false;
            _cullingCS = _data.settings.cullingCompute;
            _kernelID = _cullingCS.FindKernel(CSKernelNames.gpuCulling);
            if (_kernelID < 0)
                return false;

            return _isSetup = true;
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!_isSetup && !Setup())
                return;

            Camera camera = renderingData.cameraData.camera;
            var cameraFrustumPlanes = math.mul(camera.projectionMatrix, camera.worldToCameraMatrix);

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.Clear();
            cmd.SetBufferCounterValue(_data.buffers.visibleIndicesAppendBuffer, 0);
            cmd.SetComputeBufferParam(_cullingCS, _kernelID, ShaderVariableIDs.visibleInstancesBufferID, _data.buffers.visibleIndicesAppendBuffer);
            cmd.SetComputeBufferParam(_cullingCS, _kernelID, ShaderVariableIDs.aabbsBufferID, _data.buffers.AABBs);
            cmd.SetComputeMatrixParam(_cullingCS, ShaderVariableIDs.cameraFrustrumID, cameraFrustumPlanes);
            cmd.SetComputeIntParam(_cullingCS, ShaderVariableIDs.totalInstancesID, (int)_data.settings.instances);
            // int threadsX = Mathf.CeilToInt(Mathf.Sqrt(_data.settings.instances));
            // int threadsY = Mathf.CeilToInt(Mathf.Sqrt(_data.settings.instances));
            // cmd.SetComputeIntParam(_cullingCS, ShaderVariableIDs.threadDimensionX, (int)threadsX);
            cmd.DispatchCompute(_cullingCS, _kernelID, Mathf.CeilToInt((int)_data.settings.instances / (float)threadGroups.x), threadGroups.y, threadGroups.z);

            cmd.CopyCounterValue(_data.buffers.visibleIndicesAppendBuffer, _data.buffers.indirectArgsBuffer, sizeof(uint));
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

        }

    }
    // class GrassShadowCasterPass : ExtraShadowCasterPass
    // {
    //     private SharedData _sharedData;

    //     MaterialPropertyBlock _block;
    //     public GrassShadowCasterPass(SharedData sharedData)
    //     {
    //         _sharedData = sharedData;
    //         _block = new MaterialPropertyBlock();
    //         if (sharedData.settings.IsValid())
    //         {
    //             _block.SetMatrix(ShaderVariableIDs.objectToWorldID, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
    //             _block.SetBuffer(ShaderVariableIDs.positionsBufferID, sharedData.buffers.positions);
    //         }
    //     }

    //     public override void ExecuteDuringShadows(CommandBuffer cmd, ref ScriptableRenderContext context, ref ShadowSliceData shadowSliceData, ref ShadowDrawingSettings settings)
    //     {
    //         if (!_sharedData.settings.IsValid())
    //             return;

    //         cmd.DrawMeshInstancedIndirect(_sharedData.settings.mesh, 0, _sharedData.settings.material, _sharedData.settings.material.FindPass("ShadowCaster"), _sharedData.buffers.shadowIndirectArgsBuffer, 0, _block);
    //     }

    // }

    class GrassRenderPass : ScriptableRenderPass
    {
        private MaterialPropertyBlock _block;
        private SharedData _sharedData;

        public GrassRenderPass(SharedData sharedData)
        {
            _sharedData = sharedData;
            base.profilingSampler = new ProfilingSampler("Grass Forward Pass");
            _block = new MaterialPropertyBlock();
            _block.SetMatrix(ShaderVariableIDs.objectToWorldID, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, sharedData.settings.scale));
            _block.SetBuffer(ShaderVariableIDs.positionsBufferID, sharedData.buffers.positions);
            _block.SetBuffer(ShaderVariableIDs.visibleInstancesBufferID, sharedData.buffers.visibleIndicesAppendBuffer);
            _block.SetBuffer(ShaderVariableIDs.grassInstanceDataBufferID, sharedData.buffers.grassInstancesBuffer);
            _block.SetBuffer(ShaderVariableIDs.drawIndirectArgs, sharedData.buffers.indirectArgsBuffer);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!_sharedData.settings.IsValid())
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.Clear();
            // RenderingUtils.SetViewAndProjectionMatrices(cmd, renderingData.cameraData.GetViewMatrix(), renderingData.cameraData.GetProjectionMatrix(), false);
            cmd.DrawMeshInstancedIndirect(_sharedData.settings.mesh, 0, _sharedData.settings.material, 0, _sharedData.buffers.indirectArgsBuffer, 0, _block);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }


    }
}







