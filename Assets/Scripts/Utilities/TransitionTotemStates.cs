using System;
using System.Collections.Generic;
using Core;
using Puzzles;
using UnityEngine;
using UnityEngine.InputSystem;
using Managers;
using NUnit.Framework;
using UnityEngine.PlayerLoop;

public class TransitionDancerAnimation : MonoBehaviour
{
    [SerializeField]
    private Animator lead;
    
    private TentsPuzzleGenerator puzzleGenerator;

    private bool unhappy = false;
    
    EventManager eventManager;
    GridController gridController;
    
    List<Vector2Int> invalidGridPositions = new();
    private Vector2Int myOldPosition = -Vector2Int.one;
    
    private void Start()
    {
        puzzleGenerator = FindAnyObjectByType<TentsPuzzleGenerator>();
        eventManager = Services.Get<EventManager>();
        // eventManager.invalidPositions += onInvalidPositions;
        eventManager.validPosition += onValidPosition;
        eventManager.invalidPosition += onInvalidPosition;
        // gridController =
        gridController = FindAnyObjectByType<GridController>();
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;
        
        if (puzzleGenerator != null)
        {
            var amISelected = puzzleGenerator.SelectedTotem == gameObject;
            
            lead.SetBool("Selected", amISelected);
        }

        CheckIfImInvalid();
    }

    private void CheckIfImInvalid()
    {
        var validGrid = gridController.TryGetGridPositionFromWorld(transform.position, out var gridPosition);

        if (validGrid)
        {
            if (invalidGridPositions.Contains(gridPosition))
            {
                TransitionToUnhappy();
                myOldPosition = gridPosition;
            }
            else
            {
                TransitionToIdle();
            }
        }
    }

    public void onInvalidPosition(Vector2Int position)
    {
        if (!invalidGridPositions.Contains(position))
        {
            invalidGridPositions.Add(position);
        }
    }

    public void onValidPosition(Vector2Int position)
    {
        var validGrid = gridController.TryGetGridPositionFromWorld(transform.position, out var gridPosition);
        
        if (invalidGridPositions.Contains((position)))
            invalidGridPositions.Remove(position);
        // if (myOldPosition.Equals(gridPosition))
        //     return;
        
        
        // invalidGridPositions.Add(position);
    }
    
    public void onInvalidPositions(List<Vector2Int> positions)
    {
        // if (positions == null)
        // {
        //     invalidGridPositions.Clear();
        //     return;
        // }
        //
        // invalidGridPositions.RemoveAll(position => !positions.Contains(position));
        //
        // foreach (var position in positions)
        // {
        //     if (invalidGridPositions.Contains(position))
        //         continue;
        //     invalidGridPositions.Add(position);
        // }
        
        
        // invalidGridPositions = positions;
    }


    public void TransitionToUnhappy()
    {
        lead.SetBool("Unhappy", true);
    }
    
    public void TransitionToIdle()
    {
        lead.SetBool("Unhappy", false);
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
