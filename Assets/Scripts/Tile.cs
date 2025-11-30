using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x, z;
    public bool isLight;                    // bu kare açık mı koyu mu?

    [Header("Base Square")]
    public Renderer rend;                   // alttaki kare (SquareLight / SquareDark)

    [Header("Highlight Overlay")]
    public Renderer highlightRenderer;      // üstteki saydam quad (HighlightOverlay)

    void Awake()
    {
        // Alt kare renderer'ını garantiye al
        if (!rend)
        {
            rend = GetComponent<Renderer>();
            if (!rend)
                rend = GetComponentInChildren<Renderer>();
        }

        // Overlay başta görünmesin
        if (highlightRenderer != null)
        {
            highlightRenderer.enabled = false;
        }
    }
}
