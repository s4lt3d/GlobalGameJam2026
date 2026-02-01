using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneLoader: sceneName is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning("SceneLoader: scene name is empty.");
            return;
        }

        SceneManager.LoadScene(name);
    }
}
