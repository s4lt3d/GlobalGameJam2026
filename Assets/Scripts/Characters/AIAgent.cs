using UnityEngine;
[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class AIAgent : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private Vector3 destination;
    [SerializeField] private float distanceToDestination;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0.1f, 0.8f, 1f, 1f);
    [SerializeField] private float gizmoRadius = 0.15f;

    [SerializeField]
    private TotemType totemType = TotemType.tent;

    public int TotemColor = 0;
    
    public TotemType Totem => totemType;

    [Header("VFX")]
    [SerializeField] private ParticleSystem velocityParticles;
    [SerializeField] private float minVelocityForParticles = 0.1f;
    [SerializeField] private float maxVelocityForParticles = 6f;
    [SerializeField] private float maxRateOverTime = 20f;

    private UnityEngine.AI.NavMeshAgent agent;
    private ParticleSystem.EmissionModule emissionModule;

    private void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (velocityParticles != null)
            emissionModule = velocityParticles.emission;
    }

    private void Update()
    {
        distanceToDestination = Vector3.Distance(transform.position, destination);
        UpdateVelocityParticles();
    }

    public bool SetDestination(Vector3 target)
    {
        if (agent == null)
            return false;

        destination = target;
        return agent.SetDestination(destination);
    }

    public void Stop()
    {
        if (agent == null)
            return;

        agent.isStopped = true;
    }

    public void Resume()
    {
        if (agent == null)
            return;

        agent.isStopped = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(destination, gizmoRadius);
        Gizmos.DrawLine(transform.position, destination);
    }

    private void UpdateVelocityParticles()
    {
        if (velocityParticles == null)
            return;

        float speed = agent != null ? agent.velocity.magnitude : 0f;
        float rate = 0f;

        if (speed >= minVelocityForParticles)
        {
            float t = Mathf.InverseLerp(minVelocityForParticles, maxVelocityForParticles, speed);
            rate = Mathf.Lerp(0f, maxRateOverTime, t);
        }

        var rateOverTime = emissionModule.rateOverTime;
        rateOverTime.constant = rate;
        emissionModule.rateOverTime = rateOverTime;
        emissionModule.enabled = rate > 0f;
    }
}

public enum TotemType
{
    tree, 
    tent
}
