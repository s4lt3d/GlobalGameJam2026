using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public enum MusicTrackId
    {
        MainMenu,
        Level1,
        Level2
    }

    [CreateAssetMenu(fileName = "MusicLibrary", menuName = "Audio/Music Library")]
    public class MusicLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public MusicTrackId track;
            public AudioClip clip;
        }

        [SerializeField] private List<Entry> tracks = new List<Entry>();

        public AudioClip GetClip(MusicTrackId track)
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i].track == track)
                    return tracks[i].clip;
            }

            return null;
        }
    }
}
