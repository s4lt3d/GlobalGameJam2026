using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

public class TransitionDancerAnimation : MonoBehaviour
{
    [SerializeField]
    private Animator lead;

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            SayNo();
        
        if (Mouse.current.rightButton.wasPressedThisFrame)
            StartDancing();
    }


    public void SayNo()
    {
        lead.SetTrigger("StartNo");
    }

    public void StartDancing()
    {
        lead.SetTrigger("StartDance");
    }
}
