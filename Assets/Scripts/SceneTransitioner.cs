using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitioner : MonoBehaviour
{
    private static SceneTransitioner instance;
    private static bool isQuitting;
    private Coroutine transitionRoutine;
    private bool isTransitioning;
    private readonly List<Camera> disabledCameras = new(8);
    private readonly List<Canvas> disabledCanvases = new(8);
    private readonly List<GameObject> dissolveVisibilityTargets = new(4);
    private Material transitionMaterial;
    private int dissolvePropertyId;
    private Canvas transitionCanvas;
    private float currentDissolveValue = -1f;

    public static void LoadScene(
        string targetScene,
        string transitionScene,
        float holdSeconds,
        float dissolveInSeconds,
        float dissolveOutSeconds
    )
    {
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogWarning("SceneTransitioner: target scene is empty.");
            return;
        }

        var runner = EnsureInstance();
        if (runner == null)
            return;

        if (runner.isTransitioning)
        {
            Debug.LogWarning("SceneTransitioner: transition already in progress.");
            return;
        }

        if (runner.transitionRoutine != null)
            runner.StopCoroutine(runner.transitionRoutine);

        runner.transitionRoutine = runner.StartCoroutine(
            runner.TransitionRoutine(
                targetScene,
                transitionScene,
                holdSeconds,
                dissolveInSeconds,
                dissolveOutSeconds
            )
        );
    }

    private static SceneTransitioner EnsureInstance()
    {
        if (instance != null)
            return instance;
        if (!Application.isPlaying || isQuitting)
            return null;

        var go = new GameObject("SceneTransitioner");
        instance = go.AddComponent<SceneTransitioner>();
        DontDestroyOnLoad(go);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private IEnumerator TransitionRoutine(
        string targetScene,
        string transitionScene,
        float holdSeconds,
        float dissolveInSeconds,
        float dissolveOutSeconds
    )
    {
        isTransitioning = true;

        var currentScene = SceneManager.GetActiveScene();
        bool hasDissolve = false;

        if (!string.IsNullOrWhiteSpace(transitionScene))
        {
            if (!SceneManager.GetSceneByName(transitionScene).isLoaded)
                yield return SceneManager.LoadSceneAsync(transitionScene, LoadSceneMode.Additive);

            var transition = SceneManager.GetSceneByName(transitionScene);
            if (transition.IsValid() && transition.isLoaded)
                SceneManager.SetActiveScene(transition);

            CacheTransitionCanvas(transitionScene);

            var currentCamera = FindSceneCamera(currentScene);
            var transitionCamera = FindSceneCamera(transition);
            var activeCamera = currentCamera != null ? currentCamera : transitionCamera;
            SetTransitionCanvasCamera(activeCamera);
            DisableOtherCameras(activeCamera);
            DisableNonTransitionCanvases(transitionScene);
            CacheDissolveVisibilityTargets(transitionScene);
            UpdateDissolveVisibility(0f);

            hasDissolve = TryPrepareTransitionDissolve(transitionScene);
            if (hasDissolve)
                yield return TweenDissolve(0f, 1f, dissolveInSeconds);
            else if (dissolveInSeconds > 0f)
                yield return new WaitForSecondsRealtime(dissolveInSeconds);
        }

        if (currentScene.IsValid() && currentScene.isLoaded && currentScene.name != transitionScene)
        {
            var transition = SceneManager.GetSceneByName(transitionScene);
            var transitionCamera = FindSceneCamera(transition);
            SetTransitionCanvasCamera(transitionCamera);
            DisableOtherCameras(transitionCamera);
            yield return SceneManager.UnloadSceneAsync(currentScene);
        }

        AsyncOperation loadOp = null;
        if (!SceneManager.GetSceneByName(targetScene).isLoaded)
        {
            loadOp = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
            if (loadOp != null)
                loadOp.allowSceneActivation = false;
        }

        float minHold = Mathf.Max(holdSeconds, 1f);
        float holdStart = Time.unscaledTime;
        bool holdComplete = minHold <= 0f;

        while (true)
        {
            if (!holdComplete && Time.unscaledTime - holdStart >= minHold)
                holdComplete = true;

            bool loadReady = loadOp == null || loadOp.progress >= 0.9f;
            if (holdComplete && loadReady)
                break;

            yield return null;
        }

        if (loadOp != null)
        {
            loadOp.allowSceneActivation = true;
            yield return loadOp;
        }

        var target = SceneManager.GetSceneByName(targetScene);
        if (target.IsValid() && target.isLoaded)
            SceneManager.SetActiveScene(target);

        if (!string.IsNullOrWhiteSpace(transitionScene))
        {
            var targetCamera = FindSceneCamera(target);
            SetTransitionCanvasCamera(targetCamera);
            DisableOtherCameras(targetCamera);
            DisableNonTransitionCanvases(transitionScene);
        }

        if (hasDissolve)
            yield return TweenDissolve(1f, 0f, dissolveOutSeconds);

        if (!string.IsNullOrWhiteSpace(transitionScene))
        {
            var transition = SceneManager.GetSceneByName(transitionScene);
            if (transition.IsValid() && transition.isLoaded && transition.name != targetScene)
                yield return SceneManager.UnloadSceneAsync(transition);
        }

        RestoreCameras();
        RestoreCanvases();
        CleanupTransitionMaterial();
        ClearDissolveVisibilityTargets();
        isTransitioning = false;
    }

    private void DisableOtherCameras(Camera keepCamera)
    {
        if (keepCamera != null)
        {
            keepCamera.enabled = true;
            disabledCameras.Remove(keepCamera);
        }

        var cameras = Object.FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            var camera = cameras[i];
            if (camera == null || !camera.enabled)
                continue;

            if (keepCamera != null && camera == keepCamera)
                continue;

            camera.enabled = false;
            if (!disabledCameras.Contains(camera))
                disabledCameras.Add(camera);
        }
    }

    private void RestoreCameras()
    {
        for (int i = 0; i < disabledCameras.Count; i++)
        {
            var camera = disabledCameras[i];
            if (camera != null)
                camera.enabled = true;
        }
        disabledCameras.Clear();
    }

    private void DisableNonTransitionCanvases(string transitionScene)
    {
        if (string.IsNullOrWhiteSpace(transitionScene))
            return;

        var canvases = Object.FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            var canvas = canvases[i];
            if (canvas == null || !canvas.enabled)
                continue;

            var canvasScene = canvas.gameObject.scene;
            if (canvasScene.IsValid() && canvasScene.isLoaded && canvasScene.name == transitionScene)
                continue;

            canvas.enabled = false;
            disabledCanvases.Add(canvas);
        }
    }

    private void RestoreCanvases()
    {
        for (int i = 0; i < disabledCanvases.Count; i++)
        {
            var canvas = disabledCanvases[i];
            if (canvas != null)
                canvas.enabled = true;
        }
        disabledCanvases.Clear();
    }

    private void CacheTransitionCanvas(string transitionScene)
    {
        if (transitionCanvas != null)
        {
            var scene = transitionCanvas.gameObject.scene;
            if (scene.IsValid() && scene.isLoaded && scene.name == transitionScene)
                return;
        }

        transitionCanvas = null;
        if (string.IsNullOrWhiteSpace(transitionScene))
            return;

        var canvases = Object.FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            var canvas = canvases[i];
            if (canvas == null)
                continue;

            var canvasScene = canvas.gameObject.scene;
            if (!canvasScene.IsValid() || !canvasScene.isLoaded || canvasScene.name != transitionScene)
                continue;

            transitionCanvas = canvas;
            break;
        }
    }

    private void SetTransitionCanvasCamera(Camera camera)
    {
        if (transitionCanvas == null || camera == null)
            return;

        transitionCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        transitionCanvas.worldCamera = camera;
    }

    private static Camera FindSceneCamera(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        Camera enabledMain = null;
        Camera enabledAny = null;
        Camera anyMain = null;
        Camera any = null;

        var cameras = Object.FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            var camera = cameras[i];
            if (camera == null)
                continue;

            var cameraScene = camera.gameObject.scene;
            if (!cameraScene.IsValid() || !cameraScene.isLoaded || cameraScene != scene)
                continue;

            if (any == null)
                any = camera;

            bool isMain = camera.CompareTag("MainCamera");
            if (isMain && anyMain == null)
                anyMain = camera;

            if (!camera.enabled || !camera.gameObject.activeInHierarchy)
                continue;

            if (isMain && enabledMain == null)
                enabledMain = camera;

            if (enabledAny == null)
                enabledAny = camera;
        }

        if (enabledMain != null)
            return enabledMain;
        if (enabledAny != null)
            return enabledAny;
        if (anyMain != null)
            return anyMain;
        return any;
    }

    private bool TryPrepareTransitionDissolve(string transitionScene)
    {
        transitionMaterial = null;
        dissolvePropertyId = -1;
        currentDissolveValue = -1f;

        if (string.IsNullOrWhiteSpace(transitionScene))
            return false;

        var graphics = Object.FindObjectsOfType<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            var graphic = graphics[i];
            if (graphic == null)
                continue;

            var graphicScene = graphic.gameObject.scene;
            if (!graphicScene.IsValid() || !graphicScene.isLoaded || graphicScene.name != transitionScene)
                continue;

            var material = graphic.material;
            if (material == null)
                continue;

            int dissolveId = Shader.PropertyToID("_Dissolve");
            if (!material.HasProperty(dissolveId))
            {
                int altId = Shader.PropertyToID("Dissolve");
                if (!material.HasProperty(altId))
                    continue;
                dissolveId = altId;
            }

            transitionMaterial = new Material(material);
            graphic.material = transitionMaterial;
            dissolvePropertyId = dissolveId;
            transitionMaterial.SetFloat(dissolvePropertyId, 0f);
            UpdateDissolveVisibility(0f);
            return true;
        }

        return false;
    }

    private IEnumerator TweenDissolve(float from, float to, float duration)
    {
        if (transitionMaterial == null || dissolvePropertyId == -1)
            yield break;

        if (duration <= 0f)
        {
            transitionMaterial.SetFloat(dissolvePropertyId, to);
            UpdateDissolveVisibility(to);
            yield break;
        }

        float elapsed = 0f;
        transitionMaterial.SetFloat(dissolvePropertyId, from);
        UpdateDissolveVisibility(from);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float value = Mathf.Lerp(from, to, t);
            transitionMaterial.SetFloat(dissolvePropertyId, value);
            UpdateDissolveVisibility(value);
            yield return null;
        }
        transitionMaterial.SetFloat(dissolvePropertyId, to);
        UpdateDissolveVisibility(to);
    }

    private void CleanupTransitionMaterial()
    {
        if (transitionMaterial == null)
            return;

        Destroy(transitionMaterial);
        transitionMaterial = null;
        dissolvePropertyId = -1;
    }

    private void CacheDissolveVisibilityTargets(string transitionScene)
    {
        dissolveVisibilityTargets.Clear();

        if (string.IsNullOrWhiteSpace(transitionScene))
            return;

        var targets = Object.FindObjectsOfType<TransitionDissolveVisibilityTarget>(true);
        for (int i = 0; i < targets.Length; i++)
        {
            var marker = targets[i];
            if (marker == null)
                continue;

            var markerScene = marker.gameObject.scene;
            if (!markerScene.IsValid() || !markerScene.isLoaded || markerScene.name != transitionScene)
                continue;

            var target = marker.Target != null ? marker.Target : marker.gameObject;
            if (target == null || dissolveVisibilityTargets.Contains(target))
                continue;

            dissolveVisibilityTargets.Add(target);
        }
    }

    private void ClearDissolveVisibilityTargets()
    {
        dissolveVisibilityTargets.Clear();
        currentDissolveValue = -1f;
    }

    private void UpdateDissolveVisibility(float value)
    {
        currentDissolveValue = value;
        bool shouldShow = value >= 0.999f;

        for (int i = dissolveVisibilityTargets.Count - 1; i >= 0; i--)
        {
            var target = dissolveVisibilityTargets[i];
            if (target == null)
            {
                dissolveVisibilityTargets.RemoveAt(i);
                continue;
            }

            if (target.activeSelf != shouldShow)
                target.SetActive(shouldShow);
        }
    }
}
