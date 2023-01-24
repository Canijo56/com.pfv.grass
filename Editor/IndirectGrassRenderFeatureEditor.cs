using UnityEngine;
using UnityEditor;

namespace PFV.Grass
{

    [CustomEditor(typeof(IndirectGrassRenderFeature))]
    public class IndirectGrassRenderFeatureEditor : Editor
    {
        static GUIStyle _infoStyle;
        static GUIStyle infoStyle
        {
            get
            {
                if (_infoStyle == null)
                {
                    _infoStyle = new GUIStyle(EditorStyles.label)
                    {
                        richText = true
                    };
                }
                return _infoStyle;
            }
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label((target as IndirectGrassRenderFeature).debugInfo, infoStyle);
            base.OnInspectorGUI();

        }
    }
}