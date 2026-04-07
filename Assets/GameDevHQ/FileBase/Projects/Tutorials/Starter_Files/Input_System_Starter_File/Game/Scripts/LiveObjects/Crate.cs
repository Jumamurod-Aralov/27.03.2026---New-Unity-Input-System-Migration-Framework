using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.LiveObjects
{
    public class Crate : MonoBehaviour
    {
        [SerializeField] private float _punchDelay;
        [SerializeField] private GameObject _wholeCrate, _brokenCrate;
        [SerializeField] private Rigidbody[] _pieces;
        [SerializeField] private BoxCollider _crateCollider;
        [SerializeField] private InteractableZone _interactableZone;
        [SerializeField] private float _normalForce = 1f;
        [SerializeField] private float _holdForceMultiplier = 2f;
        [SerializeField] private float _holdDuration = 0.3f; // Time to consider as "hold" instead of "tap"

        private bool _isReadyToBreak = false;
        private List<Rigidbody> _brakeOff = new List<Rigidbody>();

        // NEW - Input System fields
        private PlayerInput _playerInput;
        private InputAction _punchCrateAction;
        private bool _inCrateZone = false;
        private float _holdStartTime = 0f;

        private void OnEnable()
        {
            InteractableZone.onZoneInteractionComplete += InteractableZone_onZoneInteractionComplete;

            // NEW - Initialize Input System
            _playerInput = FindObjectOfType<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("PlayerInput not found in scene!");
                return;
            }

            _punchCrateAction = _playerInput.actions["PunchCrate"];
            if (_punchCrateAction != null)
            {
                _punchCrateAction.performed += OnPunchCratePerformed;
                _punchCrateAction.canceled += OnPunchCrateCanceled;
            }
        }

        private void InteractableZone_onZoneInteractionComplete(InteractableZone zone)
        {
            if (zone.GetZoneID() == 6) // Crate zone
            {
                _inCrateZone = true;
            }

            if (_isReadyToBreak == false && _brakeOff.Count > 0)
            {
                _wholeCrate.SetActive(false);
                _brokenCrate.SetActive(true);
                _isReadyToBreak = true;
            }

            if (_isReadyToBreak && zone.GetZoneID() == 6) //Crate zone            
            {
                if (_brakeOff.Count > 0)
                {
                    BreakPart(_normalForce);
                    StartCoroutine(PunchDelay());
                }
                else if (_brakeOff.Count == 0)
                {
                    _isReadyToBreak = false;
                    _crateCollider.enabled = false;
                    _interactableZone.CompleteTask(6);
                    Debug.Log("Completely Busted");
                }
            }
        }

        private void Start()
        {
            _brakeOff.AddRange(_pieces);
        }

        // NEW - Input callback for PunchCrate action (F key) - pressed
        private void OnPunchCratePerformed(InputAction.CallbackContext context)
        {
            if (_inCrateZone && _isReadyToBreak && _brakeOff.Count > 0)
            {
                _holdStartTime = Time.time;
            }
        }

        // NEW - Input callback for PunchCrate action (F key) - released
        private void OnPunchCrateCanceled(InputAction.CallbackContext context)
        {
            if (_inCrateZone && _isReadyToBreak && _brakeOff.Count > 0)
            {
                float holdDurationTime = Time.time - _holdStartTime;

                // Determine force multiplier based on hold duration
                float forceMultiplier = holdDurationTime >= _holdDuration ? _holdForceMultiplier : 1f;

                BreakPart(forceMultiplier * _normalForce);
                StartCoroutine(PunchDelay());
            }
        }

        // OLD CODE - Commented out for legacy reference
        /*
        public void BreakPart()
        {
            int rng = Random.Range(0, _brakeOff.Count);
            _brakeOff[rng].constraints = RigidbodyConstraints.None;
            _brakeOff[rng].AddForce(new Vector3(1f, 1f, 1f), ForceMode.Force);
            _brakeOff.Remove(_brakeOff[rng]);            
        }
        */

        // NEW - Updated BreakPart with force parameter
        public void BreakPart(float forceMultiplier)
        {
            if (_brakeOff.Count == 0)
                return;

            int rng = Random.Range(0, _brakeOff.Count);
            _brakeOff[rng].constraints = RigidbodyConstraints.None;
            _brakeOff[rng].AddForce(new Vector3(1f, 1f, 1f) * forceMultiplier, ForceMode.Force);
            _brakeOff.Remove(_brakeOff[rng]);
        }

        IEnumerator PunchDelay()
        {
            float delayTimer = 0;
            while (delayTimer < _punchDelay)
            {
                yield return new WaitForEndOfFrame();
                delayTimer += Time.deltaTime;
            }
            _interactableZone.ResetAction(6);
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= InteractableZone_onZoneInteractionComplete;

            // NEW - Unsubscribe from input callbacks
            if (_punchCrateAction != null)
            {
                _punchCrateAction.performed -= OnPunchCratePerformed;
                _punchCrateAction.canceled -= OnPunchCrateCanceled;
            }
        }
    }
}