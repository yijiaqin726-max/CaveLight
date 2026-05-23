using System.Collections.Generic;
using UnityEngine;

public class RunStatsManager : MonoBehaviour
{
    private const int MaxRecentPurchasedItems = 5;

    private static RunStatsManager instance;

    public static RunStatsManager Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<RunStatsManager>();
            if (instance != null)
            {
                return instance;
            }

            GameObject managerObject = new GameObject(nameof(RunStatsManager));
            instance = managerObject.AddComponent<RunStatsManager>();
            return instance;
        }
    }

    public int cavesCleared;
    public int monstersKilled;
    public readonly List<string> recentPurchasedItems = new List<string>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetRun()
    {
        cavesCleared = 0;
        monstersKilled = 0;
        recentPurchasedItems.Clear();
    }

    public void AddCaveCleared()
    {
        cavesCleared++;
    }

    public void AddMonsterKilled()
    {
        monstersKilled++;
    }

    public void AddPurchasedItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return;
        }

        recentPurchasedItems.Add(itemName);
        while (recentPurchasedItems.Count > MaxRecentPurchasedItems)
        {
            recentPurchasedItems.RemoveAt(0);
        }
    }

    public string GetRecentPurchasedItemsText()
    {
        if (recentPurchasedItems.Count == 0)
        {
            return "\u6682\u65e0\u8d2d\u4e70\u5546\u54c1";
        }

        return string.Join("\u3001", recentPurchasedItems);
    }
}
