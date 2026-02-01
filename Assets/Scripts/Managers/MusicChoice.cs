using Core;
using UnityEngine;

public class MusicChoice : MonoBehaviour
{
    [SerializeField] 
    MusicTrackId levelTrack = MusicTrackId.Level1;
    
    void Start()
    {
        MusicManager musicManager = Services.Get<MusicManager>();
        musicManager.Play(levelTrack);
    }
}
