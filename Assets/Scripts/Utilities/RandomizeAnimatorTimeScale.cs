using UnityEngine;

public class RandomizeAnimatorTimeScale : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var animator =  GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = Random.Range(0.9f, 1.1f);
        }
    }
}
