using System;
using UnityEngine;

public class MoveCellTest : MonoBehaviour
{
    [SerializeField] private Core.GridController gridController;
    [SerializeField] private Vector2Int firstGridPosition = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int secondGridPosition = new Vector2Int(1, 0);
    [SerializeField] private Vector2Int thirdGridPosition = new Vector2Int(1, 0);
    [SerializeField] private float waitSeconds = 0.75f;

    
    void Start()
    {
        StartCoroutine(MoveSequence());
    }

    private System.Collections.IEnumerator MoveSequence()
    {
        if (gridController == null)
            yield break;

        Vector3 firstWorldPosition = gridController.MoveSelectionToGridPosition(firstGridPosition);
        Debug.Log(firstGridPosition);
        Debug.Log(firstWorldPosition);
        yield return new WaitForSeconds(waitSeconds);
        Vector3 secondWorldPosition = gridController.MoveSelectionToGridPosition(secondGridPosition);
        Debug.Log(secondGridPosition);
        Debug.Log(secondWorldPosition);
        yield return secondWorldPosition;
        yield return new WaitForSeconds(waitSeconds);
        Vector3 thirdWorldPosition = gridController.MoveSelectionToGridPosition(thirdGridPosition);
        Debug.Log(thirdGridPosition);
        Debug.Log(thirdWorldPosition);
        yield return thirdWorldPosition;
    }

   
}
