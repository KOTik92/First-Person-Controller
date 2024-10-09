using System;
using UnityEngine;

namespace Player
{
    [Serializable]
    public class CameraController
    {
        [SerializeField] private Camera characterCamera;
        [SerializeField] private Transform targetCamera;
        [Space]
        [BigHeader("Camera settings")]
        [SerializeField] private bool lockCursor;
        [SerializeField] private Vector2 pitchLimitVertical;
        [SerializeField] private Vector2 pitchLimitHorizontal;
        [SerializeField] private Vector2 sensitivity;

        internal float FOV
        {
            set
            {
                characterCamera.fieldOfView = value;
            }
        }
        
        private Transform _transform;
        private Vector2 look = Vector2.zero;
        private Vector2 _pitchLimitVertical;
        private Vector2 _pitchLimitHorizontal;

        internal void Init(Transform transform)
        {
            _transform = transform;
            _pitchLimitVertical = pitchLimitVertical;
            _pitchLimitHorizontal = pitchLimitHorizontal;

            SetCursorState(lockCursor);
        }

        internal void CameraToTarget()
        {
            characterCamera.transform.position = targetCamera.position;
            characterCamera.transform.eulerAngles = new Vector3(characterCamera.transform.eulerAngles.x,characterCamera.transform.eulerAngles.y,targetCamera.eulerAngles.z);
        }

        internal void Rotate()
        {
            look.x = Inputs.MouseX * sensitivity.x;
            look.y -= Inputs.MouseY * sensitivity.y;

            look.x = Mathf.Clamp(look.x, _pitchLimitHorizontal.x, _pitchLimitHorizontal.y);
            look.y = Mathf.Clamp(look.y, _pitchLimitVertical.x, _pitchLimitVertical.y);

            characterCamera.transform.localRotation = Quaternion.Euler(look.y, 0, 0);
            _transform.rotation *= Quaternion.Euler(_transform.eulerAngles.x, look.x, _transform.eulerAngles.z);
        }
        
        internal void SetCursorState(bool state)
        {
            Cursor.lockState = state ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !state;
        }

        internal void SetCameraRotationRestriction(Vector2 verticalValueRange, Vector2 horizontalValueRange)
        {
            Quaternion tempRotation = characterCamera.transform.rotation;

            characterCamera.transform.rotation = tempRotation;
        }
        
        internal void ResetCameraRotationRestriction()
        {
            Quaternion tempRotation = characterCamera.transform.rotation;

            characterCamera.transform.rotation = tempRotation;
        }
    }
}
