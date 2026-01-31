using System;
using Core.Interfaces;
using UnityEngine;

namespace Managers
{
    public class EventManager : IService
    {
        public bool IsPaused => false;

        public Action<Vector2Int> GridChanged;
        public Action<Vector2Int> GridSelected;
        public Action<Vector3> CellSelected;

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