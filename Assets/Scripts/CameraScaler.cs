using UnityEngine;

public class CameraScaler : MonoBehaviour
{
    // Aumenta questo valore se vuoi vedere più spazio "extra" oltre i bordi
    public float padding = 1.1f; 

    [Header("Borders Reference")]
    public Transform borderTop;
    public Transform borderBottom;
    public Transform borderLeft;
    public Transform borderRight;

    void Update()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null || borderTop == null || borderLeft == null) return;

        // 1. Calcoliamo l'altezza e la larghezza del tuo campo di gioco attuale
        // Basandoci sulla posizione dei tuoi bordi viola
        float playAreaHeight = Vector3.Distance(borderTop.position, borderBottom.position);
        float playAreaWidth = Vector3.Distance(borderLeft.position, borderRight.position);

        // 2. Calcoliamo la dimensione ortografica necessaria
        // Per l'altezza
        float sizeBasedOnHeight = playAreaHeight / 2f;
        // Per la larghezza (considerando l'aspect ratio dello schermo attuale)
        float sizeBasedOnWidth = (playAreaWidth / cam.aspect) / 2f;

        // 3. Scegliamo la dimensione maggiore per essere sicuri che tutto sia dentro
        cam.orthographicSize = Mathf.Max(sizeBasedOnHeight, sizeBasedOnWidth) * padding;
    }
}