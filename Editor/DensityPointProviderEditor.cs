using UnityEngine;
using UnityEditor;

namespace PFV.Grass.Editors
{
    [CustomEditor(typeof(GrassProvider), true)]
    public class DensityPointProviderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Bake"))
            {
                (target as GrassProvider).Bake();
            }

        }
    }
}