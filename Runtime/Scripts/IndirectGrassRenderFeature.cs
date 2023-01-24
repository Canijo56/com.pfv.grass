using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Mathematics;
using System;

namespace PFV.Grass
{

    [System.Serializable]
    public struct DrawIndirectArgs
    {
        public uint indexCountPerInstance;
        public uint instanceCount;
        public uint startIndexLocation;
        public uint baseVertexLocation;
        public uint startInstanceLocation;

        public override string ToString()
        {
            return @$"indexCountPerInstance: {indexCountPerInstance}
instanceCount: {instanceCount}
startIndexLocation: {startIndexLocation}
baseVertexLocation: {baseVertexLocation}
startInstanceLocation: {startInstanceLocation}";

        }
    }
    [System.Serializable]
    public struct ComputeIndirectArgs
    {
        public uint threadGroupsX;
        public uint threadGroupsY;
        public uint threadGroupsZ;

        public override string ToString()
        {
            return @$"threadGroupsX : {threadGroupsX}
threadGroupsY : {threadGroupsY}
threadGroupsZ : {threadGroupsZ}";
        }
    }
    public class IndirectGrassRenderFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private CameraType _allowedCameras = CameraType.Game | CameraType.SceneView | CameraType.Reflection | CameraType.VR;

        private CullingPass _cullingPass;
        private DepthPrePass _depthPrePass;
        private GrassRenderPass _grassPass;
        private GrassShadowCasterPass _shadowCasterPass;

#if UNITY_EDITOR
        [NonSerialized]
        private DebugInfoLog _debugInfo;
        public DebugInfoLog debugInfo => _debugInfo != null ? _debugInfo : _debugInfo = new DebugInfoLog();
#endif

        GrassRendererManager _mgr;
        [NonSerialized]
        bool _hasCreatedFirstTime = false;
        private void OnValidate()
        {
        }
        /// <inheritdoc/>
        /// 
        public void SetupForRender(GrassRendererManager mgr)
        {
            if (_mgr != null)
            {
                if (_mgr == mgr)
                    return;
                _mgr.OnRenderDataChanged -= OnRenderDataChanged;
            }

            _mgr = mgr;
            if (_mgr)
            {
                _mgr.OnRenderDataChanged -= OnRenderDataChanged;
                _mgr.OnRenderDataChanged += OnRenderDataChanged;
                OnRenderDataChanged(_mgr.renderData);
            }
        }

        private void OnRenderDataChanged(RenderSharedData data)
        {
            if (data != null)
            {
                _cullingPass.Setup(data);
                _shadowCasterPass.Setup(data);
                _depthPrePass.Setup(data);
                _grassPass.Setup(data);
            }
        }

        public override void Create()
        {
            if (!_hasCreatedFirstTime)
            {
                _depthPrePass = new DepthPrePass(this);
                _grassPass = new GrassRenderPass(this);
                _cullingPass = new CullingPass(this);
                _shadowCasterPass = new GrassShadowCasterPass(this);
                _hasCreatedFirstTime = true;
            }
            SetupForRender(GrassRendererManager.Instance);
        }
        protected override void Dispose(bool disposing)
        {
            if (_mgr)
            {
                _mgr.OnRenderDataChanged -= OnRenderDataChanged;
                _mgr = null;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!_allowedCameras.HasFlag(renderingData.cameraData.cameraType))
                return;
            if (_mgr == null || _mgr.renderData == null || !_mgr.renderData.canRender)
            {
                return;
            }

            debugInfo.Clear();

            if (_mgr.renderData.settings.doCulling)
            {
                _cullingPass.renderPassEvent = _mgr.renderData.settings.cullingPassEvt;
                renderer.EnqueuePass(_cullingPass);
            }

            // _depthPrePass.renderPassEvent = _mgr.renderData.settings.depthPrePassEvt;
            // renderer.EnqueuePass(_depthPrePass);
            // _depthPrePass.ConfigureTarget(renderer.cameraDepthTarget);

            _grassPass.renderPassEvent = _mgr.renderData.settings.renderPassEvt;
            renderer.EnqueuePass(_grassPass);
            // _grassPass.ConfigureTarget(renderer.cameraColorTarget, renderer.cameraDepthTarget);

            if (_mgr.renderData.settings.doShadows && renderer is UniversalRenderer universalRenderer)
            {
                universalRenderer.EnqueueShadowCasterPass(_shadowCasterPass);
            }
        }
        static class ShaderPropertyIDs
        {
            public static int bladeSourceDataBufferID = Shader.PropertyToID("_BladeSourceData");
            public static int grassInstanceDataBufferID = Shader.PropertyToID("_GrassInstanceData");
            public static int visibleTriangleIndexesBufferID = Shader.PropertyToID("_VisibleTriangleIndexes");
            public static int isVisiblePerVertexBufferID = Shader.PropertyToID("_IsVisiblePerVertex");
            public static int auxIntBufferID => Shader.PropertyToID("_AuxIntBuffer");

            public static int cameraFrustrumID => Shader.PropertyToID("_CameraFrustrumMatrix");
            public static int objectToWorldID => Shader.PropertyToID("_ObjectToWorld");

            public static int verticesBufferID => Shader.PropertyToID("_Vertices");
            public static int trianglesBufferID => Shader.PropertyToID("_Triangles");

            public static int totalVerticesID => Shader.PropertyToID("_TotalVertices");
            public static int totalTrianglesID => Shader.PropertyToID("_TotalTriangles");
            public static int totalBladesID => Shader.PropertyToID("_TotalBlades");

            public static int trianglePerBlade => Shader.PropertyToID("_TrianglePerBlade");
            public static int drawIndirectArgs => Shader.PropertyToID("_DrawIndirectArgs");
            public static int computeIndirectArgsID => Shader.PropertyToID("_ComputeIndirectArgs");
            public static int generateBladesArgsOffsetID = Shader.PropertyToID("_GenerateBladesArgsOffset");
            public static int interpolateBladesArgsOffsetID = Shader.PropertyToID("_InterpolateBladeArgsOffset");
            public static int bladesPerDensityID = Shader.PropertyToID("_BladesPerDensity");

            public static int vertexSimulatedHeightID => Shader.PropertyToID("_VertexSimulatedHeight");
        }
        static class CSKernelNames
        {
            public const string grassVertexCulling = "CS_GrassVertexCulling";
            public const string collectVisibleTriangles = "CS_CollectVisibleTriangles";
            public const string generateBlades = "CS_GenerateBladesAmount";
            public const string interpolateBladeData = "CS_InterpolateBlades";
        }
        class CullingPass : ScriptableRenderPass
        {
            public RenderSharedData sharedData { get; set; }

            private int _grassCullingKernelID;
            private int _collectVisibleTrianglesKernelID;
            private int _generateBladesKernelID;
            private int _interpolateBladeDataKernelID;

            private ComputeShader _grassCullingCS;
            private ComputeShader _generateBladesCS;
            private ComputeShader _interpolateBladeDataCS;
            private ComputeShader _collectVisibleTrianglesCS;

            private static Vector3Int cullingThreadsPerGroup = new Vector3Int(128, 1, 1);

            private IndirectGrassRenderFeature _feature;

            private const int MAX_CS_THREAD_GROUPS = 65535;

            public CullingPass(IndirectGrassRenderFeature feature) : base()
            {
                _feature = feature;
            }

            public void Setup(RenderSharedData sharedData)
            {
                this.sharedData = sharedData;
                if (_grassCullingCS != sharedData.settings.grassCulling)
                {
                    _grassCullingCS = sharedData.settings.grassCulling;
                    if (_grassCullingCS)
                        _grassCullingKernelID = _grassCullingCS.FindKernel(CSKernelNames.grassVertexCulling);
                }
                if (_collectVisibleTrianglesCS != sharedData.settings.collectVisibleTriangles)
                {
                    _collectVisibleTrianglesCS = sharedData.settings.collectVisibleTriangles;
                    if (_collectVisibleTrianglesCS)
                        _grassCullingKernelID = _collectVisibleTrianglesCS.FindKernel(CSKernelNames.collectVisibleTriangles);
                }
                if (_generateBladesCS != sharedData.settings.generateBlades)
                {
                    _generateBladesCS = sharedData.settings.generateBlades;
                    if (_generateBladesCS)
                        _generateBladesKernelID = _generateBladesCS.FindKernel(CSKernelNames.generateBlades);
                }
                if (_interpolateBladeDataCS != sharedData.settings.interpolateBladeData)
                {
                    _interpolateBladeDataCS = sharedData.settings.interpolateBladeData;
                    if (_interpolateBladeDataCS)
                        _interpolateBladeDataKernelID = _interpolateBladeDataCS.FindKernel(CSKernelNames.interpolateBladeData);
                }
            }


            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                var cameraFrustumPlanes = math.mul(camera.projectionMatrix, camera.worldToCameraMatrix);

                CommandBuffer cmd = CommandBufferPool.Get();
                cmd.Clear();

                _feature.debugInfo.Header("Culling Pass");
                // Reset args


                _feature.debugInfo.ListStart("Resetting buffers...");

                // sharedData.buffers.computeArgsBuffer.ResetToDefault(cmd);
                sharedData.buffers.drawArgsBuffer.ResetToDefault(cmd);
                sharedData.buffers.generateDispatch.SetData(new[] { 0, 1, 1 });
                sharedData.buffers.interpolateDispatch.SetData(new[] { 0, 1, 1 });

                // cmd.SetBufferCounterValue(sharedData.buffers.isVisiblePerVertex, 0);
                cmd.SetBufferCounterValue(sharedData.buffers.visibleTrianglesAppend, 0);
                cmd.SetBufferCounterValue(sharedData.buffers.grassInstanceData, 0);
                cmd.SetBufferCounterValue(sharedData.buffers.bladeSourceData, 0);
                cmd.SetBufferData(sharedData.buffers.auxInt, new uint[3] { 0, 0, 0 });


                _feature.debugInfo.NextLine();
                _feature.debugInfo.ListStart("Starting GrassCulling");

                int cullingThreadGroupsX = Mathf.CeilToInt(sharedData.buffers.vertexAmount / (float)cullingThreadsPerGroup.x);
                _feature.debugInfo.ListItem($"vertexAmount: {sharedData.buffers.vertexAmount}");
                _feature.debugInfo.ListItem($"threadsPerGoup: {cullingThreadsPerGroup.x}");
                _feature.debugInfo.ListItem($"threadsX: {cullingThreadGroupsX}");
                _feature.debugInfo.NextLine();
                // Cull each vertex, and mark it as visible/non-visible
                cmd.SetComputeBufferParam(_grassCullingCS, _grassCullingKernelID, ShaderPropertyIDs.verticesBufferID, sharedData.buffers.vertexBuffer);
                cmd.SetComputeBufferParam(_grassCullingCS, _grassCullingKernelID, ShaderPropertyIDs.isVisiblePerVertexBufferID, sharedData.buffers.isVisiblePerVertex);
                cmd.SetComputeIntParam(_grassCullingCS, ShaderPropertyIDs.vertexSimulatedHeightID, sharedData.settings.vertexSimulatedHeight);
                cmd.SetComputeIntParam(_grassCullingCS, ShaderPropertyIDs.totalVerticesID, (int)sharedData.buffers.vertexAmount);
                cmd.SetComputeMatrixParam(_grassCullingCS, ShaderPropertyIDs.cameraFrustrumID, cameraFrustumPlanes);

                _feature.debugInfo.ListStart("Dispatch GrassCulling");
                cmd.DispatchCompute(_grassCullingCS, _grassCullingKernelID, cullingThreadGroupsX, cullingThreadsPerGroup.y, cullingThreadsPerGroup.z);

                _feature.debugInfo.NextLine();
                _feature.debugInfo.ListStart("Starting CollectVisibleTriangles");
                int collectVisible_ThreadGroupsX = Mathf.CeilToInt(sharedData.buffers.triangleAmount / (float)cullingThreadsPerGroup.x);
                _feature.debugInfo.ListItem($"triangleCount: {sharedData.buffers.triangleAmount}");
                _feature.debugInfo.ListItem($"threadsPerGoup: {cullingThreadsPerGroup.x}");
                _feature.debugInfo.ListItem($"threadsX: {collectVisible_ThreadGroupsX}");
                // Look at each triangle, it will be visible if any of its 3 vertex is visible
                // cmd.SetComputeIntParam(_collectVisibleTrianglesCS, ShaderPropertyIDs.generateBladesArgsOffsetID, (int)sharedData.buffers.computeArgsBuffer[RenderBuffers.ComputeArgs.GenerateBladesArgs].startOffset);
                cmd.SetComputeIntParam(_collectVisibleTrianglesCS, ShaderPropertyIDs.totalTrianglesID, sharedData.buffers.triangleAmount);
                cmd.SetComputeBufferParam(_collectVisibleTrianglesCS, _collectVisibleTrianglesKernelID, ShaderPropertyIDs.trianglesBufferID, sharedData.buffers.trianglesBuffer);
                cmd.SetComputeBufferParam(_collectVisibleTrianglesCS, _collectVisibleTrianglesKernelID, ShaderPropertyIDs.isVisiblePerVertexBufferID, sharedData.buffers.isVisiblePerVertex);
                // cmd.SetComputeBufferParam(_collectVisibleTrianglesCS, _collectVisibleTrianglesKernelID, ShaderPropertyIDs.computeIndirectArgsID, sharedData.buffers.computeArgsBuffer.buffer);
                cmd.SetComputeBufferParam(_collectVisibleTrianglesCS, _collectVisibleTrianglesKernelID, ShaderPropertyIDs.computeIndirectArgsID, sharedData.buffers.generateDispatch);
                cmd.SetComputeBufferParam(_collectVisibleTrianglesCS, _collectVisibleTrianglesKernelID, ShaderPropertyIDs.visibleTriangleIndexesBufferID, sharedData.buffers.visibleTrianglesAppend);
                cmd.SetComputeBufferParam(_collectVisibleTrianglesCS, _collectVisibleTrianglesKernelID, ShaderPropertyIDs.auxIntBufferID, sharedData.buffers.auxInt);
                cmd.DispatchCompute(_collectVisibleTrianglesCS, _collectVisibleTrianglesKernelID, collectVisible_ThreadGroupsX, cullingThreadsPerGroup.y, cullingThreadsPerGroup.z);

                // For each visible triangle, compute the amount of grass that belongs to it
                float unscaledTime = Time.unscaledTime;
                // cmd.SetComputeIntParam(_generateBladesCS, ShaderPropertyIDs.interpolateBladesArgsOffsetID, (int)sharedData.buffers.computeArgsBuffer[RenderBuffers.ComputeArgs.InterpolateBladesArgs].startOffset);
                cmd.SetComputeIntParam(_generateBladesCS, ShaderPropertyIDs.bladesPerDensityID, sharedData.settings.bladesPerDensity);
                cmd.SetComputeVectorParam(_interpolateBladeDataCS, "_Time", new Vector4(unscaledTime, unscaledTime * 2f, unscaledTime / 2f, unscaledTime / 4f));
                cmd.SetComputeBufferParam(_generateBladesCS, _generateBladesKernelID, ShaderPropertyIDs.trianglesBufferID, sharedData.buffers.trianglesBuffer);
                cmd.SetComputeBufferParam(_generateBladesCS, _generateBladesKernelID, ShaderPropertyIDs.verticesBufferID, sharedData.buffers.vertexBuffer);
                cmd.SetComputeBufferParam(_generateBladesCS, _generateBladesKernelID, ShaderPropertyIDs.drawIndirectArgs, sharedData.buffers.drawArgsBuffer.buffer);
                // cmd.SetComputeBufferParam(_generateBladesCS, _generateBladesKernelID, ShaderPropertyIDs.computeIndirectArgsID, sharedData.buffers.computeArgsBuffer.buffer);
                cmd.SetComputeBufferParam(_generateBladesCS, _generateBladesKernelID, ShaderPropertyIDs.computeIndirectArgsID, sharedData.buffers.interpolateDispatch);
                cmd.SetComputeBufferParam(_generateBladesCS, _generateBladesKernelID, ShaderPropertyIDs.visibleTriangleIndexesBufferID, sharedData.buffers.visibleTrianglesAppend);
                cmd.SetComputeBufferParam(_generateBladesCS, _generateBladesKernelID, ShaderPropertyIDs.bladeSourceDataBufferID, sharedData.buffers.bladeSourceData);
                cmd.SetComputeBufferParam(_generateBladesCS, _generateBladesKernelID, ShaderPropertyIDs.grassInstanceDataBufferID, sharedData.buffers.grassInstanceData);
                cmd.SetComputeBufferParam(_generateBladesCS, _generateBladesKernelID, ShaderPropertyIDs.auxIntBufferID, sharedData.buffers.auxInt);
                // cmd.DispatchCompute(_generateBladesCS, _generateBladesKernelID, sharedData.buffers.computeArgsBuffer.buffer, sharedData.buffers.computeArgsBuffer[RenderBuffers.ComputeArgs.GenerateBladesArgs].bytesStartOffset);
                cmd.DispatchCompute(_generateBladesCS, _generateBladesKernelID, sharedData.buffers.generateDispatch, 0);

                // cmd.SetComputeBufferParam(_generateBladesCS, _generateBladesKernelID, ShaderPropertyIDs.grassInstanceDataBufferID, sharedData.buffers.grassInstanceData);
                // For each grass blade, interpolate its positions / normal values
                cmd.SetComputeVectorParam(_interpolateBladeDataCS, "_Time", new Vector4(unscaledTime, unscaledTime * 2f, unscaledTime / 2f, unscaledTime / 4f));
                cmd.SetComputeVectorParam(_interpolateBladeDataCS, "_Lalal", sharedData.settings.lalala);
                cmd.SetComputeBufferParam(_interpolateBladeDataCS, _interpolateBladeDataKernelID, ShaderPropertyIDs.trianglesBufferID, sharedData.buffers.trianglesBuffer);
                cmd.SetComputeBufferParam(_interpolateBladeDataCS, _interpolateBladeDataKernelID, ShaderPropertyIDs.verticesBufferID, sharedData.buffers.vertexBuffer);
                cmd.SetComputeBufferParam(_interpolateBladeDataCS, _interpolateBladeDataKernelID, ShaderPropertyIDs.bladeSourceDataBufferID, sharedData.buffers.bladeSourceData);
                cmd.SetComputeBufferParam(_interpolateBladeDataCS, _interpolateBladeDataKernelID, ShaderPropertyIDs.grassInstanceDataBufferID, sharedData.buffers.grassInstanceData);
                cmd.SetComputeBufferParam(_interpolateBladeDataCS, _interpolateBladeDataKernelID, ShaderPropertyIDs.drawIndirectArgs, sharedData.buffers.drawArgsBuffer.buffer);
                cmd.SetComputeBufferParam(_interpolateBladeDataCS, _interpolateBladeDataKernelID, ShaderPropertyIDs.auxIntBufferID, sharedData.buffers.auxInt);
                // cmd.SetComputeBufferParam(_interpolateBladeDataCS, _interpolateBladeDataKernelID, ShaderPropertyIDs.computeIndirectArgsID, sharedData.buffers.computeArgsBuffer.buffer);
                // cmd.DispatchCompute(_interpolateBladeDataCS, _interpolateBladeDataKernelID, sharedData.buffers.computeArgsBuffer.buffer, sharedData.buffers.computeArgsBuffer[RenderBuffers.ComputeArgs.InterpolateBladesArgs].bytesStartOffset);
                cmd.DispatchCompute(_interpolateBladeDataCS, _interpolateBladeDataKernelID, sharedData.buffers.interpolateDispatch, 0);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

        }
        class GrassShadowCasterPass : ExtraShadowCasterPass
        {
            public RenderSharedData sharedData { get; set; }

            private MaterialPropertyBlock _block;
            private IndirectGrassRenderFeature _feature;
            public void Setup(RenderSharedData sharedData)
            {
                this.sharedData = sharedData;
                _block.SetMatrix(ShaderPropertyIDs.objectToWorldID, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, sharedData.settings.scale));
                _block.SetBuffer(ShaderPropertyIDs.grassInstanceDataBufferID, sharedData.buffers.grassInstanceData);
            }

            public GrassShadowCasterPass(IndirectGrassRenderFeature feature) : base()
            {
                this._feature = feature;
                _block = new MaterialPropertyBlock();
            }

            public override void ExecuteDuringShadows(CommandBuffer cmd, ref ScriptableRenderContext context, ref ShadowSliceData shadowSliceData, ref ShadowDrawingSettings settings)
            {
                cmd.DrawMeshInstancedIndirect(
                    sharedData.settings.mesh,
                    0,
                    sharedData.settings.material,
                    sharedData.settings.material.FindPass("ShadowCaster"),
                    sharedData.buffers.drawArgsBuffer.buffer,
                    (int)sharedData.buffers.drawArgsBuffer[RenderBuffers.DrawArgs.DrawArgs].bytesStartOffset, _block);
            }

        }

        class GrassRenderPass : ScriptableRenderPass
        {
            public RenderSharedData sharedData { get; set; }
            private MaterialPropertyBlock _block;
            private IndirectGrassRenderFeature _feature;

            public void Setup(RenderSharedData sharedData)
            {
                this.sharedData = sharedData;
            }
            public GrassRenderPass(IndirectGrassRenderFeature feature) : base()
            {
                this._feature = feature;
                base.profilingSampler = new ProfilingSampler("Grass Forward Pass");
                _block = new MaterialPropertyBlock();
            }
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                _block.Clear();
                _block.SetMatrix(ShaderPropertyIDs.objectToWorldID, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, sharedData.settings.scale));
                _block.SetBuffer(ShaderPropertyIDs.grassInstanceDataBufferID, sharedData.buffers.grassInstanceData);
                _block.SetBuffer(ShaderPropertyIDs.drawIndirectArgs, sharedData.buffers.drawArgsBuffer.buffer);
                CommandBuffer cmd = CommandBufferPool.Get();
                cmd.Clear();
                cmd.DrawMeshInstancedIndirect(
                    sharedData.settings.mesh, 0,
                    sharedData.settings.material,
                    0,
                    sharedData.buffers.drawArgsBuffer.buffer,
                    // (int)sharedData.buffers.drawArgsBuffer[RenderBuffers.DrawArgs.DrawArgs].bytesStartOffset,
                    0,
                    _block);
                // cmd.DrawProceduralIndirect()
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
        class DepthPrePass : ScriptableRenderPass
        {
            private RenderTargetHandle _colorRT;
            private RenderTargetHandle _depthRT;
            private MaterialPropertyBlock _block;
            private IndirectGrassRenderFeature _feature;

            public RenderSharedData sharedData { get; set; }

            public void Setup(RenderSharedData sharedData)
            {
                this.sharedData = sharedData;
                _block.SetMatrix(ShaderPropertyIDs.objectToWorldID, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, sharedData.settings.scale));
                _block.SetBuffer(ShaderPropertyIDs.grassInstanceDataBufferID, sharedData.buffers.grassInstanceData);
            }
            public DepthPrePass(IndirectGrassRenderFeature feature) : base()
            {
                this._feature = feature;
                _colorRT.Init("_CameraColorAttachmentA");
                _depthRT.Init("_CameraDepthTexture");
                base.profilingSampler = new ProfilingSampler("Grass Depth PrePass");
                _block = new MaterialPropertyBlock();
            }


            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                _block.SetBuffer(ShaderPropertyIDs.grassInstanceDataBufferID, sharedData.buffers.grassInstanceData);
                CommandBuffer cmd = CommandBufferPool.Get();
                cmd.Clear();

                cmd.DrawMeshInstancedIndirect(
                    sharedData.settings.mesh, 0,
                    sharedData.settings.material,
                    sharedData.settings.material.FindPass("DepthOnly"),
                    sharedData.buffers.drawArgsBuffer.buffer,
                    (int)sharedData.buffers.drawArgsBuffer[RenderBuffers.DrawArgs.DrawArgs].bytesStartOffset,
                    _block);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                base.OnCameraSetup(cmd, ref renderingData);
                this.ConfigureTarget(_depthRT.Identifier());
            }

        }
    }
}