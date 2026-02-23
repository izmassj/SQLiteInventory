using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueUIController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset _playerInputAction;

    [Header("UI")]
    [SerializeField] private GameObject _dialoguePanel;
    [SerializeField] private TMP_Text _dialogueText;
    [SerializeField] private GameObject _continueArrow;

    [Header("Settings")]
    [SerializeField] private float _lettersPerSecond;

    [Header("Choice UI")]
    [SerializeField] private GameObject _choiceRoot;
    [SerializeField] private RectTransform _choiceSelectionBox;
    [SerializeField] private int _choiceStep;

    [Header("Test")]
    [SerializeField] private DialogueData _test;

    private bool _isWaitingForChoice;
    private int _choiceIndex;

    private DialogueData _currentDialogue;
    private int _currentPageIndex;
    private Coroutine _typingCoroutine;

    private bool _isTyping;
    private bool _isDialogueActive;

    private Vector2 _originalPoschoiceSelectionBox;

    public bool show = false;

    private InputAction _nextAction;

    private void Awake()
    {
        _playerInputAction.Enable();
        _nextAction = _playerInputAction.FindActionMap("Action", true).FindAction("East");
    }

    private void Start()
    {
        _dialoguePanel.SetActive(false);
        if (_choiceSelectionBox != null)
        {
            _originalPoschoiceSelectionBox = _choiceSelectionBox.anchoredPosition3D;
        }
    }

    void Update()
    {
        if (!_isDialogueActive) return;

        if (_isWaitingForChoice)
        {
            HandleChoiceInput();
            return;
        }

        if (_nextAction.triggered)
        {
            if (_isTyping)
            {
                CompleteTypingInstantly();
            }
            else
            {
                NextPage();
            }
        }
    }

    public void StartDialogue(DialogueData data)
    {
        _currentDialogue = data;
        _currentPageIndex = 0;
        _isDialogueActive = true;

        _dialoguePanel.SetActive(true);
        ShowPage();
    }

    void ShowPage()
    {
        _continueArrow.SetActive(false);

        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _typingCoroutine = StartCoroutine(TypeText(_currentDialogue.pages[_currentPageIndex]));
    }

    IEnumerator TypeText(string text)
    {
        _isTyping = true;
        _dialogueText.text = "";

        float delay = 1f / _lettersPerSecond;

        foreach (char letter in text)
        {
            _dialogueText.text += letter;
            yield return new WaitForSeconds(delay);
        }

        _isTyping = false;
        _continueArrow.SetActive(true);
    }

    void CompleteTypingInstantly()
    {
        StopCoroutine(_typingCoroutine);
        _dialogueText.text = _currentDialogue.pages[_currentPageIndex];
        _isTyping = false;
        _continueArrow.SetActive(true);
    }

    void NextPage()
    {
        if (_currentDialogue.hasChoice &&
            _currentPageIndex == _currentDialogue.choicePageIndex)
        {
            OpenChoice();
            return;
        }

        _currentPageIndex++;

        if (_currentPageIndex >= _currentDialogue.pages.Length)
        {
            EndDialogue();
            return;
        }

        ShowPage();
    }

    void OpenChoice()
    {
        _isWaitingForChoice = true;
        _choiceIndex = 0;

        _choiceRoot.SetActive(true);
        UpdateChoiceVisual();
    }

    void HandleChoiceInput()
    {
        Vector2 nav = _playerInputAction.FindActionMap("Action", true).FindAction("Navigate").ReadValue<Vector2>();

        if (nav.y < 0f)
        {
            _choiceIndex = 1;
            UpdateChoiceVisual();
        }
        else if (nav.y > 0f)
        {
            _choiceIndex = 0;
            UpdateChoiceVisual();
        }

        if (_nextAction.triggered)
        {
            ConfirmChoice();
        }
    }

    void ConfirmChoice()
    {
        _isWaitingForChoice = false;
        _choiceRoot.SetActive(false);

        if (_choiceIndex == 0)
            _currentDialogue.onYes?.Invoke();
        else
            _currentDialogue.onNo?.Invoke();

        EndDialogue();
    }

    void UpdateChoiceVisual()
    {
        Vector2 pos = _choiceSelectionBox.anchoredPosition;
        pos.y = -_choiceIndex * _choiceStep;
        _choiceSelectionBox.anchoredPosition = _originalPoschoiceSelectionBox + new Vector2(0, pos.y);
    }

    void EndDialogue()
    {
        _isDialogueActive = false;
        _dialoguePanel.SetActive(false);

        _currentDialogue.onDialogueEnd.Invoke();
    }

    public bool IsActive()
    {
        return _isDialogueActive;
    }
}