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

    public string Id => id;
    public string Title => title;
    public string Description => description;
    public int RequiredAmount => requiredAmount;
    public int CurrentAmount => currentAmount;
    public bool IsComplete => isComplete;

    public float ProgressPercentage => requiredAmount > 0 ? (float)currentAmount / requiredAmount : 1f;

    public Objective(string id, string title, string description, int requiredAmount)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.requiredAmount = requiredAmount;
        this.currentAmount = 0;
        this.isComplete = false;
    }

    public bool AddProgress(int amount)
    {
        if (isComplete) return false;

        currentAmount += amount;

        if (currentAmount >= requiredAmount)
        {
            currentAmount = requiredAmount;
            isComplete = true;
            return true;
        }

        return false;
    }
}