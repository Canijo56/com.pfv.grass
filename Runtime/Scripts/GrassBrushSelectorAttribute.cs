using UnityEngine;

namespace PFV.Grass
{
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class GrassBrushSelectorAttribute : PropertyAttribute
    {
        public GrassBrushSelectorAttribute()
        {
        }

    }
}