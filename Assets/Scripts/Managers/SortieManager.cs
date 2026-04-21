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

    [SerializeField] GameObject Player;

    float _currentScore; //ULTRAKILL
    float _timeElapsedForSortie;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        Player = GameObject.FindAnyObjectByType<Player>().gameObject;

        //test objective
        CreateDynamicObjective("test_objective", "Testing Testing", "Get Sabrina's Number", 1);
        CreateDynamicObjective("kill_test_dummy", "Kill Test Dummies", "Hiyyaa", 3);
        CreateDynamicObjective("kill_basic_enemy", "Kill Enemies", "Hyuuuaaa", 3);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            //update test objective
        }
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