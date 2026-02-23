// PlayerMovementController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerAnimationController _anim;

    [Header("Collision")]
    [SerializeField] private LayerMask _wallLayer;
    [SerializeField] private Vector3 _collisionBoxHalfExtents = new Vector3(0.4f, 0.5f, 0.4f);

    [Header("Input")]
    [SerializeField] private InputActionAsset _playerInputAction;

    [Header("Movement Settings")]
    [SerializeField] private float _walkSpeed;
    [SerializeField] private float _runSpeed;
    [SerializeField] private float _gridSize;
    [SerializeField] private float _tapThreshold;

    private float _lucasSpeed;
    private bool _isRunning;

    public PlayerState _currentState = PlayerState.Idle;
    private Vector2 _inputMovement;
    private Vector2 _lastInputMovement;
    private Vector2 _tapStartDirection;
    private float _tapStartTime;
    private bool _isTapping;

    private Coroutine _tapCoroutine;
    private Coroutine _moveCoroutine;

    private InputAction _moveAction;
    private InputAction _runAction;

    private bool _isLeftStep = true;
    private bool _isMovingToGrid = false;

    private Vector3 _startPos;
    private Vector3 _targetPos;

    private void Awake()
    {
        _playerInputAction.Enable();
        _moveAction = _playerInputAction.FindActionMap("Movement", true).FindAction("Move");
        _runAction = _playerInputAction.FindActionMap("Action").FindAction("South");
    }

    private void Start()
    {
        _isTapping = false;
        _lucasSpeed = _walkSpeed;
        _isLeftStep = true;
        _isRunning = false;
        _anim?.SetRunning(false);
    }

    private void Update()
    {
        _inputMovement = _moveAction.ReadValue<Vector2>();
        ProcessRunInput();

        switch (_currentState)
        {
            case PlayerState.Idle:
                if (_inputMovement != Vector2.zero)
                    ProcessMovementInput();
                break;

            case PlayerState.Turning:
                if (_inputMovement == Vector2.zero)
                {
                    _currentState = PlayerState.Idle;
                }
                else if (Time.time - _tapStartTime >= _tapThreshold)
                {
                    _moveCoroutine = StartCoroutine(MoveToGridPosition(_tapStartDirection));
                }
                break;

            case PlayerState.Moving:
                break;
        }
    }

    private bool IsBlocked(Vector3 targetPosition)
    {
        return Physics.CheckBox(
            targetPosition,
            _collisionBoxHalfExtents,
            Quaternion.identity,
            _wallLayer
        );
    }

    private void ProcessRunInput()
    {
        bool wantsRun = (_runAction.ReadValue<float>() != 0) && (_inputMovement != Vector2.zero);

        _lucasSpeed = wantsRun ? _runSpeed : _walkSpeed;

        if (_isRunning != wantsRun)
        {
            _isRunning = wantsRun;
            _anim?.SetRunning(_isRunning);
        }
    }

    private void ProcessMovementInput()
    {
        Vector2 processedInput = Vector2.zero;
        if (_currentState != PlayerState.Idle && _currentState != PlayerState.Turning) return;

        if (Mathf.Abs(_inputMovement.x) > 0.1f || Mathf.Abs(_inputMovement.y) > 0.1f)
        {
            if (Mathf.Abs(_inputMovement.x) > 0.1f && Mathf.Abs(_inputMovement.y) > 0.1f)
            {
                if (Mathf.Abs(_lastInputMovement.x) > Mathf.Abs(_lastInputMovement.y))
                    processedInput = new Vector2(0, Mathf.Sign(_inputMovement.y));
                else
                    processedInput = new Vector2(Mathf.Sign(_inputMovement.x), 0);
            }
            else if (Mathf.Abs(_inputMovement.x) > 0.1f)
            {
                processedInput = new Vector2(Mathf.Sign(_inputMovement.x), 0);
            }
            else if (Mathf.Abs(_inputMovement.y) > 0.1f)
            {
                processedInput = new Vector2(0, Mathf.Sign(_inputMovement.y));
            }

            if (processedInput != Vector2.zero)
            {
                if (processedInput != _lastInputMovement)
                {
                    StartTapTurn(processedInput);
                }
                else
                {
                    if (_isTapping)
                        CancelTap();

                    _moveCoroutine = StartCoroutine(MoveToGridPosition(processedInput));
                }

                _lastInputMovement = processedInput;
            }
        }
    }

    private void StartTapTurn(Vector2 direction)
    {
        _tapStartDirection = direction;
        _tapStartTime = Time.time;
        _isTapping = true;

        _anim?.SetDirection(direction);
        _currentState = PlayerState.Turning;

        if (_tapCoroutine != null)
            StopCoroutine(_tapCoroutine);

        _tapCoroutine = StartCoroutine(TapTimer());
    }

    private IEnumerator TapTimer()
    {
        yield return new WaitForSeconds(_tapThreshold);

        if (_currentState == PlayerState.Turning && _isTapping)
        {
            _moveCoroutine = StartCoroutine(MoveToGridPosition(_tapStartDirection));
            yield break;
        }

        _anim?.SetStepFrame(_isLeftStep);

        yield return new WaitForSeconds(_tapThreshold);

        _anim?.SetIdleFrame(_isLeftStep);
    }

    private void CancelTap()
    {
        _isTapping = false;

        if (_tapCoroutine != null)
        {
            StopCoroutine(_tapCoroutine);
            _tapCoroutine = null;
        }
    }

    private IEnumerator MoveToGridPosition(Vector2 direction)
    {
        if (_isMovingToGrid) yield break;
        _isMovingToGrid = true;

        if (_currentState == PlayerState.Menu)
        {
            _isMovingToGrid = false;
            if (_isRunning) { _isRunning = false; _anim?.SetRunning(false); }
            yield break;
        }

        CancelTap();

        _currentState = PlayerState.Moving;
        _startPos = transform.position;

        Vector3 potentialTarget = _startPos + new Vector3(
            direction.x * _gridSize,
            0,
            direction.y * _gridSize
        );

        if (IsBlocked(potentialTarget))
        {
            _currentState = PlayerState.Idle;
            _isMovingToGrid = false;
            yield break;
        }

        _targetPos = potentialTarget;

        _anim?.SetDirection(direction);

        float duration = 1f / _lucasSpeed;
        float elapsed = 0f;

        bool hasChangedToStep = false;
        bool hasChangedToIdle = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Min(elapsed / duration, 1f);

            float wantedDuration = 1f / _lucasSpeed;
            if (!Mathf.Approximately(duration, wantedDuration))
                duration = Mathf.Lerp(duration, wantedDuration, 0.1f);

            if (t >= 0f && !hasChangedToStep)
            {
                _anim?.SetStepFrame(_isLeftStep);
                hasChangedToStep = true;
            }

            if (t >= 0.7f && !hasChangedToIdle)
            {
                _anim?.SetIdleFrame(_isLeftStep);
                hasChangedToIdle = true;
            }

            transform.position = Vector3.Lerp(_startPos, _targetPos, t);
            yield return null;
        }

        transform.position = _targetPos;

        _isLeftStep = !_isLeftStep;
        _isMovingToGrid = false;

        if (_currentState == PlayerState.Menu)
        {
            if (_isRunning) 
            { 
                _isRunning = false; 
                _anim?.SetRunning(false); 
            }
            yield break;
        }

        _currentState = PlayerState.Idle;

        if (_inputMovement != Vector2.zero)
        {
            ProcessMovementInput();
        }
    }

    // misc

    public void StopMovementCoroutine()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
        CancelTap();
    }

    // getters

    public PlayerState GetCurrentPlayerState()
    {
        return _currentState;
    }

    public bool GetIsMovingToGrid()
    {
        return _isMovingToGrid;
    }

    public Vector2 GetFacingDirection()
    {
        return _lastInputMovement == Vector2.zero ? Vector2.up : _lastInputMovement;
    }

    // setter

    public void SetCurrentPlayerState(PlayerState newState)
    {
        _currentState = newState;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_targetPos, _collisionBoxHalfExtents * 2f);
    }
}
