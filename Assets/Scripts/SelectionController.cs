using System.Collections.Generic;
using UnityEngine;

public class SelectionController : MonoBehaviour
{
    public Camera cam;

    // Karelerin normal materyalleri (fallback için, overlay kullanıyoruz aslında)
    public Material normalLightMat;
    public Material normalDarkMat;

    // Seçili kare ve legal hamle overlay materyalleri
    public Material highlightMat;      // seçili kare
    public Material moveHighlightMat;  // legal hedef kare

    private Tile  selectedTile;        // şu an seçili kare
    private Piece selectedPiece;       // şu an seçili taş

    // Legal hedef karelerin listesi (sonra hepsini kapatmak için)
    private readonly List<Tile> moveHighlightTiles = new List<Tile>();

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        // Terfi bekliyorsak veya oyun bittiyse input alma
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

        // Terfi paneli açıksa veya oyun bittiyse hiçbir şey yapma
        if (PieceManager.I.promotionPending || PieceManager.I.gameOver)
            return;

        // Karede taş var mı?
        var pieceOnTile = PieceManager.I.GetAt(tile.x, tile.z);

        // Henüz seçim yoksa ve karede taş varsa → ve OYUN SIRASI o taştaysa seç
        if (selectedPiece == null)
        {
            if (pieceOnTile != null && IsCurrentTurn(pieceOnTile.side))
            {
                SetSelection(tile, pieceOnTile);
            }
            return;
        }

        // Seçiliyken kendi taşına tıklarsan → o taşa yeniden seç
        if (pieceOnTile != null && pieceOnTile.side == selectedPiece.side)
        {
            SetSelection(tile, pieceOnTile);
            return;
        }

        // Hedef kareye hamle denemesi
        bool moved = PieceManager.I.Move(selectedPiece, tile.x, tile.z);

        // DİKKAT: PieceManager.Move başarılı olduğunda artık KENDİ İÇİNDE
        // snapshot alıp AfterMove() çağırıyor; burada tekrar çağırmıyoruz!

        // Seçimi ve overlay'leri temizle
        ClearSelection();
    }

    bool IsCurrentTurn(PieceSide side)
    {
        if (PieceManager.I == null) return true;
        return PieceManager.I.sideToMove == side;
    }

    // ------------------- HIGHLIGHT / OVERLAY YARDIMCILARI -------------------

    void SetSelection(Tile tile, Piece piece)
    {
        // Önce eski seçimi + legal kareleri tamamen temizle
        ClearSelection();

        selectedTile  = tile;
        selectedPiece = piece;

        // Seçili kareyi highlight et
        EnableOverlay(tile, highlightMat);

        // Legal hamleleri göster
        HighlightLegalMoves(piece);
    }

    void HighlightLegalMoves(Piece piece)
    {
        moveHighlightTiles.Clear();

        if (PieceManager.I == null || piece == null) return;

        var allTiles = FindObjectsOfType<Tile>();

        foreach (var t in allTiles)
        {
            // aynı kareyi (taşın şu an bulunduğu kare) atla
            if (t.x == piece.x && t.z == piece.z)
                continue;

            // PieceManager'a "bu kareye legal hamle mi?" diye soracağız
            if (!PieceManager.I.IsLegalMove(piece, t.x, t.z))
                continue;

            // Legal ise overlay aç
            EnableOverlay(t, moveHighlightMat);
            moveHighlightTiles.Add(t);
        }
    }

    void EnableOverlay(Tile t, Material mat)
    {
        if (t == null) return;

        if (t.highlightRenderer != null)
        {
            if (mat != null)
                t.highlightRenderer.sharedMaterial = mat;

            t.highlightRenderer.enabled = true;
        }
        else
        {
            // Overlay yoksa, eski sisteme geri dön: taban materyali değiştir
            var r = t.rend != null ? t.rend : t.GetComponent<Renderer>();
            if (!r) return;

            if (mat != null)
                r.sharedMaterial = mat;
        }
    }

    void DisableOverlay(Tile t)
    {
        if (t == null) return;

        if (t.highlightRenderer != null)
        {
            t.highlightRenderer.enabled = false;
        }
        else
        {
            // Fallback: taban materyali eski haline döndür
            var r = t.rend != null ? t.rend : t.GetComponent<Renderer>();
            if (!r) return;

            r.sharedMaterial = t.isLight ? normalLightMat : normalDarkMat;
        }
    }

    void ClearSelection()
    {
        // Seçili kare overlay'ini kapat
        if (selectedTile != null)
            DisableOverlay(selectedTile);

        // Legal kare overlay'lerini kapat
        if (moveHighlightTiles != null)
        {
            foreach (var t in moveHighlightTiles)
            {
                if (t != null)
                    DisableOverlay(t);
            }
            moveHighlightTiles.Clear();
        }

        selectedTile  = null;
        selectedPiece = null;
    }
}
