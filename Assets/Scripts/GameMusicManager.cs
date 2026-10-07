using UnityEngine;

public class GameMusicManager : MonoBehaviour
{
    public AudioSource musicSource;
    public static GameMusicManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
            SetVolume(savedVolume);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = volume;

        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    public void PlayMusic(AudioClip clip = null, bool loop = true)
    {
        if (musicSource == null) return;

        if (clip != null) musicSource.clip = clip;
        if (!musicSource.isPlaying) musicSource.Play();
        musicSource.loop = loop;
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
    }
}
