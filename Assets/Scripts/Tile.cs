using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x, z;
    public bool isLight;        // bu kare açık mı?
    public Renderer rend;

    void Awake()
    {
        // güvence: rend dolu değilse otomatik bul
        rend = rend ? rend : (GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>());
    }
}
