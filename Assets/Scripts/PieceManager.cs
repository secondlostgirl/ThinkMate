using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct PieceSprites
{
    public Sprite wPawn, wRook, wKnight, wBishop, wQueen, wKing;
    public Sprite bPawn, bRook, bKnight, bBishop, bQueen, bKing;
}

[System.Serializable]
public struct PieceState
{
    public Piece piece;
    public PieceType type;
    public PieceSide side;
    public int x, z;
    public bool hasMoved;
    public bool isActive;
}

// Tahtanın tamamının bir anlık görüntüsü
[System.Serializable]
public class BoardSnapshot
{
    public List<PieceState> pieces;

    public PieceSide sideToMove;
    public bool gameOver;
    public bool isStalemate;
    public PieceSide winner;
    public Vector2Int? enPassantTarget;

    public bool promotionPending;
    public Piece promotingPawn;
}

public class PieceManager : MonoBehaviour
{
    public static PieceManager I;
    public Piece piecePrefab;
    public int boardSize = 8;
    public float tileSize = 1f;

    public PieceSprites sprites;

    public Piece[,] grid;
    private Vector2Int? enPassantTarget = null;

    // 🔥 Hamle geçmişi
    private Stack<BoardSnapshot> moveHistory = new Stack<BoardSnapshot>();

    // Terfi
    public bool promotionPending = false;
    public Piece promotingPawn = null;

    // Sıra & oyun sonu
    public PieceSide sideToMove = PieceSide.White;   // oyun beyazla başlasın
    public bool gameOver = false;
    public PieceSide winner;        // checkmate durumunda kazanan
    public bool isStalemate = false; // pat mı değil mi

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
            offset + rank * tileSize,   // Y
            0f
        );
    }

    public void ClearAll()
    {
        foreach (var p in GetComponentsInChildren<Piece>(true))
        {
#if UNITY_EDITOR
            DestroyImmediate(p.gameObject);
#else
            Destroy(p.gameObject);
#endif
        }

        grid = new Piece[boardSize, boardSize];
        moveHistory.Clear();
    }

    // --- Sprite seçici ---
    Sprite GetSprite(PieceType type, PieceSide side)
    {
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

        string code = (side == PieceSide.White ? "w_" : "b_") + type.ToString().ToLower();
        return Resources.Load<Sprite>("Sprites/" + code);
    }

    public Piece Spawn(PieceType type, PieceSide side, int x, int z, Sprite sprite = null)
    {
        var p = Instantiate(piecePrefab, ToWorld(x, z), Quaternion.identity, transform);

        p.type = type;
        p.side = side;
        p.x = x;
        p.z = z;
        p.hasMoved = false;

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
            float spriteWidthUnits = s.bounds.size.x;
            if (spriteWidthUnits > 0f)
            {
                float scale = (tileSize / spriteWidthUnits) * 0.9f;
                p.transform.localScale = Vector3.one * scale;
            }
            else
            {
                p.transform.localScale = Vector3.one * 0.9f;
            }
        }

        p.name = $"{side}_{type}_{(char)('A' + x)}{z + 1}";
        grid[x, z] = p;

        return p;
    }

    public Piece GetAt(int x, int z) => InBounds(x, z) ? grid[x, z] : null;

    // ================== SNAPSHOT SİSTEMİ ==================

    BoardSnapshot CreateSnapshot()
    {
        var snap = new BoardSnapshot();
        snap.pieces = new List<PieceState>();

        foreach (var p in GetComponentsInChildren<Piece>(true))
        {
            PieceState st = new PieceState
            {
                piece = p,
                type = p.type,
                side = p.side,
                x = p.x,
                z = p.z,
                hasMoved = p.hasMoved,
                isActive = p.gameObject.activeSelf
            };
            snap.pieces.Add(st);
        }

        snap.sideToMove       = sideToMove;
        snap.gameOver         = gameOver;
        snap.isStalemate      = isStalemate;
        snap.winner           = winner;
        snap.enPassantTarget  = enPassantTarget;
        snap.promotionPending = promotionPending;
        snap.promotingPawn    = promotingPawn;

        return snap;
    }

    void RestoreSnapshot(BoardSnapshot snap)
    {
        grid = new Piece[boardSize, boardSize];

        sideToMove       = snap.sideToMove;
        gameOver         = snap.gameOver;
        isStalemate      = snap.isStalemate;
        winner           = snap.winner;
        enPassantTarget  = snap.enPassantTarget;
        promotionPending = snap.promotionPending;
        promotingPawn    = snap.promotingPawn;

        foreach (var st in snap.pieces)
        {
            if (st.piece == null) continue;

            var p = st.piece;

            p.type     = st.type;
            p.side     = st.side;
            p.hasMoved = st.hasMoved;
            p.x        = st.x;
            p.z        = st.z;

            p.gameObject.SetActive(st.isActive);

            if (st.isActive)
            {
                p.transform.position = ToWorld(st.x, st.z);
                grid[st.x, st.z] = p;

                var sprite = GetSprite(p.type, p.side);
                if (p.sr != null && sprite != null)
                {
                    p.sr.sprite = sprite;

                    float spriteWidthUnits = sprite.bounds.size.x;
                    if (spriteWidthUnits > 0f)
                    {
                        float scale = (tileSize / spriteWidthUnits) * 0.9f;
                        p.transform.localScale = Vector3.one * scale;
                    }
                }
            }
        }
    }

    public void UndoLastMove()
    {
        if (moveHistory.Count == 0)
            return;

        var snap = moveHistory.Pop();
        RestoreSnapshot(snap);
    }

    // ================== ANA MOVE ==================
    public bool Move(Piece p, int toX, int toZ)
    {
        // Oyun bittiyse hamle yok
        if (gameOver) return false;

        // Sıra kimdeyse sadece o oynayabilsin
        if (p.side != sideToMove) return false;

        if (!InBounds(toX, toZ)) return false;
        if (p.x == toX && p.z == toZ) return false;

        var target = grid[toX, toZ];
        if (target && target.side == p.side) return false;

        int dx = toX - p.x;
        int dz = toZ - p.z;
        int absDx = Mathf.Abs(dx);
        int absDz = Mathf.Abs(dz);

        // ---------- PİYON ----------
        if (p.type == PieceType.Pawn)
        {
            int dir    = (p.side == PieceSide.White) ? +1 : -1;
            int startZ = (p.side == PieceSide.White) ? 1  : 6;

            int dzPawn = toZ - p.z;
            int dxPawn = Mathf.Abs(toX - p.x);

            // 1 kare düz
            if (dxPawn == 0 && dzPawn == dir && grid[toX, toZ] == null)
            {
                if (WouldLeaveKingInCheck(p, toX, toZ))
                    return false;

                moveHistory.Push(CreateSnapshot());

                enPassantTarget = null;
                bool moved = ApplyMove(p, toX, toZ);
                CheckPromotion(p);
                AfterMove();
                return moved;
            }

            // 2 kare ilk hamle
            if (dxPawn == 0 && p.z == startZ && dzPawn == 2 * dir)
            {
                int midZ = p.z + dir;
                if (grid[toX, midZ] == null && grid[toX, toZ] == null)
                {
                    if (WouldLeaveKingInCheck(p, toX, toZ))
                        return false;

                    moveHistory.Push(CreateSnapshot());

                    enPassantTarget = new Vector2Int(toX, midZ);
                    bool moved = ApplyMove(p, toX, toZ);
                    AfterMove();
                    return moved;
                }
                return false;
            }

            // Çapraz alma
            if (dxPawn == 1 && dzPawn == dir)
            {
                // normal capture
                if (target != null && target.side != p.side)
                {
                    if (WouldLeaveKingInCheck(p, toX, toZ))
                        return false;

                    moveHistory.Push(CreateSnapshot());

                    target.gameObject.SetActive(false);
                    grid[toX, toZ] = null;

                    enPassantTarget = null;
                    bool moved = ApplyMove(p, toX, toZ);
                    CheckPromotion(p);
                    AfterMove();
                    return moved;
                }

                // en passant
                if (enPassantTarget.HasValue &&
                    enPassantTarget.Value.x == toX &&
                    enPassantTarget.Value.y == toZ)
                {
                    int capturedZ = toZ - dir;
                    var captured = grid[toX, capturedZ];
                    if (captured != null && captured.side != p.side && captured.type == PieceType.Pawn)
                    {
                        // (Tam doğrusu için burada da WouldLeaveKingInCheck ile özel sim yapmalıyız)

                        moveHistory.Push(CreateSnapshot());

                        captured.gameObject.SetActive(false);
                        grid[toX, capturedZ] = null;
                        enPassantTarget = null;
                        bool moved = ApplyMove(p, toX, toZ);
                        AfterMove();
                        return moved;
                    }
                }
            }

            return false;
        }

        // piyon dışında en passant sıfırlansın
        enPassantTarget = null;

        // ---------- ROOK ----------
        if (p.type == PieceType.Rook)
        {
            if (p.x != toX && p.z != toZ) return false;
            if (!IsPathClearStraight(p.x, p.z, toX, toZ)) return false;

            if (WouldLeaveKingInCheck(p, toX, toZ))
                return false;

            moveHistory.Push(CreateSnapshot());

            if (target != null && target.side != p.side)
            {
                target.gameObject.SetActive(false);
                grid[toX, toZ] = null;
            }

            bool moved = ApplyMove(p, toX, toZ);
            AfterMove();
            return moved;
        }

        // ---------- BISHOP ----------
        if (p.type == PieceType.Bishop)
        {
            if (absDx != absDz) return false;
            if (!IsPathClearDiagonal(p.x, p.z, toX, toZ)) return false;

            if (WouldLeaveKingInCheck(p, toX, toZ))
                return false;

            moveHistory.Push(CreateSnapshot());

            if (target != null && target.side != p.side)
            {
                target.gameObject.SetActive(false);
                grid[toX, toZ] = null;
            }

            bool moved = ApplyMove(p, toX, toZ);
            AfterMove();
            return moved;
        }

        // ---------- QUEEN ----------
        if (p.type == PieceType.Queen)
        {
            bool straight = (p.x == toX || p.z == toZ);
            bool diagonal = (absDx == absDz);

            if (!straight && !diagonal) return false;

            if (straight && !IsPathClearStraight(p.x, p.z, toX, toZ)) return false;
            if (diagonal && !IsPathClearDiagonal(p.x, p.z, toX, toZ)) return false;

            if (WouldLeaveKingInCheck(p, toX, toZ))
                return false;

            moveHistory.Push(CreateSnapshot());

            if (target != null && target.side != p.side)
            {
                target.gameObject.SetActive(false);
                grid[toX, toZ] = null;
            }

            bool moved = ApplyMove(p, toX, toZ);
            AfterMove();
            return moved;
        }

        // ---------- KNIGHT ----------
        if (p.type == PieceType.Knight)
        {
            bool isKnightMove =
                (absDx == 1 && absDz == 2) ||
                (absDx == 2 && absDz == 1);

            if (!isKnightMove) return false;

            if (WouldLeaveKingInCheck(p, toX, toZ))
                return false;

            moveHistory.Push(CreateSnapshot());

            if (target != null && target.side != p.side)
            {
                target.gameObject.SetActive(false);
                grid[toX, toZ] = null;
            }

            bool moved = ApplyMove(p, toX, toZ);
            AfterMove();
            return moved;
        }

        // ---------- KING (ŞAH + ROK) ----------
        if (p.type == PieceType.King)
        {
            // 1) ROK
            if (!p.hasMoved && dz == 0 && absDx == 2)
            {
                int dir = dx > 0 ? 1 : -1;
                int rookFromX = (dir > 0) ? boardSize - 1 : 0;
                int rookZ = p.z;

                Piece rook = grid[rookFromX, rookZ];

                if (rook != null &&
                    rook.type == PieceType.Rook &&
                    rook.side == p.side &&
                    !rook.hasMoved)
                {
                    if (IsPathClearStraight(p.x, p.z, rookFromX, rookZ))
                    {
                        int kingToX = p.x + 2 * dir;
                        int rookToX = kingToX - dir;

                        // (Tam rok legality için kralın geçtiği karelerin
                        // tehdit altında olmaması lazım, onu sonra ekleriz.)

                        moveHistory.Push(CreateSnapshot());

                        grid[p.x, p.z] = null;
                        grid[rookFromX, rookZ] = null;

                        p.x = kingToX;
                        p.z = rookZ;
                        p.transform.position = ToWorld(p.x, p.z);

                        rook.x = rookToX;
                        rook.z = rookZ;
                        rook.transform.position = ToWorld(rook.x, rook.z);

                        grid[p.x, p.z] = p;
                        grid[rook.x, rook.z] = rook;

                        p.hasMoved = true;
                        rook.hasMoved = true;
                        enPassantTarget = null;

                        AfterMove();
                        return true;
                    }
                }
                // Rok olmazsa normal harekete düşer
            }

            // 2) Normal şah
            if (absDx > 1 || absDz > 1) return false;
            if (absDx == 0 && absDz == 0) return false;

            if (WouldLeaveKingInCheck(p, toX, toZ))
                return false;

            moveHistory.Push(CreateSnapshot());

            if (target != null && target.side != p.side)
            {
                target.gameObject.SetActive(false);
                grid[toX, toZ] = null;
            }

            enPassantTarget = null;
            bool movedKing = ApplyMove(p, toX, toZ);
            AfterMove();
            return movedKing;
        }

        return false;
    }

    // ====== HAMLE SONRASI (TURN + GAME OVER) ======

    public void AfterMove()
    {
        // Piyon terfisi bekleniyorsa, terfi bitmeden sıra değiştirme
        if (promotionPending)
            return;

        // Sıra değişsin
        sideToMove = Opponent(sideToMove);

        // Checkmate / pat kontrolü
        CheckGameOver();
    }

    void CheckGameOver()
    {
        PieceSide side = sideToMove;   // sıradaki taraf

        bool inCheck = IsKingInCheck(side);
        bool hasMove = HasAnyLegalMove(side);

        if (!hasMove)
        {
            gameOver = true;
            isStalemate = !inCheck;

            if (inCheck)
            {
                // Şah-mat
                winner = Opponent(side);
                Debug.Log($"Checkmate! Winner: {winner}");
            }
            else
            {
                // Pat
                winner = PieceSide.White; // anlamsız ama dolu dursun
                Debug.Log("Stalemate (Pat). Draw game.");
            }
        }
    }

    // Bir tarafın en az bir legal hamlesi var mı?
    bool HasAnyLegalMove(PieceSide side)
    {
        for (int x = 0; x < boardSize; x++)
        {
            for (int z = 0; z < boardSize; z++)
            {
                var p = grid[x, z];
                if (p == null || p.side != side) continue;

                for (int tx = 0; tx < boardSize; tx++)
                {
                    for (int tz = 0; tz < boardSize; tz++)
                    {
                        if (tx == p.x && tz == p.z) continue;

                        if (!CanMoveByRules(p, tx, tz)) continue;

                        if (WouldLeaveKingInCheck(p, tx, tz)) continue;

                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Sadece hareket kurallarını kontrol eder (uygulamaz)
    bool CanMoveByRules(Piece p, int toX, int toZ)
    {
        if (!InBounds(toX, toZ)) return false;
        if (p.x == toX && p.z == toZ) return false;

        var target = grid[toX, toZ];
        if (target && target.side == p.side) return false;

        int dx = toX - p.x;
        int dz = toZ - p.z;
        int absDx = Mathf.Abs(dx);
        int absDz = Mathf.Abs(dz);

        if (p.type == PieceType.Pawn)
        {
            int dir = (p.side == PieceSide.White) ? +1 : -1;
            int startZ = (p.side == PieceSide.White) ? 1 : 6;

            int dzPawn = toZ - p.z;
            int dxPawn = Mathf.Abs(toX - p.x);

            // 1 kare düz
            if (dxPawn == 0 && dzPawn == dir && grid[toX, toZ] == null)
                return true;

            // 2 kare ilk hamle
            if (dxPawn == 0 && p.z == startZ && dzPawn == 2 * dir)
            {
                int midZ = p.z + dir;
                if (grid[toX, midZ] == null && grid[toX, toZ] == null)
                    return true;
            }

            // Çapraz alma
            if (dxPawn == 1 && dzPawn == dir)
            {
                if (target != null && target.side != p.side)
                    return true;
            }

            return false;
        }

        if (p.type == PieceType.Rook)
        {
            if (p.x != toX && p.z != toZ) return false;
            if (!IsPathClearStraight(p.x, p.z, toX, toZ)) return false;
            return true;
        }

        if (p.type == PieceType.Bishop)
        {
            if (absDx != absDz) return false;
            if (!IsPathClearDiagonal(p.x, p.z, toX, toZ)) return false;
            return true;
        }

        if (p.type == PieceType.Queen)
        {
            bool straight = (p.x == toX || p.z == toZ);
            bool diagonal = (absDx == absDz);

            if (!straight && !diagonal) return false;

            if (straight && !IsPathClearStraight(p.x, p.z, toX, toZ)) return false;
            if (diagonal && !IsPathClearDiagonal(p.x, p.z, toX, toZ)) return false;

            return true;
        }

        if (p.type == PieceType.Knight)
        {
            bool isKnightMove =
                (absDx == 1 && absDz == 2) ||
                (absDx == 2 && absDz == 1);

            return isKnightMove;
        }

        if (p.type == PieceType.King)
        {
            if (absDx == 0 && absDz == 0) return false;
            if (absDx > 1 || absDz > 1) return false;
            return true;
        }

        return false;
    }

    // ====== TERFİ YARDIMCILARI ======

    void CheckPromotion(Piece p)
    {
        if (p.type != PieceType.Pawn) return;

        int lastRank = (p.side == PieceSide.White) ? 7 : 0;

        if (p.z == lastRank)
        {
            promotionPending = true;
            promotingPawn = p;

            if (PromotionUI.Instance != null)
            {
                PromotionUI.Instance.Show(p.side);
            }
            else
            {
                Debug.LogWarning("PromotionUI yok, piyon terfisi için otomatik vezir yapabilirsin.");
            }
        }
    }

    public void PromotePawn(PieceType toType)
    {
        if (!promotionPending || promotingPawn == null)
        {
            Debug.LogWarning("PromotePawn çağrıldı ama bekleyen piyon yok.");
            return;
        }

        Piece p = promotingPawn;

        if (p.type != PieceType.Pawn)
        {
            Debug.LogWarning("PromotePawn: promotingPawn piyon değil.");
            promotionPending = false;
            promotingPawn = null;
            return;
        }

        p.type = toType;

        var s = GetSprite(toType, p.side);
        if (p.sr != null && s != null)
            p.sr.sprite = s;

        promotionPending = false;
        promotingPawn = null;

        Debug.Log($"Pawn promoted to {toType} at {(char)('A' + p.x)}{p.z + 1}");

        // Terfiden sonra sıra değişsin + game over kontrolü
        AfterMove();
    }

    // ====== CHECK / KING YARDIMCILARI ======

    PieceSide Opponent(PieceSide side)
    {
        return side == PieceSide.White ? PieceSide.Black : PieceSide.White;
    }

    Piece FindKing(PieceSide side)
    {
        for (int x = 0; x < boardSize; x++)
        {
            for (int z = 0; z < boardSize; z++)
            {
                var p = grid[x, z];
                if (p != null && p.type == PieceType.King && p.side == side)
                    return p;
            }
        }
        return null;
    }

    bool IsSquareAttacked(int x, int z, PieceSide bySide)
    {
        for (int fx = 0; fx < boardSize; fx++)
        {
            for (int fz = 0; fz < boardSize; fz++)
            {
                var attacker = grid[fx, fz];
                if (attacker == null || attacker.side != bySide) continue;

                int dx = x - fx;
                int dz = z - fz;
                int absDx = Mathf.Abs(dx);
                int absDz = Mathf.Abs(dz);

                switch (attacker.type)
                {
                    case PieceType.Pawn:
                    {
                        int dir = (bySide == PieceSide.White) ? +1 : -1;
                        if (dz == dir && absDx == 1)
                            return true;
                        break;
                    }
                    case PieceType.Knight:
                    {
                        bool isKnightMove =
                            (absDx == 1 && absDz == 2) ||
                            (absDx == 2 && absDz == 1);
                        if (isKnightMove) return true;
                        break;
                    }
                    case PieceType.Bishop:
                    {
                        if (absDx == absDz &&
                            IsPathClearDiagonal(fx, fz, x, z))
                            return true;
                        break;
                    }
                    case PieceType.Rook:
                    {
                        if ((fx == x || fz == z) &&
                            IsPathClearStraight(fx, fz, x, z))
                            return true;
                        break;
                    }
                    case PieceType.Queen:
                    {
                        bool straight = (fx == x || fz == z);
                        bool diagonal = (absDx == absDz);

                        if (straight && IsPathClearStraight(fx, fz, x, z))
                            return true;
                        if (diagonal && IsPathClearDiagonal(fx, fz, x, z))
                            return true;
                        break;
                    }
                    case PieceType.King:
                    {
                        if (absDx <= 1 && absDz <= 1 && (absDx + absDz > 0))
                            return true;
                        break;
                    }
                }
            }
        }

        return false;
    }

    bool IsKingInCheck(PieceSide side)
    {
        var king = FindKing(side);
        if (king == null) return false;

        return IsSquareAttacked(king.x, king.z, Opponent(side));
    }

    bool WouldLeaveKingInCheck(Piece p, int toX, int toZ)
    {
        PieceSide side = p.side;

        int fromX = p.x;
        int fromZ = p.z;
        Piece captured = grid[toX, toZ];

        grid[fromX, fromZ] = null;
        p.x = toX;
        p.z = toZ;
        grid[toX, toZ] = p;

        bool inCheck = IsKingInCheck(side);

        grid[toX, toZ] = captured;
        p.x = fromX;
        p.z = fromZ;
        grid[fromX, fromZ] = p;

        return inCheck;
    }

    // ================== PATH & GENEL ==================

    bool IsPathClearStraight(int fromX, int fromZ, int toX, int toZ)
    {
        if (fromX != toX && fromZ != toZ)
            return false;

        int stepX = toX == fromX ? 0 : (toX > fromX ? 1 : -1);
        int stepZ = toZ == fromZ ? 0 : (toZ > fromZ ? 1 : -1);

        int x = fromX + stepX;
        int z = fromZ + stepZ;

        while (x != toX || z != toZ)
        {
            if (grid[x, z] != null)
                return false;

            x += stepX;
            z += stepZ;
        }

        return true;
    }

    bool IsPathClearDiagonal(int fromX, int fromZ, int toX, int toZ)
    {
        int dx = toX - fromX;
        int dz = toZ - fromZ;

        if (Mathf.Abs(dx) != Mathf.Abs(dz))
            return false;

        int stepX = dx > 0 ? 1 : -1;
        int stepZ = dz > 0 ? 1 : -1;

        int x = fromX + stepX;
        int z = fromZ + stepZ;

        while (x != toX || z != toZ)
        {
            if (grid[x, z] != null)
                return false;

            x += stepX;
            z += stepZ;
        }

        return true;
    }

    bool ApplyMove(Piece p, int toX, int toZ)
    {
        grid[p.x, p.z] = null;

        p.x = toX;
        p.z = toZ;
        p.transform.position = ToWorld(toX, toZ);

        grid[toX, toZ] = p;

        p.hasMoved = true;

        return true;
    }

    bool InBounds(int x, int z) => x >= 0 && x < boardSize && z >= 0 && z < boardSize;

    void Start()
    {
        ClearAll();

        // Piyonlar
        for (int x = 0; x < 8; x++) Spawn(PieceType.Pawn, PieceSide.White, x, 1);
        for (int x = 0; x < 8; x++) Spawn(PieceType.Pawn, PieceSide.Black, x, 6);

        // Arka sıra dizilişi
        PieceType[] back = {
            PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
            PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
        };

        for (int x = 0; x < 8; x++) Spawn(back[x], PieceSide.White, x, 0);
        for (int x = 0; x < 8; x++) Spawn(back[x], PieceSide.Black, x, 7);

        sideToMove = PieceSide.White;
        gameOver = false;
        promotionPending = false;
        promotingPawn = null;
        isStalemate = false;

        // Başlangıç pozisyonunu istersen history'e atabilirsin:
        moveHistory.Push(CreateSnapshot());

        Debug.Log("Spawn complete. Total pieces: " + GetComponentsInChildren<Piece>().Length);
    }
}
