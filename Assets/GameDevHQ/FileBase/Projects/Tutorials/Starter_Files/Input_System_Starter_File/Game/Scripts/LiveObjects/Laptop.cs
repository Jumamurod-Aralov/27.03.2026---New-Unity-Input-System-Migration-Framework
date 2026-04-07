using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Cinemachine;

namespace Game.Scripts.LiveObjects
{
    public class Laptop : MonoBehaviour
    {
        [SerializeField] private Slider _progressBar;
        [SerializeField] private int _hackTime = 5;
        private bool _hacked = false;
        [SerializeField] private CinemachineVirtualCamera[] _cameras;
        private int _activeCamera = 0;
        [SerializeField] private InteractableZone _interactableZone;

        // NEW - Input System fields
        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private InputAction _exitHackAction;

        public static event Action onHackComplete;
        public static event Action onHackEnded;

        private void OnEnable()
        {
            InteractableZone.onHoldStarted += InteractableZone_onHoldStarted;
            InteractableZone.onHoldEnded += InteractableZone_onHoldEnded;

            // NEW - Initialize Input System
            _playerInput = FindObjectOfType<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("PlayerInput not found in scene!");
                return;
            }

            _interactAction = _playerInput.actions["Interact"];
            if (_interactAction != null)
            {
                _interactAction.performed += OnInteractPerformed;
            }

            _exitHackAction = _playerInput.actions["ExitHack"];
            if (_exitHackAction != null)
            {
                _exitHackAction.performed += OnExitHackPerformed;
            }
        }

        // OLD CODE - Commented out for legacy reference
        /*
        private void Update()
        {
            if (_hacked == true)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    var previous = _activeCamera;
                    _activeCamera++;
                    if (_activeCamera >= _cameras.Length)
                        _activeCamera = 0;
                    _cameras[_activeCamera].Priority = 11;
                    _cameras[previous].Priority = 9;
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _hacked = false;
                    onHackEnded?.Invoke();
                    ResetCameras();
                }
            }
        }
        */

        // NEW - Input System callback for Interact action (E key)
        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            // Only cycle camera if hack is complete and in hacked state
            if (_hacked == true)
            {
                var previous = _activeCamera;
                _activeCamera++;
                if (_activeCamera >= _cameras.Length)
                    _activeCamera = 0;
                _cameras[_activeCamera].Priority = 11;
                _cameras[previous].Priority = 9;
            }
        }

        void ResetCameras()
        {
            foreach (var cam in _cameras)
            {
                cam.Priority = 9;
            }
        }

        private void InteractableZone_onHoldStarted(int zoneID)
        {
            if (zoneID == 3 && _hacked == false) //Hacking terminal
            {
                _progressBar.gameObject.SetActive(true);
                StartCoroutine(HackingRoutine());
                onHackComplete?.Invoke();
            }
        }

        private void InteractableZone_onHoldEnded(int zoneID)
        {
            if (zoneID == 3) //Hacking terminal
            {
                if (_hacked == true)
                    return;
                StopAllCoroutines();
                _progressBar.gameObject.SetActive(false);
                _progressBar.value = 0;
                onHackEnded?.Invoke();
            }
        }

        IEnumerator HackingRoutine()
        {
            while (_progressBar.value < 1)
            {
                _progressBar.value += Time.deltaTime / _hackTime;
                yield return new WaitForEndOfFrame();
            }
            //successfully hacked
            _hacked = true;
            _interactableZone.CompleteTask(3);
            //hide progress bar
            _progressBar.gameObject.SetActive(false);
            //enable Vcam1
            _cameras[0].Priority = 11;
        }

        // NEW - Input callback for ExitHack action (Q key)
        private void OnExitHackPerformed(InputAction.CallbackContext context)
        {
            if (_hacked)
            {
                _hacked = false;
                onHackEnded?.Invoke();
                ResetCameras();
            }
        }

        private void OnDisable()
        {
            InteractableZone.onHoldStarted -= InteractableZone_onHoldStarted;
            InteractableZone.onHoldEnded -= InteractableZone_onHoldEnded;

            // NEW - Unsubscribe from input callbacks
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
            }

            if (_exitHackAction != null)
            {
                _exitHackAction.performed -= OnExitHackPerformed;
            }
        }
    }
}