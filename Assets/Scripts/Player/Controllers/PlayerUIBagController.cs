using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerUIBagController : MonoBehaviour
{
    private const float TOP_SELECTION_Y = 48f;

    private const float BOTTOM_SELECTION_Y = -48f;

    private const float CONTENT_SCROLL_STEP = 16f;

    private const float CONTENT_SCROLL_DOWN_SIGN = 1f;

    private const int ACTION_COUNT = 2;

    [Header("Input")]
    [SerializeField] private InputActionAsset _playerInputAction;

    [Header("Refs")]
    [SerializeField] private PlayerMovementController _playerMovementController;

    [Header("Inventory UI")]
    [SerializeField] private InventoryUIController _inventoryUIController;
    [SerializeField] private RectTransform _selectionBoxTransform;
    [SerializeField] private RectTransform _contentTransform;

    [Header("Bag UI GameObjects")]
    [SerializeField] private GameObject _bag;
    [SerializeField] private Image _currentItemSpriteUI;
    [SerializeField] private TMP_Text _currentItemDescription;
    [SerializeField] private CanvasGroup _fadeBottomScreen;
    [SerializeField] private List<GameObject> _bagTypeItemsUI;

    [Header("UI Parameters")]
    [SerializeField] private int _itemTypeSelectionBoxMovement = 16;

    [Header("Action Menu")]
    [SerializeField] private GameObject _actionMenuRoot;
    [SerializeField] private RectTransform _actionSelectionBox;
    [SerializeField] private int _actionSelectionStep;

    private ItemType _currentBagItemType = ItemType.Object;

    private int _cursorRow = 0;
    private int _scrollRow = 0;

    private InventorySlot _currentSelectedSlot;

    private GameObject _currentBagTypeItem;

    private InputAction _udlrAction;
    private InputAction _acceptAction;
    private InputAction _cancelAction;

    private Vector2 _contentBaseAnchoredPos;
    private Vector2 _actionSelectionBoxOriginalPos;

    private BagState _bagState = BagState.Browsing;

    private int _actionIndex = 0;



    private void Awake()
    {
        _udlrAction = _playerInputAction.FindActionMap("Action", true).FindAction("Navigate");
        _acceptAction = _playerInputAction.FindActionMap("Action", true).FindAction("East");
        _cancelAction = _playerInputAction.FindActionMap("Action", true).FindAction("South");
    }

    private void OnEnable()
    {
        if (_udlrAction != null) _udlrAction.started += NavigateBag;
        if (_acceptAction != null) _acceptAction.started += AcceptSelection;
        if (_cancelAction != null) _cancelAction.started += CancelBag;
    }

    private void OnDisable()
    {
        if (_udlrAction != null) _udlrAction.started -= NavigateBag;
        if (_acceptAction != null) _acceptAction.started -= AcceptSelection;
        if (_cancelAction != null) _cancelAction.started -= CancelBag;
    }

    private void Start()
    {
        if (_bagTypeItemsUI.Count != 0)
        {
            _currentBagTypeItem = _bagTypeItemsUI[0];
        }

        if (_contentTransform == null && _inventoryUIController != null)
        {
            _contentTransform = _inventoryUIController.contentParent as RectTransform;
        }

        if (_contentTransform != null)
        {
            _contentBaseAnchoredPos = _contentTransform.anchoredPosition3D;
        }

        if (_actionSelectionBox != null)
        {
            _actionSelectionBoxOriginalPos = _actionSelectionBox.anchoredPosition3D;
        }
    }

    public void OpenBag()
    {
        _playerMovementController.SetCurrentPlayerState(PlayerState.Bag);
        Debug.Log(_playerMovementController.GetCurrentPlayerState());

        RefreshPocketUI(resetCursorAndScroll: true);

        _fadeBottomScreen.DOFade(1f, 1).OnComplete(() =>
        {
            _bag.SetActive(true);
            _fadeBottomScreen.gameObject.GetComponent<RectTransform>()
                .DOMoveY(-_fadeBottomScreen.gameObject.GetComponent<RectTransform>().localScale.y, 0.5f);
        });

        UpdateSelectedItemUI();
    }

    private void AcceptSelection(InputAction.CallbackContext context)
    {
        if (_playerMovementController.GetCurrentPlayerState() != PlayerState.Bag)
            return;

        if (_bagState == BagState.Browsing)
        {
            if (_currentSelectedSlot == null || _currentSelectedSlot.item == null)
                return;

            OpenActionMenu();
        }
        else if (_bagState == BagState.ActionMenu)
        {
            ExecuteAction();
        }
    }

    private void CancelBag(InputAction.CallbackContext context)
    {
        if (_playerMovementController.GetCurrentPlayerState() != PlayerState.Bag)
            return;

        if (_bagState == BagState.ActionMenu)
        {
            CloseActionMenu();
            return;
        }

        CloseBag();
    }

    private void NavigateBag(InputAction.CallbackContext context)
    {
        if (_playerMovementController.GetCurrentPlayerState() != PlayerState.Bag)
            return;

        Vector2 nav = _udlrAction.ReadValue<Vector2>();

        if (_bagState == BagState.ActionMenu)
        {
            HandleActionMenuNavigation(nav);
            return;
        }

        if (Mathf.Abs(nav.x) > Mathf.Abs(nav.y))
        {
            if (nav.x > 0f) ChangePocket(next: true);
            else if (nav.x < 0f) ChangePocket(next: false);

            return;
        }

        if (nav.y < 0f) MoveSelection(down: true);
        else if (nav.y > 0f) MoveSelection(down: false);
    }

    private void CloseBag()
    {
        _bagState = BagState.Browsing;

        if (_actionMenuRoot != null)
            _actionMenuRoot.SetActive(false);

        ResetCursorAndScroll();

        _fadeBottomScreen.alpha = 0.0f;

        _fadeBottomScreen.gameObject.GetComponent<RectTransform>().anchoredPosition3D = Vector2.zero;

        _bag.SetActive(false);

        _playerMovementController.SetCurrentPlayerState(PlayerState.Idle);
    }

    private void OpenActionMenu()
    {
        _bagState = BagState.ActionMenu;
        _actionIndex = 0;

        if (_actionMenuRoot != null)
            _actionMenuRoot.SetActive(true);

        UpdateActionSelectionVisual();
    }

    private void CloseActionMenu()
    {
        _bagState = BagState.Browsing;

        if (_actionMenuRoot != null)
            _actionMenuRoot.SetActive(false);
    }

    private void HandleActionMenuNavigation(Vector2 nav)
    {
        if (nav.y < 0f)
        {
            _actionIndex++;
            if (_actionIndex >= ACTION_COUNT)
                _actionIndex = 0;

            UpdateActionSelectionVisual();
        }
        else if (nav.y > 0f)
        {
            _actionIndex--;
            if (_actionIndex < 0)
                _actionIndex = ACTION_COUNT - 1;

            UpdateActionSelectionVisual();
        }
    }

    private void UpdateActionSelectionVisual()
    {
        if (_actionSelectionBox == null)
            return;

        Vector2 pos = _actionSelectionBox.anchoredPosition3D;
        pos.y = -_actionIndex * _actionSelectionStep ;
        _actionSelectionBox.anchoredPosition3D = new Vector2(_actionSelectionBoxOriginalPos.x, pos.y + _actionSelectionBoxOriginalPos.y);
    }

    private void ExecuteAction()
    {
        switch (_actionIndex)
        {
            case 0:
                ThrowItem();
                break;

            case 1:
                CloseActionMenu();
                break;
        }
    }

    private void ThrowItem()
    {
        if (_currentSelectedSlot == null)
            return;

        Inventory.Instance.RemoveItem(_currentSelectedSlot.item, 1);

        CloseActionMenu();

        RefreshPocketUI(resetCursorAndScroll: false);
    }

    private void ChangePocket(bool next)
    {
        ItemType[] values = (ItemType[])Enum.GetValues(typeof(ItemType));
        int index = Array.IndexOf(values, _currentBagItemType);

        index = next
            ? (index + 1) % values.Length
            : (index - 1 + values.Length) % values.Length;

        _currentBagItemType = values[index];

        if (_bagTypeItemsUI != null && _bagTypeItemsUI.Count > index)
            _currentBagTypeItem = _bagTypeItemsUI[index];

        RefreshPocketUI(resetCursorAndScroll: true);
    }

    private void RefreshPocketUI(bool resetCursorAndScroll)
    {
        if (_inventoryUIController != null)
            _inventoryUIController.OpenPocket(_currentBagItemType);

        if (resetCursorAndScroll)
            ResetCursorAndScroll();

        UpdateSelectedSlotReference();
        UpdateSelectedItemUI();
    }

    private void ResetCursorAndScroll()
    {
        _cursorRow = 0;
        _scrollRow = 0;

        if (_selectionBoxTransform != null)
        {
            Vector2 pos = _selectionBoxTransform.anchoredPosition;
            pos.y = TOP_SELECTION_Y;
            _selectionBoxTransform.anchoredPosition = pos;
        }

        if (_contentTransform != null)
            _contentTransform.anchoredPosition = _contentBaseAnchoredPos;
    }

    private void MoveSelection(bool down)
    {
        List<InventorySlot> pocket = Inventory.Instance.GetPocket(_currentBagItemType);
        int itemCount = (pocket != null) ? pocket.Count : 0;

        if (itemCount <= 0)
        {
            _currentSelectedSlot = null;
            UpdateSelectedItemUI();
            return;
        }

        int visibleRows = GetVisibleRows();
        int maxScroll = Mathf.Max(0, itemCount - visibleRows);

        int selectedIndex = GetSelectedIndex();

        if (down)
        {
            if (selectedIndex >= itemCount - 1)
            {
                UpdateSelectedSlotReference();
                UpdateSelectedItemUI();
                return;
            }

            if (_cursorRow < visibleRows - 1 && _cursorRow < itemCount - 1 - _scrollRow)
            {
                _cursorRow++;
                MoveSelectionBoxBy(GetStepDownY());
            }
            else
            {
                if (_scrollRow < maxScroll)
                {
                    _scrollRow++;
                    ApplyContentScroll();
                }
                else
                {
                    UpdateSelectedSlotReference();
                    UpdateSelectedItemUI();
                    return;
                }
            }
        }
        else // up
        {
            if (selectedIndex <= 0)
            {
                UpdateSelectedSlotReference();
                UpdateSelectedItemUI();
                return;
            }

            if (_cursorRow > 0)
            {
                _cursorRow--;
                MoveSelectionBoxBy(-GetStepDownY());
            }
            else
            {
                if (_scrollRow > 0)
                {
                    _scrollRow--;
                    ApplyContentScroll();
                }
                else
                {
                    UpdateSelectedSlotReference();
                    UpdateSelectedItemUI();
                    return;
                }
            }
        }

        UpdateSelectedSlotReference();
        UpdateSelectedItemUI();
    }

    private void UpdateSelectedItemUI()
    {
        if (_currentItemSpriteUI == null || _currentItemDescription == null)
            return;

        if (_currentSelectedSlot == null || _currentSelectedSlot.item == null)
        {
            _currentItemSpriteUI.sprite = null;
            _currentItemDescription.text = "";
            return;
        }

        _currentItemSpriteUI.sprite = _currentSelectedSlot.item.sprite;

        if (_currentItemSpriteUI.sprite != null)
            _currentItemSpriteUI.rectTransform.sizeDelta = _currentItemSpriteUI.sprite.rect.size;

        _currentItemDescription.text = _currentSelectedSlot.item.description;
    }

    private void MoveSelectionBoxBy(float deltaY)
    {
        if (_selectionBoxTransform == null)
            return;

        Vector2 pos = _selectionBoxTransform.anchoredPosition3D;
        pos.y += deltaY;

        float minY = Mathf.Min(TOP_SELECTION_Y, BOTTOM_SELECTION_Y);
        float maxY = Mathf.Max(TOP_SELECTION_Y, BOTTOM_SELECTION_Y);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        _selectionBoxTransform.anchoredPosition3D = pos;
    }

    private void ApplyContentScroll()
    {
        if (_contentTransform == null)
            return;

        float deltaY = _scrollRow * CONTENT_SCROLL_STEP * CONTENT_SCROLL_DOWN_SIGN;
        _contentTransform.anchoredPosition3D = _contentBaseAnchoredPos + new Vector2(0f, deltaY);
    }

    private int GetVisibleRows()
    {
        float move = Mathf.Max(1f, Mathf.Abs(_itemTypeSelectionBoxMovement));
        float range = Mathf.Abs(BOTTOM_SELECTION_Y - TOP_SELECTION_Y);
        return Mathf.FloorToInt(range / move) + 1;
    }

    private float GetStepDownY()
    {
        float move = Mathf.Abs(_itemTypeSelectionBoxMovement);
        float sign = Mathf.Sign(BOTTOM_SELECTION_Y - TOP_SELECTION_Y);
        return move * sign;
    }

    private int GetSelectedIndex()
    {
        return Mathf.Max(0, _scrollRow + _cursorRow);
    }

    private void UpdateSelectedSlotReference()
    {
        List<InventorySlot> pocket = Inventory.Instance.GetPocket(_currentBagItemType);
        if (pocket == null || pocket.Count == 0)
        {
            _currentSelectedSlot = null;
            return;
        }

        int index = Mathf.Clamp(GetSelectedIndex(), 0, pocket.Count - 1);
        _currentSelectedSlot = pocket[index];
    }
}