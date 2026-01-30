using Core;
using Core.Interfaces;
using UnityEngine;

namespace Managers
{
    public class TimeManager : MonoBehaviour, IService
    {
        public static float DeltaGameTime;
        public static float DeltaFixedGameTime;
        public static float GameTime;

        public static float deltaTime => DeltaGameTime;
        public static float fixedDeltaTime => DeltaFixedGameTime;

        private EventManager eventManager;

        public void Start()
        {
            eventManager = Services.Get<EventManager>();
        }

        private void Update()
        {
            DeltaGameTime = eventManager.IsPaused ? 0f : Time.deltaTime;
            GameTime += DeltaGameTime;
        }

        private void FixedUpdate()
        {
            DeltaFixedGameTime = eventManager.IsPaused ? 0f : Time.fixedDeltaTime;
        }

        public void InitializeService()
        {
            // empty
        }

        public void StartService()
        {
            // empty
        }

        public void CleanupService()
        {
            // empty
        }
    }
}