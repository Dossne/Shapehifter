using System.Collections;
using UnityEngine;

public class ShapeshiftVfxController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float crossfadeDuration = 0.1f;
    [SerializeField] private float totalDuration = 0.16f;

    [Header("Scale Bounce")]
    [SerializeField] private float scaleDownMultiplier = 0.94f;
    [SerializeField] private float scaleUpMultiplier = 1.1f;
    [SerializeField] private float scaleDownTime = 0.04f;
    [SerializeField] private float scaleUpTime = 0.05f;

    [Header("Poof")]
    [SerializeField] private int poofMinParticles = 8;
    [SerializeField] private int poofMaxParticles = 14;

    private SpriteRenderer mainRenderer;
    private SpriteRenderer ghostRenderer;
    private ParticleSystem poofParticles;
    private Coroutine activeRoutine;
    private Vector3 baseScale;

    private static Material poofMaterial;

    private void Awake()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        EnsureGhostRenderer();
        EnsurePoofParticles();
    }

    public void Play(Sprite previousSprite, Color previousColor, Sprite nextSprite, Color nextColor)
    {
        if (mainRenderer == null || previousSprite == null || nextSprite == null)
        {
            return;
        }

        StopActiveRoutine();
        ResetVisuals();

        ghostRenderer.sprite = previousSprite;
        ghostRenderer.color = new Color(previousColor.r, previousColor.g, previousColor.b, 1f);
        ghostRenderer.enabled = true;

        mainRenderer.sprite = nextSprite;
        mainRenderer.color = new Color(nextColor.r, nextColor.g, nextColor.b, 0f);

        PlayPoof();
        activeRoutine = StartCoroutine(PlayRoutine());
    }

    private void EnsureGhostRenderer()
    {
        if (ghostRenderer != null || mainRenderer == null)
        {
            return;
        }

        GameObject ghostObject = new GameObject("ShapeshiftGhost");
        ghostObject.transform.SetParent(transform, false);
        ghostObject.transform.localPosition = Vector3.zero;
        ghostObject.transform.localScale = Vector3.one;

        ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
        ghostRenderer.sortingLayerID = mainRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = mainRenderer.sortingOrder;
        ghostRenderer.enabled = false;
    }

    private void EnsurePoofParticles()
    {
        if (poofParticles != null)
        {
            return;
        }

        GameObject poofObject = new GameObject("ShapeshiftPoof");
        poofObject.transform.SetParent(transform, false);
        poofObject.transform.localPosition = Vector3.zero;

        poofParticles = poofObject.AddComponent<ParticleSystem>();
        var main = poofParticles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
        main.gravityModifier = 0f;
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = poofParticles.emission;
        emission.rateOverTime = 0f;

        var shape = poofParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        var renderer = poofParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerID = mainRenderer != null ? mainRenderer.sortingLayerID : renderer.sortingLayerID;
        renderer.sortingOrder = mainRenderer != null ? mainRenderer.sortingOrder + 1 : renderer.sortingOrder;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetPoofMaterial();
    }

    private IEnumerator PlayRoutine()
    {
        float duration = Mathf.Max(totalDuration, crossfadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float crossfadeT = crossfadeDuration > 0f ? Mathf.Clamp01(elapsed / crossfadeDuration) : 1f;
            SetAlpha(mainRenderer, crossfadeT);
            SetAlpha(ghostRenderer, 1f - crossfadeT);

            float scale = EvaluateBounceScale(elapsed, duration);
            transform.localScale = baseScale * scale;

            yield return null;
        }

        ResetVisuals();
    }

    private float EvaluateBounceScale(float elapsed, float duration)
    {
        float downTime = Mathf.Min(scaleDownTime, duration);
        float upTime = Mathf.Min(scaleUpTime, Mathf.Max(0f, duration - downTime));
        float returnTime = Mathf.Max(0f, duration - downTime - upTime);

        if (elapsed <= downTime && downTime > 0f)
        {
            float t = elapsed / downTime;
            return Mathf.Lerp(1f, scaleDownMultiplier, t);
        }

        if (elapsed <= downTime + upTime && upTime > 0f)
        {
            float t = (elapsed - downTime) / upTime;
            return Mathf.Lerp(scaleDownMultiplier, scaleUpMultiplier, t);
        }

        if (returnTime > 0f)
        {
            float t = (elapsed - downTime - upTime) / returnTime;
            return Mathf.Lerp(scaleUpMultiplier, 1f, t);
        }

        return 1f;
    }

    private void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }

    private void PlayPoof()
    {
        if (poofParticles == null)
        {
            return;
        }

        int count = Random.Range(poofMinParticles, poofMaxParticles + 1);
        poofParticles.Emit(count);
    }

    private void StopActiveRoutine()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
    }

    private void ResetVisuals()
    {
        if (mainRenderer != null)
        {
            SetAlpha(mainRenderer, 1f);
        }

        if (ghostRenderer != null)
        {
            SetAlpha(ghostRenderer, 0f);
            ghostRenderer.enabled = false;
        }

        transform.localScale = baseScale;
    }

    private static Material GetPoofMaterial()
    {
        if (poofMaterial != null)
        {
            return poofMaterial;
        }

        Texture2D texture = CreatePoofTexture(16);
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = texture;
        poofMaterial = material;
        return poofMaterial;
    }

    private static Texture2D CreatePoofTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - (distance / radius));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }
}
