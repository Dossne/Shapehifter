using UnityEngine;

public class TokenVisual : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private float iconScale = 0.55f;
    [SerializeField] private Color bubbleColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private int bubbleSortingOrder = 1;
    [SerializeField] private int iconSortingOrder = 2;

    [Header("Idle Motion")]
    [SerializeField] private float bobAmplitude = 0.03f;
    [SerializeField] private float bobSpeed = 1.5f;

    private SpriteRenderer bubbleRenderer;
    private SpriteRenderer iconRenderer;
    private Vector3 baseLocalPosition;
    private static Sprite bubbleFallbackSprite;

    public void Configure(Sprite iconSprite, Color iconColor, Sprite bubbleSprite)
    {
        EnsureRenderers();

        bubbleRenderer.sprite = bubbleSprite != null ? bubbleSprite : GetBubbleFallback();
        bubbleRenderer.color = bubbleColor;
        bubbleRenderer.sortingOrder = bubbleSortingOrder;
        bubbleRenderer.enabled = bubbleRenderer.sprite != null;

        iconRenderer.sprite = iconSprite;
        iconRenderer.color = iconColor;
        iconRenderer.sortingOrder = iconSortingOrder;
        iconRenderer.transform.localScale = Vector3.one * iconScale;
    }

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        if (bobAmplitude <= 0f)
        {
            return;
        }

        float offset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = baseLocalPosition + new Vector3(0f, offset, 0f);
    }

    private void EnsureRenderers()
    {
        if (bubbleRenderer == null)
        {
            GameObject bubbleObject = new GameObject("Bubble");
            bubbleObject.transform.SetParent(transform, false);
            bubbleRenderer = bubbleObject.AddComponent<SpriteRenderer>();
        }

        if (iconRenderer == null)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(transform, false);
            iconRenderer = iconObject.AddComponent<SpriteRenderer>();
        }
    }

    private static Sprite GetBubbleFallback()
    {
        if (bubbleFallbackSprite != null)
        {
            return bubbleFallbackSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float radius = size * 0.46f;
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.Clamp01(1f - (distance / radius));
                float alpha = Mathf.Pow(t, 1.6f) * 0.4f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        bubbleFallbackSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return bubbleFallbackSprite;
    }
}
