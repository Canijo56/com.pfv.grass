using System;
using System.Linq;

using UnityEditor;

using UnityEngine;

using Object = UnityEngine.Object;

namespace PFV.Grass.Editors
{
    public class GrassDefinitionAssetPostProcessor : UnityEditor.AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            string currentAssetPath;
            for (int i = 0; i < importedAssets.Length; i++)
            {
                currentAssetPath = importedAssets[i];
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(currentAssetPath);
                if (typeof(GrassDefinition).IsAssignableFrom(assetType)
                 && AssetDatabase.LoadAssetAtPath<GrassDefinition>(currentAssetPath) is GrassDefinition definition)
                {
                    GrassProjectSettings.AddDefinition(definition);
                }
            }
        }
    }
}