using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
namespace PFV.Grass
{
    public class Singleton : MonoBehaviour
    {
        internal static event Action ReleaseInstances;
        internal static bool isClosingScene;
        // TODO: not really necesary with OnDomainLoad /Unload callbacks?

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeOnLoad()
        {
            ReleaseInstances?.Invoke();
        }

        static Singleton()
        {
#if UNITY_EDITOR
            EditorSceneManager.sceneClosing -= OnClosingScene;
            EditorSceneManager.sceneClosing += OnClosingScene;
            EditorSceneManager.sceneClosed -= OnClosedScene;
            EditorSceneManager.sceneClosed += OnClosedScene;
#endif
        }



        private static void OnClosedScene(Scene scene)
        {
            isClosingScene = false;
        }

        private static void OnClosingScene(Scene scene, bool removingScene)
        {
            isClosingScene = true;
        }

    }
    public class Singleton<T> : Singleton
        where T : Singleton<T>
    {
        [NonSerialized]
        private bool hasInitialized = false;
        public static bool autoInstantiate = false;
        public static bool dontDestroyOnLoad = false;
        public static bool playmodeCallbacks = false;
        public static bool beforeAssemblyReloadCallback = false;
        public static bool afterAssemblyReloadCallback = false;
        public static bool onDomainUnloadCallback = false;

        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance)
                    return _instance;
                _instance = GameObject.FindObjectOfType<T>(false);
                if (!_instance)
                {
                    if (autoInstantiate
#if UNITY_EDITOR
                    && !isClosingScene
                    && !EditorSceneManager.IsReloading(EditorSceneManager.GetActiveScene())
#endif
                    )
                    {
                        // Debug.Log("Auto instantiating");
                        _instance = new GameObject(typeof(T).Name).AddComponent<T>();
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            UnityEditor.Undo.RegisterCreatedObjectUndo(_instance.gameObject, $"Auto instantiated {typeof(T).Name} Singleton");
                            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_instance.gameObject.scene);
                        }
#endif
                    }
                }
                else
                {
                    _instance.Awake();

                }
                return _instance;
            }
        }

        static Singleton()
        {
            Singleton.ReleaseInstances -= ReleaseInstance;
            Singleton.ReleaseInstances += ReleaseInstance;
        }

        public static bool HasInstance()
        {
            return _instance != null;
        }


        private void Awake()
        {
            if (hasInitialized)
            {
                Debug.Log("Awake initialized", this);
                return;
            }

            if (_instance != null && _instance != this)
            {
                Debug.Log($"Destroying awake", this);
                Destroy(gameObject);
                return;
            }


            Debug.Log($" {GetInstanceID()} Initialized from awake", this);
            hasInitialized = true;
            _instance = this as T;

#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                if (dontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            if (playmodeCallbacks)
            {
                UnityEditor.EditorApplication.playModeStateChanged -= OnPlaymodeChange;
                UnityEditor.EditorApplication.playModeStateChanged += OnPlaymodeChange;
            }
#endif

            OnSingletonAwake();
        }

        public static void ReleaseInstance()
        {
            if (_instance != null)
            {
                Debug.Log($"{_instance.GetInstanceID()} Releasing");
                _instance.hasInitialized = false;
            }
            _instance = null;
        }

        protected virtual void OnEnable()
        {

            if (_instance != null && !ReferenceEquals(_instance, this))
            {
                Debug.Log($"{GetInstanceID()} Destroying enable {hasInitialized} |existing: {_instance.GetInstanceID()}  ", this);
                Destroy(gameObject);
                return;
            }
            if (!hasInitialized)
            {

                Debug.Log($"{GetInstanceID()} Enable initializing", this);
                Awake();
            }
            _instance = this as T;
#if UNITY_EDITOR
            if (beforeAssemblyReloadCallback)
            {
                UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
                UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            }

            if (afterAssemblyReloadCallback)
            {
                UnityEditor.AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
                UnityEditor.AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            }
#endif
            AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;

            OnSingletonEnable();
        }

        protected virtual void OnDisable()
        {

            if (!hasInitialized)
            {
                Debug.Log($"{GetInstanceID()} Disable, not initialized", this);
                return;
            }

            if (_instance == this)
            {
                Debug.Log($"{GetInstanceID()} Disable, initialized: {hasInitialized}", this);
                OnSingletonDisable();
                _instance = null;

            }
            else
            {
                Debug.Log($"{GetInstanceID()} Disable, nothing");
            }
        }

        private void OnDestroy()
        {

            if (!hasInitialized)
            {
                Debug.Log($"{GetInstanceID()} Destroy non initialized");
                return;
            }
#if UNITY_EDITOR
            if (playmodeCallbacks)
            {
                UnityEditor.EditorApplication.playModeStateChanged -= OnPlaymodeChange;
            }
            if (beforeAssemblyReloadCallback)
            {
                UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            }
            if (afterAssemblyReloadCallback)
            {
                UnityEditor.AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            }
#endif
            OnSingletonDestroy();
            ReleaseInstance();
            Debug.Log($"{GetInstanceID()} Destroyed");
            AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
        }

        private void OnDomainUnload(object sender, EventArgs e)
        {
            // Log($"On Assembly Unload {GetType().Name}");

            AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
            ReleaseInstance();
        }


#if UNITY_EDITOR
        protected virtual void OnAfterAssemblyReload()
        {
        }

        protected virtual void OnBeforeAssemblyReload()
        {
        }

        protected virtual void OnPlaymodeChange(UnityEditor.PlayModeStateChange state)
        {
            // switch(state)
            // {
            // case UnityEditor.PlayModeStateChange.EnteredEditMode: break;
            // case UnityEditor.PlayModeStateChange.ExitingEditMode: break;
            // case UnityEditor.PlayModeStateChange.EnteredPlayMode: break;
            // case UnityEditor.PlayModeStateChange.ExitingPlayMode: break;
            // }
        }
#endif

        protected virtual void OnSingletonAwake() { }
        protected virtual void OnSingletonDestroy() { }
        protected virtual void OnSingletonEnable() { }
        protected virtual void OnSingletonDisable() { }
    }
}