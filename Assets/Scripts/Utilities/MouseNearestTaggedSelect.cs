using Core;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Utilities
{
    public class MouseNearestTaggedSelect : MonoBehaviour
    {
        [SerializeField] private string objectTag = "Untagged";
        [SerializeField] private string groundTag = "Ground";
        [SerializeField] private float maxRayDistance = 500f;
        [SerializeField] private float maxDistanceToHit = 3f;
        
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
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
                return;

            if (hit.collider == null || !hit.collider.CompareTag(groundTag))
                return;

            float bestDistance = float.PositiveInfinity;
            Transform best = null;

            var candidates = GameObject.FindGameObjectsWithTag(objectTag);
            foreach (var go in candidates)
            {
                if (go == null)
                    continue;

                float distance = Vector3.Distance(go.transform.position, hit.point);
                if (distance > maxDistanceToHit || distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = go.transform;
            }

            if (best != null)
                eventManager.gameObjectSelected?.Invoke(best);
        }
    }
}
