using UnityEngine;

namespace INeedToPEEak
{
    /// <summary>
    /// Procedurally generated art: status icons, item icons and materials.
    /// No asset bundles required — everything is drawn in code at startup.
    /// </summary>
    internal static class BathroomAssets
    {
        public static Sprite PooIcon { get; private set; }
        public static Sprite PeeIcon { get; private set; }
        public static Sprite DirtyIcon { get; private set; }
        public static Sprite StinkIcon { get; private set; }
        public static Texture2D PooItemIcon { get; private set; }
        public static Texture2D ToiletPaperItemIcon { get; private set; }

        private static Material _pooMaterial;
        private static Material _paperMaterial;
        private static Material _puddleMaterial;

        private static readonly Color PooBrown = new Color(0.38f, 0.23f, 0.06f);
        private static readonly Color PooBrownDark = new Color(0.27f, 0.16f, 0.04f);
        private static readonly Color PeeYellow = new Color(0.95f, 0.87f, 0.25f);
        private static readonly Color PaperWhite = new Color(0.95f, 0.95f, 0.92f);

        public static void CreateAll()
        {
            PooIcon = MakeSprite(DrawPooIcon(64));
            PeeIcon = MakeSprite(DrawDropIcon(64));
            DirtyIcon = MakeSprite(DrawDirtIcon(64));
            StinkIcon = MakeSprite(DrawStinkIcon(64));
            PooItemIcon = DrawPooIcon(128);
            ToiletPaperItemIcon = DrawToiletPaperIcon(128);
        }

        public static Material PooMaterial => _pooMaterial != null ? _pooMaterial : (_pooMaterial = MakeMaterial(PooBrown, false));
        public static Material PaperMaterial => _paperMaterial != null ? _paperMaterial : (_paperMaterial = MakeMaterial(PaperWhite, false));
        public static Material PuddleMaterial => _puddleMaterial != null ? _puddleMaterial : (_puddleMaterial = MakeMaterial(new Color(PeeYellow.r, PeeYellow.g, PeeYellow.b, 0.62f), true));

        private static Material MakeMaterial(Color color, bool transparent)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", transparent ? 0.9f : 0.25f);
            if (transparent)
            {
                // URP transparent surface setup
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            Object.DontDestroyOnLoad(mat);
            return mat;
        }

        private static Sprite MakeSprite(Texture2D tex)
        {
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            Object.DontDestroyOnLoad(sprite);
            return sprite;
        }

        private static Texture2D NewTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var clear = new Color(0, 0, 0, 0);
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            tex.SetPixels(px);
            Object.DontDestroyOnLoad(tex);
            return tex;
        }

        private static void FillCircle(Texture2D tex, float cx, float cy, float r, Color color)
        {
            int size = tex.width;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r)), x1 = Mathf.Min(size - 1, Mathf.CeilToInt(cx + r));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r)), y1 = Mathf.Min(size - 1, Mathf.CeilToInt(cy + r));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= r * r) tex.SetPixel(x, y, color);
                }
            }
        }

        /// <summary>Classic three-tier poo swirl.</summary>
        private static Texture2D DrawPooIcon(int s)
        {
            var tex = NewTexture(s);
            float u = s / 64f;
            FillCircle(tex, 32 * u, 14 * u, 17 * u, PooBrownDark);
            FillCircle(tex, 32 * u, 14 * u, 15 * u, PooBrown);
            FillCircle(tex, 32 * u, 30 * u, 13 * u, PooBrownDark);
            FillCircle(tex, 32 * u, 30 * u, 11 * u, PooBrown);
            FillCircle(tex, 32 * u, 44 * u, 9 * u, PooBrownDark);
            FillCircle(tex, 32 * u, 44 * u, 7 * u, PooBrown);
            FillCircle(tex, 34 * u, 52 * u, 4 * u, PooBrown);
            // eyes
            FillCircle(tex, 26 * u, 18 * u, 3.4f * u, Color.white);
            FillCircle(tex, 38 * u, 18 * u, 3.4f * u, Color.white);
            FillCircle(tex, 26 * u, 18 * u, 1.6f * u, Color.black);
            FillCircle(tex, 38 * u, 18 * u, 1.6f * u, Color.black);
            tex.Apply();
            return tex;
        }

        /// <summary>Yellow droplet.</summary>
        private static Texture2D DrawDropIcon(int s)
        {
            var tex = NewTexture(s);
            float u = s / 64f;
            var dark = new Color(0.72f, 0.63f, 0.1f);
            FillCircle(tex, 32 * u, 22 * u, 17 * u, dark);
            FillCircle(tex, 32 * u, 22 * u, 15 * u, PeeYellow);
            // tapering top of the drop
            for (int i = 0; i < 22; i++)
            {
                float t = i / 22f;
                float y = (38 + t * 18) * u;
                float r = Mathf.Lerp(11f, 1.2f, t) * u;
                FillCircle(tex, 32 * u, y, r + 1.5f * u, dark);
                FillCircle(tex, 32 * u, y, r, PeeYellow);
            }
            FillCircle(tex, 27 * u, 25 * u, 3.2f * u, new Color(1f, 1f, 0.85f)); // shine
            tex.Apply();
            return tex;
        }

        /// <summary>Grey grime splatter.</summary>
        private static Texture2D DrawDirtIcon(int s)
        {
            var tex = NewTexture(s);
            float u = s / 64f;
            var grey = new Color(0.62f, 0.62f, 0.6f);
            var darkGrey = new Color(0.42f, 0.42f, 0.4f);
            FillCircle(tex, 32 * u, 30 * u, 15 * u, darkGrey);
            FillCircle(tex, 32 * u, 30 * u, 13 * u, grey);
            // deterministic blobs around the middle
            float[,] blobs = { { 14, 44, 5 }, { 50, 42, 6 }, { 20, 14, 5 }, { 46, 16, 4 }, { 55, 28, 3 }, { 9, 28, 3.5f } };
            for (int i = 0; i < blobs.GetLength(0); i++)
            {
                FillCircle(tex, blobs[i, 0] * u, blobs[i, 1] * u, (blobs[i, 2] + 1.2f) * u, darkGrey);
                FillCircle(tex, blobs[i, 0] * u, blobs[i, 1] * u, blobs[i, 2] * u, grey);
            }
            tex.Apply();
            return tex;
        }

        /// <summary>Three rising stink waves.</summary>
        private static Texture2D DrawStinkIcon(int s)
        {
            var tex = NewTexture(s);
            float u = s / 64f;
            var olive = new Color(0.72f, 0.78f, 0.28f);
            var dark = new Color(0.45f, 0.5f, 0.14f);
            // three wavy vertical lines rising like smell
            for (int wave = 0; wave < 3; wave++)
            {
                float cx = (18 + wave * 14) * u;
                for (int i = 0; i <= 40; i++)
                {
                    float t = i / 40f;
                    float y = (10 + t * 44) * u;
                    float x = cx + Mathf.Sin(t * Mathf.PI * 2.5f) * 4.5f * u;
                    FillCircle(tex, x, y, 3.4f * u, dark);
                    FillCircle(tex, x, y, 2.4f * u, olive);
                }
            }
            tex.Apply();
            return tex;
        }

        /// <summary>White roll with a hole, viewed at an angle.</summary>
        private static Texture2D DrawToiletPaperIcon(int s)
        {
            var tex = NewTexture(s);
            float u = s / 128f;
            var shadow = new Color(0.65f, 0.65f, 0.62f);
            // dangling sheet
            for (int y = (int)(8 * u); y < (int)(64 * u); y++)
            {
                for (int x = (int)(78 * u); x < (int)(104 * u); x++)
                {
                    tex.SetPixel(x, y, PaperWhite);
                }
            }
            FillCircle(tex, 64 * u, 74 * u, 42 * u, shadow);
            FillCircle(tex, 64 * u, 74 * u, 39 * u, PaperWhite);
            FillCircle(tex, 64 * u, 74 * u, 15 * u, shadow);
            FillCircle(tex, 64 * u, 74 * u, 12 * u, new Color(0.8f, 0.75f, 0.68f)); // cardboard tube
            tex.Apply();
            return tex;
        }
    }
}
