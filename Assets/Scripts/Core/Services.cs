using System;
using System.Collections.Generic;
using Core.Interfaces;
using Managers;
using UnityEngine;

namespace Core
{
    /// <summary>
    ///     Helps create and manage services in the game.
    ///     !!! Will execute on start without a scene !!!
    ///     Uses the RuntimeInitializeOnLoadMethod attribute to ensure it runs before any scene is loaded.
    ///     Order of initialization:
    ///     BeforeSceneLoad -> CreateServices -> All other monobehaviour's Awake -> LateAwake
    /// </summary>
    public class Services : MonoBehaviour, IServiceLocator
    {
        private readonly static ServiceContainer serviceContainer = ServiceContainer.Create();
        private static bool isQuitting;
        private static bool isInitialized;
        private static bool isInitializing;
        public static Services Instance { get; private set; }

        private static bool CanInitialize()
        {
            return !isQuitting && Application.isPlaying;
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        // Add your services in this method
        ///////////////////////////////////////////////////////////////////////////////////////
        private void CreateServices()
        {
            AddService(new EventManager());
            AddMonoComponentService<TimeManager>();
            AddMonoComponentService<GameInputManager>();
            AddPrefabService<MusicManager>("MusicManager", true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        // You don't need to change anything below this
        ///////////////////////////////////////////////////////////////////////////////////////
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            isQuitting = false;
            isInitialized = false;
            isInitializing = false;
            EnsureInstance();
        }

        private static void EnsureInstance()
        {
            if (!CanInitialize())
                return;
            if (Instance != null)
                return;

            var go = new GameObject("Services");
            Instance = go.AddComponent<Services>();
            DontDestroyOnLoad(go);

            Instance.InitializeServices();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance != this || isQuitting)
                return;

            serviceContainer.Cleanup();
            Instance = null;
            isInitialized = false;
            isInitializing = false;
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            serviceContainer.Cleanup();
            isInitialized = false;
            isInitializing = false;
        }

        public void AddService<T>(T service) where T : IService
        {
            serviceContainer.Add(service);
        }

        public bool HasService<T>() where T : IService
        {
            return serviceContainer.Has<T>();
        }

        public T GetService<T>() where T : IService
        {
            return serviceContainer.Get<T>();
        }

        public void RemoveService<T>() where T : IService
        {
            serviceContainer.Remove<T>();
        }

        private void InitializeServices()
        {
            if (isInitialized || isInitializing)
                return;

            isInitializing = true;
            try
            {
                CreateServices();
                serviceContainer.InitializeServices();
                serviceContainer.StartServices();
                isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Services] Initialization failed: {ex}");
            }
            finally
            {
                isInitializing = false;
            }
        }

        private static void EnsureServicesInitialized()
        {
            if (!CanInitialize())
                return;

            EnsureInstance();
            if (!isInitialized && !isInitializing && Instance != null)
                Instance.InitializeServices();
        }

        // Static helpers so callers don't have to reference Services.Instance
        public static void Add<T>(T service) where T : IService
        {
            if (!CanInitialize())
                return;
            EnsureServicesInitialized();
            serviceContainer.Add(service);
        }

        public static bool Has<T>() where T : IService
        {
            if (!CanInitialize())
                return false;
            EnsureServicesInitialized();
            return serviceContainer.Has<T>();
        }

        public static T Get<T>() where T : IService
        {
            if (!CanInitialize())
                return default;
            EnsureServicesInitialized();
            return serviceContainer.Get<T>();
        }

        public static void Remove<T>() where T : IService
        {
            if (!CanInitialize())
                return;
            EnsureServicesInitialized();
            serviceContainer.Remove<T>();
        }


        private T AddMonoComponentService<T>() where T : MonoBehaviour, IService
        {
            if (HasService<T>())
                return GetService<T>();

            var service = gameObject.GetComponent<T>();
            if (service == null)
                service = gameObject.AddComponent<T>();

            AddService(service);
            return service;
        }

        public void CreatePrefab(string prefabPath, bool parentToServices)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return;

            var prefab = Resources.Load(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Services] Prefab at '{prefabPath}' not found in Resources. Skipping instantiate.");
                return;
            }

            var parent = parentToServices ? transform : null;
            var instance = Instantiate(prefab, parent);
            if (instance == null)
            {
                Debug.Log($"Failed to instantiate startup prefab at path: {prefabPath}");
            }
        }

        public T AddPrefabService<T>(string prefabPath, bool parentToServices) where T : MonoBehaviour, IService
        {
            if (string.IsNullOrEmpty(prefabPath))
                return null;

            if (HasService<T>())
                return GetService<T>();

            T prefab = Resources.Load<T>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Services] Service prefab '{prefabPath}' not loaded (type {typeof(T).Name}).");
                return null;
            }

            var parent = parentToServices ? transform : null;
            var instance = Instantiate(prefab, parent);
            instance.gameObject.name = prefab.gameObject.name;
            DontDestroyOnLoad(instance.gameObject);
            AddService(instance);
            return instance;
        }

        private sealed class ServiceContainer
        {
            private readonly Dictionary<Type, object> container = new(8);
            private bool initialized;
            private bool started;

            private ServiceContainer()
            {
            }

            public static ServiceContainer Create()
            {
                return new ServiceContainer();
            }

            public void Add<T>(T service) where T : IService
            {
                var key = typeof(T);
                if (service == null)
                    throw new ArgumentNullException(nameof(service));
                if (service is not T)
                    throw new ArgumentException("Type mismatch", nameof(service));
                if (!container.TryAdd(key, service))
                    throw new InvalidOperationException($"{key} already registered");

                // If services are already initialized/started, bring late additions up to date.
                if (initialized)
                    (service as IService)?.InitializeService();
                if (started)
                    (service as IService)?.StartService();
            }

            public bool Remove<T>() where T : IService
            {
                return container.Remove(typeof(T));
            }

            public bool Has<T>() where T : IService
            {
                return container.ContainsKey(typeof(T));
            }

            public T Get<T>() where T : IService
            {
                var found = container.TryGetValue(typeof(T), out var obj);
                if (found)
                    return (T)obj;
                Debug.LogError($"Service of type {typeof(T).FullName} not found");
                return default;
            }

            public void InitializeServices()
            {
                if (initialized)
                    return;

                foreach (var obj in container.Values)
                    (obj as IService)?.InitializeService();
                initialized = true;
            }

            public void StartServices()
            {
                if (started)
                    return;

                foreach (var obj in container.Values)
                    (obj as IService)?.StartService();
                started = true;
            }

            public void Cleanup()
            {
                foreach (var obj in container.Values)
                    (obj as IService)?.CleanupService();
                container.Clear();
            }
        }
    }
}