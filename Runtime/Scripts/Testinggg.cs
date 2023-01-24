using UnityEngine;

public class Testinggg : MonoBehaviour
{
    [SerializeField]
    [Range(0, 1)]
    private float _value;

    [SerializeField]
    private Renderer _renderer;

    MaterialPropertyBlock _block;
    private void OnValidate()
    {
        if (_renderer && _renderer.sharedMaterial)
        {
            // _renderer.sharedMaterial.SetFloat("_LerpThing", _value);
            if (_block == null)
                _block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat("_LerpThing", _value);
            _renderer.SetPropertyBlock(_block);
            // Shader.SetGlobalFloat("_LerpThing", _value);
        }
    }
}