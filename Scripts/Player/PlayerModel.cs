using System;
using UnityEngine;

namespace Player
{
    [Serializable]
    internal class PlayerModel
    {
        [SerializeField] private Animator animator;

        internal bool IsLanding => animator.GetBool("IsLanding");
        
        private float _mouseX;

        internal void MotionAnimation(Vector3 _vec, bool sprint)
        {
            animator.SetBool("isSprinting", sprint);

            animator.SetFloat("X", _vec.x);
            animator.SetFloat("Y", _vec.y);

            float mouseX = Inputs.MouseX;
            _mouseX = Mathf.Lerp(_mouseX, mouseX, 10 * Time.deltaTime);
            animator.SetFloat("MouseX", _mouseX);
        }

        internal void SetFlight(bool isFlight)
        {
            animator.SetBool("Flight", isFlight);
        }
        
        internal void SetBoolAnimation(string nameAnim, bool isActivate)
        {
            animator.SetBool(nameAnim, isActivate);
        }
        
        internal void SetTriggerAnimation(string nameAnim)
        {
            animator.SetTrigger(nameAnim);
        }
    }
}
