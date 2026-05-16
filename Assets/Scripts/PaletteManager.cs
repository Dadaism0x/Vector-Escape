using UnityEngine;
using System.Collections;

public class PaletteManager : MonoBehaviour
{
    public static PaletteManager Instance { get; private set; }

    [Header("Milestone")]
    public int milestoneInterval = 100;

    // Pastel palettes: (background, obstacle color)
    private static readonly Color[] backgrounds = {
        new Color(0.10f, 0.06f, 0.16f), // purple night
        new Color(0.05f, 0.12f, 0.11f), // deep teal
        new Color(0.12f, 0.05f, 0.08f), // deep rose
        new Color(0.12f, 0.09f, 0.04f), // deep peach
        new Color(0.05f, 0.10f, 0.07f), // deep green
        new Color(0.04f, 0.06f, 0.13f), // deep blue
    };

    private static readonly Color[] obstacleColors = {
        new Color(0.80f, 0.65f, 0.95f), // lavender
        new Color(0.58f, 0.89f, 0.83f), // mint
        new Color(0.96f, 0.76f, 0.91f), // pink
        new Color(0.98f, 0.70f, 0.53f), // peach
        new Color(0.65f, 0.89f, 0.63f), // sage green
        new Color(0.54f, 0.71f, 0.98f), // periwinkle
    };

    // Complementary to obstacles so the player always stands out
    private static readonly Color[] playerColors = {
        new Color(0.98f, 0.89f, 0.69f), // soft yellow  (vs lavender)
        new Color(0.96f, 0.76f, 0.91f), // pink         (vs mint)
        new Color(0.58f, 0.89f, 0.83f), // mint         (vs pink)
        new Color(0.54f, 0.71f, 0.98f), // periwinkle   (vs peach)
        new Color(0.80f, 0.65f, 0.95f), // lavender     (vs green)
        new Color(0.98f, 0.70f, 0.53f), // peach        (vs periwinkle)
    };

    public static event System.Action OnPaletteChanged;

    private int currentIndex = 0;
    private Camera mainCam;

    void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
    }

    void Start()
    {
        mainCam.backgroundColor = backgrounds[0];
    }

    public Color ObstacleColor => obstacleColors[currentIndex];
    public Color PlayerColor  => playerColors[currentIndex];

    public void CheckMilestone(int score)
    {
        int idx = (score / milestoneInterval) % backgrounds.Length;
        if (idx == currentIndex) return;
        currentIndex = idx;
        StopAllCoroutines();
        StartCoroutine(TransitionBackground(backgrounds[currentIndex]));
        OnPaletteChanged?.Invoke();
    }

    IEnumerator TransitionBackground(Color target)
    {
        Color start = mainCam.backgroundColor;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 1.5f;
            mainCam.backgroundColor = Color.Lerp(start, target, t);
            yield return null;
        }
        mainCam.backgroundColor = target;
    }
}
