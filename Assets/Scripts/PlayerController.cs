using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement; // Necessario per ricaricare la scena

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float thrustForce = 25f; 
    public float maxSpeed = 12f;
    public GameObject boosterFlame;
    public GameObject explosionEffect;

    [Header("Input Actions")]
    public InputAction moveForward;
    public InputAction lookPosition;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private bool isDead = false;

    // Logica Score
    private float score = 0;
    private int highScore = 0;

    // Riferimenti UI Toolkit
    private VisualElement root;
    private Button restartButton;
    private Label highScoreLabel;
    private Label scoreLabel;

    void Start()
    {
        Application.targetFrameRate = -1;
        rb = GetComponent<Rigidbody2D>();
        highScore = PlayerPrefs.GetInt("HighScore", 0); // Carica il record salvato
        
        moveForward.Enable();
        lookPosition.Enable();

        UIDocument uiDoc = FindObjectOfType<UIDocument>();
        if (uiDoc != null)
        {
            root = uiDoc.rootVisualElement;
            
            // --- COLLEGAMENTI UI (Verifica i nomi nel UI Builder!) ---
            restartButton = root.Q<Button>("RestartButton"); 
            highScoreLabel = root.Q<Label>("HighScoreLabel");
            scoreLabel = root.Q<Label>("ScoreLabel");

            // Configura il pulsante Restart
            if (restartButton != null)
            {
                restartButton.clicked += RestartGame; // Collega la funzione al click
                restartButton.style.display = DisplayStyle.None;
            }

            if (highScoreLabel != null) highScoreLabel.style.display = DisplayStyle.None;
        }
    }

    void Update()
    {
        if (isDead) return;

        // Aumenta lo score nel tempo
        score += Time.deltaTime * 10; 
        if (scoreLabel != null) scoreLabel.text = "Score: " + Mathf.FloorToInt(score).ToString();

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
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") && !isDead)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        isDead = true;
        
        if (explosionEffect != null) 
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in allRenderers) r.enabled = false;
        
        if (boosterFlame != null) boosterFlame.SetActive(false);
        rb.linearVelocity = Vector2.zero;

        // Gestione High Score
        if (score > highScore)
        {
            highScore = Mathf.FloorToInt(score);
            PlayerPrefs.SetInt("HighScore", highScore);
        }

        // Mostra UI
        if (restartButton != null) restartButton.style.display = DisplayStyle.Flex;
        if (highScoreLabel != null) 
        {
            highScoreLabel.text = "Best Score: " + highScore;
            highScoreLabel.style.display = DisplayStyle.Flex;
        }
    }

    void RestartGame()
    {
        // Ricarica la scena attuale
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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