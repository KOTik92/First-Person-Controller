using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Player
{
    [Serializable]
    internal class MovementController
    {
        [SerializeField] private PlayerModel playerModel = new PlayerModel();
        [Space]
        [SerializeField] private float gravityMultiplier;
        [Space]
        [BigHeader("Movement settings")]
        [SerializeField] private bool canMove;
        [SerializeField] private bool canRun;
        [SerializeField] private float smoothingMovement;
        [Header("Speeds")]
        [SerializeField] private float walkForwardSpeed;
        [SerializeField] private float walkBackwardSpeed;
        [SerializeField] private float walkSidewaysSpeed; 
        [Space]
        [SerializeField] private float runSpeed;
        [SerializeField] private float offsetSidewaysRunSpeed;
        [Space]
        [BigHeader("Falling")]
        [SerializeField] private bool canFalling = true;
        [ShowIf("canFalling")] 
        [SerializeField, Range(0, 10)] private float minFalling; 
        [ShowIf("canFalling")]
        [SerializeField] private float minHeightForFalling;
        [ShowIf("canFalling")]
        [SerializeField] private string animLandingName;
        [ShowIf("canFalling")]
        [SerializeField] private string animLandingSmallName;
        [Space]
        [BigHeader("Checking for collisions witch obstacles")] 
        [SerializeField] private bool canCollisionCheck;
        [ShowIf("canCollisionCheck")]
        [SerializeField] private Transform raycastOutputPoint;
        [ShowIf("canCollisionCheck")]
        [SerializeField] private float lengthRaycast;
        [ShowIf("canCollisionCheck")]
        [SerializeField] private LayerMask layerMask;

        internal event Action<bool> OnLanding;
        
        private bool _isMoving;
        private bool _isRunning;
        private bool _isGrounded;
        private Vector3 _velocity;
        private CharacterController _characterController;
        private Vector3 _lastPos;
        private bool _isCollisionWithObstacle;
        private float _currentSpeed;
        
        internal void Init(CharacterController characterController)
        {
            _characterController = characterController;
        }

        internal void Update()
        {
            if(!canMove)
                return;
            
            Move();
            CheckFalling();
        }
        
        private void Move()
        {
            float horizontal = Inputs.Horizontal;
            float vertical = Inputs.Vertical;

            _isGrounded = _characterController.isGrounded;
            _isMoving = _velocity.sqrMagnitude > 0.01f;
            _isRunning = canRun && Inputs.IsLeftShift && _isMoving && vertical > 0;

            Vector3 direction = new Vector3();
            direction = (_characterController.transform.forward * vertical) + (_characterController.transform.right * horizontal);
            
            GetSpeed(out _currentSpeed, horizontal, vertical);

            if (_isGrounded)
                _velocity = direction * _currentSpeed;

            if (_isGrounded)
                _velocity.y = -0.1f;
            else 
                _velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
            
            _characterController.Move(_velocity * Time.deltaTime);
            playerModel.MotionAnimation(new Vector3(horizontal * _currentSpeed, vertical * _currentSpeed, 0), _isRunning);

            if (_isMoving && canCollisionCheck)
            {
                CheckingCollisionsWitchObstacles(direction);
            }
        }

        private void GetSpeed(out float currentSpeed, float horizontal, float vertical)
        {
            float tempWalkSpeed = vertical < 0 && horizontal != 0 ? 
                walkBackwardSpeed : horizontal != 0 ? 
                    walkSidewaysSpeed : vertical > 0 ? 
                        walkForwardSpeed : walkBackwardSpeed;

            float tempRunSpeed = horizontal != 0 ? runSpeed / offsetSidewaysRunSpeed : runSpeed;

            float speed = _isRunning ? tempRunSpeed : tempWalkSpeed;
            float speedWithCollisionCheck = !_isCollisionWithObstacle ? speed : 0;
            
            currentSpeed = Mathf.MoveTowards(_currentSpeed, speedWithCollisionCheck, (smoothingMovement * speed) * Time.deltaTime);
        }
        
        private void CheckFalling()
        {
            if (!canFalling) return;

            if (_velocity.y < -minFalling & _lastPos == Vector3.zero & !_isGrounded)
            {
                _lastPos = _characterController.transform.position;
                playerModel.SetFlight(true);
            }

            if (_isGrounded & _lastPos != Vector3.zero)
            {
                playerModel.SetFlight(false);
                float height = _lastPos.y - _characterController.transform.position.y;
                _lastPos = Vector3.zero;

                if (height >= minHeightForFalling)
                {
                    Coroutines.StartRoutine(Landing(_isRunning ? animLandingName : animLandingSmallName));
                }
            }
        }

        private void CheckingCollisionsWitchObstacles(Vector3 direction)
        {
            Ray ray = new Ray(raycastOutputPoint.position, direction);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, lengthRaycast, layerMask))
            {
                if (hit.collider != null)
                {
                    _isCollisionWithObstacle = true;
                }
            }
            else
            {
                _isCollisionWithObstacle = false;
            }

            Debug.DrawRay(raycastOutputPoint.position, direction, Color.red, lengthRaycast);
        }
        
        private IEnumerator Landing(string nameAnim)
        {
            OnLanding?.Invoke(true);
            playerModel.SetTriggerAnimation(nameAnim);

            yield return null;
            yield return new WaitUntil(() => !playerModel.IsLanding);

            OnLanding?.Invoke(false);
        }
    }
}
