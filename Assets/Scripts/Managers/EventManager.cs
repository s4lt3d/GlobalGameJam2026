using System;
using System.Collections.Generic;
using Core.Interfaces;
using UnityEngine;

namespace Managers
{     
    public class EventManager : IService
    {
        public bool IsPaused => false;

        public Action<Vector2Int> GridChanged;
        public Action<Vector2Int> GridSelected;
        public Action<Transform> gameObjectSelected;
        public Action LevelWin;
        public Action<AIAgent> AgentReachedDestination;
        public Action<List<Vector2Int>> invalidPositions;
        public Action<Vector2Int> invalidPosition;
        public Action<Vector2Int> validPosition;
        
        // public Action<GameObject> gameObjectSelected;
        
        public void InitializeService()
        {
        }

        public void StartService()
        {
        }

        public void CleanupService()
        {
        }
    }
}
