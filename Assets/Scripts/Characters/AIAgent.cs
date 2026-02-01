using System.Collections;
using Core;
using Managers;
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

    [Header("Win Dance")]
    [SerializeField] private bool danceOnWin = true;
    [SerializeField] private float danceJumpHeight = 0.75f;
    [SerializeField] private float danceJumpDuration = 0.4f;
    [SerializeField] private float danceRotateDuration = 1.2f;
    [SerializeField] private int danceTurnsPerDirection = 3;
    [SerializeField] private float randomJumpDuration = 2f;
    [SerializeField] private float randomJumpMinInterval = 0.1f;
    [SerializeField] private float randomJumpMaxInterval = 0.4f;
    [SerializeField] private float randomJumpMinHeight = 0.3f;
    [SerializeField] private float randomJumpMaxHeight = 0.9f;
    [SerializeField] private float randomJumpMinDuration = 0.2f;
    [SerializeField] private float randomJumpMaxDuration = 0.4f;

    [Header("Destination")]
    [SerializeField] private float destinationReachedDistance = 0.1f;

    [Header("Idle Facing")]
    [SerializeField] private float faceCameraDistance = 0.1f;
    [SerializeField] private float faceCameraRotationSpeed = 8f;

    private UnityEngine.AI.NavMeshAgent agent;
    private ParticleSystem.EmissionModule emissionModule;
    private EventManager eventManager;
    private bool hasDanced;
    private bool hasDestination;
    private bool destinationReachedFired;
    private Coroutine randomJumpRoutine;

    private void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (velocityParticles != null)
            emissionModule = velocityParticles.emission;
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

    private void Update()
    {
        if (hasDestination)
            distanceToDestination = Vector3.Distance(transform.position, destination);
        UpdateVelocityParticles();
        FaceCameraWhenAtDestination();
        CheckDestinationReached();
    }

    public bool SetDestination(Vector3 target)
    {
        if (agent == null)
            return false;

        destination = target;
        hasDestination = true;
        destinationReachedFired = false;
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

    private void FaceCameraWhenAtDestination()
    {
        if (!hasDestination)
            return;

        if (distanceToDestination > faceCameraDistance)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        Vector3 toCamera = cam.transform.position - transform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(toCamera, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            faceCameraRotationSpeed * Time.deltaTime);
    }

    private void HandleLevelWin()
    {
        if (!danceOnWin || hasDanced)
            return;

        hasDanced = true;

        if (agent != null)
            agent.isStopped = true;

        float baseY = transform.position.y;
        float turnAngle = 360f * Mathf.Max(1, danceTurnsPerDirection);
        float turnDuration = danceRotateDuration * Mathf.Max(1, danceTurnsPerDirection);

        LeanTween.rotateAround(gameObject, Vector3.up, turnAngle, turnDuration).setOnComplete(() =>
        {
            LeanTween.rotateAround(gameObject, Vector3.up, -turnAngle, turnDuration).setOnComplete(() =>
            {
                LeanTween.moveY(gameObject, baseY + danceJumpHeight, danceJumpDuration)
                    .setLoopPingPong(1)
                    .setOnComplete(() =>
                    {
                        if (randomJumpRoutine != null)
                            StopCoroutine(randomJumpRoutine);
                        randomJumpRoutine = StartCoroutine(RandomJumpSequence(baseY));
                    });
            });
        });
    }

    public bool IsAtDestination(float threshold)
    {
        if (!hasDestination)
            return true;

        return distanceToDestination <= threshold;
    }

    public bool HasDestination()
    {
        return hasDestination;
    }

    public bool HasReachedDestination()
    {
        return hasDestination && distanceToDestination <= destinationReachedDistance;
    }

    private void CheckDestinationReached()
    {
        if (!hasDestination || destinationReachedFired)
            return;

        if (distanceToDestination > destinationReachedDistance)
            return;

        destinationReachedFired = true;
        eventManager?.AgentReachedDestination?.Invoke(this);
    }

    private IEnumerator RandomJumpSequence(float baseY)
    {
        float elapsed = 0f;
        while (elapsed < randomJumpDuration)
        {
            float duration = Random.Range(randomJumpMinDuration, randomJumpMaxDuration);
            float height = Random.Range(randomJumpMinHeight, randomJumpMaxHeight);
            float interval = Random.Range(randomJumpMinInterval, randomJumpMaxInterval);

            LeanTween.moveY(gameObject, baseY + height, duration).setLoopPingPong(1);

            float wait = duration * 2f + interval;
            elapsed += wait;
            yield return new WaitForSeconds(wait);
        }
    }
}

public enum TotemType
{
    tree, 
    tent
}
