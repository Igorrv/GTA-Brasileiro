using System.Collections.Generic;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Sprites gerados em runtime (o projeto não tem nenhum arquivo de imagem). São eles que permitem
    /// mostrador <b>redondo</b> de verdade no painel do carro e máscara circular no radar — em vez do
    /// quadrado que a <c>Image</c> do uGUI desenha por padrão.
    ///
    /// Tudo com borda suavizada por amostragem da distância ao centro, e cacheado por chave: cada
    /// formato é gerado uma única vez por sessão.
    /// </summary>
    public static class UiTextures
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>Disco cheio.</summary>
        public static Sprite Circulo(int tam = 256) => Obter("circ" + tam, tam, (d, _) => Suavizar(d, 0.5f));

        /// <summary>Anel (mostrador). <paramref name="espessura"/> em fração do raio.</summary>
        public static Sprite Anel(float espessura = 0.12f, int tam = 256)
            => Obter($"anel{espessura:F2}_{tam}", tam, (d, _) =>
            {
                float externo = Suavizar(d, 0.5f);
                float interno = Suavizar(d, 0.5f - espessura);
                return Mathf.Clamp01(externo - interno);
            });

        /// <summary>Retângulo de cantos arredondados (cartão, botão, pílula).</summary>
        public static Sprite Arredondado(float raio = 0.25f, int tam = 128)
            => Obter($"round{raio:F2}_{tam}", tam, (_, uv) =>
            {
                Vector2 p = new Vector2(Mathf.Abs(uv.x - 0.5f), Mathf.Abs(uv.y - 0.5f));
                Vector2 q = p - new Vector2(0.5f - raio, 0.5f - raio);
                float dist = (q.x > 0f && q.y > 0f) ? q.magnitude : Mathf.Max(q.x, q.y);
                return Mathf.Clamp01((raio - dist) * tam * 0.5f);
            });

        // ------------------------------------------------------------------
        private static Sprite Obter(string chave, int tam, System.Func<float, Vector2, float> alfa)
        {
            if (_cache.TryGetValue(chave, out var s) && s != null) return s;

            var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp,
                name       = "UiTex_" + chave
            };

            var px = new Color32[tam * tam];
            for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                Vector2 uv = new Vector2((x + 0.5f) / tam, (y + 0.5f) / tam);
                float d = Vector2.Distance(uv, new Vector2(0.5f, 0.5f));
                float a = Mathf.Clamp01(alfa(d, uv));
                px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);

            s = Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f, 0,
                              SpriteMeshType.FullRect, Vector4.one * (tam * 0.25f));
            _cache[chave] = s;
            return s;
        }

        /// <summary>Borda de ~1,5 px: some a serrilha sem custar shader.</summary>
        private static float Suavizar(float dist, float raio) => Mathf.Clamp01((raio - dist) * 160f);

        public static void Clear() => _cache.Clear();
    }
}
