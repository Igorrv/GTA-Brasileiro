using System.Collections.Generic;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>Superfícies da cidade. Cada uma tem um desenho próprio gerado em código.</summary>
    public enum Superficie
    {
        Reboco, Tijolo, Asfalto, Calcada, Telha, Grama, Areia, Madeira, Metal,
        Fachada,      // parede de prédio com fileiras de janela
        Vitrine,      // térreo comercial: vidro grande + esquadria
        Chapisco,     // muro sem reboco, granulado

        // ---- veículo ----
        Pintura,      // lataria: verniz com leve flake metálico
        Pneu,         // banda de rodagem com sulcos + flanco
        VidroCarro,   // vidro fumê com degradê de reflexo
        Grade,        // grade do radiador
        Placa,        // placa Mercosul (tarja azul em cima)

        // ---- personagem ----
        Pele,         // poro fino + variação de tom
        Tecido,       // trama de camiseta
        Jeans,        // sarja com fio claro

        // ---- envelhecimento ----
        Rodape,       // faixa de umidade e mofo na base da parede
        Lambe         // cartaz colado + pichação por cima
    }

    /// <summary>
    /// Texturas geradas em runtime — o projeto não importa nenhuma imagem. É isto que tira o aspecto
    /// de "caixa colorida": tijolo com junta de argamassa, asfalto manchado e remendado, pedra
    /// portuguesa em xadrez, telha ondulada, grama com variação e — a mais importante — a
    /// <b>fachada com fileiras de janela</b>, que faz um bloco virar prédio na hora.
    ///
    /// Tudo é <b>tileável</b> (a borda casa com a oposta) e cacheado: cada superfície é desenhada uma
    /// vez por sessão, em 128², e reaproveitada pela cidade inteira.
    /// </summary>
    public static class CityTextures
    {
        private const int kTam = 128;
        private static readonly Dictionary<Superficie, Texture2D> _cache = new Dictionary<Superficie, Texture2D>();

        public static Texture2D Obter(Superficie s)
        {
            if (_cache.TryGetValue(s, out var t) && t != null) return t;
            t = Desenhar(s);
            _cache[s] = t;
            return t;
        }

        /// <summary>Quantos metros um "ladrilho" da textura cobre — define a densidade do tiling.</summary>
        public static float MetrosPorTile(Superficie s)
        {
            switch (s)
            {
                case Superficie.Tijolo:   return 1.2f;
                case Superficie.Chapisco: return 1.6f;
                case Superficie.Asfalto:  return 8f;
                case Superficie.Calcada:  return 2.4f;
                case Superficie.Telha:    return 1.8f;
                case Superficie.Grama:    return 6f;
                case Superficie.Areia:    return 5f;
                case Superficie.Madeira:  return 2f;
                case Superficie.Metal:    return 3f;
                case Superficie.Fachada:  return 3.2f;   // ~1 andar por ladrilho
                case Superficie.Vitrine:  return 4f;
                case Superficie.Pintura:  return 2.5f;
                case Superficie.Pneu:     return 0.55f;  // sulco miúdo
                case Superficie.VidroCarro: return 3f;
                case Superficie.Grade:    return 0.5f;
                case Superficie.Placa:    return 0.5f;
                case Superficie.Pele:     return 0.9f;
                case Superficie.Tecido:   return 0.35f;
                case Superficie.Jeans:    return 0.30f;
                case Superficie.Rodape:   return 4f;
                case Superficie.Lambe:    return 2.2f;
                default:                  return 2.5f;   // reboco
            }
        }

        public static void Clear()
        {
            foreach (var kv in _cache) if (kv.Value != null) Object.Destroy(kv.Value);
            _cache.Clear();
        }

        // ==================================================================== desenho
        private static Texture2D Desenhar(Superficie s)
        {
            var tex = new Texture2D(kTam, kTam, TextureFormat.RGB24, true)
            {
                name = "CaosTex_" + s,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };

            var px = new Color[kTam * kTam];
            for (int y = 0; y < kTam; y++)
            for (int x = 0; x < kTam; x++)
                px[y * kTam + x] = Pixel(s, x, y);

            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        private static Color Pixel(Superficie s, int x, int y)
        {
            float u = x / (float)kTam, v = y / (float)kTam;
            switch (s)
            {
                case Superficie.Tijolo:   return Tijolo(x, y);
                case Superficie.Chapisco: return Cinza(0.72f, 0.16f, x, y, 2.2f);
                case Superficie.Asfalto:  return Asfalto(x, y);
                case Superficie.Calcada:  return Calcada(x, y);
                case Superficie.Telha:    return Telha(x, y);
                case Superficie.Grama:    return Grama(x, y);
                case Superficie.Areia:    return Cinza(0.93f, 0.05f, x, y, 3.5f) * new Color(1f, 0.96f, 0.82f);
                case Superficie.Madeira:  return Madeira(x, y);
                case Superficie.Metal:    return Metal(x, y);
                case Superficie.Fachada:  return Fachada(u, v);
                case Superficie.Vitrine:  return Vitrine(u, v);
                case Superficie.Pintura:  return Pintura(x, y);
                case Superficie.Pneu:     return Pneu(u, v, x, y);
                case Superficie.VidroCarro: return VidroCarro(u, v);
                case Superficie.Grade:    return Grade(u, v);
                case Superficie.Placa:    return Placa(u, v);
                case Superficie.Pele:     return Pele(x, y);
                case Superficie.Tecido:   return Tecido(x, y);
                case Superficie.Jeans:    return Jeans(x, y);
                case Superficie.Rodape:   return Rodape(u, v, x, y);
                case Superficie.Lambe:    return Lambe(u, v, x, y);
                default:                  return Cinza(0.88f, 0.09f, x, y, 1.6f);   // reboco
            }
        }

        // ---- ruído tileável: soma de senos, então a borda casa com a oposta ----
        private static float Ruido(int x, int y, float escala)
        {
            float u = x / (float)kTam * Mathf.PI * 2f;
            float v = y / (float)kTam * Mathf.PI * 2f;
            float n = Mathf.Sin(u * escala) * Mathf.Cos(v * escala * 1.3f)
                    + Mathf.Sin(u * escala * 2.7f + 1.3f) * Mathf.Cos(v * escala * 2.1f)
                    + Mathf.Sin((u + v) * escala * 4.1f);
            return n / 3f;   // −1..1
        }

        /// <summary>Granulado fino determinístico (poeira, grão de areia, textura de reboco).</summary>
        private static float Grao(int x, int y)
        {
            float f = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
            return (f - Mathf.Floor(f)) * 2f - 1f;
        }

        private static Color Cinza(float baseV, float amp, int x, int y, float escala)
        {
            float n = Ruido(x, y, escala) * 0.6f + Grao(x, y) * 0.4f;
            float c = Mathf.Clamp01(baseV + n * amp);
            return new Color(c, c, c);
        }

        // ---- superfícies ----
        private static Color Tijolo(int x, int y)
        {
            const int alturaFiada = 16, comprimento = 32, junta = 3;
            int fiada = y / alturaFiada;
            int desloc = (fiada % 2 == 0) ? 0 : comprimento / 2;     // amarração
            int xx = (x + desloc) % comprimento;
            int yy = y % alturaFiada;

            bool argamassa = yy < junta || xx < junta;
            if (argamassa)
            {
                float c = 0.74f + Grao(x, y) * 0.05f;
                return new Color(c, c * 0.98f, c * 0.94f);
            }
            // cada tijolo puxa para um tom (queima desigual)
            float tom = ((fiada * 7 + (x + desloc) / comprimento * 13) % 5) / 5f;
            float r = Mathf.Lerp(0.55f, 0.72f, tom) + Grao(x, y) * 0.05f;
            return new Color(r, r * 0.52f, r * 0.42f);
        }

        private static Color Asfalto(int x, int y)
        {
            float n = Ruido(x, y, 3.2f) * 0.5f + Grao(x, y) * 0.5f;
            float c = Mathf.Clamp01(0.235f + n * 0.075f);

            // remendo mais escuro: o asfalto brasileiro é uma colcha de retalhos
            float remendo = Ruido(x, y, 0.9f);
            if (remendo > 0.45f) c *= 0.72f;

            // trinca fina
            float trinca = Mathf.Abs(Ruido(x, y, 1.7f));
            if (trinca < 0.03f) c *= 0.6f;

            return new Color(c, c, c * 1.03f);
        }

        private static Color Calcada(int x, int y)
        {
            // pedra portuguesa: xadrez de pedra clara e escura com junta
            const int pedra = 16;
            int cx = x / pedra, cy = y / pedra;
            bool clara = (cx + cy) % 2 == 0;
            int ix = x % pedra, iy = y % pedra;
            bool junta = ix < 2 || iy < 2;

            float grao = Grao(x, y) * 0.05f;
            if (junta) { float j = 0.60f + grao; return new Color(j, j, j * 0.97f); }
            float c = (clara ? 0.88f : 0.30f) + grao + Ruido(x, y, 6f) * 0.03f;
            return new Color(c, c, c * 0.98f);
        }

        private static Color Telha(int x, int y)
        {
            // ondulação senoidal + variação por canaleta
            float onda = Mathf.Sin(x / (float)kTam * Mathf.PI * 8f);
            float sombra = 0.72f + onda * 0.22f;
            float desgaste = Ruido(x, y, 4f) * 0.06f;
            float r = Mathf.Clamp01((0.62f + desgaste) * sombra);
            return new Color(r, r * 0.48f, r * 0.38f);
        }

        private static Color Grama(int x, int y)
        {
            float n = Ruido(x, y, 7f) * 0.5f + Grao(x, y) * 0.5f;
            float g = Mathf.Clamp01(0.42f + n * 0.16f);
            // manchas secas (não existe gramado uniforme no Brasil)
            float seco = Ruido(x, y, 1.4f);
            float r = seco > 0.4f ? g * 1.15f : g * 0.55f;
            return new Color(r * 0.75f, g, r * 0.35f);
        }

        private static Color Madeira(int x, int y)
        {
            float veio = Mathf.Sin(y / (float)kTam * Mathf.PI * 6f + Ruido(x, y, 2f) * 2.5f);
            float c = 0.46f + veio * 0.08f + Grao(x, y) * 0.03f;
            return new Color(c, c * 0.68f, c * 0.42f);
        }

        private static Color Metal(int x, int y)
        {
            // zinco ondulado do telhado de galpão
            float onda = Mathf.Sin(x / (float)kTam * Mathf.PI * 16f);
            float c = Mathf.Clamp01(0.62f + onda * 0.12f + Grao(x, y) * 0.03f);
            float ferrugem = Ruido(x, y, 2.3f);
            if (ferrugem > 0.55f) return new Color(c * 0.85f, c * 0.55f, c * 0.38f);
            return new Color(c, c * 1.01f, c * 1.04f);
        }

        /// <summary>
        /// Fachada: uma fileira de janelas por ladrilho, com peitoril e parede em volta. É a textura
        /// que mais muda a cara da cidade — um cubo com isso já lê como prédio.
        /// </summary>
        private static Color Fachada(float u, float v)
        {
            const float janelaLarg = 0.26f, janelaAlt = 0.42f;
            float ju = Mathf.Repeat(u * 2f, 1f);   // 2 janelas por ladrilho na horizontal

            bool naJanelaX = ju > 0.5f - janelaLarg * 0.5f && ju < 0.5f + janelaLarg * 0.5f;
            bool naJanelaY = v > 0.30f && v < 0.30f + janelaAlt;

            if (naJanelaX && naJanelaY)
            {
                // vidro com reflexo diagonal e caixilho
                float borda = 0.03f;
                bool caixilho = ju < 0.5f - janelaLarg * 0.5f + borda || ju > 0.5f + janelaLarg * 0.5f - borda
                             || v < 0.30f + borda || v > 0.30f + janelaAlt - borda;
                if (caixilho) return new Color(0.30f, 0.30f, 0.32f);

                float reflexo = Mathf.Clamp01((ju - v) * 2.4f + 0.5f);
                var vidro = Color.Lerp(new Color(0.14f, 0.19f, 0.24f), new Color(0.48f, 0.60f, 0.70f), reflexo);
                return vidro;
            }

            // peitoril claro embaixo da janela
            if (naJanelaX && v > 0.26f && v <= 0.30f) return new Color(0.80f, 0.79f, 0.76f);

            float c = 0.80f + Ruido((int)(u * kTam), (int)(v * kTam), 2.2f) * 0.05f;
            // faixa de laje entre andares
            if (v < 0.06f) c *= 0.88f;
            return new Color(c, c * 0.99f, c * 0.95f);
        }

        // ================================================================== veículo
        /// <summary>
        /// Lataria: quase branca (a cor vem do tint do material), com o <b>flake metálico</b> quase
        /// imperceptível e uma variação suave de verniz. Sem isso a pintura fica chapada como plástico.
        /// </summary>
        private static Color Pintura(int x, int y)
        {
            float flake = Grao(x, y) * 0.035f;                 // partícula metálica
            float verniz = Ruido(x, y, 1.1f) * 0.025f;         // ondulação da camada de tinta
            float c = Mathf.Clamp01(1f + flake + verniz);
            return new Color(c, c, c);
        }

        /// <summary>Pneu: banda com sulcos em V no centro e flanco liso e mais escuro nas pontas.</summary>
        private static Color Pneu(float u, float v, int x, int y)
        {
            // v atravessa a largura do pneu: as bordas são o flanco
            bool flanco = v < 0.18f || v > 0.82f;
            if (flanco)
            {
                float f = 0.16f + Grao(x, y) * 0.02f;
                // letras/frisos do flanco
                if (Mathf.Repeat(u * 12f, 1f) < 0.35f && v > 0.05f && v < 0.13f) f += 0.10f;
                return new Color(f, f, f);
            }

            // banda de rodagem: sulcos em V
            float centro = Mathf.Abs(v - 0.5f) * 2f;           // 0 no meio, 1 na borda da banda
            float onda = Mathf.Repeat(u * 8f + centro * 0.45f, 1f);
            bool sulco = onda < 0.34f;
            float c = sulco ? 0.07f : 0.20f;
            c += Grao(x, y) * 0.02f;
            return new Color(c, c, c);
        }

        /// <summary>Vidro do carro: fumê com reflexo do céu descendo de cima.</summary>
        private static Color VidroCarro(float u, float v)
        {
            float reflexo = Mathf.Clamp01(v * 1.2f - 0.1f + (u - 0.5f) * 0.25f);
            var c = Color.Lerp(new Color(0.06f, 0.08f, 0.10f), new Color(0.55f, 0.66f, 0.76f), reflexo * reflexo);
            return c;
        }

        /// <summary>Grade do radiador: colmeia de barras horizontais com vão escuro.</summary>
        private static Color Grade(float u, float v)
        {
            float linha = Mathf.Repeat(v * 7f, 1f);
            if (linha < 0.42f) return new Color(0.05f, 0.05f, 0.06f);     // vão
            float coluna = Mathf.Repeat(u * 20f, 1f);
            float c = coluna < 0.25f ? 0.28f : 0.42f;                     // barra com brilho
            return new Color(c, c, c * 1.05f);
        }

        /// <summary>
        /// Placa Mercosul: fundo branco, tarja azul em cima e o bloco escuro dos caracteres.
        /// Não dá pra ler a placa de perto, mas de longe é exatamente o que o olho espera ver.
        /// </summary>
        private static Color Placa(float u, float v)
        {
            if (v > 0.78f) return new Color(0.10f, 0.20f, 0.55f);         // tarja azul (Mercosul)
            if (v < 0.06f || v > 0.94f || u < 0.04f || u > 0.96f)
                return new Color(0.15f, 0.15f, 0.16f);                    // moldura

            // caracteres: 7 blocos escuros com espaçamento
            float ch = Mathf.Repeat((u - 0.08f) * 7.6f, 1f);
            bool letra = ch > 0.18f && ch < 0.78f && v > 0.22f && v < 0.66f && u > 0.06f && u < 0.94f;
            return letra ? new Color(0.08f, 0.08f, 0.09f) : new Color(0.94f, 0.94f, 0.93f);
        }

        // ================================================================== envelhecimento
        /// <summary>
        /// Rodapé de umidade: a faixa escura que sobe da calçada em toda parede brasileira, com
        /// mofo esverdeado e escorrido de chuva descendo de cima. É provavelmente o detalhe que mais
        /// diferencia "prédio no Brasil" de "prédio genérico de asset store".
        /// </summary>
        private static Color Rodape(float u, float v, int x, int y)
        {
            // v = 0 na base. A sujeira sobe irregular, com a borda superior serrilhada pelo ruído.
            float alturaSujeira = 0.30f + Ruido(x, y, 1.6f) * 0.14f;
            float sujo = Mathf.Clamp01((alturaSujeira - v) / Mathf.Max(0.01f, alturaSujeira));

            // escorrido vertical de chuva: faixas finas que descem de cima
            float escorrido = 0f;
            float faixa = Mathf.Repeat(u * 9f + Ruido(x, y, 0.7f) * 2f, 1f);
            if (faixa < 0.22f && v > 0.25f) escorrido = (1f - v) * 0.5f;

            float mancha = Mathf.Clamp01(sujo * 0.85f + escorrido);
            if (mancha < 0.02f) return new Color(1f, 1f, 1f);   // parte limpa: deixa o tint passar

            // mofo puxa pro verde-escuro; barro puxa pro marrom
            float verde = Ruido(x, y, 4f) * 0.5f + 0.5f;
            var suja = Color.Lerp(new Color(0.42f, 0.44f, 0.36f), new Color(0.46f, 0.40f, 0.32f), verde);
            return Color.Lerp(Color.white, suja, mancha * 0.92f);
        }

        /// <summary>
        /// Muro de rua: lambe-lambe (cartaz colado, meio rasgado) com pichação por cima. Cartaz e
        /// tinta são as duas camadas que todo muro de esquina tem no Brasil.
        /// </summary>
        private static Color Lambe(float u, float v, int x, int y)
        {
            var baseCor = new Color(0.80f, 0.79f, 0.76f);

            // cartazes: retângulos colados em fileira, alguns rasgados na borda de baixo
            float cu = Mathf.Repeat(u * 3f, 1f);
            float cv = Mathf.Repeat(v * 2f, 1f);
            bool naFolha = cu > 0.10f && cu < 0.88f && cv > 0.14f && cv < 0.82f;
            if (naFolha)
            {
                float rasgo = Ruido(x, y, 6f);
                if (!(cv < 0.24f && rasgo > 0.25f))     // pedaço arrancado embaixo
                {
                    int qual = Mathf.Abs(Mathf.RoundToInt(u * 3f + v * 2f)) % 3;
                    baseCor = qual == 0 ? new Color(0.92f, 0.88f, 0.72f)      // papel amarelado
                            : qual == 1 ? new Color(0.86f, 0.36f, 0.30f)      // cartaz vermelho
                                        : new Color(0.35f, 0.52f, 0.78f);     // cartaz azul
                    // linhas de texto do cartaz
                    if (Mathf.Repeat(cv * 9f, 1f) < 0.34f && cv < 0.70f)
                        baseCor *= 0.72f;
                }
            }

            // pichação por cima de tudo: traço grosso e anguloso
            float tag = Mathf.Abs(Mathf.Sin(u * 11f + Mathf.Sin(v * 7f) * 2.2f));
            if (tag < 0.10f && v > 0.18f && v < 0.78f)
                return new Color(0.10f, 0.10f, 0.12f);

            return baseCor * (0.96f + Grao(x, y) * 0.04f);
        }

        // ================================================================== personagem
        /// <summary>
        /// Pele: quase branca (o tom vem do tint), com poro fino e manchas suaves. Sem isso o braço
        /// fica um tubo de plástico de cor lisa.
        /// </summary>
        private static Color Pele(int x, int y)
        {
            float poro = Grao(x, y) * 0.028f;
            float mancha = Ruido(x, y, 3.5f) * 0.022f;
            float c = Mathf.Clamp01(1f + poro + mancha);
            return new Color(c, c * 0.995f, c * 0.985f);
        }

        /// <summary>Camiseta: trama de algodão (fio horizontal e vertical alternados).</summary>
        private static Color Tecido(int x, int y)
        {
            bool trama = ((x / 2) + (y / 2)) % 2 == 0;
            float c = (trama ? 1f : 0.955f) + Grao(x, y) * 0.02f;
            float vinco = Ruido(x, y, 2.2f) * 0.03f;                 // amassado do tecido
            return new Color(c + vinco, c + vinco, c + vinco);
        }

        /// <summary>Jeans: sarja diagonal com fio claro por cima.</summary>
        private static Color Jeans(int x, int y)
        {
            bool sarja = ((x + y) / 3) % 2 == 0;                      // trama na diagonal
            float c = sarja ? 1f : 0.88f;
            float desbotado = Ruido(x, y, 2.8f) * 0.06f;
            c = Mathf.Clamp01(c + desbotado + Grao(x, y) * 0.02f);
            return new Color(c * 0.96f, c * 0.98f, c);                // puxa levemente pro azul
        }

        /// <summary>Térreo comercial: vitrine grande, esquadria e soleira.</summary>
        private static Color Vitrine(float u, float v)
        {
            if (v > 0.12f && v < 0.86f)
            {
                float mu = Mathf.Repeat(u * 3f, 1f);
                if (mu < 0.06f) return new Color(0.26f, 0.26f, 0.28f);        // montante
                float reflexo = Mathf.Clamp01((mu - v) * 1.8f + 0.55f);
                return Color.Lerp(new Color(0.10f, 0.14f, 0.18f), new Color(0.55f, 0.66f, 0.74f), reflexo);
            }
            if (v <= 0.12f) return new Color(0.34f, 0.33f, 0.32f);            // soleira
            return new Color(0.72f, 0.70f, 0.67f);                            // verga
        }
    }
}
