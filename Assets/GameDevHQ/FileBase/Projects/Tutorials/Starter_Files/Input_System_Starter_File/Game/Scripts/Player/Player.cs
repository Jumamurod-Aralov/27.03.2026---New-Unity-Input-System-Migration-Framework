using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Scripts.LiveObjects;
using Cinemachine;
using UnityEngine.InputSystem;

namespace Game.Scripts.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class Player : MonoBehaviour
    {
        private CharacterController _controller;
        private Animator _anim;
        [SerializeField] private float _speed = 1.0f;
        private bool _playerGrounded;
        [SerializeField] private Detonator _detonator;
        private bool _canMove = true;
        [SerializeField] private CinemachineVirtualCamera _followCam;
        [SerializeField] private GameObject _model;

        // Input System
        private PlayerInput _playerInput;
        private InputActionMap _playerControlsMap;
        private InputAction _moveAction;

        private void OnEnable()
        {
            InteractableZone.onZoneInteractionComplete += InteractableZone_onZoneInteractionComplete;
            Laptop.onHackComplete += ReleasePlayerControl;
            Laptop.onHackEnded += ReturnPlayerControl;
            Forklift.onDriveModeEntered += ReleasePlayerControl;
            Forklift.onDriveModeExited += ReturnPlayerControl;
            Forklift.onDriveModeEntered += HidePlayer;
            Drone.OnEnterFlightMode += ReleasePlayerControl;
            Drone.onExitFlightmode += ReturnPlayerControl;
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            if (_controller == null)
                Debug.LogError("No Character Controller Present");

            _anim = GetComponentInChildren<Animator>();
            if (_anim == null)
                Debug.Log("Failed to connect the Animator");

            // Initialize Input System
            _playerInput = FindObjectOfType<PlayerInput>();
            if (_playerInput != null)
            {
                _playerControlsMap = _playerInput.actions.FindActionMap("Player");
                if (_playerControlsMap != null)
                {
                    _moveAction = _playerControlsMap.FindAction("Move");
                }
            }
        }

        private void Update()
        {
            if (_canMove == true)
                CalcutateMovement();
        }

        private void CalcutateMovement()
        {
            _playerGrounded = _controller.isGrounded;

            // Read movement input from Input System EVERY frame
            Vector2 moveInput = Vector2.zero;
            if (_moveAction != null)
            {
                moveInput = _moveAction.ReadValue<Vector2>();
            }

            float h = moveInput.x;
            float v = moveInput.y;

            transform.Rotate(transform.up, h);
            var direction = transform.forward * v;
            var velocity = direction * _speed;
            _anim.SetFloat("Speed", Mathf.Abs(velocity.magnitude));

            if (_playerGrounded)
                velocity.y = 0f;

            if (!_playerGrounded)
            {
                velocity.y += -20f * Time.deltaTime;
            }

            _controller.Move(velocity * Time.deltaTime);
        }

        private void InteractableZone_onZoneInteractionComplete(InteractableZone zone)
        {
            switch (zone.GetZoneID())
            {
                case 1: //place c4
                    _detonator.Show();
                    break;
                case 2: //Trigger Explosion
                    TriggerExplosive();
                    break;
            }
        }

        private void ReleasePlayerControl()
        {
            _canMove = false;
            _followCam.Priority = 9;
        }

        private void ReturnPlayerControl()
        {
            _model.SetActive(true);
            _canMove = true;
            _followCam.Priority = 10;
        }

        private void HidePlayer()
        {
            _model.SetActive(false);
        }

        private void TriggerExplosive()
        {
            _detonator.TriggerExplosion();
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= InteractableZone_onZoneInteractionComplete;
            Laptop.onHackComplete -= ReleasePlayerControl;
            Laptop.onHackEnded -= ReturnPlayerControl;
            Forklift.onDriveModeEntered -= ReleasePlayerControl;
            Forklift.onDriveModeExited -= ReturnPlayerControl;
            Forklift.onDriveModeEntered -= HidePlayer;
            Drone.OnEnterFlightMode -= ReleasePlayerControl;
            Drone.onExitFlightmode -= ReturnPlayerControl;
        }
    }
}