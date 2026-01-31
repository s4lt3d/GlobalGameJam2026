using System;
using Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseTest : MonoBehaviour
{
    [SerializeField] private string groundTag = "Ground";

    [SerializeField]
    private GridController gridController;
    
    // Update is called once per frame
    void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        var cam =  Camera.main;
        
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        // Ray ray = Physics.RaycastAll(cam.transform.position)
        
        var hits = Physics.RaycastAll(ray, 500f);

        if (hits.Length > 0)
        {
            
            
            foreach (var hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag(groundTag))
                {
                    if (gridController == null)
                        break;
                    
                    var isInGrid = gridController.TryGetGridPositionFromWorld(hit.point,  out var gridLocation);

                    Debug.Log(isInGrid ? $"Mouse hit: {gridLocation}" : $"not in grid: {gridLocation}");
                }
            }
        }
        
        // if (Physics.RaycastAll(ray, out RaycastHit hit))
        // {
        //     if (hit.collider != null && hit.collider.CompareTag(groundTag))
        //     {
        //         Debug.Log($"Mouse hit {groundTag} at {hit.point}");
        //     }
        // }
    }
}
