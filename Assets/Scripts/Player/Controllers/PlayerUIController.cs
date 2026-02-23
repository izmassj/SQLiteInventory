using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovementController _playerMovementController;
    [SerializeField] private PlayerUIMenuController _playerUIMenuController;

    [Header("Input")]
    [SerializeField] private InputActionAsset _playerInputAction;
    
    [Header("Menu UI GameObjects")] 
    [SerializeField] private GameObject _forebackMenu;

    private InputAction _openMenuAction;

    private void Awake()
    {
        _forebackMenu.SetActive(false);

        _playerInputAction.Enable();
        _openMenuAction = _playerInputAction.FindActionMap("Action", true).FindAction("North");
    }

    private void Start()
    {
        _openMenuAction.started += HandleMenuUI;
    }

    private void HandleMenuUI(InputAction.CallbackContext obj)
    {
        if ((_playerMovementController.GetCurrentPlayerState() != PlayerState.Menu) && (!_playerMovementController.GetIsMovingToGrid()) && (_playerMovementController.GetCurrentPlayerState() != PlayerState.Dialogue) && (_playerMovementController.GetCurrentPlayerState() == PlayerState.Idle)) 
        {
            OpenMenuUI();
        }
        else
        {
            CloseMenuUI();
        }
    }

    public void OpenMenuUI()
    {
        _forebackMenu.SetActive(true);
        FindAnyObjectByType<PlayerUIMenuController>().ResetCurrentIcon();
        FindAnyObjectByType<PlayerUIMenuController>()._itemUIAnimation = StartCoroutine(FindAnyObjectByType<PlayerUIMenuController>().SetCurrentIcon());
        _playerMovementController.SetCurrentPlayerState(PlayerState.Menu);
    }

    public void CloseMenuUI()
    {
        _forebackMenu.SetActive(false);
        if ((_playerMovementController.GetCurrentPlayerState() != PlayerState.Dialogue) && (_playerUIMenuController._currentState != MenuStateUI.Bag))
        {
            _playerMovementController.SetCurrentPlayerState(PlayerState.Idle);
        }
    }
}
