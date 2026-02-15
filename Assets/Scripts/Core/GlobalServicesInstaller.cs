using Managers;

namespace Core
{
    public static class GlobalServicesInstaller
    {
        public static void Install()
        {
            Services.Add(new EventManager());
            Services.AddComponent<TimeManager>();
            Services.AddComponent<GameInputManager>();
            Services.AddPrefab<MusicManager>("MusicManager", true);
        }
    }
}
