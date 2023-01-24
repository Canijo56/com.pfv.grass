using UnityEngine;

namespace PFV.Grass
{

    [CreateAssetMenu(fileName = "Brush_00", menuName = "Grass Rendering/Brush Data", order = 0)]
    public class GrassDefinition : ScriptableObject
    {
        [SerializeField]
        private Mesh _mesh;
        public Mesh mesh => _mesh;

        [SerializeField]
        private MaterialData _materialData;
        public MaterialData materialData => _materialData;
    }
}