using System;
using Core;
using UnityEngine;

namespace Test
{
    
    public class InstaceGameObjectsInCells : MonoBehaviour
    {
        [SerializeField]
        private GameObject gameObjectToSpawn;

        [SerializeField]
        private GridController gridController; 
        
        private void Start()
        {
            for (int i = 0; i < gridController.GridSize; i++)
            {
                for (int j = 0; j < gridController.GridSize; j++)
                    gridController.SpawnInGrid(new Vector2Int(i, j), gameObjectToSpawn);
            }
        }
    }
}