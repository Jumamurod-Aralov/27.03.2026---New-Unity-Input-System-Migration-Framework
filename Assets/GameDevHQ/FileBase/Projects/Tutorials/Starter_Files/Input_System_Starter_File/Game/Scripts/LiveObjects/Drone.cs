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

                    // Subscribe to input callbacks for Thrust and Descent (these need started/canceled)
                    if (_thrustAction != null)
                    {
                        _thrustAction.started += OnThrustStarted;
                        _thrustAction.canceled += OnThrustCanceled;
                    }

                    if (_descentAction != null)
                    {
                        _descentAction.started += OnDescentStarted;
                        _descentAction.canceled += OnDescentCanceled;
                    }

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
            {
                CalculateMovementFixedUpdate();
            }
        }

        // NEW - Input callback for Thrust action (Space) - started
        private void OnThrustStarted(InputAction.CallbackContext context)
        {
            _thrustInput = 1f;
        }

        // NEW - Input callback for Thrust action (Space) - canceled
        private void OnThrustCanceled(InputAction.CallbackContext context)
        {
            _thrustInput = 0f;
        }

        // NEW - Input callback for Descent action (V) - started
        private void OnDescentStarted(InputAction.CallbackContext context)
        {
            _descentInput = 1f;
        }

        // NEW - Input callback for Descent action (V) - canceled
        private void OnDescentCanceled(InputAction.CallbackContext context)
        {
            _descentInput = 0f;
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
            // Read rotation input EVERY frame (ARROW KEYS ONLY)
            _rotateInput = _rotateAction.ReadValue<float>();

            // LEFT/RIGHT ARROW KEYS - Rotate Y axis only (SPIN IN PLACE)
            // NO TILT - purely yaw rotation
            if (_rotateInput < 0) // Left Arrow - rotate left
            {
                var tempRot = transform.localRotation.eulerAngles;
                tempRot.y -= _speed / 3;
                transform.localRotation = Quaternion.Euler(tempRot);
            }
            if (_rotateInput > 0) // Right Arrow - rotate right
            {
                var tempRot = transform.localRotation.eulerAngles;
                tempRot.y += _speed / 3;
                transform.localRotation = Quaternion.Euler(tempRot);
            }
        }

        private void CalculateMovementFixedUpdate()
        {
            // Space = Ascend, V = Descend (continuous)
            // Only vertical movement with Space/V - no horizontal movement
            if (_thrustInput > 0)
            {
                _rigidbody.AddForce(transform.up * _speed, ForceMode.Acceleration);
            }
            if (_descentInput > 0)
            {
                _rigidbody.AddForce(-transform.up * _speed, ForceMode.Acceleration);
            }

            // Dampen horizontal velocity when no WASD input to keep drone stable
            _moveInput = _moveAction.ReadValue<Vector2>();
            if (_moveInput.magnitude == 0)
            {
                // Remove horizontal velocity to keep drone stable
                Vector3 vel = _rigidbody.velocity;
                vel.x *= 0.95f; // Dampen X velocity
                vel.z *= 0.95f; // Dampen Z velocity
                _rigidbody.velocity = vel;
            }
        }

        private void CalculateTilt()
        {
            // Read WASD movement input EVERY frame
            _moveInput = _moveAction.ReadValue<Vector2>();

            // Target tilt angles (will return to 0 if no input)
            float targetX = 0;  // X axis = forward/backward tilt
            float targetZ = 0;  // Z axis = left/right tilt

            // WASD keys control TILT ONLY (not actual movement)
            if (_moveInput.x < 0) // A key - tilt left
            {
                targetZ = 30;
            }
            else if (_moveInput.x > 0) // D key - tilt right
            {
                targetZ = -30;
            }

            if (_moveInput.y > 0) // W key - tilt forward
            {
                targetX = 30;
            }
            else if (_moveInput.y < 0) // S key - tilt backward
            {
                targetX = -30;
            }

            // Smoothly transition to target tilt rotation
            var currentRot = transform.localRotation.eulerAngles;
            float smoothSpeed = 8f; // Increased for faster stabilization when releasing keys

            // Normalize angles to -180 to 180 range for proper Lerp
            float currentX = NormalizeAngle(currentRot.x);
            float currentZ = NormalizeAngle(currentRot.z);

            float newX = Mathf.Lerp(currentX, targetX, Time.deltaTime * smoothSpeed);
            float newZ = Mathf.Lerp(currentZ, targetZ, Time.deltaTime * smoothSpeed);
            float newY = currentRot.y; // Y rotation ONLY changes with arrow keys - NEVER modified here

            // Clamp to prevent drift and ensure perfect neutral when close to 0
            if (Mathf.Abs(newX) < 0.1f) newX = 0f;
            if (Mathf.Abs(newZ) < 0.1f) newZ = 0f;

            transform.localRotation = Quaternion.Euler(newX, newY, newZ);
        }

        // Helper function to normalize angles to -180 to 180 range
        private float NormalizeAngle(float angle)
        {
            while (angle > 180) angle -= 360;
            while (angle < -180) angle += 360;
            return angle;
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= EnterFlightMode;

            // NEW - Unsubscribe from input callbacks
            if (_thrustAction != null)
            {
                _thrustAction.started -= OnThrustStarted;
                _thrustAction.canceled -= OnThrustCanceled;
            }

            if (_descentAction != null)
            {
                _descentAction.started -= OnDescentStarted;
                _descentAction.canceled -= OnDescentCanceled;
            }

            if (_exitAction != null)
                _exitAction.performed -= OnExitPerformed;
        }
    }
}