using System.Collections.Generic;
using UnityEngine;

public static class Leaderboard
{
    private const string KeyPrefix = "LB_Score_";
    private const int MaxEntries = 5;

    public static List<int> GetScores()
    {
        var scores = new List<int>();
        for (int i = 0; i < MaxEntries; i++)
            scores.Add(PlayerPrefs.GetInt(KeyPrefix + i, 0));
        return scores;
    }

    public static void Submit(int score)
    {
        var scores = GetScores();
        scores.Add(score);
        scores.Sort((a, b) => b.CompareTo(a));
        if (scores.Count > MaxEntries)
            scores.RemoveRange(MaxEntries, scores.Count - MaxEntries);
        for (int i = 0; i < scores.Count; i++)
            PlayerPrefs.SetInt(KeyPrefix + i, scores[i]);
        PlayerPrefs.Save();
    }
}
