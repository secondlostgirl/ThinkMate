using UnityEngine;

public class PieceManager : MonoBehaviour
{
    public static PieceManager I;        // kolay erişim
    public Piece piecePrefab;            // Prefabs/Piece
    public int boardSize = 8;
    public float tileSize = 1f;

    public Piece[,] grid;                // [x,z] -> Piece veya null

    void Awake()
    {
        I = this;
        grid = new Piece[boardSize, boardSize];
    }

    Vector3 ToWorld(int x, int z)
    {
        float offset = -(boardSize - 1) * 0.5f * tileSize;
        return new Vector3(offset + x * tileSize, 0.02f, offset + z * tileSize);
    }

    public void ClearAll()
    {
        foreach (var p in GetComponentsInChildren<Piece>())
            DestroyImmediate(p.gameObject);
        grid = new Piece[boardSize, boardSize];
    }

    public Piece Spawn(PieceType type, PieceSide side, int x, int z, Sprite sprite = null)
    {
        var p = Instantiate(piecePrefab, ToWorld(x, z), Quaternion.identity, transform);
        p.type = type; p.side = side; p.x = x; p.z = z;
        if (p.sr && sprite) p.sr.sprite = sprite;
        grid[x, z] = p;
        return p;
    }

    public Piece GetAt(int x, int z) => InBounds(x,z) ? grid[x,z] : null;

    public bool Move(Piece p, int toX, int toZ)
{
    if (!InBounds(toX, toZ)) return false;

    // Aynı kareye gitme
    if (p.x == toX && p.z == toZ) return false;

    // Hedefte kendi taşın var mı?
    var target = grid[toX, toZ];
    if (target && target.side == p.side) return false;

    // --- Sadece PİYON kuralları (şimdilik) ---
    if (p.type == PieceType.Pawn)
    {
        int dir = (p.side == PieceSide.White) ? +1 : -1;  // beyaz yukarı, siyah aşağı
        int startZ = (p.side == PieceSide.White) ? 1 : 6;

        int dz = toZ - p.z;
        int dx = Mathf.Abs(toX - p.x);

        // 1) Düz ilerleme: 1 kare (önü boş olmalı)
        if (dx == 0 && dz == dir && grid[toX, toZ] == null)
        {
            return ApplyMove(p, toX, toZ);
        }

        // 2) İlk hamlede 2 kare: yol tamamen boş olmalı
        if (dx == 0 && p.z == startZ && dz == 2 * dir)
        {
            int midZ = p.z + dir;
            if (grid[toX, midZ] == null && grid[toX, toZ] == null)
                return ApplyMove(p, toX, toZ);
            return false;
        }

        // 3) Çapraz alma: 1 sağ/sol + 1 ileri, hedefte rakip olmalı
        if (dx == 1 && dz == dir && target != null && target.side != p.side)
        {
            Destroy(target.gameObject);
            return ApplyMove(p, toX, toZ);
        }

        // (En passant ve terfi sonra)
        return false;
    }

    // Diğer taşlar: şimdilik serbest taşı (kuralsız)
    // return ApplyMove(p, toX, toZ);

    return false; // piyon dışında kuralsız taşımayı istemiyorsan böyle bırak
}

// Yardımcı: tabloyu/görseli güncelle
bool ApplyMove(Piece p, int toX, int toZ)
{
    grid[p.x, p.z] = null;
    p.x = toX; p.z = toZ;
    p.transform.position = ToWorld(toX, toZ);
    grid[toX, toZ] = p;
    return true;
}

    bool InBounds(int x,int z) => x>=0 && x<boardSize && z>=0 && z<boardSize;

    // Başlangıç dizilimi
    void Start()
    {
        ClearAll();

        // Beyaz piyonlar (z=1)
        for (int x=0; x<8; x++) Spawn(PieceType.Pawn, PieceSide.White, x, 1);

        // Siyah piyonlar (z=6)
        for (int x=0; x<8; x++) Spawn(PieceType.Pawn, PieceSide.Black, x, 6);

        // Arka sıra: R N B Q K B N R
        PieceType[] back = {
            PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
            PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
        };

        for (int x=0; x<8; x++) Spawn(back[x], PieceSide.White, x, 0);
        for (int x=0; x<8; x++) Spawn(back[x], PieceSide.Black, x, 7);
    }
}
