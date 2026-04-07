using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

namespace Game.Scripts.LiveObjects
{
    public class Forklift : MonoBehaviour
    {
        [SerializeField]
        private GameObject _lift, _steeringWheel, _leftWheel, _rightWheel, _rearWheels;
        [SerializeField]
        private Vector3 _liftLowerLimit, _liftUpperLimit;
        [SerializeField]
        private float _speed = 5f, _liftSpeed = 1f;
        [SerializeField]
        private CinemachineVirtualCamera _forkliftCam;
        [SerializeField]
        private GameObject _driverModel;
        private bool _inDriveMode = false;
        [SerializeField]
        private InteractableZone _interactableZone;

        // NEW - Input System fields
        private PlayerInput _playerInput;
        private InputActionMap _forkliftControlsMap;
        private InputActionMap _playerControlsMap;

        private InputAction _moveAction;
        private InputAction _liftUpAction;
        private InputAction _liftDownAction;
        private InputAction _exitAction;

        // Input values
        private Vector2 _moveInput;
        private float _liftUpInput;
        private float _liftDownInput;

        public static event Action onDriveModeEntered;
        public static event Action onDriveModeExited;

        private void OnEnable()
        {
            InteractableZone.onZoneInteractionComplete += EnterDriveMode;

            // NEW - Initialize Input System
            _playerInput = FindObjectOfType<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("PlayerInput not found in scene!");
                return;
            }

            _forkliftControlsMap = _playerInput.actions.FindActionMap("ForkliftControls");
            _playerControlsMap = _playerInput.actions.FindActionMap("Player");

            if (_forkliftControlsMap != null)
            {
                _moveAction = _forkliftControlsMap.FindAction("Move");
                _liftUpAction = _forkliftControlsMap.FindAction("LiftUp");
                _liftDownAction = _forkliftControlsMap.FindAction("LiftDown");
                _exitAction = _forkliftControlsMap.FindAction("Exit");

                // Subscribe to input callbacks
                if (_moveAction != null)
                    _moveAction.performed += OnMovePerformed;
                if (_liftUpAction != null)
                    _liftUpAction.performed += OnLiftUpPerformed;
                if (_liftDownAction != null)
                    _liftDownAction.performed += OnLiftDownPerformed;
                if (_exitAction != null)
                    _exitAction.performed += OnExitPerformed;
            }
        }

        private void EnterDriveMode(InteractableZone zone)
        {
            if (_inDriveMode != true && zone.GetZoneID() == 5) //Enter ForkLift
            {
                _inDriveMode = true;
                _forkliftCam.Priority = 11;
                onDriveModeEntered?.Invoke();
                _driverModel.SetActive(true);
                _interactableZone.CompleteTask(5);

                // NEW - Switch input maps
                if (_playerInput != null)
                {
                    _playerControlsMap.Disable();
                    _forkliftControlsMap.Enable();
                }
            }
        }

        private void ExitDriveMode()
        {
            _inDriveMode = false;
            _forkliftCam.Priority = 9;
            _driverModel.SetActive(false);
            onDriveModeExited?.Invoke();

            // NEW - Switch back to player controls
            if (_playerInput != null)
            {
                _forkliftControlsMap.Disable();
                _playerControlsMap.Enable();
            }
        }

        private void Update()
        {
            if (_inDriveMode == true)
            {
                LiftControls();
                CalcutateMovement();
            }
        }

        // OLD CODE - Commented out for legacy reference
        /*
        private void CalcutateMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            var direction = new Vector3(0, 0, v);
            var velocity = direction * _speed;
            transform.Translate(velocity * Time.deltaTime);
            if (Mathf.Abs(v) > 0)
            {
                var tempRot = transform.rotation.eulerAngles;
                tempRot.y += h * _speed / 2;
                transform.rotation = Quaternion.Euler(tempRot);
            }
        }
        */

        // NEW - Input callback for Move action (WASD)
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        // NEW - Input callback for LiftUp action (R)
        private void OnLiftUpPerformed(InputAction.CallbackContext context)
        {
            _liftUpInput = context.ReadValue<float>();
        }

        // NEW - Input callback for LiftDown action (T)
        private void OnLiftDownPerformed(InputAction.CallbackContext context)
        {
            _liftDownInput = context.ReadValue<float>();
        }

        // NEW - Input callback for Exit action (Escape)
        private void OnExitPerformed(InputAction.CallbackContext context)
        {
            if (_inDriveMode)
            {
                ExitDriveMode();
            }
        }

        // NEW - Updated movement calculation using input values
        private void CalcutateMovement()
        {
            float h = _moveInput.x; // Horizontal (A/D)
            float v = _moveInput.y; // Vertical (W/S)

            var direction = new Vector3(0, 0, v);
            var velocity = direction * _speed;
            transform.Translate(velocity * Time.deltaTime);

            if (Mathf.Abs(v) > 0)
            {
                var tempRot = transform.rotation.eulerAngles;
                tempRot.y += h * _speed / 2;
                transform.rotation = Quaternion.Euler(tempRot);
            }
        }

        // OLD CODE - Commented out for legacy reference
        /*
        private void LiftControls()
        {
            if (Input.GetKey(KeyCode.R))
                LiftUpRoutine();
            else if (Input.GetKey(KeyCode.T))
                LiftDownRoutine();
        }
        */

        // NEW - Updated lift controls using input values
        private void LiftControls()
        {
            if (_liftUpInput > 0)
                LiftUpRoutine();
            else if (_liftDownInput > 0)
                LiftDownRoutine();
        }

        private void LiftUpRoutine()
        {
            if (_lift.transform.localPosition.y < _liftUpperLimit.y)
            {
                Vector3 tempPos = _lift.transform.localPosition;
                tempPos.y += Time.deltaTime * _liftSpeed;
                _lift.transform.localPosition = new Vector3(tempPos.x, tempPos.y, tempPos.z);
            }
            else if (_lift.transform.localPosition.y >= _liftUpperLimit.y)
                _lift.transform.localPosition = _liftUpperLimit;
        }

        private void LiftDownRoutine()
        {
            if (_lift.transform.localPosition.y > _liftLowerLimit.y)
            {
                Vector3 tempPos = _lift.transform.localPosition;
                tempPos.y -= Time.deltaTime * _liftSpeed;
                _lift.transform.localPosition = new Vector3(tempPos.x, tempPos.y, tempPos.z);
            }
            else if (_lift.transform.localPosition.y <= _liftUpperLimit.y)
                _lift.transform.localPosition = _liftLowerLimit;
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= EnterDriveMode;

            // NEW - Unsubscribe from input callbacks
            if (_moveAction != null)
                _moveAction.performed -= OnMovePerformed;
            if (_liftUpAction != null)
                _liftUpAction.performed -= OnLiftUpPerformed;
            if (_liftDownAction != null)
                _liftDownAction.performed -= OnLiftDownPerformed;
            if (_exitAction != null)
                _exitAction.performed -= OnExitPerformed;
        }
    }
}