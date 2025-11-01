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
                var pos = new Vector3(offset + x * tileSize, 0f, offset + z * tileSize);
                var square = Instantiate(squarePrefab, pos, Quaternion.Euler(90, 0, 0), transform);

                // Açık/Koyu seçimi (satranç deseni)
                bool isLight = (x + z) % 2 == 0;
                var rend = square.GetComponent<Renderer>();
                if (rend) rend.sharedMaterial = isLight ? lightMat : darkMat;

           // <<< BURAYA EKLE >>>
                var tile = square.GetComponent<Tile>();
                if (tile)
                {
                    tile.x = x;
                    tile.z = z;
                    tile.isLight = isLight; // ♟ açık/koyu bilgisini kaydet
                    tile.rend = tile.rend ?? square.GetComponent<Renderer>();
                }

                // A1-H8 isimlendirme
                char file = (char)('A' + x);
                int rank = z + 1;
                square.name = $"{file}{rank}";
            }
        }
    }
}
