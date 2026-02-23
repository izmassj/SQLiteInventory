using UnityEngine;

public class RowanController : MonoBehaviour
{
    [SerializeField] private PlayerMovementController _playerMovementController;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private DialogueData _rowanDialogue;

    void Start()
    {
        _rowanDialogue.onYes.AddListener(() => _inventory.ClearInventory());
        _rowanDialogue.onNo.AddListener(ReturnToNormal);
    }

    private void ReturnToNormal()
    {
        _playerMovementController.SetCurrentPlayerState(PlayerState.Idle);
    }
}
