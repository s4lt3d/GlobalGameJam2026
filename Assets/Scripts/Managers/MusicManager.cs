using System.Collections;
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
        [SerializeField] private float fadeDuration = 1f;

        private Coroutine fadeRoutine;

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

            if (audioSource.clip == clip && audioSource.isPlaying)
                return;

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeToClip(clip));
        }

        public void Stop()
        {
            if (audioSource != null)
                audioSource.Stop();
        }

        private IEnumerator FadeToClip(AudioClip clip)
        {
            float targetVolume = Mathf.Max(0f, audioSource.volume);

            if (fadeDuration <= 0f)
            {
                audioSource.loop = true;
                audioSource.clip = clip;
                audioSource.volume = targetVolume;
                audioSource.Play();
                yield break;
            }

            if (audioSource.isPlaying && audioSource.clip != null)
                yield return FadeVolume(audioSource.volume, 0f, fadeDuration);

            audioSource.Stop();
            audioSource.loop = true;
            audioSource.clip = clip;
            audioSource.volume = 0f;
            audioSource.Play();

            yield return FadeVolume(0f, targetVolume, fadeDuration);
            audioSource.volume = targetVolume;
        }

        private IEnumerator FadeVolume(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                audioSource.volume = Mathf.Lerp(from, to, t);
                yield return null;
            }
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
