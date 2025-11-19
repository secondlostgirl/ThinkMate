using UnityEngine;
using TMPro;

public class BoardLabels : MonoBehaviour
{
    public int boardSize = 8;
    public float tileSize = 1f;
    public TMP_Text labelPrefab;

    void Start() { GenerateLabels(); }

    void GenerateLabels()
    {
        float offset = -(boardSize - 1) * 0.5f * tileSize;

        // ALTTAKİ HARFLER (A, B, C...)
        for (int x = 0; x < boardSize; x++)
        {
            char letter = (char)('A' + x);

            // ❶ X, Y düzleminde; Z = 0
            Vector3 pos = new Vector3(
                offset + x * tileSize,      // X: sütun
                offset - tileSize * 0.7f,   // Y: tahtanın biraz altı
                0f                          // Z: sabit
            );

            CreateLabel(letter.ToString(), pos);
        }

        // SOLDAN SAYILAR (1, 2, 3...)
        for (int z = 0; z < boardSize; z++)
        {
            string num = (z + 1).ToString();

            Vector3 pos = new Vector3(
                offset - tileSize * 0.7f,   // X: tahtanın biraz solu
                offset + z * tileSize,      // Y: satır
                0f                          // Z: sabit
            );

            CreateLabel(num, pos);
        }
    }

    void CreateLabel(string text, Vector3 pos)
    {
        // ❷ Rotasyon artık 90 derece değil, düz:
        TMP_Text t = Instantiate(labelPrefab, pos, Quaternion.identity, transform);
        t.text = text;
        t.fontSize = 0.4f;
        t.alignment = TextAlignmentOptions.Center;
    }
}
