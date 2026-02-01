using Core.Interfaces;
using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour, IService
    {
        [SerializeField] private MusicLibrary library;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private MusicTrackId startTrack = MusicTrackId.MainMenu;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource != null)
                audioSource.loop = true;
        }

        private void Start()
        {
            if (playOnStart)
                Play(startTrack);
        }

        public void Play(MusicTrackId track)
        {
            if (library == null)
            {
                Debug.LogWarning("[MusicManager] No MusicLibrary assigned.", this);
                return;
            }

            if (audioSource == null)
            {
                Debug.LogWarning("[MusicManager] Missing AudioSource.", this);
                return;
            }

            var clip = library.GetClip(track);
            if (clip == null)
            {
                Debug.LogWarning($"[MusicManager] Missing clip for track {track}.", this);
                return;
            }

            audioSource.loop = true;
            audioSource.clip = clip;
            audioSource.Play();
        }

        public void Stop()
        {
            if (audioSource != null)
                audioSource.Stop();
        }

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
