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
        [Min(0f)]
        private float cellPadding = 0f;

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
            float spacing = 1f + cellPadding;
            return new Vector3(
                gridPosition.x * spacing + 0.5f * spacing,
                quadHeight,
                gridPosition.y * spacing + 0.5f * spacing);
        }

        public bool SpawnInGrid(Vector2Int gridPosition, GameObject cellPrefab)
        {
            if (!IsInBounds(gridPosition))
                return false;

            float spacing = 1f + cellPadding;
            var position = transform.position + new Vector3(gridPosition.x * spacing, 0, gridPosition.y * spacing);
            
            var instance = Instantiate(cellPrefab, position, Quaternion.identity);
            return true;
        }

        public Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            float spacing = 1f + cellPadding;
            return transform.position + new Vector3(gridPosition.x * spacing, 0, gridPosition.y * spacing);
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
