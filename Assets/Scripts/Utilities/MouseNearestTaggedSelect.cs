using Core;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Utilities
{
    public class MouseNearestTaggedSelect : MonoBehaviour
    {
        [SerializeField] private string objectTag = "Untagged";
        [SerializeField] private float maxDistance = 500f;
        [SerializeField] private float maxScreenDistance = 1f;

        [SerializeField] private LayerMask raycastMask;
        
        private EventManager eventManager;

        private void Awake()
        {
            eventManager = Services.Get<EventManager>();
        }

        private void Update()
        {
            if (Mouse.current == null)
                return;

            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hits = Physics.RaycastAll(ray, maxDistance);

            float bestDistance = float.PositiveInfinity;
            Transform best = null;

            foreach (var hit in hits)
            {
                if (hit.collider == null || !hit.collider.CompareTag(objectTag))
                    continue;

                var colliders = Physics.OverlapSphere(hit.point, maxScreenDistance, raycastMask);

                
                foreach (var col in colliders)
                {
                    // distance     
                }
                
                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    best = hit.collider.transform;
                }
            }

            if (best != null)
                eventManager.gameObjectSelected?.Invoke(best);
        }
    }
}
