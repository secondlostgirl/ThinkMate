using UnityEngine;

public class SelectionController : MonoBehaviour
{
    public Camera cam;

    // Normal kare materyalleri
    public Material normalLightMat;
    public Material normalDarkMat;

    // Seçili kare için vurgu
    public Material highlightMat;

    private Tile selectedTile;          // highlight'lı kare
    private Piece selectedPiece;        // seçili taş

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleClick(Input.mousePosition);

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            HandleClick(Input.GetTouch(0).position);

        if (Input.GetKeyDown(KeyCode.Escape))
            ClearSelection();
    }

    void HandleClick(Vector2 screenPos)
    {
        if (!cam) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            var tile = hit.collider.GetComponent<Tile>();
            if (tile != null) OnTileClicked(tile);
        }
    }

    void OnTileClicked(Tile tile)
    {
        // Karede taş var mı?
        var pieceOnTile = PieceManager.I ? PieceManager.I.GetAt(tile.x, tile.z) : null;

        // Hiç seçim yoksa ve karede taş varsa: SIRA kontrolüyle seç
        if (selectedPiece == null)
        {
            if (pieceOnTile != null && IsCurrentTurn(pieceOnTile.side))
            {
                SetSelection(tile, pieceOnTile);
            }
            return;
        }

        // Seçiliyken kendi taşına tıklarsan: seçimi o taşa taşı (reselect)
        if (pieceOnTile != null && pieceOnTile.side == selectedPiece.side)
        {
            SetSelection(tile, pieceOnTile);
            return;
        }

        // Hedefe taşıma denemesi
        bool moved = PieceManager.I && PieceManager.I.Move(selectedPiece, tile.x, tile.z);
        if (moved)
        {
            TurnManager.I?.Next();     // başarılı hamlede sıra değişsin
        }

        // Görseli ve seçimi temizle
        ClearSelection();
    }

    bool IsCurrentTurn(PieceSide side)
    {
        if (TurnManager.I == null) return true; // turn manager yoksa serbest
        return (TurnManager.I.current == Turn.White && side == PieceSide.White) ||
               (TurnManager.I.current == Turn.Black && side == PieceSide.Black);
    }

    void SetSelection(Tile tile, Piece piece)
    {
        // Eski highlight'ı kapat
        if (selectedTile != null) ResetTileVisual(selectedTile);

        selectedTile  = tile;
        selectedPiece = piece;

        if (selectedTile.rend != null) selectedTile.rend.sharedMaterial = highlightMat;

        // Debug.Log($"Selected: {selectedPiece.side} {selectedPiece.type} at {Format(tile)}");
    }

    string Format(Tile t) => $"{(char)('A' + t.x)}{t.z + 1}";

    void ResetTileVisual(Tile t)
    {
        if (t == null || t.rend == null) return;
        t.rend.sharedMaterial = t.isLight ? normalLightMat : normalDarkMat;
    }

    void ClearSelection()
    {
        if (selectedTile != null) ResetTileVisual(selectedTile);
        selectedTile = null;
        selectedPiece = null;
    }
}
