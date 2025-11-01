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

        for (int x = 0; x < boardSize; x++)
        {
            char letter = (char)('A' + x);
            Vector3 pos = new Vector3(offset + x * tileSize, 0.01f, offset - tileSize * 0.7f);
            CreateLabel(letter.ToString(), pos);
        }
        for (int z = 0; z < boardSize; z++)
        {
            string num = (z + 1).ToString();
            Vector3 pos = new Vector3(offset - tileSize * 0.7f, 0.01f, offset + z * tileSize);
            CreateLabel(num, pos);
        }
    }

    void CreateLabel(string text, Vector3 pos)
    {
        TMP_Text t = Instantiate(labelPrefab, pos, Quaternion.Euler(90, 0, 0), transform);
        t.text = text;
        t.fontSize = 0.4f;
        t.alignment = TextAlignmentOptions.Center;
    }
}
