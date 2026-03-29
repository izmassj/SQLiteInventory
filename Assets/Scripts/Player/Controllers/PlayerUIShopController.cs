using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerUIShopController : MonoBehaviour
{
    private const float TOP_SELECTION_Y = 48f;
    private const float BOTTOM_SELECTION_Y = -32f;
    private const float CONTENT_SCROLL_STEP = 16f;
    private const float CONTENT_SCROLL_DOWN_SIGN = 1f;

    private const int CHOICE_COUNT = 3;

    private enum ShopMode
    {
        Buy,
        Sell
    }

    [Header("Input")]
    [SerializeField] private InputActionAsset _playerInputAction;

    [Header("Refs")]
    [SerializeField] private PlayerMovementController _playerMovementController;
    [SerializeField] private InventoryDbController _inventoryDbController;

    [Header("Shop Root")]
    [SerializeField] private GameObject _shopRoot;

    [Header("Choice Menu")]
    [SerializeField] private GameObject _choiceRoot;
    [SerializeField] private RectTransform _choiceSelectionBox;
    [SerializeField] private int _choiceSelectionStep = 16;

    [Header("Main List UI")]
    [SerializeField] private GameObject _listRoot;
    [SerializeField] private Transform _contentParent;
    [SerializeField] private RectTransform _contentTransform;
    [SerializeField] private RectTransform _selectionBoxTransform;
    [SerializeField] private GameObject _slotPrefab;

    [Header("Owned Counter")]
    [SerializeField] private TMP_Text _ownedCountText;
    [SerializeField] private Image _currentItemSpriteUI;
    [SerializeField] private TMP_Text _currentItemDescription;
    [SerializeField] private TMP_Text _moneyText;

    [Header("Quantity UI")]
    [SerializeField] private GameObject _quantityRoot;
    [SerializeField] private TMP_Text _qtyAmountText;

    [Header("UI Parameters")]
    [SerializeField] private int _selectionBoxMovement = 16;

    [Header("Debug")]
    [SerializeField] private bool _simulateDbErrorOnNextTransaction;

    private InputAction _udlrAction;
    private InputAction _acceptAction;
    private InputAction _cancelAction;

    private Vector2 _contentBaseAnchoredPos;
    private Vector2 _choiceSelectionBoxAnchoredPos;

    private ShopState _state;
    private ShopMode _mode = ShopMode.Buy;

    private ShopData _currentShop;
    private List<Item> _items = new List<Item>();

    private int _cursorRow;
    private int _scrollRow;

    private int _choiceIndex;
    private Item _selectedItem;

    private int _buyAmount = 1;
    private string _statusMessage = string.Empty;

    private ShopTransactionService _shopTransactionService;

    private void Awake()
    {
        _udlrAction = _playerInputAction.FindActionMap("Action", true).FindAction("Navigate");
        _acceptAction = _playerInputAction.FindActionMap("Action", true).FindAction("East");
        _cancelAction = _playerInputAction.FindActionMap("Action", true).FindAction("South");

        if (_inventoryDbController == null)
            _inventoryDbController = FindAnyObjectByType<InventoryDbController>();

        if (DatabaseManager.Instance != null && DatabaseManager.Instance.IsInitialized)
            _shopTransactionService = new ShopTransactionService(DatabaseManager.Instance.Db);
    }

    private void Start()
    {
        if (_contentTransform == null && _contentParent != null)
            _contentTransform = _contentParent as RectTransform;

        if (_contentTransform != null)
            _contentBaseAnchoredPos = _contentTransform.anchoredPosition3D;

        _choiceSelectionBoxAnchoredPos = _choiceSelectionBox.anchoredPosition3D;

        EnsureChoiceOptionsUI();
        TryFindMoneyText();
        HideAll();
    }

    private void OnEnable()
    {
        if (_udlrAction != null) _udlrAction.started += OnNavigate;
        if (_acceptAction != null) _acceptAction.started += OnAccept;
        if (_cancelAction != null) _cancelAction.started += OnCancel;
    }

    private void OnDisable()
    {
        if (_udlrAction != null) _udlrAction.started -= OnNavigate;
        if (_acceptAction != null) _acceptAction.started -= OnAccept;
        if (_cancelAction != null) _cancelAction.started -= OnCancel;
    }

    public void OpenShop(ShopData shop)
    {
        if (shop == null) return;

        _currentShop = shop;
        _items = shop.items != null ? new List<Item>(shop.items) : new List<Item>();

        _playerMovementController.SetCurrentPlayerState(PlayerState.Shop);

        ResetAll();
        BuildList();

        if (_shopRoot != null) _shopRoot.SetActive(true);

        OpenChoice();
        UpdateMoneyUI();
        UpdateSelectedItemUI();
    }

    private IEnumerator CloseShop()
    {
        ResetAll();
        HideAll();

        if (_shopRoot != null) _shopRoot.SetActive(false);

        yield return new WaitForSeconds(0.1f);

        _playerMovementController.SetCurrentPlayerState(PlayerState.Idle);

        _currentShop = null;
        _items = new List<Item>();
    }

    private void ResetAll()
    {
        _state = ShopState.Choice;
        _mode = ShopMode.Buy;
        _choiceIndex = 0;
        _buyAmount = 1;
        _statusMessage = string.Empty;

        ResetCursorAndScroll();
        _selectedItem = null;

        if (_ownedCountText != null) _ownedCountText.text = "0";
        if (_qtyAmountText != null) _qtyAmountText.text = "1";
    }

    private void HideAll()
    {
        if (_choiceRoot != null) _choiceRoot.SetActive(false);
        if (_listRoot != null) _listRoot.SetActive(false);
        if (_quantityRoot != null) _quantityRoot.SetActive(false);
        if (_shopRoot != null) _shopRoot.SetActive(false);
    }

    private void OpenChoice()
    {
        _state = ShopState.Choice;

        if (_choiceRoot != null) _choiceRoot.SetActive(true);
        if (_listRoot != null) _listRoot.SetActive(false);
        if (_quantityRoot != null) _quantityRoot.SetActive(false);

        EnsureChoiceOptionsUI();
        UpdateChoiceVisual();
    }

    private void OpenBrowsing(ShopMode mode)
    {
        _state = ShopState.Browsing;
        _mode = mode;

        if (_choiceRoot != null) _choiceRoot.SetActive(false);
        if (_listRoot != null) _listRoot.SetActive(true);
        if (_quantityRoot != null) _quantityRoot.SetActive(false);

        ResetCursorAndScroll();
        BuildList();
        UpdateSelectedItemReference();
        UpdateOwnedCounterMain();
        UpdateMoneyUI();
        UpdateSelectedItemUI();
    }

    private void OpenQuantity()
    {
        if (_selectedItem == null) return;

        _state = ShopState.Quantity;
        _buyAmount = 1;

        if (_choiceRoot != null) _choiceRoot.SetActive(false);
        if (_quantityRoot != null) _quantityRoot.SetActive(true);

        ClampQuantityToCurrentLimits();
        UpdateQuantityText();
        UpdateSelectedItemUI();
    }

    private void OnAccept(InputAction.CallbackContext context)
    {
        if (_playerMovementController.GetCurrentPlayerState() != PlayerState.Shop)
            return;

        switch (_state)
        {
            case ShopState.Choice:
                if (_choiceIndex == 0) OpenBrowsing(ShopMode.Buy);
                else if (_choiceIndex == 1) OpenBrowsing(ShopMode.Sell);
                else StartCoroutine(CloseShop());
                break;

            case ShopState.Browsing:
                UpdateSelectedItemReference();
                if (_selectedItem == null)
                    return;

                if (GetMaxSelectableQuantity() <= 0)
                {
                    SetStatus(_mode == ShopMode.Buy ? "Ara no es pot comprar." : "No en tens.");
                    return;
                }

                OpenQuantity();
                break;

            case ShopState.Quantity:
                SubmitCurrentTransaction();
                break;
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (_playerMovementController.GetCurrentPlayerState() != PlayerState.Shop)
            return;

        switch (_state)
        {
            case ShopState.Choice:
                StartCoroutine(CloseShop());
                break;

            case ShopState.Browsing:
                OpenChoice();
                break;

            case ShopState.Quantity:
                OpenBrowsing(_mode);
                break;
        }
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (_playerMovementController.GetCurrentPlayerState() != PlayerState.Shop)
            return;

        Vector2 nav = _udlrAction.ReadValue<Vector2>();

        if (_state == ShopState.Choice)
        {
            if (nav.y < 0f)
            {
                _choiceIndex = Mathf.Clamp(_choiceIndex + 1, 0, CHOICE_COUNT - 1);
                UpdateChoiceVisual();
            }
            else if (nav.y > 0f)
            {
                _choiceIndex = Mathf.Clamp(_choiceIndex - 1, 0, CHOICE_COUNT - 1);
                UpdateChoiceVisual();
            }
            return;
        }

        if (_state == ShopState.Browsing)
        {
            if (nav.y < 0f) MoveSelection(down: true);
            else if (nav.y > 0f) MoveSelection(down: false);
            return;
        }

        if (_state == ShopState.Quantity)
        {
            if (nav.x > 0.1f || nav.y > 0.1f)
            {
                _buyAmount++;
                ClampQuantityToCurrentLimits();
                UpdateQuantityText();
                UpdateSelectedItemUI();
            }
            else if (nav.x < -0.1f || nav.y < -0.1f)
            {
                _buyAmount--;
                ClampQuantityToCurrentLimits();
                UpdateQuantityText();
                UpdateSelectedItemUI();
            }
        }
    }

    private void UpdateChoiceVisual()
    {
        if (_choiceSelectionBox == null) return;

        Vector2 pos = _choiceSelectionBoxAnchoredPos;
        pos.y -= _choiceIndex * _choiceSelectionStep;
        _choiceSelectionBox.anchoredPosition3D = pos;
    }

    private void BuildList()
    {
        if (_contentParent == null || _slotPrefab == null) return;

        List<Transform> toRemove = new List<Transform>();
        foreach (Transform child in _contentParent) toRemove.Add(child);
        foreach (Transform child in toRemove) Destroy(child.gameObject);

        if (_items == null) return;

        foreach (var it in _items)
        {
            if (it == null) continue;
            GameObject obj = Instantiate(_slotPrefab, _contentParent);

            ItemSlotUI ui = obj.GetComponent<ItemSlotUI>();
            if (ui != null)
            {
                ui.nameText.text = it.displayName;
                ui.quantityText.text = GetUnitPrice(it).ToString();
            }
        }
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
            _contentTransform.anchoredPosition3D = _contentBaseAnchoredPos;
    }

    private void MoveSelection(bool down)
    {
        int itemCount = (_items != null) ? _items.Count : 0;

        if (itemCount <= 0)
        {
            _selectedItem = null;
            if (_ownedCountText != null) _ownedCountText.text = "0";
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
                UpdateSelectedItemUI();
                return;
            }

            if (_cursorRow < visibleRows - 1 && _cursorRow < itemCount - 1 - _scrollRow)
            {
                _cursorRow++;
                MoveSelectionBoxBy(GetStepDownY());
            }
            else if (_scrollRow < maxScroll)
            {
                _scrollRow++;
                ApplyContentScroll();
            }
        }
        else
        {
            if (selectedIndex <= 0)
            {
                UpdateSelectedItemUI();
                return;
            }

            if (_cursorRow > 0)
            {
                _cursorRow--;
                MoveSelectionBoxBy(-GetStepDownY());
            }
            else if (_scrollRow > 0)
            {
                _scrollRow--;
                ApplyContentScroll();
            }
        }

        _statusMessage = string.Empty;
        UpdateSelectedItemReference();
        UpdateOwnedCounterMain();
        UpdateSelectedItemUI();
    }

    private void UpdateSelectedItemUI()
    {
        if (_currentItemSpriteUI == null || _currentItemDescription == null)
            return;

        if (_selectedItem == null)
        {
            _currentItemSpriteUI.sprite = null;
            _currentItemDescription.text = string.IsNullOrWhiteSpace(_statusMessage) ? string.Empty : _statusMessage;
            return;
        }

        _currentItemSpriteUI.sprite = _selectedItem.sprite;

        if (_currentItemSpriteUI.sprite != null)
            _currentItemSpriteUI.rectTransform.sizeDelta = _currentItemSpriteUI.sprite.rect.size;

        if (!string.IsNullOrWhiteSpace(_statusMessage))
        {
            _currentItemDescription.text = _statusMessage;
            return;
        }

        int unitPrice = GetUnitPrice(_selectedItem);
        int totalPrice = Mathf.Max(0, unitPrice * Mathf.Max(1, _buyAmount));
        string modeText = _mode == ShopMode.Buy ? "Compra" : "Venda";
        string baseText = modeText + " - Preu: " + unitPrice + "\n" + _selectedItem.description;

        if (_state == ShopState.Quantity)
            baseText += "\n\nTotal: " + totalPrice;

        _currentItemDescription.text = baseText;
    }

    private void MoveSelectionBoxBy(float deltaY)
    {
        if (_selectionBoxTransform == null) return;

        Vector2 pos = _selectionBoxTransform.anchoredPosition3D;
        pos.y += deltaY;

        float minY = Mathf.Min(TOP_SELECTION_Y, BOTTOM_SELECTION_Y);
        float maxY = Mathf.Max(TOP_SELECTION_Y, BOTTOM_SELECTION_Y);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        _selectionBoxTransform.anchoredPosition3D = pos;
    }

    private void ApplyContentScroll()
    {
        if (_contentTransform == null) return;

        float deltaY = _scrollRow * CONTENT_SCROLL_STEP * CONTENT_SCROLL_DOWN_SIGN;
        _contentTransform.anchoredPosition3D = _contentBaseAnchoredPos + new Vector2(0f, deltaY);
    }

    private int GetVisibleRows()
    {
        float move = Mathf.Max(1f, Mathf.Abs(_selectionBoxMovement));
        float range = Mathf.Abs(BOTTOM_SELECTION_Y - TOP_SELECTION_Y);
        return Mathf.FloorToInt(range / move) + 1;
    }

    private float GetStepDownY()
    {
        float move = Mathf.Abs(_selectionBoxMovement);
        float sign = Mathf.Sign(BOTTOM_SELECTION_Y - TOP_SELECTION_Y);
        return move * sign;
    }

    private int GetSelectedIndex()
    {
        return Mathf.Max(0, _scrollRow + _cursorRow);
    }

    private void UpdateSelectedItemReference()
    {
        if (_items == null || _items.Count == 0)
        {
            _selectedItem = null;
            return;
        }

        int index = Mathf.Clamp(GetSelectedIndex(), 0, _items.Count - 1);
        _selectedItem = _items[index];
    }

    private void UpdateOwnedCounterMain()
    {
        if (_ownedCountText == null)
            return;

        if (_selectedItem == null)
        {
            _ownedCountText.text = "0";
            return;
        }

        _ownedCountText.text = GetOwnedCount(_selectedItem).ToString();
    }

    private int GetOwnedCount(Item item)
    {
        if (item == null || Inventory.Instance == null) return 0;

        List<InventorySlot> pocket = Inventory.Instance.GetPocket(item.type);
        if (pocket == null) return 0;

        for (int i = 0; i < pocket.Count; i++)
        {
            if (pocket[i].item == item)
                return pocket[i].quantity;

            if (pocket[i].item != null && pocket[i].item.name == item.name)
                return pocket[i].quantity;
        }

        return 0;
    }

    private int GetUnitPrice(Item item)
    {
        return _mode == ShopMode.Buy ? ShopPricing.GetBuyPrice(item) : ShopPricing.GetSellPrice(item);
    }

    private void SubmitCurrentTransaction()
    {
        if (_shopTransactionService == null)
        {
            SetStatus("Botiga no disponible.");
            OpenBrowsing(_mode);
            return;
        }

        if (SessionManager.Instance == null || !SessionManager.Instance.IsLoggedIn)
        {
            SetStatus("No hi ha usuari.");
            OpenBrowsing(_mode);
            return;
        }

        if (_selectedItem == null)
        {
            OpenBrowsing(_mode);
            return;
        }

        ClampQuantityToCurrentLimits();
        if (_buyAmount <= 0)
        {
            SetStatus(_mode == ShopMode.Buy ? "Quantitat no valida." : "Quantitat no valida.");
            OpenBrowsing(_mode);
            return;
        }

        (bool ok, string message) result = _mode == ShopMode.Buy
            ? _shopTransactionService.BuyItem(SessionManager.Instance.UserId, _selectedItem, _buyAmount, _simulateDbErrorOnNextTransaction)
            : _shopTransactionService.SellItem(SessionManager.Instance.UserId, _selectedItem, _buyAmount, _simulateDbErrorOnNextTransaction);

        _simulateDbErrorOnNextTransaction = false;
        SetStatus(result.message);

        if (result.ok && _inventoryDbController != null)
            _inventoryDbController.RefreshFromDatabase();

        UpdateMoneyUI();
        OpenBrowsing(_mode);
    }

    private void SetStatus(string message)
    {
        _statusMessage = message ?? string.Empty;
        UpdateSelectedItemUI();
    }

    private void UpdateQuantityText()
    {
        if (_qtyAmountText != null)
            _qtyAmountText.text = Mathf.Max(1, _buyAmount).ToString();
    }

    private void ClampQuantityToCurrentLimits()
    {
        int max = Mathf.Max(0, GetMaxSelectableQuantity());
        if (max <= 0)
        {
            _buyAmount = 0;
            return;
        }

        _buyAmount = Mathf.Clamp(_buyAmount, 1, max);
    }

    private int GetMaxSelectableQuantity()
    {
        if (_selectedItem == null)
            return 0;

        if (_mode == ShopMode.Sell)
            return Mathf.Clamp(GetOwnedCount(_selectedItem), 0, 99);

        int owned = GetOwnedCount(_selectedItem);
        int maxByStack = _selectedItem.stackable
            ? Mathf.Max(0, Mathf.Max(1, _selectedItem.maxStack) - owned)
            : (owned > 0 ? 0 : 1);

        int price = Mathf.Max(1, ShopPricing.GetBuyPrice(_selectedItem));
        int affordable = GetCurrentMoney() / price;

        return Mathf.Clamp(Mathf.Min(maxByStack, affordable), 0, 99);
    }

    private int GetCurrentMoney()
    {
        if (_inventoryDbController != null)
            return _inventoryDbController.GetCurrentMoney();

        if (DatabaseManager.Instance == null || SessionManager.Instance == null || !SessionManager.Instance.IsLoggedIn)
            return 0;

        var playerRepo = new PlayerRepository(DatabaseManager.Instance.Db);
        return playerRepo.GetMoney(SessionManager.Instance.UserId);
    }

    private void UpdateMoneyUI()
    {
        string moneyValue = GetCurrentMoney().ToString();

        if (_listRoot != null)
        {
            Transform moneyRoot = _listRoot.transform.Find("UserMoney");
            if (moneyRoot != null)
            {
                foreach (TMP_Text txt in moneyRoot.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (string.Equals(txt.text, "$"))
                        continue;

                    txt.text = moneyValue;
                }
            }
        }

        if (_moneyText != null && !string.Equals(_moneyText.text, "$"))
            _moneyText.text = moneyValue;
    }

    private void TryFindMoneyText()
    {
        if (_moneyText != null || _listRoot == null)
            return;

        Transform moneyRoot = _listRoot.transform.Find("UserMoney");
        if (moneyRoot == null)
            return;

        TMP_Text best = moneyRoot.GetComponentsInChildren<TMP_Text>(true)
            .OrderByDescending(t => t.rectTransform.rect.width * t.rectTransform.rect.height)
            .FirstOrDefault(t => !string.Equals(t.text, "$"));

        if (best != null)
            _moneyText = best;
    }

    private void EnsureChoiceOptionsUI()
    {
    }
}
