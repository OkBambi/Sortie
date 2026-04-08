using UnityEngine.Audio;
using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public Settings settings;
    public Sound[] sounds;
    
    public static AudioManager instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        settings = GameObject.FindAnyObjectByType<Settings>();

        DontDestroyOnLoad(gameObject);

        foreach(Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            if (s.isMusic)
                s.source.volume = s.volume * settings.music;
            else
                s.source.volume = s.volume * settings.sfx;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound " + name + " was not found!");
            return;
        }
        Debug.Log(name);

        if (s.source != null)
            s.source.Play();
        else
            Invoke("s.source.Play", 0.1f);


    }

    public void UpdateVolumes()
    {
        foreach (Sound s in sounds)
        {
            if (s.source == null) return;
            if (s.isMusic)
                s.source.volume = s.volume * settings.music;
            else
                s.source.volume = s.volume * settings.sfx;
        }
    }
}
