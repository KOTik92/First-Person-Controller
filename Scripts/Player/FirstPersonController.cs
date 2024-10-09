using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private MovementController movementController = new MovementController();
        [SerializeField] private CameraController cameraController = new CameraController();

        private bool _isLanding;

        private void Awake()
        {
            movementController.Init(GetComponent<CharacterController>());
            cameraController.Init(transform);

            movementController.OnLanding += SetLanding;
        }

        private void OnDisable()
        {
            movementController.OnLanding -= SetLanding;
        }

        private void Update()
        {
            cameraController.CameraToTarget();
            
            if(_isLanding)
                return;
            
            movementController.Update();
            cameraController.Rotate();
        }

        private void SetLanding(bool isLanding)
        {
            _isLanding = isLanding;
        }
    }
}
