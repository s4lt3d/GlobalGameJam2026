using UnityEngine;

public class RandomizeAnimatorTimeScale : MonoBehaviour
{
    [SerializeField]
    private float percentage = 0.01f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var animator =  GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = Random.Range(1f - percentage, 1f + percentage);
        }
    }
}
