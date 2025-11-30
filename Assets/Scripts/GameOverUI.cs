using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    [Header("Root")]
    public GameObject rootPanel;      // GameOverPanel objesi

    [Header("Texts")]
    public TMP_Text titleText;        // CHECKMATE / STALEMATE
    public TMP_Text infoText;         // "White wins", "Draw" vs.

    [Header("Buttons")]
    public Button restartButton;
    public Button closeButton;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // DEBUG: ne olduğunu konsola yaz
        Debug.Log("[GameOverUI] Awake. rootPanel = " + (rootPanel ? rootPanel.name : "NULL"));

        // Panel başta kapalı olsun
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    // --- Dışarıdan çağrılacak metotlar ---

    public void ShowCheckmate(PieceSide winner)
    {
        Debug.Log("[GameOverUI] ShowCheckmate called. rootPanel = " + (rootPanel ? rootPanel.name : "NULL"));

        if (rootPanel == null) return;

        rootPanel.SetActive(true);

        if (titleText != null)
            titleText.text = "CHECKMATE";

        if (infoText != null)
        {
            string winnerName = (winner == PieceSide.White) ? "White" : "Black";
            infoText.text = winnerName + " wins";
        }
    }

    public void ShowStalemate()
    {
        Debug.Log("[GameOverUI] ShowStalemate called. rootPanel = " + (rootPanel ? rootPanel.name : "NULL"));

        if (rootPanel == null) return;

        rootPanel.SetActive(true);

        if (titleText != null)
            titleText.text = "STALEMATE";

        if (infoText != null)
            infoText.text = "Draw game";
    }

    // --- Buton callback'leri ---

    void OnRestartClicked()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    void OnCloseClicked()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }
}
