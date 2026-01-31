using Puzzles;
using UnityEngine;
using UnityEngine.InputSystem;

public class FollowMouseWithSelector : MonoBehaviour
{

    [SerializeField]
    private string groundTag = "Ground";

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
        var hits = Physics.RaycastAll(ray, 500f, rayLayerMask);
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

                if (!gridController.TryGetGridPositionFromWorld(hit.point, out var gridLocation))
                {
                    SetSelectionState(false, false);
                    break;
                }

                bool isValid = generator.IsValidMovePosition(gridLocation);
                SetSelectionState(isValid, !isValid);
                moveToGridPosition(gridLocation);
                break;
            }
        }

        // if (!handled)
        // {
        //     SetSelectionState(false, false);
        // }
    }

    private void moveToGridPosition(Vector2Int gridLocation)
    {
        var position = gridController.GetWorldCenter(gridLocation);
        
        transform.position = new Vector3(position.x, transform.position.y, position.z);
    }

    private void SetSelectionState(bool showValid, bool showInvalid)
    {
        if (validSelection != null)
            validSelection.SetActive(showValid);
        if (invalidSelection != null)
            invalidSelection.SetActive(showInvalid);
    }
}
