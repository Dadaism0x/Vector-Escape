using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NicknameInputHandler : MonoBehaviour
{
    public event Action<string> OnNameConfirmed;

    public string SavedNickname    => PlayerPrefs.GetString("PlayerNickname", "");
    public bool   HasSavedNickname => !string.IsNullOrEmpty(SavedNickname);
    public bool   IsVisible        { get; private set; }

    private VisualElement startOverlay;
    private Label         randomNameLabel;
    private Button        rerollButton;
    private Button        startButton;

    private string currentName = "";

    public static readonly HashSet<string> TakenNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    static readonly string[] Adjectives =
    {
        "Neon",    "Void",    "Cosmic",   "Laser",   "Solar",
        "Dark",    "Iron",    "Ghost",    "Hyper",   "Nova",
        "Cyber",   "Astro",   "Plasma",   "Turbo",   "Star",
        "Quantum", "Binary",  "Echo",     "Omega",   "Alpha",
        "Flux",    "Pixel",   "Vector",   "Apex",    "Rogue",
        "Phantom", "Vortex",  "Delta",    "Sigma",   "Blaze",
        "Frost",   "Rapid",   "Silent",   "Sonic",   "Stealth"
    };

    static readonly string[] Nouns =
    {
        "Fox",     "Shark",   "Pilot",    "Ace",     "Wolf",
        "Hawk",    "Viper",   "Storm",    "Eagle",   "Comet",
        "Dart",    "Blade",   "Rider",    "Rebel",   "Dash",
        "Runner",  "Hunter",  "Drifter",  "Striker", "Ranger",
        "Chaser",  "Breaker", "Seeker",   "Racer",   "Glider",
        "Jumper",  "Shadow",  "Core",     "Pulse",   "Surge",
        "Spark",   "Flare",   "Drive",    "Crash",   "Boost"
    };

    public static void SetTakenNames(IEnumerable<string> cleanedNames)
    {
        TakenNames.Clear();
        foreach (var n in cleanedNames)
            if (!string.IsNullOrEmpty(n)) TakenNames.Add(n);

        PlayerPrefs.SetString("TakenNames", string.Join(",", TakenNames));
        PlayerPrefs.Save();
    }

    public void Initialize(VisualElement root)
    {
        string saved = PlayerPrefs.GetString("TakenNames", "");
        if (!string.IsNullOrEmpty(saved))
            foreach (var n in saved.Split(','))
                if (!string.IsNullOrEmpty(n)) TakenNames.Add(n);

        startOverlay    = root.Q<VisualElement>("StartOverlay");
        randomNameLabel = root.Q<Label>("RandomNameLabel");
        rerollButton    = root.Q<Button>("RerollButton");
        startButton     = root.Q<Button>("StartButton");

        if (rerollButton != null) rerollButton.clicked += Reroll;
        if (startButton  != null) startButton.clicked  += Confirm;

        currentName = HasSavedNickname ? SavedNickname : GenerateName();
        UpdateDisplay();
    }

    public void Show()
    {
        currentName = HasSavedNickname ? SavedNickname : GenerateName();
        UpdateDisplay();
        if (startOverlay != null) startOverlay.style.display = DisplayStyle.Flex;
        IsVisible = true;
    }

    public void Hide()
    {
        if (startOverlay != null) startOverlay.style.display = DisplayStyle.None;
        IsVisible = false;
    }

    void Reroll()
    {
        currentName = GenerateName();
        UpdateDisplay();
    }

    void Confirm()
    {
        PlayerPrefs.SetString("PlayerNickname", currentName);
        PlayerPrefs.Save();
        Hide();
        OnNameConfirmed?.Invoke(currentName);
    }

    void UpdateDisplay()
    {
        if (randomNameLabel != null) randomNameLabel.text = currentName;
    }

    string GenerateName()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            string candidate = Adjectives[UnityEngine.Random.Range(0, Adjectives.Length)]
                             + Nouns[UnityEngine.Random.Range(0, Nouns.Length)];
            if (!TakenNames.Contains(candidate)) return candidate;
        }
        // Pool quasi esaurito: fallback con qualsiasi combinazione
        return Adjectives[UnityEngine.Random.Range(0, Adjectives.Length)]
             + Nouns[UnityEngine.Random.Range(0, Nouns.Length)];
    }
}
