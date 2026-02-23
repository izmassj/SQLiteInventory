using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class PlayerUIMenuController : MonoBehaviour
{
    // constantes
    /*
     * estas constantes son para saber cual seria la posicion en y maxima en la que
     * llegaria la selection box para volver arriba o abajo
     * soy consciente en que son iguales solo que con diferente signo, pero no estaba seguro
     * si quitaria o añadiria cosas y pues lo dejo asi por si acaso en un futuro quito o añado algo
     */
    const float TOP_SELECTION_QUANTITY = 72f;
    const float BOTT_SELECTION_QUANTITY = -72f;

    [Header("Refs")]
    [SerializeField] private PlayerMovementController _playerMovementController;
    [SerializeField] private PlayerUIController _playerUIController;
    [SerializeField] private PlayerUIBagController _playerUIBagController;

    [Header("Materials")]
    [SerializeField] private Material _grayscaleMat;

    [Header("Menu UI GameObjects")]
    [SerializeField] private RectTransform _selectionBoxTransform;
    [SerializeField] private List<GameObject> _menuItemsUI;

    [Header("UI Parameters")]
    [SerializeField] private float _selectionBoxMovement;
    [SerializeField] private float _itemScaleQuantity;
    [SerializeField] private float _itemWobbleQuantity;

    [Header("Input")]
    [SerializeField] private InputActionAsset _playerInputAction;

    public MenuStateUI _currentState = MenuStateUI.Pokedex;

    private Vector2 _inputNav;

    private GameObject _currentMenuItemUI;

    private InputAction _udlrAction;
    private InputAction _acceptAction;

    private Sequence _sequenceUIBagItemsWobble;

    public Coroutine _itemUIAnimation;

    private void Awake()
    {
        _udlrAction = _playerInputAction.FindActionMap("Action", true).FindAction("Navigate");
        _acceptAction = _playerInputAction.FindActionMap("Action", true).FindAction("East");

        _sequenceUIBagItemsWobble = DOTween.Sequence();
    }

    private void Start()
    {
        _udlrAction.started += NavigateMenu;
        _acceptAction.started += SelectItem;

        if (_menuItemsUI.Count != 0)
        {
            _currentMenuItemUI = _menuItemsUI[0];
        }
    }

    public void ResetCurrentIcon()
    {
        if (_itemUIAnimation != null)
        {
            StopCoroutine(_itemUIAnimation);
            RectTransform rect = _currentMenuItemUI.GetComponent<RectTransform>();

            _currentMenuItemUI.GetComponent<Image>().material = _grayscaleMat;

            rect.DOKill();
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }

    public IEnumerator SetCurrentIcon()
    {
        _currentMenuItemUI.GetComponent<Image>().material = null;

        if (_sequenceUIBagItemsWobble != null && _sequenceUIBagItemsWobble.IsActive())
        {
            _sequenceUIBagItemsWobble.Kill();
        }

        RectTransform rect = _currentMenuItemUI.GetComponent<RectTransform>();

        Vector3 baseScale = rect.localScale;
        rect.localRotation = Quaternion.identity;

        rect.DOScale(baseScale * _itemScaleQuantity, 0.5f).SetEase(Ease.Linear, 1.2f).SetLoops(2, LoopType.Yoyo);

        yield return new WaitForSeconds(0.35f*2);

        _sequenceUIBagItemsWobble = DOTween.Sequence();

        _sequenceUIBagItemsWobble.Append(rect.DORotate(new Vector3(0, 0, _itemWobbleQuantity * 2), 0.6f, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        _sequenceUIBagItemsWobble.Append(rect.DORotate(new Vector3(0, 0, -_itemWobbleQuantity * 4), 1.2f, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        _sequenceUIBagItemsWobble.Append(rect.DORotate(new Vector3(0, 0, _itemWobbleQuantity * 2), 0.6f, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        _sequenceUIBagItemsWobble.SetLoops(-1, LoopType.Restart);

        _sequenceUIBagItemsWobble.SetAutoKill(false);

        yield return null;
    }



    private void SelectItem(InputAction.CallbackContext obj)
    {
        if (_playerMovementController.GetCurrentPlayerState() == PlayerState.Menu)
        {
            switch (_currentState)
            {
                case MenuStateUI.Pokedex:
                    break;
                case MenuStateUI.Pokemon:
                    break;
                case MenuStateUI.Bag:
                    _playerUIBagController.OpenBag();
                    _playerUIController.CloseMenuUI();
                    break;
                case MenuStateUI.User:
                    break;
                case MenuStateUI.Save:
                    break;
                case MenuStateUI.Options:
                    break;
                case MenuStateUI.Back:
                    _playerUIController.CloseMenuUI();
                    break;
            }
        }
    }

    private MenuStateUI PreviousEnumOnMenuState(MenuStateUI current, out int outIndex)
    {
        MenuStateUI[] values = (MenuStateUI[])Enum.GetValues(typeof(MenuStateUI));
        int index = Array.IndexOf(values, current);

        outIndex = (index - 1 + values.Length) % values.Length;

        return values[(index - 1 + values.Length) % values.Length];
    }

    private MenuStateUI NextEnumOnMenuState(MenuStateUI current, out int outIndex) 
    {
        MenuStateUI[] values = (MenuStateUI[])Enum.GetValues(typeof(MenuStateUI));
        int index = Array.IndexOf(values, current);

        outIndex = (index + 1) % values.Length;

        return values[(index + 1) % values.Length];
    }

    private void NavigateMenu(InputAction.CallbackContext obj)
    {
        if (_playerMovementController.GetCurrentPlayerState() == PlayerState.Menu)
        {
            _inputNav = _udlrAction.ReadValue<Vector2>();
            // mover abajo
            if (_inputNav.y > 0f)
            {
                // si esta al tope del todo
                if (_selectionBoxTransform.anchoredPosition3D.y + _selectionBoxMovement > TOP_SELECTION_QUANTITY)
                {
                    _selectionBoxTransform.anchoredPosition3D = new Vector3(_selectionBoxTransform.anchoredPosition3D.x, BOTT_SELECTION_QUANTITY, _selectionBoxTransform.anchoredPosition3D.z);
                }
                // si no lo esta
                else 
                {
                    _selectionBoxTransform.anchoredPosition3D = new Vector3(_selectionBoxTransform.anchoredPosition3D.x, _selectionBoxTransform.anchoredPosition3D.y + _selectionBoxMovement, _selectionBoxTransform.anchoredPosition3D.z);
                }
                
                ResetCurrentIcon();

                int currentIndex;
                _currentState = PreviousEnumOnMenuState(_currentState, out currentIndex);
                _currentMenuItemUI = _menuItemsUI[currentIndex];

                _itemUIAnimation = StartCoroutine(SetCurrentIcon());
            }
            // lo mismo para arriba
            else if (_inputNav.y < 0f)
            {
                if (_selectionBoxTransform.anchoredPosition3D.y - _selectionBoxMovement < BOTT_SELECTION_QUANTITY)
                {
                    _selectionBoxTransform.anchoredPosition3D = new Vector3(_selectionBoxTransform.anchoredPosition3D.x, TOP_SELECTION_QUANTITY, _selectionBoxTransform.anchoredPosition3D.z);
                }
                else
                {
                    _selectionBoxTransform.anchoredPosition3D = new Vector3(_selectionBoxTransform.anchoredPosition3D.x, _selectionBoxTransform.anchoredPosition3D.y - _selectionBoxMovement, _selectionBoxTransform.anchoredPosition3D.z);
                }

                ResetCurrentIcon();
                
                int currentIndex;
                _currentState = NextEnumOnMenuState(_currentState, out currentIndex);
                _currentMenuItemUI = _menuItemsUI[currentIndex];

                _itemUIAnimation = StartCoroutine(SetCurrentIcon());
            }
        }
    }

    private void Update()
    {
        if (_playerMovementController.GetCurrentPlayerState() == PlayerState.Menu)
        {

        }
    }
}
