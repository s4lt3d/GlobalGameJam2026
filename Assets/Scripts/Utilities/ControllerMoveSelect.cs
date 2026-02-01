using Core;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerMoveSelect : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    [SerializeField] private GridController gridController;
    [SerializeField] private string objectTag = "MasqueradeThing";
    [SerializeField] private float stickDeadzone = 0.5f;
    [SerializeField] private float overlapRadius = 0.4f;

    private EventManager eventManager;
    private InputSystem_Actions actions;

    private void Awake()
    {
        eventManager = Services.Get<EventManager>();
    }

    private void OnEnable()
    {
        actions ??= new InputSystem_Actions();
        // actions.asset.bindingMask = InputBinding.MaskByGroup("Gamepad");
        // var groupMaskBindings = ["Gamepad", "Mouse&Keyboard"];
        string[] things = { "Gamepad", "Keyboard&Mouse" };
        actions.asset.bindingMask = InputBinding.MaskByGroups(things);
        actions.Player.AddCallbacks(this);
        actions.Player.Enable();
    }

    private void OnDisable()
    {
        if (actions == null)
            return;

        actions.Player.RemoveCallbacks(this);
        actions.Player.Disable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        Vector2 moveInput = context.ReadValue<Vector2>();
        if (moveInput.magnitude < stickDeadzone)
            return;

        Vector3 direction = GetCardinalDirection(moveInput);
        if (direction == Vector3.zero)
            return;

        Vector3 currentWorldPosition = transform.position;
        float stepDistance = GetGridSpacing();
        Vector3 targetCenter = currentWorldPosition + direction * stepDistance;

        if (TrySelectTaggedObject(targetCenter))
            return;

        TrySelectGrid(targetCenter);
    }

    private float GetGridSpacing()
    {
        if (gridController == null)
            return 1f;

        Vector3 origin = gridController.GetWorldCenter(Vector2Int.zero);
        Vector3 next = gridController.GetWorldCenter(new Vector2Int(1, 0));
        float spacing = Vector3.Distance(origin, next);
        return spacing <= 0.0001f ? 1f : spacing;
    }

    private Vector3 GetCardinalDirection(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return input.x > 0f ? Vector3.right : Vector3.left;

        return input.y > 0f ? Vector3.forward : Vector3.back;
    }

    private bool TrySelectTaggedObject(Vector3 center)
    {
        if (string.IsNullOrEmpty(objectTag))
            return false;

        Collider[] hits = Physics.OverlapSphere(center, overlapRadius);
        if (hits == null || hits.Length == 0)
            return false;

        Transform closest = null;
        float bestDistance = float.PositiveInfinity;

        foreach (var hit in hits)
        {
            if (hit == null || !hit.CompareTag(objectTag))
                continue;

            float distance = Vector3.Distance(center, hit.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = hit.transform;
            }
        }

        if (closest == null)
            return false;

        transform.position = closest.position;
        eventManager.gameObjectSelected?.Invoke(closest);
        return true;
    }

    private void TrySelectGrid(Vector3 targetCenter)
    {
        if (gridController == null)
            return;

        if (!gridController.TryGetGridPositionFromWorld(targetCenter, out var gridPosition, true))
            return;

        Vector3 gridWorld = gridController.GetWorldCenter(gridPosition);
        transform.position = gridWorld;
        eventManager.GridSelected?.Invoke(gridPosition);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
    }

    public void OnJump(InputAction.CallbackContext context)
    {
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
    }

    public void OnNext(InputAction.CallbackContext context)
    {
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
    }
}
