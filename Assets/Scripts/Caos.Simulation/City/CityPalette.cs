using System.Collections.Generic;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Materiais compartilhados da cidade + helpers de construção de primitivas.
    ///
    /// Regra de performance (docs/12 §12.7): <b>nunca</b> criar um Material por objeto — a cidade tem
    /// milhares de peças e cada material novo quebra o batching. Tudo passa por <see cref="Mat"/>, que
    /// arredonda a cor e reaproveita a instância; e props sem colisão nascem sem Collider.
    /// </summary>
    public static class CityPalette
    {
        private static readonly Dictionary<int, Material> _cache = new Dictionary<int, Material>();
        private static Shader _shader;

        private static Shader Sh => _shader != null ? _shader :
            (_shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Legacy Shaders/Diffuse"));

        /// <summary>Material compartilhado para a cor dada (quantizada em 32 níveis por canal).</summary>
        public static Material Mat(Color c) => Mat(c, 0.06f, 0f);

        /// <summary>
        /// Material compartilhado com acabamento: <paramref name="brilho"/> 0 = fosco (reboco, asfalto),
        /// 1 = espelhado (vidro, água). <paramref name="metalico"/> liga o reflexo do céu — é o que faz
        /// vidro e metal parecerem vidro e metal em vez de plástico colorido.
        /// </summary>
        public static Material Mat(Color c, float brilho, float metalico)
        {
            int key = (Mathf.RoundToInt(c.r * 31) << 17) | (Mathf.RoundToInt(c.g * 31) << 12)
                    | (Mathf.RoundToInt(c.b * 31) << 7)  | (Mathf.RoundToInt(brilho * 7) << 4)
                    |  Mathf.RoundToInt(metalico * 7);
            if (_cache.TryGetValue(key, out var m) && m != null) return m;

            m = new Material(Sh) { color = c, name = "Caos_" + key };
            m.SetFloat("_Glossiness", brilho);   // Standard
            m.SetFloat("_Smoothness", brilho);   // URP, se um dia migrar
            m.SetFloat("_Metallic", metalico);
            _cache[key] = m;
            return m;
        }

        private static readonly Dictionary<int, Material> _cacheTex = new Dictionary<int, Material>();

        /// <summary>
        /// Material <b>texturizado</b>. O tiling é calculado pelo tamanho do objeto e depois
        /// <b>quantizado em potências de 2</b> — sem isso cada parede geraria um material próprio
        /// (ou exigiria MaterialPropertyBlock, que quebra o static batching). Com a quantização,
        /// a cidade inteira usa poucas dezenas de materiais e continua batendo.
        /// </summary>
        public static Material MatTex(Superficie sup, Color tint, float larguraM, float alturaM,
                                      float brilho = 0.08f, float metalico = 0f)
        {
            float metros = CityTextures.MetrosPorTile(sup);
            int tx = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(1, Mathf.RoundToInt(larguraM / metros))), 1, 32);
            int ty = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(1, Mathf.RoundToInt(alturaM  / metros))), 1, 32);

            int key = ((int)sup << 24) | (tx << 19) | (ty << 14)
                    | (Mathf.RoundToInt(tint.r * 7) << 11) | (Mathf.RoundToInt(tint.g * 7) << 8)
                    | (Mathf.RoundToInt(tint.b * 7) << 5)  | (Mathf.RoundToInt(brilho * 7) << 2)
                    |  Mathf.RoundToInt(metalico * 3);

            if (_cacheTex.TryGetValue(key, out var m) && m != null) return m;

            m = new Material(Sh) { name = $"CaosTex_{sup}_{tx}x{ty}" };
            m.mainTexture = CityTextures.Obter(sup);
            m.mainTextureScale = new Vector2(tx, ty);
            m.color = tint;
            m.SetFloat("_Glossiness", brilho);
            m.SetFloat("_Smoothness", brilho);
            m.SetFloat("_Metallic", metalico);
            _cacheTex[key] = m;
            return m;
        }

        /// <summary>
        /// Acende (ou apaga) as janelas da cidade inteira. Como o material do vidro é <b>compartilhado</b>
        /// por todos os prédios, mudar a emissão dele uma vez ilumina a cidade toda — custo zero por
        /// janela, o que no celular é a diferença entre rodar e não rodar.
        /// </summary>
        public static void AcenderJanelas(float intensidade)
        {
            var vidro = Vidro;
            if (vidro == null) return;

            if (intensidade <= 0.01f)
            {
                vidro.DisableKeyword("_EMISSION");
                vidro.SetColor("_EmissionColor", Color.black);
                vidro.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                return;
            }
            vidro.EnableKeyword("_EMISSION");
            vidro.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            vidro.SetColor("_EmissionColor", new Color(1f, 0.86f, 0.55f) * intensidade);
        }

        public static Material Mat(string hex, Color fallback)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c)) return Mat(c);
            return Mat(fallback);
        }

        public static Color Parse(string hex, Color fallback)
            => (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c)) ? c : fallback;

        /// <summary>Varia a cor levemente (evita cidade "photocopiada").</summary>
        public static Color Vary(Color c, float amount = 0.08f)
        {
            float d = Random.Range(-amount, amount);
            return new Color(Mathf.Clamp01(c.r + d), Mathf.Clamp01(c.g + d), Mathf.Clamp01(c.b + d), c.a);
        }

        // ---- paleta fixa da cidade (cor · brilho · metálico) ----
        public static Material Asfalto    => Mat(new Color(0.20f, 0.20f, 0.22f), 0.18f, 0f);   // asfalto reflete um pouco
        public static Material AsfaltoNovo=> Mat(new Color(0.16f, 0.16f, 0.18f), 0.26f, 0f);
        public static Material Calcada    => Mat(new Color(0.62f, 0.60f, 0.56f), 0.08f, 0f);
        public static Material MeioFio    => Mat(new Color(0.80f, 0.79f, 0.75f), 0.10f, 0f);
        public static Material FaixaAmarela=> Mat(new Color(0.92f, 0.76f, 0.15f), 0.30f, 0f);
        public static Material FaixaBranca=> Mat(new Color(0.92f, 0.92f, 0.90f), 0.30f, 0f);
        public static Material Terra      => Mat(new Color(0.45f, 0.34f, 0.22f), 0.03f, 0f);
        public static Material Grama      => Mat(new Color(0.28f, 0.42f, 0.20f), 0.05f, 0f);
        public static Material GramaSeca  => Mat(new Color(0.52f, 0.51f, 0.28f), 0.05f, 0f);
        public static Material Areia      => Mat(new Color(0.87f, 0.80f, 0.60f), 0.06f, 0f);
        public static Material Mar        => Mat(new Color(0.13f, 0.35f, 0.45f), 0.92f, 0.15f); // espelha o céu
        public static Material RioSujo    => Mat(new Color(0.24f, 0.26f, 0.20f), 0.55f, 0.10f);
        public static Material Concreto   => Mat(new Color(0.66f, 0.65f, 0.62f), 0.07f, 0f);
        public static Material ConcretoEscuro => Mat(new Color(0.42f, 0.42f, 0.42f), 0.07f, 0f);
        public static Material Tijolo     => Mat(new Color(0.62f, 0.34f, 0.24f), 0.05f, 0f);
        public static Material Telha      => Mat(new Color(0.55f, 0.28f, 0.20f), 0.12f, 0f);
        public static Material Vidro      => Mat(new Color(0.30f, 0.44f, 0.52f), 0.88f, 0.55f); // vidro de verdade
        public static Material Madeira    => Mat(new Color(0.45f, 0.32f, 0.18f), 0.12f, 0f);
        public static Material Metal      => Mat(new Color(0.55f, 0.57f, 0.60f), 0.62f, 0.85f);
        public static Material MetalEscuro=> Mat(new Color(0.30f, 0.31f, 0.33f), 0.50f, 0.75f);
        public static Material CaixaDagua => Mat(new Color(0.24f, 0.45f, 0.72f), 0.35f, 0f);
        public static Material Pichacao   => Mat(new Color(0.16f, 0.16f, 0.18f), 0.10f, 0f);
        public static Material Poste      => Mat(new Color(0.58f, 0.58f, 0.56f), 0.12f, 0f);
        public static Material LuzAcesa   => Mat(new Color(1.00f, 0.92f, 0.62f), 0.40f, 0f);
        public static Material Folhagem   => Mat(new Color(0.20f, 0.45f, 0.22f), 0.08f, 0f);
        public static Material Tronco     => Mat(new Color(0.35f, 0.26f, 0.18f), 0.05f, 0f);
        public static Material Pintura    => Mat(new Color(0.75f, 0.15f, 0.15f), 0.72f, 0.25f);  // lataria

        /// <summary>
        /// Cores "de rua" (toldo de barraca, guarda-sol, roupa no varal, fachada de comércio).
        /// É um conjunto <b>fixo</b> de propósito: cor aleatória contínua geraria centenas de materiais
        /// e mataria o batching.
        /// </summary>
        private static readonly Color[] kVivas =
        {
            new Color(0.85f, 0.20f, 0.20f), new Color(0.95f, 0.60f, 0.10f), new Color(0.95f, 0.85f, 0.20f),
            new Color(0.20f, 0.60f, 0.35f), new Color(0.15f, 0.45f, 0.75f), new Color(0.55f, 0.25f, 0.65f),
            new Color(0.95f, 0.95f, 0.92f), new Color(0.20f, 0.65f, 0.65f), new Color(0.90f, 0.45f, 0.55f),
        };

        public static Color CorViva()      => kVivas[Random.Range(0, kVivas.Length)];
        public static Material MatViva()   => Mat(CorViva());

        // ------------------------------------------------------------------ helpers
        /// <summary>Cubo. <paramref name="collide"/> = false remove o Collider (props decorativos).</summary>
        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat,
                                     float yawDeg = 0f, bool collide = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale    = size;
            if (!Mathf.Approximately(yawDeg, 0f)) go.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collide) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Cyl(Transform parent, string name, Vector3 pos, float diameter, float height,
                                     Material mat, bool collide = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale    = new Vector3(diameter, height * 0.5f, diameter);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collide) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>
        /// Cápsula — a primitiva que dá forma humana: braço, perna e tronco em cápsula têm as pontas
        /// arredondadas, então a junta não mostra quina quando o membro dobra.
        /// <paramref name="comprimento"/> é a altura total (ponta a ponta).
        /// </summary>
        public static GameObject Capsule(Transform parent, string name, Vector3 pos, float diametro, float comprimento,
                                         Material mat, bool collide = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            // a cápsula padrão tem 2 de altura e 1 de diâmetro
            go.transform.localScale = new Vector3(diametro, comprimento * 0.5f, diametro);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collide) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Sphere(Transform parent, string name, Vector3 pos, float diameter, Material mat,
                                        bool collide = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale    = Vector3.one * diameter;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collide) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Placa/letreiro de texto 3D (TextMesh — sem custo de Canvas).</summary>
        public static TextMesh Label(Transform parent, string text, Vector3 pos, Color color,
                                     float size = 0.4f, float yawDeg = 0f)
        {
            var go = new GameObject("Rotulo");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.characterSize = size;
            tm.fontSize = 56;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null) tm.font = font;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && font != null) mr.sharedMaterial = font.material;
            return tm;
        }

        /// <summary>Limpa o cache (chamado ao reconstruir a cidade — evita material vazado entre Plays).</summary>
        public static void Clear()
        {
            _cache.Clear();
            _cacheTex.Clear();
            CityTextures.Clear();
        }
    }
}
