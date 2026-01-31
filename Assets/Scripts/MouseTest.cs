using UnityEngine;
using UnityEngine.InputSystem;

public class MouseTest : MonoBehaviour
{
    [SerializeField] private string groundTag = "Ground";

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        var cam =  Camera.main;
        
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider != null && hit.collider.CompareTag(groundTag))
            {
                Debug.Log($"Mouse hit {groundTag} at {hit.point}");
            }
        }
    }
}
