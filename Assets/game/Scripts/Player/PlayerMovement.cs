using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
  [SerializeField]
  private InputManager _input;

  [SerializeField]
  private CameraManager _cameraManager;

  [SerializeField]
  private PlayerAudioManager _playerAudioManager;

  [SerializeField]
  private LayerMask _groundLayer;

  [SerializeField]
  private LayerMask _climbableLayer;

  [SerializeField]
  private LayerMask _hitLayer;

  [SerializeField]
  private Transform _groundDetector;

  [SerializeField]
  private Transform _climbDetector;

  [SerializeField]
  private Transform _cameraTransform;

  [SerializeField]
  private Transform _hitDetector;

  [SerializeField]
  private Vector3 _upperStepOffset;

  [SerializeField]
  private Vector3 _climbOffset;

  [SerializeField]
  private Vector3 _glideRotationSpeed;

  [SerializeField]
  private float _walkSprintTransition;

  [SerializeField]
  private float _walkSpeed;
    
  [SerializeField]
  private float _sprintSpeed;

  [SerializeField]
  private float _rotationSmoothTime = 0.1f;

  [SerializeField]
  private float _jumpForce;

  [SerializeField]
  private float _detectorRadius;

  [SerializeField]
  private float _stepCheckerDistance;

  [SerializeField]
  private float _stepForce;

  [SerializeField]
  private float _climbCheckDistace;

  [SerializeField]
  private float _climbSpeed;

  [SerializeField]
  private float _crouchSpeed;

  [SerializeField]
  private float _glideSpeed;

  [SerializeField]
  private float _airDrag;

  [SerializeField]
  private float _minGlideRotationX;

  [SerializeField]
  private float _maxGlideRotationX;

  [SerializeField]
  private float _resetComboInterval;

  [SerializeField]
  private float _hitDetectorRadius;
  private Coroutine _resetCombo;
  private int _combo = 0;
  private bool _isPunching;
  private CapsuleCollider _collider;
  private PlayerStance _playerStance;
  private Rigidbody _rigidbody;
  private float _speed;
  private float _rotationSmoothVelocity;
  private Animator _animator;

  private Vector3 rotationDegree = Vector3.zero;


    private void CheckStep()
    {
      bool isHitLowerStep = Physics.Raycast(_groundDetector.position, 
                            transform.forward, 
                            _stepCheckerDistance);
      bool isHitUpper = Physics.Raycast(_groundDetector.position + 
                                        _upperStepOffset, 
                                        transform.forward, 
                                        _stepCheckerDistance);
      if (isHitLowerStep && !isHitUpper)
      {
          _rigidbody.AddForce(0,_stepForce,0);
      }
    }
    private bool _isGrounded;

    private void CheckIsGrounded()
    {
      _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
      if (_isGrounded)
      {
        CancelGlide();
      }
      _animator.SetBool("IsGrounded", _isGrounded);
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _speed = _walkSpeed;
        _input.OnJumpInput += Jump;
        _playerStance = PlayerStance.Stand;
        HideAndLockCursor();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<CapsuleCollider>();
    }

    private void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnSprintInput += Sprint;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimb += CancelClimb;
        _input.OnCrouchInput += Crouch;
        _input.OnGlideInput += StartGlide;
        _input.OnCancelGlide += CancelGlide;  
        _input.OnPunchInput += Punch;
        _cameraManager.OnChangePerspective += ChangePerspective;
    }

    private void OnDestroy()
    {
        _input.OnMoveInput -= Move; 
        _input.OnSprintInput -= Sprint;  
        _input.OnJumpInput -= Jump;
        _input.OnClimbInput = StartClimb;
        _input.OnCancelClimb -= CancelClimb;
        _input.OnCrouchInput -= Crouch;
        _input.OnGlideInput -= StartGlide;
        _input.OnCancelGlide -= CancelGlide; 
        _input.OnPunchInput -= Punch;
        _cameraManager.OnChangePerspective -= ChangePerspective;
    }

  private void ChangePerspective()
  {
    _animator.SetTrigger("ChangePerspective");
  }
  private void Move(Vector2 axisDirection)
  {
      Vector3 movementDirection = Vector3.zero;
      bool isPlayerStanding = _playerStance == PlayerStance.Stand;
      bool isPlayerClimbing = _playerStance == PlayerStance.Climb;
      bool isPlayerCrouch = _playerStance == PlayerStance.Crouch;
      bool isPlayerGliding = _playerStance == PlayerStance.Glide;
      if ((isPlayerStanding || isPlayerCrouch)&& !_isPunching)
      {
        switch (_cameraManager.CameraState)
        {
          case CameraState.ThirdPerson:
            if (axisDirection.magnitude >= 0.1)
            {
              float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
              float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
              transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
              movementDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
              _rigidbody.AddForce(movementDirection * _speed * Time.deltaTime );
            } 
            break;
          case CameraState.FirstPerson:
            transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y,0f);
            Vector3 verticalDirection = axisDirection.y * transform.forward;
            Vector3 horizontalDirection = axisDirection.x * transform.right;
            movementDirection = verticalDirection + horizontalDirection;
            _rigidbody.AddForce(movementDirection * _speed * Time.deltaTime);
            break;
          default:
            break;
        }
        Vector3 velocity = new Vector3(_rigidbody.velocity.x, 
          0, _rigidbody.velocity.z);
                _animator.SetFloat("Velocity", axisDirection.magnitude * 
          velocity.magnitude);
                _animator.SetFloat("VelocityZ", velocity.magnitude * axisDirection.y);
                _animator.SetFloat("VelocityX", velocity.magnitude * axisDirection.x);
      }
      else if(isPlayerClimbing)
      {
        Vector3 horizontal = Vector3.zero;
        Vector3 vertical = Vector3.zero;
        Vector3 checkerLeftPosition = transform.position + (transform.up * 1) + (-transform.right * .75f);
        Vector3 checkerRightPosition = transform.position + (transform.up * 1) + (transform.right * 1f);
        Vector3 checkerUpPosition = transform.position + (transform.up * 2.5f);
        Vector3 checkerDownPosition = transform.position + (-transform.up * .25f);
        bool isAbleClimbLeft = Physics.Raycast(checkerLeftPosition, transform.forward, _climbCheckDistace, _climbableLayer);
        bool isAbleClimbRight = Physics.Raycast(checkerRightPosition, transform.forward, _climbCheckDistace, _climbableLayer);
        bool isAbleClimbUp = Physics.Raycast(checkerUpPosition, transform.forward, _climbCheckDistace, _climbableLayer);
        bool isAbleClimbDown = Physics.Raycast(checkerDownPosition, transform.forward, _climbCheckDistace, _climbableLayer);
        if ((isAbleClimbLeft && (axisDirection.x < 0 )) || (isAbleClimbRight && (axisDirection.x > 0)))
        {
          horizontal = axisDirection.x * transform.right;
        }
        if ((isAbleClimbUp && (axisDirection.y > 0 )) || (isAbleClimbDown && (axisDirection.y < 0)))
        {
          vertical = axisDirection.y * transform.up;
        }
        movementDirection =  horizontal + vertical;
        _rigidbody.AddForce(movementDirection * Time.deltaTime * _climbSpeed);
        Vector3 velocity = new Vector3(_rigidbody.velocity.x, _rigidbody.velocity.y, 0 );
        _animator.SetFloat("ClimbVelocityY", velocity.magnitude * axisDirection.y);
        _animator.SetFloat("ClimbVelocityX", velocity.magnitude * axisDirection.x);
      }
      else if (isPlayerGliding)
      {
        rotationDegree.x += _glideRotationSpeed.x * axisDirection.y * Time.deltaTime;
        rotationDegree.x = Mathf.Clamp(rotationDegree.x, _minGlideRotationX, _maxGlideRotationX);
        rotationDegree.z += _glideRotationSpeed.z * axisDirection.x * Time.deltaTime;
        rotationDegree.y += _glideRotationSpeed.y * axisDirection.x * Time.deltaTime;
        transform.rotation = Quaternion.Euler(rotationDegree);
      }
  }

    private void Sprint(bool isSprint)
    {
        if (isSprint)
        {
          if (_speed < _sprintSpeed)
          {
            _speed = _speed + _walkSprintTransition * Time.deltaTime;
          }
        }
        else 
        {
          if (_speed > _walkSpeed)
          {
            _speed = _speed - _walkSprintTransition * Time.deltaTime;
          }
        }  
    }

    private void Jump()
    {
      if (_isGrounded && !_isPunching)
      {
        _animator.SetBool("IsJump", true);
        _animator.SetBool("IsJump", false);
        Vector3 jumpDirection = Vector3.up;
        _rigidbody.AddForce(jumpDirection * _jumpForce * Time.deltaTime);
      }
    }

    private void Update()
    {
      CheckIsGrounded();
      CheckStep();
      Glide();
    }

    private void StartClimb()
    {
      bool isInFrontOfClimbingWall = Physics.Raycast(_climbDetector.position, transform.forward, out RaycastHit hit, _climbCheckDistace, _climbableLayer);
      bool isNotClimbing = _playerStance != PlayerStance.Climb;
      if (isInFrontOfClimbingWall && _isGrounded && isNotClimbing)
      {
        _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
        Vector3 climbablePoint = hit.collider.bounds.ClosestPoint(transform.position);
        Vector3 direction = (climbablePoint - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);
        Vector3 offset = (transform.forward * _climbOffset.z) - (Vector3.up * _climbOffset.y);
        transform.position = hit.point - offset;
        _playerStance = PlayerStance.Climb;
        _animator.SetBool("IsClimbing", true);
        _rigidbody.useGravity = false;
        _cameraManager.SetTPSFieldOfView(70);
      }
    }

    private void CancelClimb()
    {
      if (_playerStance == PlayerStance.Climb)
      {
        _playerStance = PlayerStance.Stand;
        _rigidbody.useGravity = true;
        transform.position -= transform.forward * 1f;
        _cameraManager.SetFPSClampedCamera(false,transform.rotation.eulerAngles);
        _cameraManager.SetTPSFieldOfView(40);
        _animator.SetBool("IsClimbing", false);
        _collider.center = Vector3.up * 0.9f;
      }
    }

    private void HideAndLockCursor()
    {
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;
    }

    private void Crouch()
    {
      Vector3 checkerUpPosition = transform.position + (transform.up * 1.4f);
      bool isCantStand = Physics.Raycast(checkerUpPosition, transform.up, 0.25f, _groundLayer);
      if(_playerStance == PlayerStance.Stand)
      {
        _playerStance = PlayerStance.Crouch;
        _animator.SetBool("IsCrouch", true);
        _speed = _crouchSpeed;
      }
      else if (_playerStance == PlayerStance.Crouch && !isCantStand ) 
      {
          _playerStance = PlayerStance.Stand;
          _animator.SetBool("IsCrouch", false);
          _speed = _walkSpeed;
          _collider.height = 1.3f;
          _collider.center = Vector3.up * 0.66f;
      }
    }

    private void StartGlide()
    {
      if(_playerStance != PlayerStance.Glide && !_isGrounded)
      {
        rotationDegree = transform.rotation.eulerAngles;
        _playerStance = PlayerStance.Glide;
        _animator.SetBool("IsGliding", true);
        _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
        _playerAudioManager.PlayGlideSfx();
      }
    }

    private void CancelGlide()
    {
      if(_playerStance == PlayerStance.Glide)
      {
        _playerStance = PlayerStance.Stand;
        _animator.SetBool("IsGliding", false);
        _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
        _playerAudioManager.StopGlideSfx();
      }
    }

    private void Glide()
    {
      if(_playerStance == PlayerStance.Glide)
      {
        Vector3 playerRotation = transform.rotation.eulerAngles;
        float lift = playerRotation.x;
        Vector3 upForce = transform.up * (lift + _airDrag);
        Vector3 forwardForce = transform.forward * _glideSpeed;
        Vector3 totalForce = upForce + forwardForce;
        _rigidbody.AddForce(totalForce * Time.deltaTime);
      }
    }

    private void Punch()
{
    Debug.Log("Punch dilakukan!"); // Debugging

    if (!_isPunching && _playerStance == PlayerStance.Stand && _isGrounded)
    {
        _isPunching = true;

        if (_combo < 3)
        {
            _combo++;
        }
        else
        {
            _combo = 1;
        }

        _animator.SetInteger("Combo", _combo);
        _animator.SetTrigger("Punch");
    }
}
    private void EndPunch()
    {
      if(_resetCombo != null)
      {
        StopCoroutine(_resetCombo);
      }
      _resetCombo = StartCoroutine(ResetCombo());
      _isPunching = false;
    }

    private IEnumerator ResetCombo()
{
    yield return new WaitForSeconds(0.5f);
    _combo = 0;
    _isPunching = false;
}
    private void Hit()
    {
      Collider[] hitObjects = Physics.OverlapSphere(_hitDetector.position, _hitDetectorRadius, _hitLayer);
        for (int i = 0; i <hitObjects.Length; i++)
        {
          if (hitObjects[i].gameObject != null)
          {
            Destroy(hitObjects[i].gameObject);
          }
        }
    }
}