using System;
using UnityEngine;

[System.Serializable]
public class Objective
{
    [SerializeField] private string id;
    [SerializeField] private string title;
    [SerializeField] private string description;
    [SerializeField] private int requiredAmount;
    [SerializeField] private int currentAmount;
    [SerializeField] private bool isComplete;

    public string Id { get => id; private set => id = value; }
    public string Title { get => title; private set => title = value; }
    public string Description { get => description; private set => description = value; }
    public int RequiredAmount { get => requiredAmount; private set => requiredAmount = value; }
    public int CurrentAmount { get => currentAmount; private set => currentAmount = value; }
    public bool IsComplete { get => isComplete; private set => isComplete = value; }

    public Objective(string id, string title, string description, int requiredAmount)
    {
        Id = id;
        Title = title;
        Description = description;
        RequiredAmount = requiredAmount;
        CurrentAmount = 0;
        IsComplete = false;
    }

    public bool AddProgress(int amount)
    {
        if (IsComplete) return false;

        CurrentAmount += amount;

        if (CurrentAmount >= RequiredAmount)
        {
            CurrentAmount = RequiredAmount;
            IsComplete = true;
            return true;
        }

        return false;
    }
}