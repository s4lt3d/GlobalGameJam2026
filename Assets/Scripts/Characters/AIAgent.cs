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
    
    public TotemType Totem => totemType;

    private UnityEngine.AI.NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    private void Update()
    {
        distanceToDestination = Vector3.Distance(transform.position, destination);
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
}

public enum TotemType
{
    tree, 
    tent
}
