using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

public class TestRenderFeature : ScriptableRendererFeature
{
    public bool logExecute;
    public RenderPassEvent renderEvt;

    private TestPass _testPass;
    public override void Create()
    {
        _testPass = new TestPass(this);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_testPass);
        _testPass.renderPassEvent = renderEvt;
    }
    protected override void Dispose(bool disposing)
    {
    }


    class TestPass : ScriptableRenderPass
    {
        TestRenderFeature _feature;
        public TestPass(TestRenderFeature feature)
        {
            _feature = feature;
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_feature.logExecute)
            {
                Debug.Log($"Executing {Time.frameCount}");
            }

        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
        public void Dispose()
        {
        }
    }


}


