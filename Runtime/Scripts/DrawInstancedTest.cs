using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class DrawInstancedTest : MonoBehaviour
{
    [SerializeField]
    private GrassRenderFeature.Settings _settings;

    public void Update()
    {
        if (!_settings.IsValid())
            return;
        DrawMeshInstanced();
        // context.
    }

    private void DrawMeshInstanced()
    {

        Matrix4x4[] matrices = new Matrix4x4[_settings.instances];
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        Random.InitState(_settings.seed);
        for (int i = 0; i < matrices.Length; i++)
        {
            Vector3 randomPos = new Vector3(Random.value * _settings.areaSize, 0, Random.value * _settings.areaSize);
            matrices[i] = Matrix4x4.TRS(randomPos + transform.position, transform.rotation * Quaternion.AngleAxis(Random.value * 360, Vector3.up), transform.lossyScale);
        }
        // CommandBuffer cmd = CommandBufferPool.Get();
        // cmd.name = "DrawMeshInstanced Buffer";
        Graphics.DrawMeshInstanced(_settings.mesh, 0, _settings.material, matrices, matrices.Length, block, ShadowCastingMode.TwoSided, true, 0);
        // Graphics.DrawMeshInstancedIndirect(_settings.mesh, 0, _settings.material, 
    }
}