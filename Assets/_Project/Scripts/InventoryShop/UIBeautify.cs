using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime UI beautification: rounded corners, shadows, color presets.
/// All methods return the component they create for chaining.
/// </summary>
public static class UIBeautify
{
    // ---- Color Palette ----
    public static readonly Color PanelBg     = new Color(0.06f, 0.07f, 0.10f, 0.94f);
    public static readonly Color InvTitle    = new Color(0.15f, 0.40f, 0.60f, 1f);
    public static readonly Color ShopTitle   = new Color(0.55f, 0.40f, 0.12f, 1f);
    public static readonly Color SlotBg      = new Color(0.15f, 0.16f, 0.22f, 1f);
    public static readonly Color SlotEmpty   = new Color(0.10f, 0.11f, 0.15f, 1f);
    public static readonly Color BtnNormal   = new Color(0.20f, 0.45f, 0.70f, 1f);
    public static readonly Color BtnHover    = new Color(0.30f, 0.58f, 0.85f, 1f);
    public static readonly Color BtnPressed  = new Color(0.12f, 0.30f, 0.52f, 1f);
    public static readonly Color BtnGold     = new Color(0.65f, 0.48f, 0.15f, 1f);
    public static readonly Color BtnGoldHover= new Color(0.82f, 0.62f, 0.20f, 1f);
    public static readonly Color QuickBarBg  = new Color(0.04f, 0.05f, 0.08f, 0.85f);
    public static readonly Color QuickSlotBg = new Color(0.12f, 0.13f, 0.20f, 0.95f);
    public static readonly Color PopupBg     = new Color(0.08f, 0.09f, 0.14f, 0.97f);
    public static readonly Color TextPrimary = new Color(0.95f, 0.96f, 0.98f, 1f);
    public static readonly Color TextDim     = new Color(0.55f, 0.60f, 0.70f, 1f);
    public static readonly Color TextGold    = new Color(1f, 0.82f, 0.25f, 1f);
    public static readonly Color TextWarn    = new Color(1f, 0.50f, 0.40f, 1f);

    // ---- Sprite Cache ----
    private static Sprite _panelSprite;
    private static Sprite _slotSprite;
    private static Sprite _btnSprite;
    private const int TEX_SIZE = 128;

    public static Sprite CreateRoundedRect(int radius, int size = TEX_SIZE)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color white = Color.white;
        Color clear = Color.clear;
        Color[] pixels = new Color[size * size];

        float r = radius;
        float rSq = r * r;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                // Compute shortest distance to each corner
                bool inside = true;
                float dist;

                // Top-left
                dist = (px - r) * (px - r) + (py - (size - r)) * (py - (size - r));
                if (px < r && py > size - r && dist > rSq) inside = false;
                // Top-right
                dist = (px - (size - r)) * (px - (size - r)) + (py - (size - r)) * (py - (size - r));
                if (px > size - r && py > size - r && dist > rSq) inside = false;
                // Bottom-left
                dist = (px - r) * (px - r) + (py - r) * (py - r);
                if (px < r && py < r && dist > rSq) inside = false;
                // Bottom-right
                dist = (px - (size - r)) * (px - (size - r)) + (py - r) * (py - r);
                if (px > size - r && py < r && dist > rSq) inside = false;

                // Anti-alias edges
                if (inside)
                {
                    // Check distance to edge for AA
                    float edgeDist = float.MaxValue;

                    // Top edge
                    if (py > size - r) edgeDist = Mathf.Min(edgeDist, size - py);
                    // Bottom edge
                    if (py < r) edgeDist = Mathf.Min(edgeDist, py);
                    // Left edge
                    if (px < r) edgeDist = Mathf.Min(edgeDist, px);
                    // Right edge
                    if (px > size - r) edgeDist = Mathf.Min(edgeDist, size - px);

                    // Corner AA
                    if (px < r && py < r)
                        edgeDist = Mathf.Min(edgeDist, r - Mathf.Sqrt((px - r) * (px - r) + (py - r) * (py - r)));
                    if (px < r && py > size - r)
                        edgeDist = Mathf.Min(edgeDist, r - Mathf.Sqrt((px - r) * (px - r) + (py - (size - r)) * (py - (size - r))));
                    if (px > size - r && py < r)
                        edgeDist = Mathf.Min(edgeDist, r - Mathf.Sqrt((px - (size - r)) * (px - (size - r)) + (py - r) * (py - r)));
                    if (px > size - r && py > size - r)
                        edgeDist = Mathf.Min(edgeDist, r - Mathf.Sqrt((px - (size - r)) * (px - (size - r)) + (py - (size - r)) * (py - (size - r))));

                    float alpha = Mathf.Clamp01(edgeDist / 1.5f);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
                else
                {
                    pixels[y * size + x] = clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        sprite.name = $"RoundedRect_r{radius}";
        return sprite;
    }

    public static Sprite PanelSprite
    {
        get
        {
            if (_panelSprite == null) _panelSprite = CreateRoundedRect(14);
            return _panelSprite;
        }
    }

    public static Sprite SlotSprite
    {
        get
        {
            if (_slotSprite == null) _slotSprite = CreateRoundedRect(10);
            return _slotSprite;
        }
    }

    public static Sprite ButtonSprite
    {
        get
        {
            if (_btnSprite == null) _btnSprite = CreateRoundedRect(8);
            return _btnSprite;
        }
    }

    // ---- Image creation helpers ----
    public static Image SetRounded(GameObject obj, Sprite sprite, Color color)
    {
        Image img = obj.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = Image.Type.Simple;
        return img;
    }

    // ---- Button style presets ----
    public static void StyleButton(Button button, Color normal, Color hover, Color pressed)
    {
        ColorBlock cb = button.colors;
        cb.normalColor = normal;
        cb.highlightedColor = hover;
        cb.pressedColor = pressed;
        cb.selectedColor = hover;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.1f;
        button.colors = cb;
    }

    // ---- Add drop shadow to any GameObject ----
    public static Shadow AddShadow(GameObject obj, Color color, Vector2 offset)
    {
        Shadow sh = obj.AddComponent<Shadow>();
        sh.effectColor = color;
        sh.effectDistance = offset;
        return sh;
    }

    // ---- Add outline ----
    public static Outline AddOutline(GameObject obj, Color color, Vector2 size)
    {
        Outline ol = obj.AddComponent<Outline>();
        ol.effectColor = color;
        ol.effectDistance = size;
        return ol;
    }
}
