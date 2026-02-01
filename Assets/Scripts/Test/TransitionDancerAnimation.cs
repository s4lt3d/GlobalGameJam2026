using System;
using Puzzles;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

public class TransitionDancerAnimation : MonoBehaviour
{
    [SerializeField]
    private Animator lead;
    
    private TentsPuzzleGenerator puzzleGenerator;

    private void Start()
    {
        puzzleGenerator = FindAnyObjectByType<TentsPuzzleGenerator>();
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        // if (Mouse.current.leftButton.wasPressedThisFrame)
        //     SayNo();
        //
        // if (Mouse.current.rightButton.wasPressedThisFrame)
        //     StartDancing();
        if (puzzleGenerator != null && puzzleGenerator.SelectedTotem != null)
        {
            var amISelected = puzzleGenerator.SelectedTotem == gameObject;
            // var amISelected = puzzleGenerator.SelectsedTotem, gameObject);
            
            lead.SetBool("Selected", amISelected);
            if (amISelected)
                Debug.Log("I'm the selected guy");
        }
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
