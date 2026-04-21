using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class EventAudioBinding
{
    [Tooltip("Drag the exact GameObject/Script from the scene here (e.g., the Player).")]
    public MonoBehaviour targetScript;

    [Tooltip("The exact variable name of the UnityEvent (e.g., 'onJump' or 'onDash').")]
    public string eventName;

    [Tooltip("The sound to play when this event fires.")]
    public Sound sound;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public Settings settings;

    [Header("Event Bindings")]
    [Tooltip("Configure all your audio hooks here! The Manager will dynamically listen to these events.")]
    public List<EventAudioBinding> audioBindings = new List<EventAudioBinding>();

    // We track sounds that have been played so we can dynamically update their volume
    // if the user changes the Settings mid-game.
    private List<Sound> activeSounds = new List<Sound>();

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
        foreach (EventAudioBinding binding in audioBindings)
        {
            if (binding.targetScript == null || string.IsNullOrEmpty(binding.eventName)) continue;

            // Use reflection to find the event variable by its string name on the target script
            FieldInfo fieldInfo = binding.targetScript.GetType().GetField(
                binding.eventName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (fieldInfo != null && typeof(UnityEvent).IsAssignableFrom(fieldInfo.FieldType))
            {
                UnityEvent uEvent = fieldInfo.GetValue(binding.targetScript) as UnityEvent;

                if (uEvent != null)
                {
                    // Dynamically hook our manager's PlaySound method right into the target's event
                    uEvent.AddListener(() => PlaySound(binding.sound));
                }
                else
                {
                    Debug.LogWarning($"AudioManager: The event '{binding.eventName}' on {binding.targetScript.name} is null. Make sure it is initialized.");
                }
            }
            else
            {
                Debug.LogWarning($"AudioManager: Could not find a UnityEvent named '{binding.eventName}' on {binding.targetScript.name}. Check spelling!");
            }
        }
    }

    /// <summary>
    /// Any object can pass a localized Sound object to the Manager to play it globally.
    /// </summary>
    public void PlaySound(Sound s)
    {
        if (s == null || s.clip == null) return;

        if (!activeSounds.Contains(s))
        {
            activeSounds.Add(s);
        }

        if (s.source == null)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.loop = s.loop;
        }

        s.source.pitch = s.pitch;

        float masterVol = s.isMusic ? settings.music : settings.sfx;
        s.source.volume = s.volume * masterVol;

        s.source.Play();
    }

    public void UpdateVolumes()
    {
        if (settings == null) return;

        foreach (Sound s in activeSounds)
        {
            if (s != null && s.source != null)
            {
                float masterVol = s.isMusic ? settings.music : settings.sfx;
                s.source.volume = s.volume * masterVol;
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
        // Collapse to a single line if closed
        if (!property.isExpanded)
            return UnityEditor.EditorGUIUtility.singleLineHeight;

        // Calculate height for Foldout + Target Script + Event Name Dropdown
        float height = UnityEditor.EditorGUIUtility.singleLineHeight * 3 + 6;

        UnityEditor.SerializedProperty soundProp = property.FindPropertyRelative("sound");
        height += UnityEditor.EditorGUI.GetPropertyHeight(soundProp, true); // Add height of the Sound class

        return height;
    }

    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        UnityEditor.EditorGUI.BeginProperty(position, label, property);

        // Draw the collapsible Foldout menu (Element 0, Element 1, etc.)
        Rect foldoutRect = new Rect(position.x, position.y, position.width, UnityEditor.EditorGUIUtility.singleLineHeight);
        property.isExpanded = UnityEditor.EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            UnityEditor.EditorGUI.indentLevel++;

            Rect rect = new Rect(position.x, position.y + UnityEditor.EditorGUIUtility.singleLineHeight + 2, position.width, UnityEditor.EditorGUIUtility.singleLineHeight);

            UnityEditor.SerializedProperty targetProp = property.FindPropertyRelative("targetScript");
            UnityEditor.SerializedProperty eventProp = property.FindPropertyRelative("eventName");
            UnityEditor.SerializedProperty soundProp = property.FindPropertyRelative("sound");

            // 1. Draw Target Script field
            UnityEditor.EditorGUI.PropertyField(rect, targetProp);
            rect.y += UnityEditor.EditorGUIUtility.singleLineHeight + 2;

            // 2. Draw Event Dropdown (The Magic Sauce)
            if (targetProp.objectReferenceValue != null)
            {
                MonoBehaviour target = targetProp.objectReferenceValue as MonoBehaviour;

                // Grab all UnityEvents from the dragged-in script
                var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => typeof(UnityEvent).IsAssignableFrom(f.FieldType))
                    .Select(f => f.Name)
                    .ToList();

                if (fields.Count > 0)
                {
                    fields.Insert(0, "<Select Event>");
                    int currentIndex = Mathf.Max(0, fields.IndexOf(eventProp.stringValue));

                    // Draw the dropdown
                    currentIndex = UnityEditor.EditorGUI.Popup(rect, "Event Name", currentIndex, fields.ToArray());

                    // Save the string value behind the scenes based on their selection
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
                // Fallback to text box if no script is assigned yet
                UnityEditor.EditorGUI.PropertyField(rect, eventProp);
            }

            rect.y += UnityEditor.EditorGUIUtility.singleLineHeight + 2;

            // 3. Draw the full Sound property block
            UnityEditor.EditorGUI.PropertyField(rect, soundProp, true);

            UnityEditor.EditorGUI.indentLevel--;
        }

        UnityEditor.EditorGUI.EndProperty();
    }
}
#endif