using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private bool useTransition = true;
    [SerializeField] private string transitionSceneName = "TransistionScene";
    [SerializeField] private float transitionHoldSeconds = 0f;
    [SerializeField] private float dissolveInSeconds = 0.5f;
    [SerializeField] private float dissolveOutSeconds = 0.5f;

    public void LoadScene()
    {
        LoadScene(sceneName);
    }

    public void LoadScene(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning("SceneLoader: scene name is empty.");
            return;
        }

        if (useTransition)
        {
            if (string.IsNullOrWhiteSpace(transitionSceneName))
            {
                Debug.LogWarning("SceneLoader: transitionSceneName is empty. Loading directly.");
                SceneManager.LoadScene(name);
                return;
            }

            SceneTransitioner.LoadScene(
                name,
                transitionSceneName,
                transitionHoldSeconds,
                dissolveInSeconds,
                dissolveOutSeconds
            );
            return;
        }

        SceneManager.LoadScene(name);
    }
}
