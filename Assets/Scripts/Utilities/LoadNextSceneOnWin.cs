using System.Collections;
using Core;
using Managers;
using UnityEngine;

public class LoadNextSceneOnWin : MonoBehaviour
{
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private float delaySeconds = 1f;

    private EventManager eventManager;
    private Coroutine loadRoutine;
    private bool triggered;

    private void Awake()
    {
        if (sceneLoader == null)
            sceneLoader = GetComponent<SceneLoader>();
    }

    private void OnEnable()
    {
        eventManager = Services.Has<EventManager>() ? Services.Get<EventManager>() : null;
        if (eventManager != null)
            eventManager.LevelWin += HandleLevelWin;
    }

    private void OnDisable()
    {
        if (eventManager != null)
            eventManager.LevelWin -= HandleLevelWin;
    }

    private void HandleLevelWin()
    {
        if (triggered)
            return;

        triggered = true;

        if (loadRoutine != null)
            StopCoroutine(loadRoutine);
        loadRoutine = StartCoroutine(LoadAfterDelay());
    }

    private IEnumerator LoadAfterDelay()
    {
        if (delaySeconds > 0f)
            yield return new WaitForSecondsRealtime(delaySeconds);

        if (sceneLoader == null)
        {
            Debug.LogWarning("LoadNextSceneOnWin: missing SceneLoader reference.", this);
            yield break;
        }

        sceneLoader.LoadScene();
    }
}
