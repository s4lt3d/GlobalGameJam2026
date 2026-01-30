using System;
using Animators;
using Core;
using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;


namespace Player
{
    [RequireComponent(typeof(SimpleBoolAnimator))]
    [RequireComponent(typeof(Rigidbody))]
    public class BaseCharacterController : MonoBehaviour
    {
        [SerializeField]
        protected Transform cameraTransform;

        [SerializeField]
        protected float speed = 5f;

        [SerializeField]
        protected float rotationSpeed = 8f;

        [SerializeField]
        protected float dashSpeed = 10f;

        [SerializeField]
        protected float dashDuration = 0.2f;

        [SerializeField]
        protected float dashCooldown = 0.6f;

        [SerializeField]
        protected float gravity = 25f;

        [SerializeField]
        protected float terminalVelocity = 40f;

        [SerializeField]
        protected float groundedOffset = 0.1f;

        [SerializeField]
        protected float groundedRadius = 0.35f;

        [SerializeField]
        protected LayerMask groundLayers = ~0;

        [SerializeField]
        protected int playerId = 0;

        [SerializeField]
        protected List<ParticleSystem> dashParticles = new List<ParticleSystem>();

        [SerializeField]
        protected int emmiterSpeedWhenDashing = 20;

        protected GameInputManager gameInput;
        protected int player;

        protected SimpleBoolAnimator anim;
        protected Rigidbody rb;

        private bool dashBusy;
        private float dashStartTime;
        private bool isDashing;
        private bool isGrounded;
        private Vector3 prevMoveDir = Vector3.forward;
        private Quaternion lookDirection;
        private float verticalSpeed;
        private Vector3 moveIntent;

        protected virtual void Awake()
        {
            anim = GetComponent<SimpleBoolAnimator>();
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            gameInput = Services.Get<GameInputManager>();
            dashStartTime = Time.time - dashCooldown - 0.01f;
        }

        private void Start()
        {
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
        }

        protected virtual void Update()
        {
            UpdateMovementInput();
            UpdateDashing();
            UpdateParticleEmission();
        }

        private void UpdateParticleEmission()
        {
            if (dashParticles == null || dashParticles.Count == 0)
                return;

            foreach (var particle in dashParticles)
            {
                if (particle == null)
                    continue;

                var psEmission = particle.emission;
                var rateOverTime = psEmission.rateOverTime;
                rateOverTime.constant = isDashing ? emmiterSpeedWhenDashing : 0;
                psEmission.rateOverTime = rateOverTime;
                psEmission.enabled = isDashing || rateOverTime.constant > 0;
            }
        }

        protected virtual void FixedUpdate()
        {
            ApplyMovement();
        }

        protected virtual void UpdateMovementInput()
        {
            if (gameInput == null || player < 0)
            {
                moveIntent = Vector3.zero;
                return;
            }

            Vector2 input = gameInput.GetMovement(player);
            if (input.sqrMagnitude < 0.0001f)
            {
                anim.AnimateBoolMagnitude(Vector3.zero);

                moveIntent = Vector3.zero;
                return;
            }

    
            var camInput = Maths.GetCameraRelativeXZ(input, cameraTransform);
            moveIntent = new Vector3(camInput.x, 0f, camInput.y);
            anim.AnimateBoolMagnitude(moveIntent);
        }

        protected virtual void ApplyMovement()
        {
            if (rb == null)
                return;

            ApplyGravity();

            Vector3 move = moveIntent;
            if (move.magnitude > 0.1f)
                prevMoveDir = move;

            if (move.sqrMagnitude > 1f)
                move.Normalize();

            var targetSpeed = speed;
            if (isDashing)
            {
                targetSpeed = Mathf.Lerp(dashSpeed, targetSpeed, (Time.time - dashStartTime) / dashDuration);
                if (move.magnitude <= 0.1f)
                    move = prevMoveDir;
                move.Normalize();
            }

            var targetVelocity = move * targetSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, verticalSpeed, targetVelocity.z);

            if (move.sqrMagnitude > 0.0001f)
            {
                var targetRotation = Quaternion.LookRotation(move, Vector3.up);
                lookDirection = Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    rotationSpeed * TimeManager.fixedDeltaTime);
                rb.MoveRotation(lookDirection);
            }
        }

        protected virtual void UpdateDashing()
        {
            if (gameInput == null || player < 0)
                return;

            if (gameInput.GetDashing(player))
                StartCoroutine(DashCoroutine());
        }

        protected virtual IEnumerator DashCoroutine()
        {
            if (dashBusy)
                yield break;

            dashStartTime = Time.time;
            dashBusy = true;
            isDashing = true;

            yield return new WaitForSeconds(dashDuration);
            isDashing = false;

            yield return new WaitForSeconds(dashCooldown);
            dashBusy = false;
        }

        protected virtual void ApplyGravity()
        {
            isGrounded = CheckGrounded();
            if (isGrounded && verticalSpeed < 0)
            {
                verticalSpeed = -2f;
            }
            else
            {
                verticalSpeed -= gravity * TimeManager.fixedDeltaTime;
                verticalSpeed = Mathf.Clamp(verticalSpeed, -terminalVelocity, float.MaxValue);
            }
        }

        protected virtual bool CheckGrounded()
        {
            var spherePos = transform.position + Vector3.up * groundedOffset;
            return Physics.CheckSphere(
                spherePos,
                groundedRadius,
                groundLayers,
                QueryTriggerInteraction.Ignore);
        }
    }
}