using UnityEngine;

[System.Serializable]
public class Sound
{
    [Tooltip("Add multiple clips here. The manager will pick a random one each time.")]
    public AudioClip[] clips;

    [Tooltip("If true, clips will play in sequential order. If false, a random clip is chosen each time.")]
    public bool playInSeries = false;

    [HideInInspector]
    public int currentClipIndex = 0;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("Randomize volume slightly each time it plays (+/- this value)")]
    [Range(0f, 0.5f)]
    public float volumeVariance = 0f;

    [Range(0.1f, 3f)]
    public float pitch = 1f;

    [Tooltip("Randomize pitch slightly each time it plays (+/- this value)")]
    [Range(0f, 1f)]
    public float pitchVariance = 0f;

    [Tooltip("Check this if the sound should use the Music volume setting instead of SFX.")]
    public bool isMusic;
}