using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core
{
    [ExecuteInEditMode]
    public class GridController : MonoBehaviour
    {
        [SerializeField]
        private int gridSize = 3;
        public int GridSize => gridSize;

        [FormerlySerializedAs("quadHeight")] [SerializeField]
        private float spawnHeigh = 0.5f;

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
            if (gridPosition.x >= gridSize)
                return false;
            
            if (gridPosition.y >= gridSize)
                return false;
            
            return true;
        }

        // private Vector3 GetGridLocationCenter(Vector2Int gridPosition)
        // {
        //     return new Vector3(gridPosition.x + 0.5f, quadHeight, gridPosition.y + 0.5f);
        // }

        public Vector3 GetGridLocation(Vector2Int gridPosition)
        {
            float spacing = 1f + cellPadding;
            return transform.position + new Vector3(
                gridPosition.x * spacing,
                spawnHeigh,
                gridPosition.y * spacing);
        }
        
        public Vector3 SpawnInGrid(Vector2Int gridPosition, GameObject cellPrefab)
        {
            if (!IsInBounds(gridPosition))
                return Vector3.zero;

            float spacing = 1f + cellPadding;
            var position = transform.position + new Vector3(gridPosition.x * spacing, 0, gridPosition.y * spacing);
            
            var instance = Instantiate(cellPrefab, position, Quaternion.identity);
            return position;
        }

        public Vector3 MoveSelectionToGridPosition(Vector2Int gridPosition)
        {
            if (!IsInBounds(gridPosition))
                return Vector3.zero;

            if (!CanMoveGrid())
                return Vector3.zero;

            currentGridPosition = gridPosition;

            var position = GetGridLocation(gridPosition);
            if (cell != null)
                cell.transform.position = position;

            return position;
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
