using System;
using System.Collections.Generic;
using UnityEngine;

public class SortieManager : MonoBehaviour
{
    public static SortieManager Instance { get; private set; }

    public List<Objective> activeObjectives = new List<Objective>();

    //events!! these are cool and I wish I knew about them earlier
    public event Action<Objective> OnObjectiveAdded;
    public event Action<Objective> OnObjectiveUpdated;
    public event Action<Objective> OnObjectiveCompleted;

    [SerializeField] private Player _player;

    float _currentScore; //ULTRAKILL
    float _totalScore;
    float _timeElapsedForSortie;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (_player == null)
        {
            _player = FindAnyObjectByType<Player>();
        }

        // Test objectives
        //CreateDynamicObjective("test_objective", "Testing Testing", "Get Sabrina's Number", 1);
        //CreateDynamicObjective("kill_test_dummy", "Kill Test Dummies", "Hiyyaa", 3);
        //CreateDynamicObjective("kill_basic_enemy", "Kill Enemies", "Hyuuuaaa", 3);
    }

    void Update()
    {
        
    }

    public void StartSortie(int level)
    {
        _currentScore = 0;
        _timeElapsedForSortie = 0f;

        activeObjectives.Clear();

        int amountOfSorties = UnityEngine.Random.Range(1, level + 2);

        for (int i = 0; i < amountOfSorties; i++)
        {
            string randomId = $"sortie_task_{level}_{i}";
            string randomTitle = $"Task {i + 1} (Level {level})";
            string randomDescription = $"Amaze! Amaze! Amaze!";

            int requiredAmount = UnityEngine.Random.Range(level * 2, (level * 5) + 1);

            CreateDynamicObjective(randomId, randomTitle, randomDescription, requiredAmount);
        }

        Debug.Log($"Started Sortie Level {level} with {amountOfSorties} objectives!");
    }

    //called when all active sorties are complete
    public void EndSortie()
    {
        _totalScore += _currentScore;

        Debug.Log($"Sortie Complete! Score gained: {_currentScore}. Total Score: {_totalScore}");

        // Clean up active objectives to prepare for the next sortie
        activeObjectives.Clear();
    }

    public void CreateDynamicObjective(string id, string title, string description, int requiredAmount)
    {
        foreach (var obj in activeObjectives)
        {
            if (obj.Id == id)
            {
                Debug.LogWarning($"Objective {id} already exists!");
                return;
            }
        }

        Objective newObjective = new Objective(id, title, description, requiredAmount);
        activeObjectives.Add(newObjective);

        OnObjectiveAdded?.Invoke(newObjective);
    }

    //call when an in-game event happens
    public void UpdateObjective(string id, int amount)
    {
        for (int i = activeObjectives.Count - 1; i >= 0; i--)
        {
            Objective obj = activeObjectives[i];

            if (obj.Id == id && !obj.IsComplete)
            {
                bool justCompleted = obj.AddProgress(amount);

                if (justCompleted)
                {
                    OnObjectiveCompleted?.Invoke(obj);

                    //Remove it from the active list once done
                    activeObjectives.RemoveAt(i);
                }
                else
                {
                    // Only updated, not finished
                    OnObjectiveUpdated?.Invoke(obj);
                }

                return; 
            }
        }
    }
}