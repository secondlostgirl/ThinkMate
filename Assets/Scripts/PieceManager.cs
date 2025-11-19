using UnityEngine;

[System.Serializable]
public struct PieceSprites
{
    // (İsteğe bağlı) Inspector’dan bağlamak istersen:
    public Sprite wPawn, wRook, wKnight, wBishop, wQueen, wKing;
    public Sprite bPawn, bRook, bKnight, bBishop, bQueen, bKing;
}

public class PieceManager : MonoBehaviour
{
    public static PieceManager I;
    public Piece piecePrefab;
    public int boardSize = 8;
    public float tileSize = 1f;

    // Inspector’dan bağlayabilirsin; boş bırakılırsa Resources’tan yüklenir.
    public PieceSprites sprites;

    public Piece[,] grid;
    private Vector2Int? enPassantTarget = null;

    void Awake()
    {
        I = this;
        grid = new Piece[boardSize, boardSize];
    }

Vector3 ToWorld(int file, int rank)
{
    float offset = -(boardSize - 1) * 0.5f * tileSize;

    return new Vector3(
        offset + file * tileSize,   // X
        offset + rank * tileSize,   // Y   (tahta ile bire bir aynı)
        0f                          // Z
    );
}




    public void ClearAll()
    {
        foreach (var p in GetComponentsInChildren<Piece>())
        {
#if UNITY_EDITOR
            DestroyImmediate(p.gameObject);
#else
            Destroy(p.gameObject);
#endif
        }
        grid = new Piece[boardSize, boardSize];
    }

    // --- Sprite seçici: önce Inspector, boşsa Resources/Sprites/<kod> ---
    Sprite GetSprite(PieceType type, PieceSide side)
    {
        // 1) Inspector
        Sprite s = null;
        if (side == PieceSide.White)
        {
            switch (type)
            {
                case PieceType.Pawn:   s = sprites.wPawn; break;
                case PieceType.Rook:   s = sprites.wRook; break;
                case PieceType.Knight: s = sprites.wKnight; break;
                case PieceType.Bishop: s = sprites.wBishop; break;
                case PieceType.Queen:  s = sprites.wQueen; break;
                case PieceType.King:   s = sprites.wKing; break;
            }
        }
        else
        {
            switch (type)
            {
                case PieceType.Pawn:   s = sprites.bPawn; break;
                case PieceType.Rook:   s = sprites.bRook; break;
                case PieceType.Knight: s = sprites.bKnight; break;
                case PieceType.Bishop: s = sprites.bBishop; break;
                case PieceType.Queen:  s = sprites.bQueen; break;
                case PieceType.King:   s = sprites.bKing; break;
            }
        }
        if (s) return s;

        // 2) Resources fallback (Assets/Resources/Sprites/…)
        string code = (side == PieceSide.White ? "w_" : "b_") + type.ToString().ToLower();
        return Resources.Load<Sprite>("Sprites/" + code);
    }
public Piece Spawn(PieceType type, PieceSide side, int x, int z, Sprite sprite = null)
{
    // 1) Instantiate
    var p = Instantiate(piecePrefab, ToWorld(x, z), Quaternion.identity, transform);

    p.type = type;
    p.side = side;
    p.x = x;
    p.z = z;

    // 2) Sprite seç
    var s = sprite ? sprite : GetSprite(type, side);

    if (!p.sr)
        Debug.LogError("Piece prefabında SpriteRenderer (sr) bağlı değil!");

    if (p.sr)
        p.sr.sprite = s;

    if (s == null)
    {
        Debug.LogWarning($"Sprite not found -> Resources/Sprites/" +
            ((side == PieceSide.White ? "w_" : "b_") + type.ToString().ToLower()));
    }
    else
    {
        // 3) Sprite boyutuna göre ölçek hesapla
        //    (Sprite'ı kare içine sığdırmak için)
        // bounds.size.x = sprite'ın world-space'te genişliği (birim cinsinden)
        float spriteWidthUnits = s.bounds.size.x; // kare genişliği için yeterli
        if (spriteWidthUnits > 0f)
        {
            // taşın genişliği tileSize olacak şekilde scale hesapla
            float scale = (tileSize / spriteWidthUnits) * 0.9f; // 0.9 biraz içerden dursun diye
            p.transform.localScale = Vector3.one * scale;
        }
        else
        {
            // fallback: eskisi gibi
            p.transform.localScale = Vector3.one * 0.9f;
        }
    }

    // 4) İsim ve grid kaydı
    p.name = $"{side}_{type}_{(char)('A' + x)}{z + 1}";
    grid[x, z] = p;

    return p;
}


    public Piece GetAt(int x, int z) => InBounds(x, z) ? grid[x, z] : null;

    public bool Move(Piece p, int toX, int toZ)
    {
        if (!InBounds(toX, toZ)) return false;
        if (p.x == toX && p.z == toZ) return false;

        var target = grid[toX, toZ];
        if (target && target.side == p.side) return false;

        // --- PİYON KURALLARI ---
        if (p.type == PieceType.Pawn)
        {
            int dir = (p.side == PieceSide.White) ? +1 : -1;
            int startZ = (p.side == PieceSide.White) ? 1 : 6;

            int dz = toZ - p.z;
            int dx = Mathf.Abs(toX - p.x);

            // 1) Düz 1 kare
            if (dx == 0 && dz == dir && grid[toX, toZ] == null)
                return ApplyMove(p, toX, toZ);

            // 2) İlk hamlede 2 kare
            if (dx == 0 && p.z == startZ && dz == 2 * dir)
            {
                int midZ = p.z + dir;
                if (grid[toX, midZ] == null && grid[toX, toZ] == null)
                {
                    enPassantTarget = new Vector2Int(toX, midZ);
                    return ApplyMove(p, toX, toZ);
                }
                return false;
            }

            // 3) Çapraz 1 kare: alma veya en passant
            if (dx == 1 && dz == dir)
            {
                if (target != null && target.side != p.side)
                {
                    Destroy(target.gameObject);
                    return ApplyMove(p, toX, toZ);
                }

                if (enPassantTarget.HasValue &&
                    enPassantTarget.Value.x == toX &&
                    enPassantTarget.Value.y == toZ)
                {
                    int capturedZ = toZ - dir;
                    var captured = grid[toX, capturedZ];
                    if (captured != null && captured.side != p.side && captured.type == PieceType.Pawn)
                    {
                        Destroy(captured.gameObject);
                        grid[toX, capturedZ] = null;
                        return ApplyMove(p, toX, toZ);
                    }
                }
            }
            return false;
        }

        // Diğer taşlar: sonra eklenecek
        return false;
    }

    bool ApplyMove(Piece p, int toX, int toZ)
    {
        grid[p.x, p.z] = null;
        p.x = toX; p.z = toZ;
        p.transform.position = ToWorld(toX, toZ);
        grid[toX, toZ] = p;
        return true;
    }

    bool InBounds(int x, int z) => x >= 0 && x < boardSize && z >= 0 && z < boardSize;

    void Start()
    {
        ClearAll();

        // Beyaz piyonlar
        for (int x = 0; x < 8; x++) Spawn(PieceType.Pawn, PieceSide.White, x, 1);
        // Siyah piyonlar
        for (int x = 0; x < 8; x++) Spawn(PieceType.Pawn, PieceSide.Black, x, 6);

        // Arka sıra: R N B Q K B N R
        PieceType[] back = {
            PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
            PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
        };
        for (int x = 0; x < 8; x++) Spawn(back[x], PieceSide.White, x, 0);
        for (int x = 0; x < 8; x++) Spawn(back[x], PieceSide.Black, x, 7);

        Debug.Log("Spawn complete. Total pieces: " + GetComponentsInChildren<Piece>().Length);
    }
}
