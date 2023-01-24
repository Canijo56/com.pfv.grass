using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace PFV.Grass
{
    [System.Serializable]
    public struct GrassBladeInstanceData
    {
        public Vector3 positionWS;
        public uint batchIndex;
        public Vector3 normalWS;
    }
    [System.Serializable]
    public struct BladeSourceData
    {
        public uint triangleIndex;
        public uint bladeIndexPerTri;
    }
    [System.Serializable]
    public struct GrassPatch
    {
        public GrassVertex[] vertices;
        public GrassTriangle[] triangles;
    }
    [System.Serializable]
    public struct GrassVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public float density;
    }
    [System.Serializable]
    public struct GrassTriangle
    {
        public uint vertexA;
        public uint vertexB;
        public uint vertexC;
    }
    public struct IndirectBatchData
    {
        public int instanceCount;
    }
    public struct VertexCullResult
    {
        public float visible;
    }
    [System.Serializable]
    public struct MaterialData
    {
        public Color color;
    }
    [ExecuteInEditMode]
    public class GrassRendererManager : Singleton<GrassRendererManager>
    {
        [SerializeField]
        private RenderSettings _settings = new RenderSettings();
        public RenderSettings settings => _settings;
        [SerializeField]
        List<GrassProvider> _providers = new List<GrassProvider>();

        public delegate void RenderDataChangedEvent(RenderSharedData data);
        public event RenderDataChangedEvent OnRenderDataChanged;

        [NonSerialized]
        private RenderSharedData _renderData;
        public RenderSharedData renderData => _renderData;

        public bool hasRenderContent => _providers.Count > 0 && _providers.Any(p => p.hasData);

        private void OnValidate()
        {
            if (_renderData != null)
            {
                _renderData.Validate();
                OnRenderDataChanged?.Invoke(_renderData);
            }
        }
        static GrassRendererManager()
        {
            autoInstantiate = true;
        }

        public void GetAllVertexAndTriangles(ref List<GrassVertex> vertices, ref List<GrassTriangle> triangles)
        {
            for (int i = 0; i < _providers.Count; i++)
            {
                if (_providers[i])
                {
                    _providers[i].GetData(ref vertices, ref triangles);
                }
            }
        }

        public static void RegisterProvider(GrassProvider grassProvider)
        {
            GrassRendererManager mgr = Instance;
            if (!mgr)
                return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                using (UndoUtils.RecordScope(mgr))
#endif
                {
                    if (!mgr._providers.Contains(grassProvider))
                        mgr._providers.Add(grassProvider);
                }
        }
        public static void UnregisterProvider(GrassProvider grassProvider)
        {
            GrassRendererManager mgr = Instance;
            if (!mgr)
                return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                using (UndoUtils.RecordScope(mgr))
#endif
                {
                    if (mgr._providers.Contains(grassProvider))
                        mgr._providers.Remove(grassProvider);
                }
        }

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
        }

        protected override void OnSingletonDestroy()
        {
            base.OnSingletonDestroy();
        }

        protected override void OnSingletonDisable()
        {
            base.OnSingletonDisable();
            _renderData?.Dispose();
            _renderData = null;
        }

        protected override void OnSingletonEnable()
        {
            base.OnSingletonEnable();
            if (_renderData == null)
            {
                _renderData = new RenderSharedData(this);
            }
            _renderData.Validate();
            OnRenderDataChanged?.Invoke(_renderData);
        }
    }
}