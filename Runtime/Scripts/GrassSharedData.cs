using System;
using UnityEngine;

namespace PFV.Grass
{
    public class RenderSharedData : IDisposable
    {
        private GrassRendererManager _mgr;
        public GrassRendererManager mgr => _mgr;

        public RenderSettings settings => _mgr.settings;

        private RenderBuffers _buffers;
        public RenderBuffers buffers => _buffers;

        [NonSerialized]
        private bool _canRender = false;
        public bool canRender => _canRender;

        public void Validate()
        {
            _canRender = false;
            settings.Validate(this);
            _buffers.Validate(this);
            _canRender = settings.mesh && settings.material && _mgr && _mgr.hasRenderContent
            && (!settings.doCulling || (settings.grassCulling && settings.collectVisibleTriangles && settings.interpolateBladeData && settings.generateBlades));

        }
        public RenderSharedData(GrassRendererManager mgr)
        {
            _mgr = mgr;
            _buffers = new RenderBuffers();
        }


        public void Dispose()
        {
            _canRender = false;
            _buffers.Dispose();
            _buffers = null;
        }
    }
}