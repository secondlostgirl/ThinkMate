using UnityEngine;

public enum PieceType { Pawn, Rook, Knight, Bishop, Queen, King }
public enum PieceSide { White, Black }

public class Piece : MonoBehaviour
{
    public PieceType type;
    public PieceSide side;
    public int x, z;                       // tahtadaki konumu
    public SpriteRenderer sr;              // Visual içindeki SpriteRenderer

    void Reset()                           // Prefab’ta otomatik doldurmak için
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }
}
