using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

public class ArcadeLeaderboardManager : MonoBehaviour
{
    [SerializeField] private string leaderboardId = "arcade_top_scores";
    [SerializeField] public string PlayerNickname = "Player";

    public bool IsReady { get; private set; }

    // ── Inizializzazione ─────────────────────────────────────────────────────

    async void Start()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            IsReady = true;
        }
        catch (RequestFailedException) { }
        catch (Exception) { }
    }

    // ── Nuova sessione ───────────────────────────────────────────────────────

    // Sign out + re-sign in so each game gets a fresh anonymous ID,
    // allowing the same nickname to appear multiple times in the top 10.
    async Task NewSessionAsync()
    {
        if (AuthenticationService.Instance.IsSignedIn)
            AuthenticationService.Instance.SignOut(clearCredentials: true);
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // ── Invio punteggio ──────────────────────────────────────────────────────

    public async Task AddScore(string nickname, int score)
    {
        if (!IsReady) return;

        try
        {
            await NewSessionAsync();
            await AuthenticationService.Instance.UpdatePlayerNameAsync(nickname);

            await LeaderboardsService.Instance
                .AddPlayerScoreAsync(leaderboardId, score);
        }
        catch (RequestFailedException) { }
        catch (Exception) { }
    }

    // ── Recupero Top 5 ───────────────────────────────────────────────────────

    public async Task<List<LeaderboardEntryData>> GetTopScores(int limit = 10)
    {
        var results = new List<LeaderboardEntryData>();
        if (!IsReady) return results;

        try
        {
            var options = new GetScoresOptions { Limit = limit };
            LeaderboardScoresPage page = await LeaderboardsService.Instance
                .GetScoresAsync(leaderboardId, options);

            foreach (LeaderboardEntry entry in page.Results)
            {
                results.Add(new LeaderboardEntryData
                {
                    Rank       = entry.Rank + 1,
                    PlayerName = entry.PlayerName,
                    Score      = (int)entry.Score
                });
            }
        }
        catch (RequestFailedException) { }
        catch (Exception) { }

        return results;
    }
}

// ── Modello dati ─────────────────────────────────────────────────────────────

public class LeaderboardEntryData
{
    public int    Rank;
    public string PlayerName;
    public int    Score;
}
