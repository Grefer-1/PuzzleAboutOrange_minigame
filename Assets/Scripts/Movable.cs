using UnityEngine;
using UnityEngine.UI;

public class Movable : MonoBehaviour
{
    public Image pieceImage;
    public PieceData pieceData;
    public RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (pieceImage == null)
            pieceImage = GetComponent<Image>();
    }

    public void Initialize(PieceData data, Sprite sprite)
    {
        pieceData = data;

        if (pieceImage)
            pieceImage.sprite = sprite;
    }

    public void UpdateDataPosition(Vector2Int newGridPos)
    {
        pieceData.position = newGridPos;
    }
}