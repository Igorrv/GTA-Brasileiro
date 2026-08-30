using UnityEngine;

namespace Caos.Simulation.Audio
{
    /// <summary>
    /// A voz do locutor, sintetizada a partir do <b>texto que o HUD mostra</b>.
    ///
    /// Não é reconhecível como português — é um vocalise de formantes, a mesma técnica de
    /// <i>Animal Crossing</i> e <i>Banjo-Kazooie</i>, só que com filtro de formante de verdade em vez
    /// de bipe. O ganho é que a fala <b>tem a métrica da frase real</b>: cada vogal de
    /// <c>radio.json</c> vira uma vogal cantada com os formantes daquele som, cada consoante vira um
    /// ataque ou uma fricativa, vírgula respira e ponto de exclamação sobe o tom. O jogador ouve um
    /// locutor com o ritmo exato da linha que está lendo na tela.
    ///
    /// É também o que finalmente coloca a <b>AM de notícias no ar</b>: aquela estação sintetizava
    /// <c>null</c> e ficava muda; agora ela é o que uma AM de notícias é — voz e chiado.
    ///
    /// <b>Threading.</b> <see cref="Falar"/> roda na thread principal e monta a fala num objeto de um
    /// pool de dois; a thread de áudio só lê a referência publicada. Sem <c>lock</c>, sem alocação por
    /// fala depois do aquecimento.
    /// </summary>
    public sealed class LocutorVoz
    {
        private const int   kMaxSegmentos = 192;
        private const float kDurMaxima    = 11f;

        /// <summary>Timbre do apresentador. Muda por estação — é metade da identidade dela.</summary>
        public struct Perfil
        {
            /// <summary>Fundamental em Hz. ~110 é locutor grave de AM; ~190, uma voz feminina de FM.</summary>
            public float Tom;
            /// <summary>Escala dos formantes: &gt;1 encurta o trato vocal (voz mais "fina").</summary>
            public float Trato;
            /// <summary>Multiplicador de velocidade da fala. Funk fala rápido, gospel fala devagar.</summary>
            public float Ritmo;
            /// <summary>Rouquidão: mistura ruído na glote (locutor de rádio de estrada).</summary>
            public float Aspereza;
            /// <summary>Empolgação: amplia acento e excursão de tom.</summary>
            public float Energia;

            public static Perfil Padrao => new Perfil { Tom = 128f, Trato = 1f, Ritmo = 1f, Aspereza = 0.12f, Energia = 0.5f };
        }

        private enum Tipo : byte { Pausa = 0, Vogal = 1, Sonora = 2, Fricativa = 3, Explosiva = 4 }

        private struct Segmento
        {
            public Tipo  Tipo;
            public float Dur;      // segundos
            public float F1, F2, F3;
            public float Tom;      // multiplicador da fundamental
            public float Ganho;
        }

        /// <summary>Uma fala pronta. Publicada inteira, para a thread de áudio nunca ver meio objeto.</summary>
        private sealed class Fala
        {
            public readonly Segmento[] Segmentos = new Segmento[kMaxSegmentos];
            public int   Quantos;
            public float Duracao;
            public Perfil Perfil;
        }

        private readonly Fala[] _pool = { new Fala(), new Fala() };
        private int _proximoDoPool;

        private volatile Fala _noAr;

        // ---- estado da thread de áudio ----
        private int   _indice;
        private float _tempo;
        private float _fase, _faseVibrato;
        private CaosDsp.Biquad _fmt1, _fmt2, _fmt3;
        private CaosDsp.Ruido _ruido = new CaosDsp.Ruido(20250830);
        private float _f1Suave, _f2Suave, _f3Suave, _tomSuave, _ganhoSuave;
        private int   _contadorCoef;

        /// <summary>Duração total da última fala montada — o HUD usa para segurar a legenda.</summary>
        public float Duracao { get; private set; }

        /// <summary>Há fala no ar neste instante (lido pela thread principal para o ducking).</summary>
        public bool NoAr => _noAr != null;

        // ================================================================== thread principal
        /// <summary>
        /// Converte a frase em segmentos articulados. Roda uma vez por locução (a cada ~30 s), então
        /// pode se dar ao luxo de percorrer a string caractere a caractere.
        /// </summary>
        public void Falar(string texto, Perfil perfil, int semente)
        {
            if (string.IsNullOrEmpty(texto)) { Calar(); return; }

            var fala = _pool[_proximoDoPool];
            _proximoDoPool = 1 - _proximoDoPool;

            fala.Perfil  = perfil;
            fala.Quantos = 0;
            fala.Duracao = 0f;    // o objeto vem do pool: sem zerar, o teto de duração usaria o valor da fala anterior

            var rnd = new CaosDsp.Ruido(semente);
            float ritmo = Mathf.Max(0.45f, perfil.Ritmo);
            bool pergunta = texto.IndexOf('?') >= 0;
            bool exclama  = texto.IndexOf('!') >= 0;

            int vogais = 0;
            for (int i = 0; i < texto.Length && fala.Quantos < kMaxSegmentos; i++)
            {
                char c = char.ToLowerInvariant(texto[i]);

                if (c == ' ' || c == '\n' || c == '\t') { Empurrar(fala, Tipo.Pausa, 0.075f / ritmo, 0f, 0f, 0f, 1f, 0f); continue; }
                if (c == ',' || c == ';' || c == ':' || c == '—' || c == '-') { Empurrar(fala, Tipo.Pausa, 0.19f / ritmo, 0f, 0f, 0f, 1f, 0f); continue; }
                if (c == '.' || c == '!' || c == '?') { Empurrar(fala, Tipo.Pausa, 0.30f / ritmo, 0f, 0f, 0f, 1f, 0f); continue; }

                if (Vogal(c, out float f1, out float f2, out float f3, out float abertura))
                {
                    vogais++;
                    // acento a cada 2–3 sílabas + declinação da frase: é isso que impede o "robô"
                    float progresso = (float)i / texto.Length;
                    float declinio  = Mathf.Lerp(1.05f, 0.90f, progresso);
                    float acento    = (vogais % 3 == 1) ? 1f + 0.07f * perfil.Energia : 1f;
                    float tom       = declinio * acento * (1f + (rnd.Sorte() - 0.5f) * 0.05f);
                    if (pergunta && progresso > 0.72f) tom *= Mathf.Lerp(1f, 1.22f, (progresso - 0.72f) / 0.28f);
                    if (exclama  && progresso > 0.80f) tom *= 1.10f;

                    float dur = Mathf.Lerp(0.075f, 0.135f, abertura) / ritmo;
                    Empurrar(fala, Tipo.Vogal, dur, f1, f2, f3, tom, Mathf.Lerp(0.75f, 1f, abertura));
                    continue;
                }

                if (Fricativa(c)) { Empurrar(fala, Tipo.Fricativa, 0.055f / ritmo, 1800f, 4200f, 6500f, 1f, 0.5f); continue; }
                if (Explosiva(c)) { Empurrar(fala, Tipo.Explosiva, 0.042f / ritmo, 900f, 1700f, 2600f, 1f, 0.8f); continue; }
                if (Sonora(c))    { Empurrar(fala, Tipo.Sonora,    0.052f / ritmo, 320f, 1100f, 2400f, 0.97f, 0.65f); continue; }
                // dígitos e símbolos viram uma sílaba neutra — "640" precisa soar como algo
                if (char.IsLetterOrDigit(c)) Empurrar(fala, Tipo.Vogal, 0.085f / ritmo, 520f, 1400f, 2500f, 1f, 0.8f);
            }

            if (fala.Quantos == 0) { Calar(); return; }

            // fecha com uma respiração curta, senão a fala termina em corte seco
            fala.Duracao = Mathf.Min(fala.Duracao, kDurMaxima - 0.2f);
            Empurrar(fala, Tipo.Pausa, 0.12f, 0f, 0f, 0f, 1f, 0f);
            Duracao = fala.Duracao;

            _indice = 0;
            _tempo  = 0f;
            _noAr   = fala;
        }

        public void Calar()
        {
            _noAr = null;
            Duracao = 0f;
        }

        private static void Empurrar(Fala f, Tipo tipo, float dur, float f1, float f2, float f3, float tom, float ganho)
        {
            if (f.Quantos >= kMaxSegmentos || f.Duracao >= kDurMaxima) return;
            f.Segmentos[f.Quantos++] = new Segmento { Tipo = tipo, Dur = dur, F1 = f1, F2 = f2, F3 = f3, Tom = tom, Ganho = ganho };
            f.Duracao += dur;
        }

        // ---- tabela de formantes (voz masculina de referência; o perfil escala depois) ----
        private static bool Vogal(char c, out float f1, out float f2, out float f3, out float abertura)
        {
            switch (c)
            {
                case 'a': case 'á': case 'à': case 'â': f1 = 750f; f2 = 1200f; f3 = 2550f; abertura = 1.00f; return true;
                case 'ã':                               f1 = 620f; f2 = 1150f; f3 = 2400f; abertura = 0.90f; return true;
                case 'e': case 'é':                     f1 = 560f; f2 = 1900f; f3 = 2600f; abertura = 0.80f; return true;
                case 'ê':                               f1 = 420f; f2 = 2100f; f3 = 2650f; abertura = 0.65f; return true;
                case 'i': case 'í':                     f1 = 290f; f2 = 2250f; f3 = 2950f; abertura = 0.45f; return true;
                case 'o': case 'ó':                     f1 = 560f; f2 =  920f; f3 = 2450f; abertura = 0.80f; return true;
                case 'ô': case 'õ':                     f1 = 420f; f2 =  760f; f3 = 2400f; abertura = 0.60f; return true;
                case 'u': case 'ú':                     f1 = 310f; f2 =  700f; f3 = 2350f; abertura = 0.40f; return true;
            }
            f1 = f2 = f3 = 0f; abertura = 0f;
            return false;
        }

        private static bool Fricativa(char c) => c == 's' || c == 'ç' || c == 'x' || c == 'z' || c == 'f' || c == 'j' || c == 'h' || c == 'v';
        private static bool Explosiva(char c) => c == 'p' || c == 't' || c == 'k' || c == 'c' || c == 'q' || c == 'b' || c == 'd' || c == 'g';
        private static bool Sonora(char c)    => c == 'm' || c == 'n' || c == 'l' || c == 'r' || c == 'w' || c == 'y';

        // ================================================================== thread de áudio
        /// <summary>Uma amostra de voz. Devolve 0 quando não há fala no ar.</summary>
        public float Render(int taxa)
        {
            var fala = _noAr;
            if (fala == null) return 0f;

            if (_indice >= fala.Quantos) { _noAr = null; return 0f; }

            float dt = 1f / taxa;
            _tempo += dt;
            var seg = fala.Segmentos[_indice];
            if (_tempo >= seg.Dur)
            {
                _tempo -= seg.Dur;
                _indice++;
                if (_indice >= fala.Quantos) { _noAr = null; return 0f; }
                seg = fala.Segmentos[_indice];
            }

            var p = fala.Perfil;

            // alvos deslizam em ~25 ms: é a coarticulação, o que faz uma vogal "escorrer" na outra
            float k = Mathf.Min(1f, dt * 40f);
            float trato = Mathf.Max(0.6f, p.Trato);
            bool  soa   = seg.Tipo == Tipo.Vogal || seg.Tipo == Tipo.Sonora;

            _f1Suave += (seg.F1 * trato - _f1Suave) * k;
            _f2Suave += (seg.F2 * trato - _f2Suave) * k;
            _f3Suave += (seg.F3 * trato - _f3Suave) * k;
            _tomSuave += (seg.Tom - _tomSuave) * k;
            _ganhoSuave += (seg.Ganho - _ganhoSuave) * Mathf.Min(1f, dt * 90f);

            if (_ganhoSuave < 0.0005f && seg.Tipo == Tipo.Pausa) return 0f;
            if (_f1Suave < 60f) return 0f;

            // recalcular biquad custa seno e cosseno; a cada 32 amostras (~1,5 ms) o ouvido não nota a
            // diferença e o custo cai a 3% do que seria por amostra
            if (--_contadorCoef <= 0)
            {
                _contadorCoef = 32;
                _fmt1.PassaBanda(_f1Suave, 7f, taxa);
                _fmt2.PassaBanda(_f2Suave, 9f, taxa);
                _fmt3.PassaBanda(_f3Suave, 11f, taxa);
            }

            // ---- glote ----
            float fonte;
            if (soa)
            {
                _faseVibrato = CaosDsp.Avancar(_faseVibrato, 5.4f * dt);
                float vibrato = 1f + 0.006f * CaosDsp.Seno(_faseVibrato) * p.Energia;
                float f0 = Mathf.Clamp(p.Tom * _tomSuave * vibrato, 60f, 420f);
                _fase = CaosDsp.Avancar(_fase, f0 * dt);

                // pulso glotal: sobe rápido, desce devagar e fica fechado o resto do ciclo
                const float abertura = 0.42f;
                float pulso = _fase < abertura ? CaosDsp.Seno(_fase / abertura * 0.5f) : 0f;
                pulso = pulso * pulso * 2f - 0.35f;
                fonte = pulso + _ruido.Proximo() * p.Aspereza * 0.35f;
            }
            else
            {
                // fricativa/explosiva: só ruído, com a explosiva estourando no ataque
                float envelope = seg.Tipo == Tipo.Explosiva
                    ? CaosDsp.Decaimento(_tempo, 70f)
                    : Mathf.Min(1f, _tempo * 60f);
                fonte = _ruido.Proximo() * envelope * 0.8f;
            }

            float s = _fmt1.Filtrar(fonte) * 1.00f
                    + _fmt2.Filtrar(fonte) * 0.62f
                    + _fmt3.Filtrar(fonte) * 0.28f;

            return CaosDsp.Saturar(s * 2.6f * _ganhoSuave);
        }
    }
}
