using UnityEngine;

public class MobileInputController : MonoBehaviour
{
    [SerializeField] private float swipeThreshold = 60f;
    [SerializeField] private float tapThreshold = 20f;

    private GridGameManager gameManager;
    private RectTransform hudRectTransform;
    private Canvas hudCanvas;

    private bool pointerDown;
    private bool pointerStartedOnHud;
    private Vector2 pointerStart;

    public void Initialize(GridGameManager manager, RectTransform hudRect, Canvas canvas)
    {
        gameManager = manager;
        hudRectTransform = hudRect;
        hudCanvas = canvas;
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            HandleTouch(Input.GetTouch(0));
            return;
        }

        HandleMouse();
    }

    private void HandleTouch(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                BeginPointer(touch.position);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                EndPointer(touch.position);
                break;
        }
    }

    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BeginPointer(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndPointer(Input.mousePosition);
        }
    }

    private void BeginPointer(Vector2 screenPosition)
    {
        pointerDown = true;
        pointerStart = screenPosition;
        pointerStartedOnHud = IsOverHud(screenPosition);
    }

    private void EndPointer(Vector2 screenPosition)
    {
        if (!pointerDown)
        {
            return;
        }

        pointerDown = false;

        if (gameManager == null)
        {
            return;
        }

        if (pointerStartedOnHud)
        {
            return;
        }

        Vector2 delta = screenPosition - pointerStart;
        float distance = delta.magnitude;

        if (distance < tapThreshold)
        {
            gameManager.RequestCycleForm();
            return;
        }

        if (distance < swipeThreshold)
        {
            return;
        }

        Vector2Int direction = GetSwipeDirection(delta);
        if (direction != Vector2Int.zero)
        {
            gameManager.RequestMove(direction);
        }
    }

    private bool IsOverHud(Vector2 screenPosition)
    {
        if (hudRectTransform == null || hudCanvas == null)
        {
            return false;
        }

        Camera camera = hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(hudRectTransform, screenPosition, camera);
    }

    private Vector2Int GetSwipeDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return delta.x > 0f ? Vector2Int.right : Vector2Int.left;
        }

        return delta.y > 0f ? Vector2Int.up : Vector2Int.down;
    }
}
