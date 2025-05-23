using UnityEngine;

public class Cell : MonoBehaviour
{
    public Vector2Int gridPosition;
    public RectTransform rectTransform;
    public GameObject occupantBlocker;
    public Movable occupantPiece;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
}