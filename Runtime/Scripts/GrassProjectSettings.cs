using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PFV.Grass
{
    public class GrassProjectSettings : ScriptableObject
    {
        public const string DIRECTORY = "Assets/Grass_Settings/Editor";
        public const string ASSET_PATH = DIRECTORY + "/GrassProjectSettings.asset";

        public class PropertyNames
        {
            public const string GrassDefinitions = nameof(GrassProjectSettings._grassDefinitions);
            public const string MaxDensity = nameof(GrassProjectSettings._maxDensity);
        }

        static GrassProjectSettings _instance;
        public static GrassProjectSettings instance
        {
            get
            {
#if UNITY_EDITOR
                return Get();
#else
                return _instance;
#endif
            }
        }

        [SerializeField]
        private List<GrassDefinition> _grassDefinitions;
        public List<GrassDefinition> grassDefinitions => instance?._grassDefinitions;
        [SerializeField]
        private float _maxDensity = 1;
        public float maxDensity => _maxDensity;

        [SerializeField]
        private GrassProjectDebugSettings _debug;
        public GrassProjectDebugSettings debug => _debug;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                return;
            }
            _instance = this;
        }
#if UNITY_EDITOR
        public static GrassProjectSettings Get()
        {
            if (_instance)
                return _instance;
            _instance = AssetDatabase.LoadAssetAtPath<GrassProjectSettings>(ASSET_PATH);
            if (_instance == null)
            {
                _instance = ScriptableObject.CreateInstance<GrassProjectSettings>();
                if (!Directory.Exists(DIRECTORY))
                {
                    Directory.CreateDirectory(DIRECTORY);
                }
                AssetDatabase.CreateAsset(_instance, ASSET_PATH);
                AssetDatabase.SaveAssets();
            }
            return _instance;
        }
#endif

        public static void RemoveDefinition(GrassDefinition definition)
        {
            GrassProjectSettings settings = instance;
            if (!settings)
                return;
            // Wasnt in list
            if (!settings.grassDefinitions.Contains(definition))
                return;
            using (UndoUtils.RecordScope(settings))
                settings.grassDefinitions.Remove(definition);

        }
        public static void AddDefinition(GrassDefinition definition)
        {
            GrassProjectSettings settings = instance;
            if (!settings)
                return;
            // Already in array
            if (settings.grassDefinitions.Contains(definition))
                return;
            using (UndoUtils.RecordScope(settings))
                settings.grassDefinitions.Add(definition);
        }
    }
#if UNITY_EDITOR
    class GrassSettingsProvider : SettingsProvider
    {
        private SerializedObject settings;

        public const string MENU_PATH = "Project/Grass";

        class Styles
        {
            public static GUIContent logSequenciablePhases = new GUIContent("Brushes", "Grass Types");
        }


        public GrassSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public static bool IsSettingsAvailable()
        {
            return File.Exists(GrassProjectSettings.ASSET_PATH);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            settings = new SerializedObject(GrassProjectSettings.Get());
        }

        public override void OnGUI(string searchContext)
        {
            settings.Update();
            EditorGUILayout.PropertyField(settings.FindProperty(GrassProjectSettings.PropertyNames.GrassDefinitions));
            EditorGUILayout.PropertyField(settings.FindProperty(GrassProjectSettings.PropertyNames.MaxDensity));
            settings.ApplyModifiedProperties();
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateGrassSettingsProvider()
        {
            if (IsSettingsAvailable())
            {
                var provider = new GrassSettingsProvider(MENU_PATH, SettingsScope.Project);

                // Automatically extract all keywords from the Styles.
                provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
                return provider;
            }

            // Settings Asset doesn't exist yet; no need to display anything in the Settings window.
            return null;
        }
    }
#endif
}