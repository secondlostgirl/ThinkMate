using UnityEngine;
using UnityEngine.UI;

public class PromotionUI : MonoBehaviour
{
    public static PromotionUI Instance;

    [Header("Panel / Root")]
    public GameObject panelRoot;

    [Header("Buttons")]
    public Button queenButton;
    public Button rookButton;
    public Button bishopButton;
    public Button knightButton;

    void Awake()
    {
        Instance = this;

        // Paneli başta kapalı tut
        if (panelRoot != null)
            panelRoot.SetActive(false);

        // Buton eventleri
        if (queenButton)  queenButton.onClick.AddListener(() => OnClickPiece(PieceType.Queen));
        if (rookButton)   rookButton.onClick.AddListener(() => OnClickPiece(PieceType.Rook));
        if (bishopButton) bishopButton.onClick.AddListener(() => OnClickPiece(PieceType.Bishop));
        if (knightButton) knightButton.onClick.AddListener(() => OnClickPiece(PieceType.Knight));
    }

    public void Show(PieceSide side)
    {
        // İstersen burada butonların sprite'larını side'a göre ayarlayabilirsin
        // (beyaz terfi ediyorsa beyaz taş ikonları vs.)

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void OnClickPiece(PieceType toType)
    {
        if (PieceManager.I != null)
        {
            PieceManager.I.PromotePawn(toType);
        }

        Hide();
    }
}
