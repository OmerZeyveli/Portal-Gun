using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PortalCrosshair : MonoBehaviour
{
    [Header("Colors")]
    public Color centerColor = new Color(0.86f, 0.86f, 0.86f, 0.95f);
    public Color centerOutlineColor = new Color(0f, 0f, 0f, 0.45f);
    public Color blueColor = new Color(0.15f, 0.55f, 1f, 1f);
    public Color orangeColor = new Color(1f, 0.35f, 0.05f, 1f);

    [Header("Layout")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    public float centerLength = 30f;
    public float centerThickness = 3f;
    public float centerOutlinePadding = 1.5f;
    public float indicatorOffset = 44f;
    public float indicatorLength = 68f;
    public float indicatorCurveDepth = 28f;
    public float indicatorWidth = 8f;
    public float indicatorBorderThickness = 2f;

    Canvas canvas;
    CanvasScaler canvasScaler;
    RectTransform root;
    Image centerShadowHorizontal;
    Image centerShadowVertical;
    Image centerHorizontal;
    Image centerVertical;
    IndicatorParts blueIndicator;
    IndicatorParts orangeIndicator;
    Sprite filledIndicatorSprite;
    Sprite outlineIndicatorSprite;
    bool built;
    bool blueAvailable;
    bool orangeAvailable;

    const int IndicatorTextureScale = 4;

    class IndicatorParts
    {
        public Image image;
        public Sprite filledSprite;
        public Sprite outlineSprite;

        public void SetColor(Color color)
        {
            if (image)
                image.color = color;
        }

        public void SetFilled(bool filled)
        {
            if (image)
                image.sprite = filled ? filledSprite : outlineSprite;
        }
    }

    void Awake()
    {
        Build();
    }

    void OnEnable()
    {
        Build();
        ApplyAvailability();
    }

    public void SetColors(Color blue, Color orange)
    {
        blueColor = blue;
        orangeColor = orange;

        Build();
        ApplyColors();
    }

    public void SetAvailability(bool blueCanPlace, bool orangeCanPlace)
    {
        blueAvailable = blueCanPlace;
        orangeAvailable = orangeCanPlace;

        Build();
        ApplyAvailability();
    }

    void Build()
    {
        if (built)
            return;

        GameObject canvasObject = new GameObject("Crosshair Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        root = CreateRectTransform(canvasRect, "Root", new Vector2(170f, 130f), Vector2.zero);

        centerShadowHorizontal = CreateImage(root, "Center Horizontal Outline", new Vector2(centerLength + centerOutlinePadding * 2f, centerThickness + centerOutlinePadding * 2f), Vector2.zero, 0f, centerOutlineColor);
        centerShadowVertical = CreateImage(root, "Center Vertical Outline", new Vector2(centerThickness + centerOutlinePadding * 2f, centerLength + centerOutlinePadding * 2f), Vector2.zero, 0f, centerOutlineColor);
        centerHorizontal = CreateImage(root, "Center Horizontal", new Vector2(centerLength, centerThickness), Vector2.zero, 0f, centerColor);
        centerVertical = CreateImage(root, "Center Vertical", new Vector2(centerThickness, centerLength), Vector2.zero, 0f, centerColor);

        blueIndicator = CreateIndicator("Blue Indicator", new Vector2(-indicatorOffset, 0f), -1f, blueColor);
        orangeIndicator = CreateIndicator("Orange Indicator", new Vector2(indicatorOffset, 0f), 1f, orangeColor);

        built = true;
        ApplyColors();
        ApplyAvailability();
    }

    IndicatorParts CreateIndicator(string name, Vector2 position, float sideDirection, Color color)
    {
        RectTransform indicatorRoot = CreateRectTransform(root, name, new Vector2(indicatorCurveDepth + indicatorWidth, indicatorLength), position);
        indicatorRoot.localScale = new Vector3(sideDirection > 0f ? -1f : 1f, 1f, 1f);

        EnsureIndicatorSprites();

        Image image = indicatorRoot.gameObject.AddComponent<Image>();
        image.sprite = outlineIndicatorSprite;
        image.color = color;
        image.raycastTarget = false;

        IndicatorParts parts = new IndicatorParts
        {
            image = image,
            filledSprite = filledIndicatorSprite,
            outlineSprite = outlineIndicatorSprite
        };

        return parts;
    }

    void EnsureIndicatorSprites()
    {
        if (filledIndicatorSprite && outlineIndicatorSprite)
            return;

        filledIndicatorSprite = CreateIndicatorSprite(true);
        outlineIndicatorSprite = CreateIndicatorSprite(false);
    }

    Sprite CreateIndicatorSprite(bool filled)
    {
        int width = Mathf.Max(8, Mathf.CeilToInt((indicatorCurveDepth + indicatorWidth) * IndicatorTextureScale));
        int height = Mathf.Max(8, Mathf.CeilToInt(indicatorLength * IndicatorTextureScale));
        float thickness = Mathf.Max(1f, indicatorWidth * IndicatorTextureScale);
        float border = Mathf.Max(1f, indicatorBorderThickness * IndicatorTextureScale);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[width * height];
        float edgeInset = thickness * 0.55f + 1f;
        Vector2 top = new Vector2(width - edgeInset, height - edgeInset);
        Vector2 middle = new Vector2(edgeInset, (height - 1) * 0.5f);
        Vector2 bottom = new Vector2(width - edgeInset, edgeInset);
        float halfThickness = thickness * 0.5f;
        float innerHalfThickness = Mathf.Max(0f, halfThickness - border);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 point = new Vector2(x, y);
                float distance;
                float t;
                GetClosestQuadraticBezierPoint(point, top, middle, bottom, out distance, out t);

                bool inside = distance <= halfThickness;
                bool borderPixel = inside && (filled || distance >= innerHalfThickness || t <= 0.04f || t >= 0.96f);

                pixels[y * width + x] = borderPixel ? Color.white : Color.clear;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), IndicatorTextureScale);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    static void GetClosestQuadraticBezierPoint(Vector2 point, Vector2 a, Vector2 b, Vector2 c, out float closestDistance, out float closestT)
    {
        closestDistance = float.PositiveInfinity;
        closestT = 0f;
        const int samples = 48;

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector2 curvePoint = QuadraticBezier(a, b, c, t);
            float distance = Vector2.Distance(point, curvePoint);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestT = t;
            }
        }
    }

    static Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    RectTransform CreateRectTransform(Transform parent, string name, Vector2 size, Vector2 position)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);

        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    Image CreateImage(Transform parent, string name, Vector2 size, Vector2 position, float angle, Color color)
    {
        RectTransform rect = CreateRectTransform(parent, name, size, position);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    void ApplyColors()
    {
        if (centerShadowHorizontal)
            centerShadowHorizontal.color = centerOutlineColor;
        if (centerShadowVertical)
            centerShadowVertical.color = centerOutlineColor;
        if (centerHorizontal)
            centerHorizontal.color = centerColor;
        if (centerVertical)
            centerVertical.color = centerColor;

        if (blueIndicator != null)
            blueIndicator.SetColor(blueColor);
        if (orangeIndicator != null)
            orangeIndicator.SetColor(orangeColor);
    }

    void ApplyAvailability()
    {
        if (blueIndicator != null)
            blueIndicator.SetFilled(blueAvailable);
        if (orangeIndicator != null)
            orangeIndicator.SetFilled(orangeAvailable);
    }
}
