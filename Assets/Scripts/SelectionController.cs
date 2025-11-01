using UnityEngine;

public class SelectionController : MonoBehaviour
{
    public Camera cam;

    // Normal tema materyalleri
    public Material normalLightMat;
    public Material normalDarkMat;

    // Seçili kare için vurgu materyali
    public Material highlightMat;

    private Tile selected;

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        // Mouse
        if (Input.GetMouseButtonDown(0))
            HandleClick(Input.mousePosition);

        // Mobil dokunma (ilerisi için şimdiden)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            HandleClick(Input.GetTouch(0).position);

        // ESC ile seçimi iptal
        if (Input.GetKeyDown(KeyCode.Escape))
            ClearSelection();
    }

    void HandleClick(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            var tile = hit.collider.GetComponent<Tile>();
            if (tile) OnTileClicked(tile);
        }
    }

    void OnTileClicked(Tile tile)
    {
        // 1. tık: seç
        if (selected == null)
        {
            selected = tile;
            if (selected.rend) selected.rend.sharedMaterial = highlightMat;
            Debug.Log($"Selected: {Format(selected)}");
            return;
        }

        // Aynı kareye tıklandıysa iptal et
        if (tile == selected)
        {
            ClearSelection();
            return;
        }

        // 2. tık: hedef
        Debug.Log($"Move {Format(selected)} -> {Format(tile)}");

        // Şimdilik sadece görsel reset; taş taşıma sonrasında eklenecek
        ResetTileVisual(selected);
        selected = null;
    }

    string Format(Tile t) => $"{(char)('A' + t.x)}{t.z + 1}";

  void ResetTileVisual(Tile t)
{
    if (!t || !t.rend) return;
    t.rend.sharedMaterial = t.isLight ? normalLightMat : normalDarkMat;
}


    void ClearSelection()
    {
        if (selected != null) ResetTileVisual(selected);
        selected = null;
    }
    
}
