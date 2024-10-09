using System;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Animator))]
    internal class FootIK : MonoBehaviour
    {
        [BigHeader("Foot Properties")]

        [SerializeField] [Range(0, 0.25f)] private float lengthFromHeelToToes = 0.1f;
        [SerializeField] [Range(0, 60)] private float maxRotationAngle = 45;
        [SerializeField] [Range(-0.05f, 0.125f)] private float ankleHeightOffset;

        [BigHeader("IK Properties")]

        [SerializeField] private bool enableIKPositioning = true;
        [SerializeField] private bool enableIKRotating = true;
        [SerializeField] [Range(0, 1)] private float globalWeight = 1;
        [SerializeField] [Range(0, 1)] private float leftFootWeight = 1;
        [SerializeField] [Range(0, 1)] private float rightFootWeight = 1;
        [SerializeField] [Range(0, 0.1f)] private float smoothTime = 0.075f;

        [BigHeader("Ray Properties")]

        [SerializeField] [Range(0.05f, 0.1f)] private float raySphereRadius = 0.05f; 
        [SerializeField] [Range(0.1f, 2)] private float rayCastRange = 2;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private bool ignoreTriggers = true;

        [BigHeader("Raycast Start Heights")]

        [SerializeField] [Range(0.1f, 1)] private float leftFootRayStartHeight = 0.5f;
        [SerializeField] [Range(0.1f, 1)] private float rightFootRayStartHeight = 0.5f;

        [BigHeader("Advanced")]

        [SerializeField] private bool enableFootLifting = true;
        [ShowIf("enableFootLifting")] [SerializeField] private float floorRange;
        
        [SerializeField] private bool enableBodyPositioning = true;
        [ShowIf("enableBodyPositioning")] [SerializeField] private float crouchRange = 0.25f;
        [ShowIf("enableBodyPositioning")] [SerializeField] private float stretchRange;
        
        [SerializeField] private bool giveWorldHeightOffset;
        [ShowIf("giveWorldHeightOffset")] [SerializeField] private float worldHeightOffset;

        private float AnkleHeightOffset {
            get {
                return ankleHeightOffset;
            }
        }

        private float WorldHeightOffset {
            get {
                if (giveWorldHeightOffset)
                    return worldHeightOffset;
                
                return 0;
            }
        }
        
        private Animator _playerAnimator;
        private Transform _leftFootTransform;
        private Transform _rightFootTransform;
        private Transform _leftFootOrientationReference;
        private Transform _rightFootOrientationReference;
        private Vector3 _initialForwardVector;
        private Transform _transform;
        
        private RaycastHit _leftFootRayHitInfo;
        private RaycastHit _rightFootRayHitInfo;
        
        private float _leftFootRayHitHeight;
        private float _rightFootRayHitHeight;

        private Vector3 _leftFootRayStartPosition;
        private Vector3 _rightFootRayStartPosition;

        private Vector3 _leftFootDirectionVector;
        private Vector3 _rightFootDirectionVector;

        private Vector3 _leftFootProjectionVector;
        private Vector3 _rightFootProjectionVector;

        private float _leftFootProjectedAngle;
        private float _rightFootProjectedAngle;

        private Vector3 _leftFootRayHitProjectionVector;
        private Vector3 _rightFootRayHitProjectionVector;

        private float _leftFootRayHitProjectedAngle;
        private float _rightFootRayHitProjectedAngle;

        private float _leftFootHeightOffset;
        private float _rightFootHeightOffset;

        private Vector3 _leftFootIKPositionBuffer;
        private Vector3 _rightFootIKPositionBuffer;

        private Vector3 _leftFootIKPositionTarget;
        private Vector3 _rightFootIKPositionTarget;

        private float _leftFootHeightLerpVelocity;
        private float _rightFootHeightLerpVelocity;

        private Vector3 _leftFootIKRotationBuffer;
        private Vector3 _rightFootIKRotationBuffer;

        private Vector3 _leftFootIKRotationTarget;
        private Vector3 _rightFootIKRotationTarget;

        private Vector3 _leftFootRotationLerpVelocity;
        private Vector3 _rightFootRotationLerpVelocity;

        private void Awake()
        {
            _playerAnimator = GetComponent<Animator>();
            _transform = _playerAnimator.transform;
            
            InitializeVariables();

            CreateOrientationReference();
        }

        private void Update()
        {
            UpdateFootProjection();

            UpdateRayHitInfo();

            UpdateIKPositionTarget();
            UpdateIKRotationTarget();
        }
        
        private void OnAnimatorIK()
        {
            LerpIKBufferToTarget();

            ApplyFootIK();
            ApplyBodyIK();
        }
        
        private void InitializeVariables()
        {
            _leftFootTransform = _playerAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightFootTransform = _playerAnimator.GetBoneTransform(HumanBodyBones.RightFoot);

            // This is for faster development iteration purposes
            if (groundLayers.value == 0)
            {
                groundLayers = LayerMask.GetMask("Default");
            }

            // This is needed in order to wrangle with quaternions to get the final direction vector of each foot later
            _initialForwardVector = _transform.forward;

            // Initial value is given to make the first frames of lerping look natural, rotations should not need these
            _leftFootIKPositionBuffer.y = _transform.position.y + GetAnkleHeight();
            _rightFootIKPositionBuffer.y = _transform.position.y + GetAnkleHeight();
        }



        // This is being done to track bone orientation, since we cannot use footTransform's rotation in its own anyway
        private void CreateOrientationReference()
        {
            /* Just in case that this function gets called again... */

            if (_leftFootOrientationReference != null)
                UnityEngine.Object.Destroy(_leftFootOrientationReference);

            if (_rightFootOrientationReference != null)
                UnityEngine.Object.Destroy(_rightFootOrientationReference);

            /* These gameobjects hold different orientation values from footTransform.rotation, but the delta remains the same */

            _leftFootOrientationReference = new GameObject("[RUNTIME] Normal_Orientation_Reference").transform;
            _rightFootOrientationReference = new GameObject("[RUNTIME] Normal_Orientation_Reference").transform;

            _leftFootOrientationReference.position = _leftFootTransform.position;
            _rightFootOrientationReference.position = _rightFootTransform.position;

            _leftFootOrientationReference.SetParent(_leftFootTransform);
            _rightFootOrientationReference.SetParent(_rightFootTransform);
        }



        //This is being done because we want to know in what angle did the foot go underground
        private void UpdateFootProjection()
        {
            /* This is the only part in this script (except for those gizmos) that accesses footOrientationReference */

            _leftFootDirectionVector = _leftFootOrientationReference.rotation * _initialForwardVector;
            _rightFootDirectionVector = _rightFootOrientationReference.rotation * _initialForwardVector;

            /* World space based vector defines are used here for the representation of floor orientation */

            _leftFootProjectionVector = Vector3.ProjectOnPlane(_leftFootDirectionVector, Vector3.up);
            _rightFootProjectionVector = Vector3.ProjectOnPlane(_rightFootDirectionVector, Vector3.up);

            /* Cross is done in this order because we want the underground angle to be positive */

            _leftFootProjectedAngle = Vector3.SignedAngle(
                _leftFootProjectionVector,
                _leftFootDirectionVector,
                Vector3.Cross(_leftFootDirectionVector, _leftFootProjectionVector) *
                // This is needed to cancel out the cross product's axis inverting behaviour
                Mathf.Sign(_leftFootDirectionVector.y));

            _rightFootProjectedAngle = Vector3.SignedAngle(
                _rightFootProjectionVector,
                _rightFootDirectionVector,
                Vector3.Cross(_rightFootDirectionVector, _rightFootProjectionVector) *
                // This is needed to cancel out the cross product's axis inverting behaviour
                Mathf.Sign(_rightFootDirectionVector.y));
        }



        private void UpdateRayHitInfo()
        {
            /* Rays will be casted from above each foot, in the downward orientation of the world */

            _leftFootRayStartPosition = _leftFootTransform.position;
            _leftFootRayStartPosition.y += leftFootRayStartHeight;

            _rightFootRayStartPosition = _rightFootTransform.position;
            _rightFootRayStartPosition.y += rightFootRayStartHeight;

            /* SphereCast is used here just because we need a normal vector to rotate our foot towards */

            // Vector3.up is used here instead of transform.up to get normal vector in world orientation
            Physics.SphereCast(
                _leftFootRayStartPosition,
                raySphereRadius,
                Vector3.up * -1,
                out _leftFootRayHitInfo, rayCastRange, groundLayers,
                (QueryTriggerInteraction)(2 - Convert.ToInt32(ignoreTriggers)));

            // Vector3.up is used here instead of transform.up to get normal vector in world orientation
            Physics.SphereCast(
                _rightFootRayStartPosition,
                raySphereRadius,
                Vector3.up * -1,
                out _rightFootRayHitInfo, rayCastRange, groundLayers,
                (QueryTriggerInteraction)(2 - Convert.ToInt32(ignoreTriggers)));

            // Left Foot Ray Handling
            if (_leftFootRayHitInfo.collider != null)
            {
                _leftFootRayHitHeight = _leftFootRayHitInfo.point.y;

                /* Angle from the floor is also calculated to isolate the rotation caused by the animation */

                // We are doing this crazy operation because we only want to count rotations that are parallel to the foot
                _leftFootRayHitProjectionVector = Vector3.ProjectOnPlane(
                    _leftFootRayHitInfo.normal,
                    Vector3.Cross(_leftFootDirectionVector, _leftFootProjectionVector));

                _leftFootRayHitProjectedAngle = Vector3.Angle(
                    _leftFootRayHitProjectionVector,
                    Vector3.up);
            }
            else
            {
                _leftFootRayHitHeight = _transform.position.y;
            }

            // Right Foot Ray Handling
            if (_rightFootRayHitInfo.collider != null)
            {
                _rightFootRayHitHeight = _rightFootRayHitInfo.point.y;

                /* Angle from the floor is also calculated to isolate the rotation caused by the animation */

                // We are doing this crazy operation because we only want to count rotations that are parallel to the foot
                _rightFootRayHitProjectionVector = Vector3.ProjectOnPlane(
                    _rightFootRayHitInfo.normal,
                    Vector3.Cross(_rightFootDirectionVector, _rightFootProjectionVector));

                _rightFootRayHitProjectedAngle = Vector3.Angle(
                    _rightFootRayHitProjectionVector,
                    Vector3.up);
            }
            else
            {
                _rightFootRayHitHeight = _transform.position.y;
            }
        }
        
        private void UpdateIKPositionTarget()
        {
            /* We reset the offset values here instead of declaring them as local variables, since other functions reference it */

            _leftFootHeightOffset = 0;
            _rightFootHeightOffset = 0;

            /* Foot height correction based on the projected angle */

            float trueLeftFootProjectedAngle = _leftFootProjectedAngle - _leftFootRayHitProjectedAngle;

            if (trueLeftFootProjectedAngle > 0)
            {
                _leftFootHeightOffset += Mathf.Abs(Mathf.Sin(
                    Mathf.Deg2Rad * trueLeftFootProjectedAngle) *
                    lengthFromHeelToToes);

                // There's no Abs here to support negative manual height offset
                _leftFootHeightOffset += Mathf.Cos(
                    Mathf.Deg2Rad * trueLeftFootProjectedAngle) *
                    GetAnkleHeight();
            }
            else
            {
                _leftFootHeightOffset += GetAnkleHeight();
            }

            /* Foot height correction based on the projected angle */

            float trueRightFootProjectedAngle = _rightFootProjectedAngle - _rightFootRayHitProjectedAngle;

            if (trueRightFootProjectedAngle > 0)
            {
                _rightFootHeightOffset += Mathf.Abs(Mathf.Sin(
                    Mathf.Deg2Rad * trueRightFootProjectedAngle) *
                    lengthFromHeelToToes);

                // There's no Abs here to support negative manual height offset
                _rightFootHeightOffset += Mathf.Cos(
                    Mathf.Deg2Rad * trueRightFootProjectedAngle) *
                    GetAnkleHeight();
            }
            else
            {
                _rightFootHeightOffset += GetAnkleHeight();
            }

            /* Application of calculated position */

            _leftFootIKPositionTarget.y = _leftFootRayHitHeight + _leftFootHeightOffset + WorldHeightOffset;
            _rightFootIKPositionTarget.y = _rightFootRayHitHeight + _rightFootHeightOffset + WorldHeightOffset;
        }
        
        private void UpdateIKRotationTarget()
        {
            if (_leftFootRayHitInfo.collider != null)
            {
                _leftFootIKRotationTarget = Vector3.Slerp(
                    _transform.up,
                    _leftFootRayHitInfo.normal,
                    Mathf.Clamp(Vector3.Angle(_transform.up, _leftFootRayHitInfo.normal), 0, maxRotationAngle) /
                    // Addition of 1 is to prevent division by zero, not a perfect solution but somehow works
                    (Vector3.Angle(_transform.up, _leftFootRayHitInfo.normal) + 1));
            }
            else
                _leftFootIKRotationTarget = _transform.up;

            if (_rightFootRayHitInfo.collider != null)
            {
                _rightFootIKRotationTarget = Vector3.Slerp(
                    _transform.up,
                    _rightFootRayHitInfo.normal,
                    Mathf.Clamp(Vector3.Angle(_transform.up, _rightFootRayHitInfo.normal), 0, maxRotationAngle) /
                    // Addition of 1 is to prevent division by zero, not a perfect solution but somehow works
                    (Vector3.Angle(_transform.up, _rightFootRayHitInfo.normal) + 1));
            }
            else
                _rightFootIKRotationTarget = _transform.up;
        }
        
        private void LerpIKBufferToTarget()
        {
            /* Instead of wrangling with weights, we switch the lerp targets to make movement smooth */

            if (enableFootLifting == true &&
                _playerAnimator.GetIKPosition(AvatarIKGoal.LeftFoot).y >=
                _leftFootIKPositionTarget.y + floorRange)
            {
                _leftFootIKPositionBuffer.y = Mathf.SmoothDamp(
                    _leftFootIKPositionBuffer.y,
                    _playerAnimator.GetIKPosition(AvatarIKGoal.LeftFoot).y,
                    ref _leftFootHeightLerpVelocity,
                    smoothTime);
            }
            else 
            {
                _leftFootIKPositionBuffer.y = Mathf.SmoothDamp(
                    _leftFootIKPositionBuffer.y,
                    _leftFootIKPositionTarget.y,
                    ref _leftFootHeightLerpVelocity,
                    smoothTime);
            }
            
            if (enableFootLifting &&
                _playerAnimator.GetIKPosition(AvatarIKGoal.RightFoot).y >=
                _rightFootIKPositionTarget.y + floorRange)
            {
                _rightFootIKPositionBuffer.y = Mathf.SmoothDamp(
                    _rightFootIKPositionBuffer.y,
                    _playerAnimator.GetIKPosition(AvatarIKGoal.RightFoot).y,
                    ref _rightFootHeightLerpVelocity,
                    smoothTime);
            }
            else 
            {
                _rightFootIKPositionBuffer.y = Mathf.SmoothDamp(
                    _rightFootIKPositionBuffer.y,
                    _rightFootIKPositionTarget.y,
                    ref _rightFootHeightLerpVelocity,
                    smoothTime);
            }

            _leftFootIKRotationBuffer = Vector3.SmoothDamp(
                _leftFootIKRotationBuffer,
                _leftFootIKRotationTarget,
                ref _leftFootRotationLerpVelocity,
                smoothTime);

            _rightFootIKRotationBuffer = Vector3.SmoothDamp(
                _rightFootIKRotationBuffer,
                _rightFootIKRotationTarget,
                ref _rightFootRotationLerpVelocity,
                smoothTime);
        }
        
        private void ApplyFootIK()
        {
            /* Weight designation */

            _playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, globalWeight * leftFootWeight);
            _playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightFoot, globalWeight * rightFootWeight);
            
            _playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, globalWeight * leftFootWeight);
            _playerAnimator.SetIKRotationWeight(AvatarIKGoal.RightFoot, globalWeight * rightFootWeight);

            /* Position handling */

            CopyByAxis(ref _leftFootIKPositionBuffer, _playerAnimator.GetIKPosition(AvatarIKGoal.LeftFoot),
                true, false, true);

            CopyByAxis(ref _rightFootIKPositionBuffer, _playerAnimator.GetIKPosition(AvatarIKGoal.RightFoot),
                true, false, true);

            if (enableIKPositioning)
            {
                _playerAnimator.SetIKPosition(AvatarIKGoal.LeftFoot, _leftFootIKPositionBuffer);
                _playerAnimator.SetIKPosition(AvatarIKGoal.RightFoot, _rightFootIKPositionBuffer);
            }

            /* Rotation handling */

            /* This part may be a bit tricky to understand intuitively, refer to docs for an explanation in depth */

            // FromToRotation is used because we need the delta, not the final target orientation
            Quaternion leftFootRotation =
                Quaternion.FromToRotation(_transform.up, _leftFootIKRotationBuffer) *
                _playerAnimator.GetIKRotation(AvatarIKGoal.LeftFoot);

            // FromToRotation is used because we need the delta, not the final target orientation
            Quaternion rightFootRotation =
                Quaternion.FromToRotation(_transform.up, _rightFootIKRotationBuffer) *
                _playerAnimator.GetIKRotation(AvatarIKGoal.RightFoot);

            if (enableIKRotating)
            {
                _playerAnimator.SetIKRotation(AvatarIKGoal.LeftFoot, leftFootRotation);
                _playerAnimator.SetIKRotation(AvatarIKGoal.RightFoot, rightFootRotation);
            }
        }
        
        private void ApplyBodyIK()
        {
            if (!enableBodyPositioning)
                return;

            float minFootHeight = Mathf.Min(
                    _playerAnimator.GetIKPosition(AvatarIKGoal.LeftFoot).y,
                    _playerAnimator.GetIKPosition(AvatarIKGoal.RightFoot).y);

            /* This part moves the body 'downwards' by the root gameobject's height */

            _playerAnimator.bodyPosition = new Vector3(
            _playerAnimator.bodyPosition.x,
            _playerAnimator.bodyPosition.y +
            LimitValueByRange(minFootHeight - _transform.position.y, 0),
            _playerAnimator.bodyPosition.z);
        }
        
        private float GetAnkleHeight()
        {
            return raySphereRadius + AnkleHeightOffset;
        }
        
        private void CopyByAxis(ref Vector3 target, Vector3 source, bool copyX, bool copyY, bool copyZ)
        {
            target = new Vector3(
                Mathf.Lerp(
                    target.x,
                    source.x,
                    Convert.ToInt32(copyX)),
                Mathf.Lerp(
                    target.y,
                    source.y,
                    Convert.ToInt32(copyY)),
                Mathf.Lerp(
                    target.z,
                    source.z,
                    Convert.ToInt32(copyZ)));
        }
        
        private float LimitValueByRange(float value, float floor)
        {
            if (value < floor - stretchRange)
            {
                return value + stretchRange;
            }
            else if (value > floor + crouchRange)
            {
                return value - crouchRange;
            }
            else
            {
                return floor;
            }
        }
    }
}
