using UnityEngine;

public class NurseJoyController : MonoBehaviour
{
    [SerializeField] private PlayerMovementController _playerMovementController;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private DialogueData _nurseJoyDialogue;

    void Start()
    {
        _nurseJoyDialogue.onYes.AddListener(() => _inventory.FillWithAllItems());
        _nurseJoyDialogue.onNo.AddListener(ReturnToNormal);
    }

    private void ReturnToNormal()
    {
        _playerMovementController.SetCurrentPlayerState(PlayerState.Idle);
    }
}
