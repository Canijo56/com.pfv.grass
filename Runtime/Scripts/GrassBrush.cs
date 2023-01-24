using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PFV.Grass
{

    public abstract class GrassBrush : GrassProvider
    {
        [SerializeField]
        [GrassBrushSelector]
        private GrassDefinition _grass;
        public GrassDefinition grass => _grass;

        [SerializeField]
        protected GrassPatch[] _patches = new GrassPatch[0];

        [SerializeField]
        private bool _drawVertexIndex;
        [SerializeField]
        private bool _reBakeOnValidate;

        public override bool hasData => _patches.Length > 0 && _patches.Any(p => p.vertices.Length > 0);

        protected virtual void OnValidate()
        {
            if (_reBakeOnValidate)
                Bake();
        }

        public override void GetData(ref List<GrassVertex> vertex, ref List<GrassTriangle> triangles)
        {
            for (int i = 0; i < this._patches.Length; i++)
            {
                vertex.AddRange(_patches[i].vertices);
                triangles.AddRange(_patches[i].triangles);
            }
        }
        private void OnDrawGizmosSelected()
        {
            Color oldColor = Gizmos.color;
            for (int i = 0; i < _patches?.Length; i++)
            {
                for (int j = 0; j < _patches[i].vertices.Length; j++)
                {
                    Gizmos.color = GrassProjectSettings.instance.debug.GetDensityColor(_patches[i].vertices[j].density);
                    Gizmos.DrawSphere(_patches[i].vertices[j].position, 0.1f);
                    Gizmos.DrawRay(_patches[i].vertices[j].position, _patches[i].vertices[j].normal);
#if UNITY_EDITOR
                    UnityEditor.SceneView sceneView = UnityEditor.SceneView.currentDrawingSceneView;
                    Camera camera = sceneView?.camera;
                    if (!camera)
                    {
                        camera = Camera.main;
                        if (!camera)
                        {
                            Gizmos.color = oldColor;
                            return;
                        }
                    }
                    if (_drawVertexIndex && sceneView)
                    {
                        UnityEditor.Handles.BeginGUI();
                        Color oldGUIColor = GUI.color;
                        Vector3 screenPos = camera.WorldToScreenPoint(_patches[i].vertices[j].position + Vector3.up * .5f);
                        Vector2 size = GUI.skin.label.CalcSize(new GUIContent(j.ToString()));
                        GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + sceneView.position.height + 4, size.x, size.y), j.ToString());
                        GUI.color = oldGUIColor;
                        UnityEditor.Handles.EndGUI();
                    }
#endif
                }
                for (int j = 0; j < _patches[i].triangles.Length; j++)
                {
                    GrassTriangle tri = _patches[i].triangles[j];
                    Gizmos.color = Color.blue;
                    int verticesLength = _patches[i].vertices.Length;
                    if (tri.vertexA < verticesLength && tri.vertexB < verticesLength && tri.vertexC < verticesLength)
                    {
                        Gizmos.DrawLine(_patches[i].vertices[tri.vertexA].position, _patches[i].vertices[tri.vertexB].position);
                        Gizmos.DrawLine(_patches[i].vertices[tri.vertexB].position, _patches[i].vertices[tri.vertexC].position);
                        Gizmos.DrawLine(_patches[i].vertices[tri.vertexC].position, _patches[i].vertices[tri.vertexA].position);
                    }
                    else
                    {
                        Debug.Log($"Invalid index in triangle {j} (vertices {tri.vertexA} | {tri.vertexB} | {tri.vertexC}");
                    }
                }
            }
            Gizmos.color = oldColor;
        }
    }
}