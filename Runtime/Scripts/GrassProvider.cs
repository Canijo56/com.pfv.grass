using System.Collections.Generic;
using UnityEngine;

namespace PFV.Grass
{
    [ExecuteInEditMode]
    public abstract class GrassProvider : MonoBehaviour
    {
        public abstract bool hasData { get; }
        public abstract void Bake();
        public abstract void GetData(ref List<GrassVertex> vertex, ref List<GrassTriangle> triangles);

        private void OnEnable()
        {
            GrassRendererManager.RegisterProvider(this);
        }
        private void OnDisable()
        {

            GrassRendererManager.UnregisterProvider(this);
        }
    }
}