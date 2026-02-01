using Puzzles;
using UnityEngine;
using UnityEngine.InputSystem;

public class FollowMouseWithSelector : MonoBehaviour
{

    [SerializeField]
    private string groundTag = "Ground";

    [SerializeField]
    private string objectTag = "Totem";

    [SerializeField]
    private float maxRayDistance = 500f;

    [SerializeField]
    private float maxDistanceToTagged = 3f;

    [SerializeField]
    private GameObject validSelection;
    
    [SerializeField]
    private GameObject invalidSelection;

    [SerializeField]
    private TentsPuzzleGenerator generator;

    [SerializeField]
    private Core.GridController gridController;
    
    [SerializeField]
    private LayerMask rayLayerMask;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetSelectionState(false, false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current == null)
            return;
        
        var cam =  Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        var hits = Physics.RaycastAll(ray, maxRayDistance, rayLayerMask);
        bool handled = false;

        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.collider == null || !hit.collider.CompareTag(groundTag))
                    continue;

                handled = true;
                
                if (gridController == null || generator == null)
                {
                    SetSelectionState(false, false);
                    break;
                }

                var isInGrid = gridController.TryGetGridPositionFromWorld(hit.point, out var gridLocation);
                
                
                bool isValid = generator.IsValidMovePosition(gridLocation);

                if (isValid)
                {
                    SetSelectionState(isValid, !isValid);
                    moveToGridPosition(gridLocation);
                    break;
                }
                
                SetSelectionState(false, true);
                moveToGridPosition(gridLocation);
                // Debug.Log(gridLocation);
                
                // if (TryMoveToTagged(hit.point))
                // {
                //     SetSelectionState(true, false);
                //     break;
                // }
                
            }
        }

        // if (!handled)
        // {
        //     SetSelectionState(false, false);
        // }
    }

    private void moveToGridPosition(Vector2Int gridLocation)
    {
        var position = gridController.GetWorldPosition(gridLocation, true);
        
        transform.position = new Vector3(position.x, transform.position.y, position.z);
    }

    private bool TryMoveToTagged(Vector3 hitPoint)
    {
        if (string.IsNullOrEmpty(objectTag) || maxDistanceToTagged <= 0f)
            return false;

        Collider[] hits = Physics.OverlapSphere(hitPoint, maxDistanceToTagged);
        if (hits == null || hits.Length == 0)
            return false;

        Transform closest = null;
        float bestDistance = float.PositiveInfinity;

        foreach (var hit in hits)
        {
            if (hit == null || !hit.CompareTag(objectTag))
                continue;

            float distance = Vector3.Distance(hitPoint, hit.transform.position);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            closest = hit.transform;
        }

        if (closest == null)
            return false;

        transform.position = new Vector3(closest.position.x, transform.position.y, closest.position.z);
        return true;
    }

    private void SetSelectionState(bool showValid, bool showInvalid)
    {
        if (validSelection != null)
            validSelection.SetActive(showValid);
        if (invalidSelection != null)
            invalidSelection.SetActive(showInvalid);
    }
}
