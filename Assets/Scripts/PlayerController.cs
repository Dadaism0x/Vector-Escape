using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float thrustForce = 25f;
    public float maxSpeed = 12f;
    public GameObject boosterFlame;
    public GameObject explosionEffect;

    [Header("Death Effects")]
    public float slowMoScale = 0.15f;
    public float slowMoDuration = 0.35f;

    [Header("Input Actions")]
    public InputAction moveForward;
    public InputAction lookPosition;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private bool isHit = false;
    private bool isDead = false;

    private float score = 0;
    private ProceduralShape shipShape;

    // ── UI Toolkit ────────────────────────────────────────────────────────────
    private VisualElement root;
    private Label scoreLabel;
    private VisualElement scoreContainer;
    private VisualElement startOverlay;
    private TextField nicknameField;
    private Button startButton;
    private bool gameStarted = false;

    // ── Game Over Card ────────────────────────────────────────────────────────
    private VisualElement gameOverOverlay;
    private Label finalScoreLabel;
    private VisualElement leaderboardRowsContainer;
    private Button restartButton;
    private Button changeNameButton;

    // ── Online leaderboard ────────────────────────────────────────────────────
    private ArcadeLeaderboardManager arcadeLbManager;

    // ── Row color palette ─────────────────────────────────────────────────────
    static readonly Color RankPink  = new Color(1.00f, 0.71f, 0.75f);
    static readonly Color RankLilac = new Color(0.78f, 0.69f, 0.94f);
    static readonly Color RankMint  = new Color(0.67f, 0.90f, 0.80f);
    static readonly Color RankGold  = new Color(1.00f, 0.86f, 0.51f);
    static readonly Color Cream     = new Color(1.00f, 0.95f, 0.87f);

    // ── Init ──────────────────────────────────────────────────────────────────

    void Start()
    {
        Application.targetFrameRate = -1;
        rb = GetComponent<Rigidbody2D>();
        Color startColor = PaletteManager.Instance != null
            ? PaletteManager.Instance.PlayerColor
            : new Color(0.98f, 0.89f, 0.63f);

        shipShape = GetComponent<ProceduralShape>() ?? gameObject.AddComponent<ProceduralShape>();
        shipShape.Initialize(ProceduralShape.ShapeType.Arrow, startColor);

        PaletteManager.OnPaletteChanged += OnPaletteChanged;

        arcadeLbManager = FindObjectOfType<ArcadeLeaderboardManager>();

        moveForward.Enable();
        lookPosition.Enable();

        UIDocument uiDoc = FindObjectOfType<UIDocument>();
        if (uiDoc != null)
        {
            root = uiDoc.rootVisualElement;

            scoreContainer = root.Q<VisualElement>("ScoreContainer");
            scoreLabel     = root.Q<Label>("ScoreLabel");
            if (scoreContainer != null)
                scoreContainer.style.display = DisplayStyle.None;

            gameOverOverlay          = root.Q<VisualElement>("GameOverOverlay");
            finalScoreLabel          = root.Q<Label>("FinalScore");
            leaderboardRowsContainer = root.Q<VisualElement>("LeaderboardRows");
            restartButton            = root.Q<Button>("RestartButton");

            if (gameOverOverlay != null)
                gameOverOverlay.style.display = DisplayStyle.None;

            if (restartButton != null)
                restartButton.clicked += RestartGame;

            changeNameButton = root.Q<Button>("ChangeNameButton");
            if (changeNameButton != null)
                changeNameButton.clicked += OnChangeNameClicked;

            startOverlay  = root.Q<VisualElement>("StartOverlay");
            nicknameField = root.Q<TextField>("NicknameField");
            startButton   = root.Q<Button>("StartButton");

            if (nicknameField != null)
            {
                nicknameField.value = PlayerPrefs.GetString("PlayerNickname", "");
                nicknameField.RegisterCallback<KeyDownEvent>(evt => {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                        OnStartButtonClicked();
                });
            }

            if (startButton != null)
                startButton.clicked += OnStartButtonClicked;
        }

        string savedNick = PlayerPrefs.GetString("PlayerNickname", "");
        if (!string.IsNullOrEmpty(savedNick))
        {
            if (arcadeLbManager != null) arcadeLbManager.PlayerNickname = savedNick;
            if (startOverlay != null)  startOverlay.style.display  = DisplayStyle.None;
            if (scoreContainer != null) scoreContainer.style.display = DisplayStyle.Flex;
            gameStarted = true;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
        else
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;
        }
    }

    // ── Loop ──────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!gameStarted || isDead) return;

        score += Time.deltaTime * 10;
        int intScore = Mathf.FloorToInt(score);

        if (scoreLabel != null)
            scoreLabel.text = intScore.ToString();

        if (PaletteManager.Instance != null)
            PaletteManager.Instance.CheckMilestone(intScore);

        if (moveForward.IsPressed())
        {
            Vector2 screenPos = lookPosition.ReadValue<Vector2>();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            moveDirection = ((Vector2)worldPos - rb.position).normalized;
            transform.up = moveDirection;
            if (boosterFlame != null) boosterFlame.SetActive(true);
        }
        else
        {
            if (boosterFlame != null) boosterFlame.SetActive(false);
        }

        ScreenWrap();
    }

    void FixedUpdate()
    {
        if (!gameStarted || isDead) return;

        if (moveForward.IsPressed())
        {
            rb.AddForce(moveDirection * thrustForce);
            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!gameStarted) return;
        if (collision.gameObject.CompareTag("Obstacle") && !isHit)
        {
            isHit = true;
            StartCoroutine(DeathSequence());
        }
    }

    // ── Morte ─────────────────────────────────────────────────────────────────

    IEnumerator DeathSequence()
    {
        isDead = true;

        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        CameraShake.Instance?.Shake(0.5f, 0.3f);

        yield return new WaitForSecondsRealtime(0.07f);

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        if (boosterFlame != null) boosterFlame.SetActive(false);
        rb.linearVelocity = Vector2.zero;

        Time.timeScale = slowMoScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(slowMoDuration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        GameOver();
    }

    void GameOver()
    {
        int finalScore = Mathf.FloorToInt(score);
        Leaderboard.Submit(finalScore);

        if (finalScoreLabel != null) finalScoreLabel.text = finalScore.ToString();
        ShowPlaceholders();
        StartCoroutine(FadeInGameOver());

        _ = TryOnlineGameOver(finalScore);
    }

    IEnumerator FadeInGameOver()
    {
        if (gameOverOverlay == null) yield break;
        gameOverOverlay.style.display = DisplayStyle.Flex;
        gameOverOverlay.style.opacity = 0f;

        float t = 0f;
        const float dur = 0.30f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            gameOverOverlay.style.opacity = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            yield return null;
        }
        gameOverOverlay.style.opacity = 1f;
    }

    async Task TryOnlineGameOver(int finalScore)
    {
        if (arcadeLbManager == null || !arcadeLbManager.IsReady) return;

        await arcadeLbManager.AddScore(arcadeLbManager.PlayerNickname, finalScore);
        if (this == null) return;

        var entries = await arcadeLbManager.GetTopScores(LbRows);
        if (this == null || entries.Count == 0) return;

        RefreshLeaderboard(entries, arcadeLbManager.PlayerNickname);
    }

    // ── Leaderboard rows ──────────────────────────────────────────────────────

    const int LbRows = 3;

    void ShowPlaceholders()
    {
        if (leaderboardRowsContainer == null) return;
        leaderboardRowsContainer.Clear();
        for (int i = 0; i < LbRows; i++)
            leaderboardRowsContainer.Add(MakeRow(i + 1, "---", 0, false));
    }

    void RefreshLeaderboard(List<LeaderboardEntryData> entries, string currentNick)
    {
        if (leaderboardRowsContainer == null) return;
        leaderboardRowsContainer.Clear();
        int count = Mathf.Min(entries.Count, LbRows);
        for (int i = 0; i < count; i++)
        {
            var e = entries[i];
            string name = CleanName(e.PlayerName);
            bool isCurrent = name.Equals(currentNick, System.StringComparison.OrdinalIgnoreCase);
            leaderboardRowsContainer.Add(MakeRow(e.Rank, name, e.Score, isCurrent));
        }
        for (int i = count; i < LbRows; i++)
            leaderboardRowsContainer.Add(MakeRow(i + 1, "---", 0, false));
    }

    VisualElement MakeRow(int rank, string name, int score, bool isCurrent)
    {
        bool isEmpty = name == "---";

        var row = new VisualElement();
        row.AddToClassList("lb-row");
        if (isCurrent) row.AddToClassList("lb-row--current");

        Color accent = isCurrent ? RankGold :
                       rank == 1 ? RankPink  :
                       rank <= 3 ? RankLilac : RankMint;

        var rankLabel = new Label(rank.ToString());
        rankLabel.AddToClassList("lb-rank");
        rankLabel.style.color = isEmpty
            ? new Color(accent.r, accent.g, accent.b, 0.22f)
            : accent;
        row.Add(rankLabel);

        var nameLabel = new Label(name);
        nameLabel.AddToClassList("lb-name");
        if (isEmpty)       nameLabel.style.color = new Color(Cream.r, Cream.g, Cream.b, 0.22f);
        else if (isCurrent) nameLabel.style.color = RankGold;
        row.Add(nameLabel);

        var dotsLabel = new Label("· · · · · · · · · · ·");
        dotsLabel.AddToClassList("lb-dots");
        row.Add(dotsLabel);

        var scoreLabel = new Label(isEmpty ? "--" : score.ToString());
        scoreLabel.AddToClassList("lb-score");
        if (isEmpty)       scoreLabel.style.color = new Color(Cream.r, Cream.g, Cream.b, 0.22f);
        else if (isCurrent) scoreLabel.style.color = RankGold;
        row.Add(scoreLabel);

        return row;
    }

    static string CleanName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "?";
        int hash = raw.IndexOf('#');
        if (hash > 0) raw = raw.Substring(0, hash);
        if (raw.Length > 8) raw = raw.Substring(0, 8);
        return raw;
    }

    // ── Start overlay ─────────────────────────────────────────────────────────

    void OnStartButtonClicked()
    {
        string nick = nicknameField != null ? nicknameField.value.Trim() : "Player";
        if (string.IsNullOrEmpty(nick)) nick = "Player";
        if (nick.Length > 12) nick = nick.Substring(0, 12);

        PlayerPrefs.SetString("PlayerNickname", nick);
        PlayerPrefs.Save();

        if (isDead)
        {
            // Chiamato da "Change Name" dopo la morte — ricarica la scena
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (arcadeLbManager != null)
            arcadeLbManager.PlayerNickname = nick;

        if (startOverlay != null)
            startOverlay.style.display = DisplayStyle.None;

        if (scoreContainer != null)
            scoreContainer.style.display = DisplayStyle.Flex;

        gameStarted = true;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    void OnChangeNameClicked()
    {
        if (gameOverOverlay != null)  gameOverOverlay.style.display  = DisplayStyle.None;
        if (scoreContainer != null)   scoreContainer.style.display   = DisplayStyle.None;
        if (nicknameField != null)    nicknameField.value = PlayerPrefs.GetString("PlayerNickname", "");
        if (startOverlay != null)     startOverlay.style.display     = DisplayStyle.Flex;
    }

    // ── Restart ───────────────────────────────────────────────────────────────

    void RestartGame()
    {
        StartCoroutine(RestartSequence());
    }

    IEnumerator RestartSequence()
    {
        if (restartButton != null)
        {
            restartButton.SetEnabled(false);
            yield return StartCoroutine(BounceButton(restartButton));
        }

        if (gameOverOverlay != null)
            gameOverOverlay.style.display = DisplayStyle.None;

        yield return new WaitForSecondsRealtime(0.15f);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ── Palette ───────────────────────────────────────────────────────────────

    void OnDestroy()
    {
        PaletteManager.OnPaletteChanged -= OnPaletteChanged;
    }

    void OnPaletteChanged()
    {
        if (shipShape != null && PaletteManager.Instance != null)
            StartCoroutine(TransitionPlayerColor(PaletteManager.Instance.PlayerColor));
    }

    IEnumerator TransitionPlayerColor(Color target)
    {
        Color start = shipShape.CurrentColor;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 1.5f;
            shipShape.SetColor(Color.Lerp(start, target, Mathf.Clamp01(t)));
            yield return null;
        }
        shipShape.SetColor(target);
    }

    // ── UI Toolkit helpers ────────────────────────────────────────────────────

    IEnumerator BounceButton(VisualElement el)
    {
        float t = 0f;
        const float duration = 0.2f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float s = 1f + 0.25f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            el.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
            yield return null;
        }
        el.style.scale = new StyleScale(new Scale(Vector2.one));
    }

    // ── Physics ───────────────────────────────────────────────────────────────

    void ScreenWrap()
    {
        Vector3 pos = transform.position;
        float height = Camera.main.orthographicSize;
        float width  = height * Camera.main.aspect;

        if (pos.x >  width)  pos.x = -width;
        else if (pos.x < -width)  pos.x =  width;
        if (pos.y >  height) pos.y = -height;
        else if (pos.y < -height) pos.y =  height;

        transform.position = pos;
    }
}
