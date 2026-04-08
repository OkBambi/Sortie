using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Settings : MonoBehaviour
{
    public bool fullScreen;

    [Range(0f, 1f)]
    public float sfx = 0.5f;
    [Range(0f, 1f)]
    public float music = 0.5f;

    public AudioManager audioManager;

    public static Settings instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        

        UpdateSettingsUI();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name);
        Debug.Log(mode);

        UpdateSettingsUI();
    }

    private void OnEnable()
    {
        Debug.Log("here");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void UpdateSettingsUI()
    {
        
    }
}
