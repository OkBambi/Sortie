using UnityEngine;

[System.Serializable]
public class Sound
{
    //public string name;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0.1f, 3f)]
    public float pitch = 1f;

    public bool loop;
    public bool isMusic;

    // The AudioSource is created at runtime, so we hide it to keep the inspector clean.
    [HideInInspector]
    public AudioSource source;
}