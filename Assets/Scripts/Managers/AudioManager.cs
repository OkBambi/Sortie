using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class AudioCategory
{
    [Tooltip("Name of the category")]
    public string categoryName = "New Category";

    [Tooltip("The audio bindings for this specific category.")]
    public List<EventAudioBinding> bindings = new List<EventAudioBinding>();
}

[System.Serializable]
public class EventAudioBinding
{
    public enum AudioActionType { PlayOneShot, StartLoop, StopLoop }

    [Tooltip("Drag the exact GameObject/Script from the scene here")]
    public MonoBehaviour targetScript;

    [Tooltip("The exact variable name of the UnityEvent")]
    public string eventName;

    [Tooltip("What kind of audio action should this event trigger?")]
    public AudioActionType actionType = AudioActionType.PlayOneShot;

    [Tooltip("A unique string ID to identify this loop. Use the exact same ID in the StopLoop binding.")]
    public string loopId = "MyLoop";

    [Tooltip("The sound configurations.")]
    public Sound sound;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public Settings settings;

    [Header("Optimization")]
    [Tooltip("Max concurrent one-shot sounds. Prevents component bloat from machine guns/explosions.")]
    public int maxSimultaneousSounds = 30;

    [Header("Categorized Event Bindings")]
    [Tooltip("Organize all your audio hooks into collapsible sections here!")]
    public List<AudioCategory> soundCategories = new List<AudioCategory>();

    // Helper class to track looping sounds
    private class ActiveLoopData
    {
        public AudioSource source;
        public Sound soundConfig;
        public float appliedVolumeRng;
    }

    private Dictionary<string, ActiveLoopData> activeLoops = new Dictionary<string, ActiveLoopData>();

    private List<AudioSource> sourcePool = new List<AudioSource>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);

        if (settings == null) settings = FindAnyObjectByType<Settings>();
    }

    private void Start()
    {
        HookUpSceneEvents();
    }

    private void HookUpSceneEvents()
    {
        if (soundCategories == null) return;

        foreach (AudioCategory category in soundCategories)
        {
            if (category == null || category.bindings == null) continue;

            foreach (EventAudioBinding binding in category.bindings)
            {
                if (binding.targetScript == null || string.IsNullOrEmpty(binding.eventName)) continue;

                FieldInfo fieldInfo = binding.targetScript.GetType().GetField(
                    binding.eventName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic
                );

                if (fieldInfo != null && typeof(UnityEvent).IsAssignableFrom(fieldInfo.FieldType))
                {
                    UnityEvent uEvent = fieldInfo.GetValue(binding.targetScript) as UnityEvent;

                    if (uEvent != null)
                    {
                        uEvent.AddListener(() => ProcessBinding(binding));
                    }
                    else
                    {
                        Debug.LogWarning($"AudioManager: The event '{binding.eventName}' on {binding.targetScript.name} is null.");
                    }
                }
            }
        }
    }

    public void ProcessBinding(EventAudioBinding binding)
    {
        if (binding == null) return;

        if (binding.actionType == EventAudioBinding.AudioActionType.StopLoop)
        {
            StopLoop(binding.loopId);
            return;
        }

        if (binding.actionType == EventAudioBinding.AudioActionType.StartLoop)
        {
            StartLoop(binding.loopId, binding.sound);
        }
        else // PlayOneShot
        {
            PlaySound(binding.sound);
        }
    }

    public void StopLoop(string loopId)
    {
        if (!string.IsNullOrEmpty(loopId) && activeLoops.TryGetValue(loopId, out ActiveLoopData data))
        {
            if (data != null && data.source != null) data.source.Stop();
        }
    }

    public void StartLoop(string loopId, Sound s)
    {
        if (string.IsNullOrEmpty(loopId) || s == null || s.clips == null || s.clips.Length == 0) return;

        AudioClip clip;
        if (s.playInSeries)
        {
            clip = s.clips[s.currentClipIndex];
            s.currentClipIndex = (s.currentClipIndex + 1) % s.clips.Length;
        }
        else
        {
            clip = s.clips[UnityEngine.Random.Range(0, s.clips.Length)];
        }
        if (clip == null) return;

        // Apply Variance
        float pRng = UnityEngine.Random.Range(-s.pitchVariance, s.pitchVariance);
        float vRng = UnityEngine.Random.Range(-s.volumeVariance, s.volumeVariance);

        float p = Mathf.Clamp(s.pitch + pRng, 0.1f, 3f);
        float v = Mathf.Clamp01(s.volume + vRng);

        float masterVol = 1f;
        if (settings != null) masterVol = s.isMusic ? settings.music : settings.sfx;
        float finalVol = v * masterVol;

        if (!activeLoops.TryGetValue(loopId, out ActiveLoopData loopData) || loopData == null || loopData.source == null)
        {
            loopData = new ActiveLoopData { source = gameObject.AddComponent<AudioSource>() };
            activeLoops[loopId] = loopData;
        }

        loopData.soundConfig = s;
        loopData.appliedVolumeRng = vRng;

        loopData.source.clip = clip;
        loopData.source.pitch = p;
        loopData.source.volume = finalVol;
        loopData.source.loop = true;

        if (!loopData.source.isPlaying) loopData.source.Play();
    }

    public void PlaySound(Sound s)
    {
        if (s == null || s.clips == null || s.clips.Length == 0) return;

        AudioClip clip;
        if (s.playInSeries)
        {
            clip = s.clips[s.currentClipIndex];
            s.currentClipIndex = (s.currentClipIndex + 1) % s.clips.Length;
        }
        else
        {
            clip = s.clips[UnityEngine.Random.Range(0, s.clips.Length)];
        }
        if (clip == null) return;

        // Apply Variance
        float pRng = UnityEngine.Random.Range(-s.pitchVariance, s.pitchVariance);
        float vRng = UnityEngine.Random.Range(-s.volumeVariance, s.volumeVariance);

        float p = Mathf.Clamp(s.pitch + pRng, 0.1f, 3f);
        float v = Mathf.Clamp01(s.volume + vRng);

        float masterVol = 1f;
        if (settings != null) masterVol = s.isMusic ? settings.music : settings.sfx;
        float finalVol = v * masterVol;

        AudioSource src = GetAvailableSource();
        if (src != null)
        {
            src.pitch = p;
            src.volume = finalVol;
            src.clip = clip;
            src.loop = false;
            src.Play();
        }
    }

    private AudioSource GetAvailableSource()
    {
        // Clean up any accidentally destroyed sources to prevent further nulls
        sourcePool.RemoveAll(source => source == null);

        // Try to find a completely idle source
        foreach (var src in sourcePool)
        {
            if (!src.isPlaying) return src;
        }

        // If all pooled sources are busy, check if we are under the component limit
        if (sourcePool.Count < maxSimultaneousSounds)
        {
            AudioSource newSrc = gameObject.AddComponent<AudioSource>();
            sourcePool.Add(newSrc);
            return newSrc;
        }

        // VOICE STEALING: Pool is full! Recycle the sound that is closest to finishing.
        AudioSource oldestSource = sourcePool[0];
        float maxPlaybackPercentage = 0f;

        foreach (var src in sourcePool)
        {
            if (src.clip != null)
            {
                // Calculate how close the clip is to finishing (0.0 to 1.0)
                float percentage = src.time / Mathf.Max(src.clip.length, 0.01f);
                if (percentage > maxPlaybackPercentage)
                {
                    maxPlaybackPercentage = percentage;
                    oldestSource = src;
                }
            }
        }

        oldestSource.Stop(); // Cut off the old sound so it doesn't bleed over
        return oldestSource;
    }

    public void UpdateVolumes()
    {
        if (settings == null) return;

        foreach (var kvp in activeLoops)
        {
            ActiveLoopData data = kvp.Value;
            if (data.source != null && data.soundConfig != null)
            {
                float masterVol = data.soundConfig.isMusic ? settings.music : settings.sfx;
                float baseVol = Mathf.Clamp01(data.soundConfig.volume + data.appliedVolumeRng);
                data.source.volume = baseVol * masterVol;
            }
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(EventAudioBinding))]
public class EventAudioBindingDrawer : UnityEditor.PropertyDrawer
{
    public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return UnityEditor.EditorGUIUtility.singleLineHeight;

        int action = property.FindPropertyRelative("actionType").enumValueIndex;

        float lines = 4; // Foldout, Target, Event Name, Action Type
        if (action == 1 || action == 2) lines++; // Adds line for Loop ID

        float height = lines * (UnityEditor.EditorGUIUtility.singleLineHeight + 2);

        // We don't draw the sound config if the action is StopLoop
        if (action == 0 || action == 1)
        {
            UnityEditor.SerializedProperty soundProp = property.FindPropertyRelative("sound");
            height += UnityEditor.EditorGUI.GetPropertyHeight(soundProp, true);
        }

        return height;
    }

    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        UnityEditor.EditorGUI.BeginProperty(position, label, property);

        Rect foldoutRect = new Rect(position.x, position.y, position.width, UnityEditor.EditorGUIUtility.singleLineHeight);
        property.isExpanded = UnityEditor.EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            UnityEditor.EditorGUI.indentLevel++;
            float lineHeight = UnityEditor.EditorGUIUtility.singleLineHeight + 2;

            Rect rect = new Rect(position.x, position.y + lineHeight, position.width, UnityEditor.EditorGUIUtility.singleLineHeight);

            UnityEditor.SerializedProperty targetProp = property.FindPropertyRelative("targetScript");
            UnityEditor.SerializedProperty eventProp = property.FindPropertyRelative("eventName");
            UnityEditor.SerializedProperty actionProp = property.FindPropertyRelative("actionType");
            UnityEditor.SerializedProperty loopIdProp = property.FindPropertyRelative("loopId");
            UnityEditor.SerializedProperty soundProp = property.FindPropertyRelative("sound");

            // 1. Draw Target
            UnityEditor.EditorGUI.PropertyField(rect, targetProp);
            rect.y += lineHeight;

            // 2. Draw Event Dropdown
            if (targetProp.objectReferenceValue != null)
            {
                MonoBehaviour target = targetProp.objectReferenceValue as MonoBehaviour;

                var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => typeof(UnityEvent).IsAssignableFrom(f.FieldType))
                    .Select(f => f.Name)
                    .ToList();

                if (fields.Count > 0)
                {
                    fields.Insert(0, "<Select Event>");
                    int currentIndex = Mathf.Max(0, fields.IndexOf(eventProp.stringValue));

                    currentIndex = UnityEditor.EditorGUI.Popup(rect, "Event Name", currentIndex, fields.ToArray());

                    if (currentIndex > 0)
                        eventProp.stringValue = fields[currentIndex];
                    else
                        eventProp.stringValue = "";
                }
                else
                {
                    UnityEditor.EditorGUI.LabelField(rect, "Event Name", "No UnityEvents found on target!");
                }
            }
            else
            {
                UnityEditor.EditorGUI.PropertyField(rect, eventProp);
            }

            rect.y += lineHeight;

            // 3. Draw Action Type
            UnityEditor.EditorGUI.PropertyField(rect, actionProp);
            rect.y += lineHeight;

            int action = actionProp.enumValueIndex;

            // 4. Draw Loop ID (Only if StartLoop or StopLoop)
            if (action == 1 || action == 2)
            {
                UnityEditor.EditorGUI.PropertyField(rect, loopIdProp);
                rect.y += lineHeight;
            }

            // 5. Draw Sound Config (Only if PlayOneShot or StartLoop)
            if (action == 0 || action == 1)
            {
                // This draws the Sound class and automatically includes the new bool toggle
                UnityEditor.EditorGUI.PropertyField(rect, soundProp, true);
            }

            UnityEditor.EditorGUI.indentLevel--;
        }

        UnityEditor.EditorGUI.EndProperty();
    }
}
#endif