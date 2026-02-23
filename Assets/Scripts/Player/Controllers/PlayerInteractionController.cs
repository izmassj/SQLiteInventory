using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private PlayerMovementController _movement;
    [SerializeField] private InputActionAsset _playerInputAction;

    [SerializeField] private float _interactDistance = 1f;
    [SerializeField] private LayerMask _npcLayer;

    private InputAction _interactAction;

    private void Awake()
    {
        _playerInputAction.Enable();
        _interactAction = _playerInputAction.FindActionMap("Action").FindAction("East");
    }

    private void Update()
    {
        if (_movement.GetCurrentPlayerState() != PlayerState.Idle)
            return;

        if (_interactAction.triggered)
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Vector2 facing = _movement.GetFacingDirection();
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        Vector3 direction = new Vector3(facing.x, 0, facing.y);

        Debug.DrawLine(origin, origin + direction);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, _interactDistance, _npcLayer))
        {
            NPC npc = hit.collider.GetComponent<NPC>();
            if (npc != null)
            {
                npc.TryInteract(_movement);
            }
        }
    }
}