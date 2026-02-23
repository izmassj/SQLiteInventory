using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Video;

public class MenuControlller : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private AudioSource _titleAudioSource;

    [Header("Skip Cutscene")]
    [SerializeField] private CinemachineBrain _brain;
    [SerializeField] private InputActionAsset _playerInputAction;
    [SerializeField] private CanvasGroup _topMaskScreenCanvGroup;
    [SerializeField] private MenuExit _menuExit;
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private PlayableDirector _playableDirector;
    [SerializeField] private IntroTimeSync _introTimeSyncScript;
    [SerializeField] private float _blackoutWaitTime;
    private bool _introEnded;
    private InputAction _eastAction;
    private InputAction _startAction;

    [Header("Fase 0 GameObjects")]
    [SerializeField] private GameObject _topScreenCanvas;
    [SerializeField] private GameObject _bottomScreenCanvas;

    [Header("Fase 1 GameObjects")]
    [SerializeField] private CanvasGroup _bottomIntroMaskCanvGroup;
    [SerializeField] private GameObject _introScreenCanvas;

    [Header("Fase 2 GameObjects")]
    [SerializeField] private CanvasGroup _bottomMaskCanvGroup;
    [SerializeField] private CinemachineCamera _giratinaCameraPosFinal;

    [Header("Fase 3 GameObjects")]
    [SerializeField] private CanvasGroup _developedByCanvGroup;
    [SerializeField] private CanvasGroup _topIntroMaskScreenCanvGroup;

    [Header("Fase 4 GameObjects")]
    [SerializeField] private GameObject _pressStartImage;
    [SerializeField] private float _showPressStartTime;
    [SerializeField] private float _hidePressStartTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerInputAction.Enable();
        _eastAction = _playerInputAction.FindActionMap("Action", true).FindAction("East");
    }

    // Update is called once per frame
    void Update()
    {
        if (_introEnded)
        {
            Debug.Log("Intro has ended");
            if (_eastAction.triggered)
            {
                _topMaskScreenCanvGroup.gameObject.SetActive(true);
                _bottomMaskCanvGroup.gameObject.SetActive(true);
                _topMaskScreenCanvGroup.DOFade(1,1f);
                _bottomMaskCanvGroup.DOFade(1,1f).OnComplete(() =>
                {
                    _menuExit.LoadLoginRegisterScene();
                });
            }

        }
        else if (!_introEnded)
        {
            Debug.Log("Intro hasnt ended");
            if (_eastAction.triggered)
            {
                StartCoroutine(SkipCutscene());
            }
        }

    }

    public IEnumerator SkipCutscene()
    {
        Debug.Log("Cutscene Skip");

        _giratinaCameraPosFinal.Priority += 1;
        _videoPlayer.enabled = false;
        _playableDirector.enabled = false;
        _introScreenCanvas.SetActive(false);
        _introTimeSyncScript.enabled = false;
        _bottomIntroMaskCanvGroup.alpha = 0;
        _topIntroMaskScreenCanvGroup.alpha = 0;

        _topScreenCanvas.SetActive(true);
        _bottomScreenCanvas.SetActive(true);

        _developedByCanvGroup.alpha = 1f;
        _topMaskScreenCanvGroup.alpha = 1f;

        DOTween.Kill(_bottomIntroMaskCanvGroup);

        _introEnded = true;

        _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

        StartCoroutine(PressStartIntermitent());

        yield return new WaitForSeconds(_blackoutWaitTime);

        _titleAudioSource.Play();

        _topMaskScreenCanvGroup.alpha = 0f;
        _bottomMaskCanvGroup.alpha = 0f;
    }

    public void GoToMainScene()
    {

    }

    // 2D Intro empieza
    public void Fase0()
    {
        Debug.Log("2D Intro empieza");

        _topScreenCanvas.SetActive(false);
        _bottomScreenCanvas.SetActive(false);
    }

    // 2D Intro acaba Fade Out white
    public void Fase1()
    {
        Debug.Log("2D Intro acaba");

        _introScreenCanvas.SetActive(false);

        _topScreenCanvas.SetActive(true);
        _bottomScreenCanvas.SetActive(true);

        _bottomIntroMaskCanvGroup.DOFade(0, 1.82f);
    }

    // Giratina opacity fade
    public void Fase2()
    {
        Debug.Log("Giratina opacity fade");

        _bottomMaskCanvGroup.DOFade(0, 2f).SetEase(Ease.Linear);
        _giratinaCameraPosFinal.Priority += 1;
    }

    // Menu UI and fade in logo
    public void Fase3()
    {
        Debug.Log("Menu UI and fade in logo");

        _developedByCanvGroup.alpha = 1.0f;
        _topIntroMaskScreenCanvGroup.DOFade(0, 2f);
    }

    // Menu UI empieza start
    public void Fase4()
    {
        Debug.Log("Menu UI empieza start");

        _introEnded = true;

        StartCoroutine(PressStartIntermitent());
    }

    public void Fase5()
    {
        Debug.Log("Menu UI empieza start");

        _titleAudioSource.Play();

        _playableDirector.enabled = false;
        _videoPlayer.enabled = false;
    }

    public IEnumerator PressStartIntermitent()
    {
        _pressStartImage.SetActive(true);

        yield return new WaitForSeconds(_showPressStartTime);

        _pressStartImage.SetActive(false);

        yield return new WaitForSeconds(_hidePressStartTime);

        StartCoroutine(PressStartIntermitent());
    }
}
