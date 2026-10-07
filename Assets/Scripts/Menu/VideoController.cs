using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoController : MonoBehaviour
{
    VideoPlayer vp;

    void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        // Ensure isLooping is set (redundant if set in inspector)
        vp.isLooping = true;
        vp.playOnAwake = true;
    }

    public void PlayVideo()
    {
        if (!vp.isPlaying) vp.Play();
    }

    public void PauseVideo()
    {
        if (vp.isPlaying) vp.Pause();
    }

    public void TogglePlayPause()
    {
        if (vp.isPlaying) vp.Pause(); else vp.Play();
    }

    public void Restart()
    {
        vp.Stop();
        vp.Play();
    }
}
