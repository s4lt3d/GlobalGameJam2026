using System;
using UnityEngine;

namespace Core
{
    [ExecuteInEditMode]
    public class GridController : MonoBehaviour
    {
        [SerializeField]
        private int gridSize = 3;
        public int GridSize => gridSize;

        [SerializeField]
        private float quadHeight = 0.5f;

        [SerializeField]
        private GameObject cell;
        
        private int[,] cells;

        private Vector2Int currentGridPosition = Vector2Int.zero;

        private bool canMoveGrid = true;

        private bool IsInBounds(Vector2Int gridPosition)
        {
            if (gridPosition.x >= gridSize - 1)
                return false;
            
            if (gridPosition.y >= gridSize - 1)
                return false;
            
            return true;
        }

        private Vector3 GetGridLocationCenter(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x + 0.5f, quadHeight, gridPosition.y + 0.5f);
        }

        public bool SpawnInGrid(Vector2Int gridPosition, GameObject cellPrefab)
        {
            if (!IsInBounds(gridPosition))
                return false;

            var position = transform.position + new Vector3(gridPosition.x, 0, gridPosition.y);
            
            var instance = Instantiate(cellPrefab, position, Quaternion.identity);
            return true;
        }
        
        private void OnValidate()
        {
            Debug.Log("Editor causes this OnValidate");
        }

        private bool CanMoveGrid()
        {
            return canMoveGrid;
        }

        private void OnGridMoved(Vector2Int gridChange)
        {
            if (!CanMoveGrid())
                return;

            currentGridPosition.x = Mathf.Clamp(currentGridPosition.x + gridChange.x, 0, gridSize - 1);
            currentGridPosition.y = Mathf.Clamp(currentGridPosition.y + gridChange.y, 0, gridSize - 1);
        }
    }
}