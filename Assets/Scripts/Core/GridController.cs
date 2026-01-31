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
            if (gridPosition.x < 0 || gridPosition.x >= gridSize)
                return false;
            
            if (gridPosition.y < 0 || gridPosition.y >= gridSize)
                return false;
            
            return true;
        }

        private Vector3 GetGridLocationCenter(Vector2Int gridPosition)
        {
            return GetWorldPosition(gridPosition, true);
        }

        public bool SpawnInGrid(Vector2Int gridPosition, GameObject cellPrefab)
        {
            if (!IsInBounds(gridPosition))
                return false;

            var position = GetWorldCenter(gridPosition);
            
            var instance = Instantiate(cellPrefab, position, Quaternion.identity);
            return true;
        }

        public Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            return GetWorldPosition(gridPosition, false);
        }

        public Vector3 GetWorldCenter(Vector2Int gridPosition)
        {
            return GetGridLocationCenter(gridPosition);
        }

        public bool TryGetGridPositionFromWorld(Vector3 worldPosition, out Vector2Int gridPosition, bool useNearestCenter = true)
        {
            float spacing = 1f + cellPadding;
            Vector3 local = worldPosition - transform.position;
            float gx = local.x / spacing;
            float gy = local.z / spacing;

            int x = useNearestCenter ? Mathf.RoundToInt(gx - 0.5f) : Mathf.FloorToInt(gx);
            int y = useNearestCenter ? Mathf.RoundToInt(gy - 0.5f) : Mathf.FloorToInt(gy);

            gridPosition = new Vector2Int(x, y);
            return IsInBounds(gridPosition);
        }

        private Vector3 GetWorldPosition(Vector2Int gridPosition, bool center)
        {
            float spacing = 1f + cellPadding;
            float offset = center ? 0.5f * spacing : 0f;
            float height = center ? quadHeight : 0f;
            Vector3 local = new Vector3(
                gridPosition.x * spacing + offset,
                height,
                gridPosition.y * spacing + offset);
            return transform.position + local;
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
