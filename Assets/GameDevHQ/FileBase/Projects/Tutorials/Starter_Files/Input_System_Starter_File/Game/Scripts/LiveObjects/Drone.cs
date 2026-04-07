using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Game.Scripts.UI;
using UnityEngine.InputSystem;

namespace Game.Scripts.LiveObjects
{
    public class Drone : MonoBehaviour
    {
        private enum Tilt
        {
            NoTilt, Forward, Back, Left, Right
        }

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private float _speed = 5f;
        private bool _inFlightMode = false;
        [SerializeField] private Animator _propAnim;
        [SerializeField] private CinemachineVirtualCamera _droneCam;
        [SerializeField] private InteractableZone _interactableZone;

        // NEW - Input System fields
        private PlayerInput _playerInput;
        private InputActionMap _droneControlsMap;
        private InputActionMap _playerControlsMap;

        private InputAction _moveAction;
        private InputAction _rotateAction;
        private InputAction _thrustAction;
        private InputAction _descentAction;
        private InputAction _exitAction;

        // Input values
        private Vector2 _moveInput;
        private float _rotateInput;
        private float _thrustInput;
        private float _descentInput;

        public static event Action OnEnterFlightMode;
        public static event Action onExitFlightmode;

        private void OnEnable()
        {
            InteractableZone.onZoneInteractionComplete += EnterFlightMode;

            // NEW - Initialize Input System
            _playerInput = FindObjectOfType<PlayerInput>();
            if (_playerInput != null)
            {
                _droneControlsMap = _playerInput.actions.FindActionMap("DroneControls");
                _playerControlsMap = _playerInput.actions.FindActionMap("Player");

                if (_droneControlsMap != null)
                {
                    _moveAction = _droneControlsMap.FindAction("Move");
                    _rotateAction = _droneControlsMap.FindAction("Rotate");
                    _thrustAction = _droneControlsMap.FindAction("Thrust");
                    _descentAction = _droneControlsMap.FindAction("Descent");
                    _exitAction = _droneControlsMap.FindAction("Exit");

                    // Subscribe to input callbacks
                    if (_moveAction != null)
                        _moveAction.performed += OnMovePerformed;
                    if (_rotateAction != null)
                        _rotateAction.performed += OnRotatePerformed;
                    if (_thrustAction != null)
                        _thrustAction.performed += OnThrustPerformed;
                    if (_descentAction != null)
                        _descentAction.performed += OnDescentPerformed;
                    if (_exitAction != null)
                        _exitAction.performed += OnExitPerformed;
                }
            }
        }

        private void EnterFlightMode(InteractableZone zone)
        {
            if (_inFlightMode != true && zone.GetZoneID() == 4) // drone Scene
            {
                _propAnim.SetTrigger("StartProps");
                _droneCam.Priority = 11;
                _inFlightMode = true;
                OnEnterFlightMode?.Invoke();
                UIManager.Instance.DroneView(true);
                _interactableZone.CompleteTask(4);

                // NEW - Switch input maps
                if (_playerInput != null)
                {
                    _playerControlsMap.Disable();
                    _droneControlsMap.Enable();
                }
            }
        }

        private void ExitFlightMode()
        {
            _droneCam.Priority = 9;
            _inFlightMode = false;
            UIManager.Instance.DroneView(false);

            // NEW - Switch back to player controls
            if (_playerInput != null)
            {
                _droneControlsMap.Disable();
                _playerControlsMap.Enable();
            }
        }

        private void Update()
        {
            if (_inFlightMode)
            {
                CalculateTilt();
                CalculateMovementUpdate();
            }
        }

        private void FixedUpdate()
        {
            _rigidbody.AddForce(transform.up * (9.81f), ForceMode.Acceleration);
            if (_inFlightMode)
                CalculateMovementFixedUpdate();
        }

        // NEW - Input callback for Move action (WASD)
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        // NEW - Input callback for Rotate action (R and T keys)
        private void OnRotatePerformed(InputAction.CallbackContext context)
        {
            _rotateInput = context.ReadValue<float>(); // Single axis
        }

        // NEW - Input callback for Thrust action (Space)
        private void OnThrustPerformed(InputAction.CallbackContext context)
        {
            _thrustInput = context.ReadValue<float>();
        }

        // NEW - Input callback for Descent action (V)
        private void OnDescentPerformed(InputAction.CallbackContext context)
        {
            _descentInput = context.ReadValue<float>();
        }

        // NEW - Input callback for Exit action (Escape)
        private void OnExitPerformed(InputAction.CallbackContext context)
        {
            if (_inFlightMode)
            {
                _inFlightMode = false;
                onExitFlightmode?.Invoke();
                ExitFlightMode();
            }
        }

        private void CalculateMovementUpdate()
        {
            // OLD CODE - Commented out for legacy reference
            /*
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                var tempRot = transform.localRotation.eulerAngles;
                tempRot.y -= _speed / 3;
                transform.localRotation = Quaternion.Euler(tempRot);
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                var tempRot = transform.localRotation.eulerAngles;
                tempRot.y += _speed / 3;
                transform.localRotation = Quaternion.Euler(tempRot);
            }
            */

            // NEW - Use Rotate input (R = negative, T = positive on Y axis)
            if (_rotateInput < 0) // R key - rotate left
            {
                var tempRot = transform.localRotation.eulerAngles;
                tempRot.y -= _speed / 3;
                transform.localRotation = Quaternion.Euler(tempRot);
            }
            if (_rotateInput > 0) // T key - rotate right
            {
                var tempRot = transform.localRotation.eulerAngles;
                tempRot.y += _speed / 3;
                transform.localRotation = Quaternion.Euler(tempRot);
            }
        }

        private void CalculateMovementFixedUpdate()
        {
            // OLD CODE - Commented out for legacy reference
            /*
            if (Input.GetKey(KeyCode.Space))
            {
                _rigidbody.AddForce(transform.up * _speed, ForceMode.Acceleration);
            }
            if (Input.GetKey(KeyCode.V))
            {
                _rigidbody.AddForce(-transform.up * _speed, ForceMode.Acceleration);
            }
            */

            // NEW - Use Thrust and Descent inputs from InputActions
            if (_thrustInput > 0)
            {
                _rigidbody.AddForce(transform.up * _speed, ForceMode.Acceleration);
            }
            if (_descentInput > 0)
            {
                _rigidbody.AddForce(-transform.up * _speed, ForceMode.Acceleration);
            }
        }

        private void CalculateTilt()
        {
            // OLD CODE - Commented out for legacy reference
            /*
            if (Input.GetKey(KeyCode.A)) 
                transform.rotation = Quaternion.Euler(00, transform.localRotation.eulerAngles.y, 30);
            else if (Input.GetKey(KeyCode.D))
                transform.rotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, -30);
            else if (Input.GetKey(KeyCode.W))
                transform.rotation = Quaternion.Euler(30, transform.localRotation.eulerAngles.y, 0);
            else if (Input.GetKey(KeyCode.S))
                transform.rotation = Quaternion.Euler(-30, transform.localRotation.eulerAngles.y, 0);
            else 
                transform.rotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, 0);
            */

            // NEW - Use Move input from InputAction (WASD)
            if (_moveInput.x < 0) // A key
                transform.rotation = Quaternion.Euler(00, transform.localRotation.eulerAngles.y, 30);
            else if (_moveInput.x > 0) // D key
                transform.rotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, -30);
            else if (_moveInput.y > 0) // W key
                transform.rotation = Quaternion.Euler(30, transform.localRotation.eulerAngles.y, 0);
            else if (_moveInput.y < 0) // S key
                transform.rotation = Quaternion.Euler(-30, transform.localRotation.eulerAngles.y, 0);
            else
                transform.rotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, 0);
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= EnterFlightMode;

            // NEW - Unsubscribe from input callbacks
            if (_moveAction != null)
                _moveAction.performed -= OnMovePerformed;
            if (_rotateAction != null)
                _rotateAction.performed -= OnRotatePerformed;
            if (_thrustAction != null)
                _thrustAction.performed -= OnThrustPerformed;
            if (_descentAction != null)
                _descentAction.performed -= OnDescentPerformed;
            if (_exitAction != null)
                _exitAction.performed -= OnExitPerformed;
        }
    }
}