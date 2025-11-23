using UnityEngine;

public class SelectionController : MonoBehaviour
{
    public Camera cam;

    // Normal kare materyalleri
    public Material normalLightMat;
    public Material normalDarkMat;

    // Seçili kare için vurgu
    public Material highlightMat;

    private Tile  selectedTile;   // highlight'lı kare
    private Piece selectedPiece;  // seçili taş

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        // Oyun bittiyse veya terfi bekliyorsak input alma
        if (PieceManager.I != null &&
            (PieceManager.I.promotionPending || PieceManager.I.gameOver))
            return;

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
        if (PieceManager.I == null) return;

        // Terfi paneli açıkken veya oyun bittiyken hiçbir şey yapma
        if (PieceManager.I.promotionPending || PieceManager.I.gameOver)
            return;

        // Karede taş var mı?
        var pieceOnTile = PieceManager.I.GetAt(tile.x, tile.z);

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
        bool moved = PieceManager.I.Move(selectedPiece, tile.x, tile.z);
        if (moved)
        {
           
        }

        // Görseli ve seçimi temizle
        ClearSelection();
    }

    bool IsCurrentTurn(PieceSide side)
    {
        // Artık TurnManager yerine PieceManager.sideToMove kullanıyoruz
        if (PieceManager.I == null) return true;

        return PieceManager.I.sideToMove == side;
    }

    // --- Yardımcı: güvenli Renderer bulucu (tile.rend boşsa GetComponent ile bulur)
    Renderer GetRenderer(Tile t)
    {
        if (t == null) return null;
        return t.rend != null ? t.rend : t.GetComponent<Renderer>();
    }

    void SetSelection(Tile tile, Piece piece)
    {
        // Eski highlight'ı kapat
        if (selectedTile != null) ResetTileVisual(selectedTile);

        selectedTile  = tile;
        selectedPiece = piece;

        var r = GetRenderer(selectedTile);
        if (r) r.sharedMaterial = highlightMat;
    }

    void ResetTileVisual(Tile t)
    {
        var r = GetRenderer(t);
        if (!r) return;
        r.sharedMaterial = t.isLight ? normalLightMat : normalDarkMat;
    }

    void ClearSelection()
    {
        if (selectedTile != null) ResetTileVisual(selectedTile);
        selectedTile = null;
        selectedPiece = null;
    }
}
