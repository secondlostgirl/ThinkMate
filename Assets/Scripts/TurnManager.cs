using UnityEngine;

public enum Turn { White, Black }

public class TurnManager : MonoBehaviour
{
    public static TurnManager I;
    public Turn current = Turn.White;

    void Awake() => I = this;

    public void Next()
    {
        current = (current == Turn.White) ? Turn.Black : Turn.White;
    }
}
