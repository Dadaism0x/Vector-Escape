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
            Debug.Log($"[Leaderboard] Pronto. Player ID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (RequestFailedException e)
        {
            Debug.LogError($"[Leaderboard] Inizializzazione fallita ({e.ErrorCode}): {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Leaderboard] Errore inatteso: {e.Message}");
        }
    }

    // ── Nuova sessione ───────────────────────────────────────────────────────

    // Sign out + re-sign in so each game gets a fresh anonymous ID,
    // allowing the same nickname to appear multiple times in the top 10.
    async Task NewSessionAsync()
    {
        if (AuthenticationService.Instance.IsSignedIn)
            AuthenticationService.Instance.SignOut(clearCredentials: true);
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log($"[Leaderboard] Nuova sessione. Player ID: {AuthenticationService.Instance.PlayerId}");
    }

    // ── Invio punteggio ──────────────────────────────────────────────────────

    public async Task AddScore(string nickname, int score)
    {
        if (!IsReady)
        {
            Debug.LogWarning("[Leaderboard] Servizi non ancora pronti.");
            return;
        }

        try
        {
            await NewSessionAsync();
            await AuthenticationService.Instance.UpdatePlayerNameAsync(nickname);

            LeaderboardEntry entry = await LeaderboardsService.Instance
                .AddPlayerScoreAsync(leaderboardId, score);

            Debug.Log($"[Leaderboard] Punteggio inviato — {entry.PlayerName}: {entry.Score} (rank {entry.Rank + 1})");
        }
        catch (RequestFailedException e)
        {
            Debug.LogError($"[Leaderboard] Invio fallito ({e.ErrorCode}): {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Leaderboard] Errore inatteso durante l'invio: {e.Message}");
        }
    }

    // ── Recupero Top 5 ───────────────────────────────────────────────────────

    /// <summary>
    /// Recupera i primi <paramref name="limit"/> punteggi dalla leaderboard.
    /// Restituisce una lista vuota in caso di errore di rete.
    /// </summary>
    public async Task<List<LeaderboardEntryData>> GetTopScores(int limit = 10)
    {
        var results = new List<LeaderboardEntryData>();

        if (!IsReady)
        {
            Debug.LogWarning("[Leaderboard] Servizi non ancora pronti.");
            return results;
        }

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
        catch (RequestFailedException e)
        {
            Debug.LogError($"[Leaderboard] Recupero fallito ({e.ErrorCode}): {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Leaderboard] Errore inatteso durante il recupero: {e.Message}");
        }

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
