using UnityEngine;

public class CheckAnimationPlayback : StateMachineBehaviour
{
    [SerializeField] private string nameBoolParameter;
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(nameBoolParameter, true);
    }
    
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool(nameBoolParameter, false);
    }
}
