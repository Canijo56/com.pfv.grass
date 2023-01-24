using System;
using System.Linq;

using UnityEditor;

using UnityEngine;

using Object = UnityEngine.Object;

namespace PFV.Grass.Editors
{
    public class GrassDefinitionModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public delegate void DeleteGrassDefinitionEvent(string assetPath);
        public static event DeleteGrassDefinitionEvent OnWillDeleteGrassDefinition;

        static string _ONLY_SAVE_ASSET_PATH = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeOnLoad()
        {
            OnWillDeleteGrassDefinition = null;
        }

        public static string[] OnWillSaveAssets(string[] paths)
        {
            if (!string.IsNullOrEmpty(_ONLY_SAVE_ASSET_PATH))
            {
                if (paths.Contains(_ONLY_SAVE_ASSET_PATH))
                    paths = new[] { _ONLY_SAVE_ASSET_PATH };
                _ONLY_SAVE_ASSET_PATH = string.Empty;
            }

            for (int i = 0; i < paths.Length; i++)
            {
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(paths[i]);
                if (typeof(GrassDefinition).IsAssignableFrom(assetType))
                {
                    OnWillSaveDefinition(paths[i]);
                }
            }
            return paths;
        }

        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions removeOptions)
        {
            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (typeof(GrassDefinition).IsAssignableFrom(assetType))
            {
                OnWillDeleteDefinition(assetPath);
            }
            return AssetDeleteResult.DidNotDelete;
        }


        private static void OnWillDeleteDefinition(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GrassDefinition>(path) is GrassDefinition definition)
                GrassProjectSettings.RemoveDefinition(definition);
        }

        private static void OnWillSaveDefinition(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GrassDefinition>(path) is GrassDefinition definition)
                GrassProjectSettings.AddDefinition(definition);
        }

        public static void SaveOnlyAsset(string originalDefinitionAssetPath)
        {
            _ONLY_SAVE_ASSET_PATH = originalDefinitionAssetPath;
            AssetDatabase.SaveAssets();
        }

        public static void RefreshSubAssetName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            // save will deselect objects
            Object[] oldSelection = Selection.objects;
            Object activeContext = Selection.activeContext;
            Object activeObject = Selection.activeObject;


            // TODO ......... cant rename subassets & refresh propperly.
            // The only way i can refresh the project view for SubAsset names is
            // adding a dumy asset, saving the main asset, (this refreshes the view, but
            // with the dummy asset) , then removing the dummy and saving again
            DummyAsset dummy = ScriptableObject.CreateInstance<DummyAsset>();
            AssetDatabase.AddObjectToAsset(dummy, path);
            SaveOnlyAsset(path);
            AssetDatabase.RemoveObjectFromAsset(dummy);
            Object.DestroyImmediate(dummy);
            SaveOnlyAsset(path);
            AssetDatabase.Refresh();
            EditorApplication.delayCall += () =>
            {
                Selection.objects = oldSelection;
                Selection.SetActiveObjectWithContext(activeObject, activeContext);
            };
        }

    }
}