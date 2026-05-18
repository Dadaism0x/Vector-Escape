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
    private bool gameStarted = false;

    // ── Name input ────────────────────────────────────────────────────────────
    private NicknameInputHandler nameInputHandler;

    // ── Game Over Card ────────────────────────────────────────────────────────
    private VisualElement gameOverOverlay;
    private Label finalScoreLabel;
    private VisualElement leaderboardRowsContainer;
    private Button restartButton;
    private Button changeNameButton;

    // ── Online leaderboard ────────────────────────────────────────────────────
    private ArcadeLeaderboardManager arcadeLbManager;

    // ── Row color palette ─────────────────────────────────────────────────────
    static readonly Color[] RankColors =
    {
        new Color(1.00f, 0.71f, 0.64f), // #1  corallo soft
        new Color(1.00f, 0.87f, 0.66f), // #2  giallo sabbia
        new Color(0.71f, 0.92f, 0.84f), // #3  menta
        new Color(0.66f, 0.85f, 0.92f), // #4  acqua
        new Color(0.72f, 0.75f, 1.00f), // #5  lilla cielo
        new Color(0.79f, 0.72f, 0.91f), // #6  lavanda
        new Color(0.79f, 0.72f, 0.91f), // #7
        new Color(0.79f, 0.72f, 0.91f), // #8
        new Color(0.79f, 0.72f, 0.91f), // #9
        new Color(0.79f, 0.72f, 0.91f), // #10
    };
    static readonly Color RankGold  = new Color(1.00f, 0.86f, 0.51f);
    static readonly Color Cream     = new Color(1.00f, 0.95f, 0.87f);

    const int LbRows = 10;
    bool isCompactLayout = false;

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

        arcadeLbManager = FindFirstObjectByType<ArcadeLeaderboardManager>();

        moveForward.Enable();
        lookPosition.Enable();

        UIDocument uiDoc = FindFirstObjectByType<UIDocument>();
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

        }

        AdjustLayout();

        nameInputHandler = FindFirstObjectByType<NicknameInputHandler>();
        if (nameInputHandler != null)
        {
            nameInputHandler.Initialize(root);
            nameInputHandler.OnNameConfirmed += HandleNameConfirmed;
        }

        if (nameInputHandler != null && nameInputHandler.HasSavedNickname)
        {
            if (arcadeLbManager != null) arcadeLbManager.PlayerNickname = nameInputHandler.SavedNickname;
            nameInputHandler.Hide();
            if (scoreContainer != null) scoreContainer.style.display = DisplayStyle.Flex;
            gameStarted = true;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
        else
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;
            nameInputHandler?.Show();
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

        var cleanedNames = new List<string>(count);
        for (int i = 0; i < count; i++)
            cleanedNames.Add(CleanName(entries[i].PlayerName));
        NicknameInputHandler.SetTakenNames(cleanedNames);

        for (int i = 0; i < count; i++)
        {
            bool isCurrent = cleanedNames[i].Equals(currentNick, System.StringComparison.OrdinalIgnoreCase);
            leaderboardRowsContainer.Add(MakeRow(entries[i].Rank, cleanedNames[i], entries[i].Score, isCurrent));
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
        if (isCompactLayout) { row.style.minHeight = 18; row.style.marginBottom = 1; }

        int colorIdx = Mathf.Clamp(rank - 1, 0, RankColors.Length - 1);
        Color accent = isCurrent ? RankGold : RankColors[colorIdx];

        var rankLabel = new Label(rank.ToString());
        rankLabel.AddToClassList("lb-rank");
        rankLabel.style.color = isEmpty
            ? new Color(accent.r, accent.g, accent.b, 0.22f)
            : accent;
        row.Add(rankLabel);

        var nameLabel = new Label(name);
        nameLabel.AddToClassList("lb-name");
        nameLabel.style.color = isEmpty
            ? new Color(accent.r, accent.g, accent.b, 0.22f)
            : accent;
        row.Add(nameLabel);

        var dotsLabel = new Label("· · · · ·");
        dotsLabel.AddToClassList("lb-dots");
        dotsLabel.style.flexGrow   = 0;
        dotsLabel.style.flexShrink = 0;
        dotsLabel.style.width      = 22;
        row.Add(dotsLabel);

        var scoreLabel = new Label(isEmpty ? "--" : score.ToString());
        scoreLabel.AddToClassList("lb-score");
        if (isEmpty)        scoreLabel.style.color = new Color(Cream.r, Cream.g, Cream.b, 0.22f);
        else if (isCurrent) scoreLabel.style.color = RankGold;
        row.Add(scoreLabel);

        return row;
    }

    static string CleanName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "?";
        int hash = raw.IndexOf('#');
        if (hash > 0) raw = raw.Substring(0, hash);
        if (raw.Length > 12) raw = raw.Substring(0, 11) + "…";
        return raw;
    }

    // ── Name input ───────────────────────────────────────────────────────────

    void HandleNameConfirmed(string nick)
    {
        if (arcadeLbManager != null) arcadeLbManager.PlayerNickname = nick;

        if (isDead)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (scoreContainer != null) scoreContainer.style.display = DisplayStyle.Flex;
        moveForward.Enable();
        gameStarted = true;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    void OnChangeNameClicked()
    {
        if (gameOverOverlay != null) gameOverOverlay.style.display = DisplayStyle.None;
        if (scoreContainer  != null) scoreContainer.style.display  = DisplayStyle.None;
        nameInputHandler?.Show();
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

    // ── Mobile layout ─────────────────────────────────────────────────────────

    void AdjustLayout()
    {
        if (root == null) return;
        float dpi = Screen.dpi > 0f ? Screen.dpi : 96f;
        float cssH = Screen.height * 96f / dpi;
        isCompactLayout = cssH < 400f;

        var card = root.Q<VisualElement>("GameOverCard");
        if (card == null) return;

        var titleEl  = root.Q<Label>("GameOverTitle");
        var scoreEl  = root.Q<Label>("FinalScore");
        var lbRowsEl = root.Q<VisualElement>("LeaderboardRows");

        card.Clear();

        card.style.flexDirection = FlexDirection.Row;
        card.style.alignItems    = Align.Stretch;
        card.style.paddingTop    = isCompactLayout ? 10 : 14;
        card.style.paddingBottom = isCompactLayout ? 10 : 14;
        card.style.paddingLeft   = isCompactLayout ? 14 : 20;
        card.style.paddingRight  = isCompactLayout ? 14 : 20;
        card.style.width         = new StyleLength(new Length(92f, LengthUnit.Percent));
        card.style.maxWidth      = new StyleLength(380f);

        // ── Left column: title + score + buttons ──────────────────────────────
        var leftCol = new VisualElement();
        leftCol.style.flexDirection    = FlexDirection.Column;
        leftCol.style.alignItems       = Align.Center;
        leftCol.style.justifyContent   = Justify.Center;
        leftCol.style.width            = new StyleLength(new Length(40f, LengthUnit.Percent));
        leftCol.style.flexShrink       = 0;
        leftCol.style.paddingRight     = isCompactLayout ? 10 : 14;
        leftCol.style.borderRightWidth = 1f;
        leftCol.style.borderRightColor = new StyleColor(new Color(0.44f, 0.31f, 0.71f, 0.35f));

        if (titleEl != null)
        {
            titleEl.style.marginBottom = 0;
            leftCol.Add(titleEl);
        }
        if (scoreEl != null)
        {
            scoreEl.style.fontSize     = isCompactLayout ? 20 : 26;
            scoreEl.style.marginBottom = isCompactLayout ? 6 : 8;
            leftCol.Add(scoreEl);
        }
        if (restartButton != null)
        {
            restartButton.style.paddingTop    = isCompactLayout ? 6 : 8;
            restartButton.style.paddingBottom = isCompactLayout ? 6 : 8;
            restartButton.style.paddingLeft   = isCompactLayout ? 8 : 10;
            restartButton.style.paddingRight  = isCompactLayout ? 8 : 10;
            restartButton.style.marginTop     = isCompactLayout ? 2 : 4;
            restartButton.style.alignSelf     = Align.Center;
            leftCol.Add(restartButton);
        }
        if (changeNameButton != null)
        {
            changeNameButton.style.paddingTop    = 4;
            changeNameButton.style.paddingBottom = 4;
            changeNameButton.style.paddingLeft   = isCompactLayout ? 6 : 8;
            changeNameButton.style.paddingRight  = isCompactLayout ? 6 : 8;
            if (isCompactLayout) changeNameButton.style.fontSize = 9;
            changeNameButton.style.marginTop = isCompactLayout ? 3 : 5;
            leftCol.Add(changeNameButton);
        }

        // ── Right column: scrollable leaderboard ──────────────────────────────
        var rightCol = new VisualElement();
        rightCol.style.flexDirection  = FlexDirection.Column;
        rightCol.style.flexGrow       = 1;
        rightCol.style.paddingLeft    = isCompactLayout ? 10 : 14;
        rightCol.style.justifyContent = Justify.Center;

        var scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.style.flexShrink  = 0;
        scroll.style.height      = new StyleLength(isCompactLayout ? 110f : 155f);
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        scroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;

        if (lbRowsEl != null)
        {
            lbRowsEl.style.marginBottom = 0;
            lbRowsEl.style.alignSelf    = Align.Stretch;
            scroll.Add(lbRowsEl);
        }
        rightCol.Add(scroll);

        card.Add(leftCol);
        card.Add(rightCol);
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

        if      (pos.x >  width)  pos.x = -width;
        else if (pos.x < -width)  pos.x =  width;
        if      (pos.y >  height) pos.y = -height;
        else if (pos.y < -height) pos.y =  height;

        transform.position = pos;
    }
}
