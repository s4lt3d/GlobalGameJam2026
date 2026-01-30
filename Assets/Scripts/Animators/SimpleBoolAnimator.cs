using UnityEngine;

namespace Animators
{
    public class SimpleBoolAnimator : MonoBehaviour
    {
        [SerializeField]
        private Animator playerAnimator;

        [SerializeField]
        private string animationBoolParameterName = "IsWalking";

        public void AnimateBoolMagnitude(Vector3 vector, float threshold = 0.1f)
        {
            if (playerAnimator == null)
                return;

            if (vector.sqrMagnitude > threshold)
                playerAnimator.SetBool(animationBoolParameterName, true);
            else
                playerAnimator.SetBool(animationBoolParameterName, false);
        }

        public bool AnimateBool(bool state)
        {
            if (playerAnimator == null)
                return false;

            playerAnimator.SetBool(animationBoolParameterName, state);
            return state;
        }
    }
}