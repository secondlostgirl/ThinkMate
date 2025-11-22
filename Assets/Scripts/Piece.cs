using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class Piece : MonoBehaviour
{
    public PieceType type;
    public PieceSide side;
    public int x, z;

    [SerializeField] public SpriteRenderer sr;
     public bool hasMoved = false;

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
    }

#if UNITY_EDITOR
    // Prefaba eklendiğinde/Reset dendiğinde otomatik bağlar
    void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
    }
#endif
}

public enum PieceType { Pawn, Rook, Knight, Bishop, Queen, King }
public enum PieceSide { White, Black }
