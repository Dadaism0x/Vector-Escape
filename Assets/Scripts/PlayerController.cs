using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

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

    private VisualElement root;
    private Button restartButton;
    private Label scoreLabel;
    private VisualElement leaderboardContainer;
    private Label[] leaderboardLabels = new Label[5];

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

        moveForward.Enable();
        lookPosition.Enable();

        UIDocument uiDoc = FindObjectOfType<UIDocument>();
        if (uiDoc != null)
        {
            root = uiDoc.rootVisualElement;
            restartButton = root.Q<Button>("RestartButton");
            scoreLabel = root.Q<Label>("ScoreLabel");
            leaderboardContainer = root.Q<VisualElement>("LeaderboardContainer");

            for (int i = 0; i < 5; i++)
                leaderboardLabels[i] = root.Q<Label>("LB_" + i);

            if (restartButton != null)
            {
                restartButton.clicked += RestartGame;
                restartButton.style.display = DisplayStyle.None;
            }

            if (leaderboardContainer != null)
                leaderboardContainer.style.display = DisplayStyle.None;
        }
    }

    void Update()
    {
        if (isDead) return;

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
        if (isDead) return;

        if (moveForward.IsPressed())
        {
            rb.AddForce(moveDirection * thrustForce);
            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") && !isHit)
        {
            isHit = true;
            StartCoroutine(DeathSequence());
        }
    }

    IEnumerator DeathSequence()
    {
        isDead = true;

        // Freeze sull'impatto
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        CameraShake.Instance?.Shake(0.5f, 0.3f);

        yield return new WaitForSecondsRealtime(0.07f);

        // La nave esplode e sparisce durante il freeze
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        if (boosterFlame != null) boosterFlame.SetActive(false);
        rb.linearVelocity = Vector2.zero;

        // Slow-mo aftermath: gli ostacoli continuano lentamente
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

        var scores = Leaderboard.GetScores();
        if (leaderboardContainer != null)
        {
            string[] medals = { "", "", "", "", "" };
            Color[] rankColors = {
                new Color(0.98f, 0.89f, 0.63f), // gold
                new Color(0.80f, 0.85f, 0.92f), // silver
                new Color(0.98f, 0.72f, 0.55f), // bronze
                new Color(0.70f, 0.56f, 0.82f), // dim lavender
                new Color(0.70f, 0.56f, 0.82f), // dim lavender
            };
            for (int i = 0; i < leaderboardLabels.Length; i++)
            {
                if (leaderboardLabels[i] == null) continue;
                string value = scores[i] > 0 ? scores[i].ToString() : "—";
                leaderboardLabels[i].text = medals[i] + (i + 1) + ".  " + value;
                leaderboardLabels[i].style.color = rankColors[i];
                leaderboardLabels[i].style.fontSize = i == 0 ? 13 : 12;
            }
            leaderboardContainer.style.display = DisplayStyle.Flex;
            StartCoroutine(SlideIn(leaderboardContainer, 350f, 0.45f));
        }

        if (restartButton != null)
            restartButton.style.display = DisplayStyle.Flex;
    }

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

        if (leaderboardContainer != null)
            yield return StartCoroutine(SlideOut(leaderboardContainer, 350f, 0.3f));

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator SlideIn(VisualElement el, float fromX, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float x = Mathf.Lerp(fromX, 0f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            el.style.translate = new StyleTranslate(new Translate(new Length(x), new Length(0f)));
            yield return null;
        }
        el.style.translate = new StyleTranslate(new Translate(new Length(0f), new Length(0f)));
    }

    IEnumerator SlideOut(VisualElement el, float toX, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float x = Mathf.Lerp(0f, toX, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            el.style.translate = new StyleTranslate(new Translate(new Length(x), new Length(0f)));
            yield return null;
        }
    }

    IEnumerator BounceButton(VisualElement el)
    {
        float t = 0f;
        float duration = 0.2f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float s = 1f + 0.25f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            el.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
            yield return null;
        }
        el.style.scale = new StyleScale(new Scale(Vector2.one));
    }

    void ScreenWrap()
    {
        Vector3 pos = transform.position;
        float height = Camera.main.orthographicSize;
        float width = height * Camera.main.aspect;

        if (pos.x > width) pos.x = -width;
        else if (pos.x < -width) pos.x = width;
        if (pos.y > height) pos.y = -height;
        else if (pos.y < -height) pos.y = height;

        transform.position = pos;
    }
}
