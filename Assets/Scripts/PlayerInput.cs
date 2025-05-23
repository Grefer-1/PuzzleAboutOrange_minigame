using UnityEngine;
using System;

public class PlayerInput : MonoBehaviour
{
    public static event Action<Vector2Int> OnSwipeDetected;

    public float minSwipeDistance = 50f;
    private Vector2 touchStartPos;
    private bool isSwiping = false;

    void Update()
    {
        if (ProcessKeyboardInput())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
            isSwiping = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isSwiping)
            {
                Vector2 touchEndPos = Input.mousePosition;
                DetectSwipe(touchEndPos);
                isSwiping = false;
            }
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                if (isSwiping)
                {
                    DetectSwipe(touch.position);
                    isSwiping = false;
                }
            }
        }
    }

    bool ProcessKeyboardInput()
    {
        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            direction = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            direction = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            direction = Vector2Int.right;

        if (direction != Vector2Int.zero)
        {
            OnSwipeDetected?.Invoke(direction);
            return true;
        }

        return false;
    }

    void DetectSwipe(Vector2 endPos)
    {
        Vector2 swipeDelta = endPos - touchStartPos;

        if (swipeDelta.magnitude < minSwipeDistance)
            return;

        Vector2Int direction = Vector2Int.zero;

        if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
        {
            if (swipeDelta.x > 0)
                direction = Vector2Int.right;
            else
                direction = Vector2Int.left;
        }
        else
        {
            if (swipeDelta.y > 0)
                direction = Vector2Int.up;
            else
                direction = Vector2Int.down;
        }

        if (direction != Vector2Int.zero)
            OnSwipeDetected?.Invoke(direction);
    }
}