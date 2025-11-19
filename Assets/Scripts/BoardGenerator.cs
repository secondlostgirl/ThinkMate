using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [Header("Refs")]
    public GameObject squarePrefab;
    public Material lightMat;
    public Material darkMat;

    [Header("Board Settings")]
    [Range(2, 16)] public int boardSize = 8;
    public float tileSize = 1f;

    void OnEnable()
    {
        Generate();
    }
public void Generate()
{
    if (!squarePrefab || !lightMat || !darkMat)
    {
        Debug.LogWarning("Assign refs!");
        return;
    }

    // Eski çocukları temizle
    for (int i = transform.childCount - 1; i >= 0; i--)
        DestroyImmediate(transform.GetChild(i).gameObject);

    // Ortalamak için offset
    float offset = -(boardSize - 1) * 0.5f * tileSize;

    for (int z = 0; z < boardSize; z++)
    {
        for (int x = 0; x < boardSize; x++)
        {
            // ❶ XY düzleminde, Z sabit 0
            var pos = new Vector3(offset + x * tileSize,
                                  offset + z * tileSize,
                                  0f);

            // ❷ Artık 90 derece döndürme YOK
            var square = Instantiate(squarePrefab, pos,
                                     Quaternion.identity,
                                     transform);

            bool isLight = (x + z) % 2 == 0;
            var rend = square.GetComponent<Renderer>();
            if (rend) rend.sharedMaterial = isLight ? lightMat : darkMat;

            var tile = square.GetComponent<Tile>();
            if (tile)
            {
                tile.x = x;
                tile.z = z;
                tile.isLight = isLight;
                tile.rend = tile.rend ?? square.GetComponent<Renderer>();
            }

            char file = (char)('A' + x);
            int rank = z + 1;
            square.name = $"{file}{rank}";
        }
    }
}
}