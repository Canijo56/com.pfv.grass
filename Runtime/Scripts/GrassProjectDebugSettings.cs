using System;
using UnityEngine;
namespace PFV.Grass
{
    [System.Serializable]
    public class GrassProjectDebugSettings
    {
        [SerializeField]
        private Gradient _colorPerDensity;
        public Gradient colorPerDensity => _colorPerDensity;

        public Color GetDensityColor(float density)
        {
            GrassProjectSettings settings = GrassProjectSettings.instance;
            float t = density / settings.maxDensity;
            return colorPerDensity?.Evaluate(t) ?? Color.white;
        }
    }
}