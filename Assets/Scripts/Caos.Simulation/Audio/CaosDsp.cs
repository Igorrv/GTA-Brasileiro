using UnityEngine;

namespace Caos.Simulation.Audio
{
    /// <summary>
    /// Blocos de DSP usados por todo o áudio do jogo. Tudo aqui roda <b>na thread de áudio</b>, uma vez
    /// por amostra, então há três regras que valem para o arquivo inteiro:
    ///
    ///  1. <b>Zero alocação</b> — nada de <c>new</c>, <c>string</c> ou API da Unity durante a renderização.
    ///  2. <b>Determinístico</b> — o ruído é um xorshift semeado, não <c>Random</c>; a mesma semente
    ///     produz o mesmo som em qualquer aparelho, que é o contrato do projeto.
    ///  3. <b>Barato</b> — o alvo é um celular intermediário mexendo três fluxos ao mesmo tempo, então
    ///     seno é aproximação polinomial e os filtros são de 1ª/2ª ordem, sem convolução.
    ///
    /// Os filtros são <c>struct</c> de propósito: viram campos dentro dos sintetizadores e não geram
    /// lixo nem indireção por ponteiro.
    /// </summary>
    public static class CaosDsp
    {
        public const float Tau = 6.2831853071795865f;

        // ------------------------------------------------------------------ osciladores
        /// <summary>
        /// Seno a partir de <b>fase normalizada</b> (0..1 = uma volta), com a aproximação parabólica de
        /// Bhaskara refinada — erro abaixo de 0,1%, cerca de 6× mais rápida que <c>Mathf.Sin</c> e sem
        /// a conversão double/float que <c>System.Math</c> faz por dentro.
        ///
        /// Trabalhar em fase normalizada (e não em <c>t</c> absoluto) é o que garante que um fluxo de
        /// horas não perca precisão: a fase volta pra zero a cada ciclo em vez de virar um float gigante.
        /// </summary>
        public static float Seno(float fase)
        {
            fase -= Mathf.Floor(fase);
            float u = fase * 2f - 1f;                 // -1..1 ≡ -π..π
            float y = 4f * u * (1f - (u < 0f ? -u : u));
            y = 0.225f * (y * (y < 0f ? -y : y) - y) + y;
            return -y;                                // compensa o deslocamento de meia volta
        }

        /// <summary>
        /// Avança um acumulador de fase mantendo-o em 0..1.
        ///
        /// Isso não é preciosismo: um fluxo procedural roda por horas, e uma fase que só cresce chega a
        /// 700 mil depois de uma hora a 200 Hz — magnitude em que o <c>float</c> tem passo de 0,06 e o
        /// oscilador simplesmente para de oscilar direito. Enrolar a fase a cada volta mantém a
        /// precisão intacta para sempre.
        /// </summary>
        public static float Avancar(float fase, float incremento)
        {
            fase += incremento;
            if (fase >= 1f || fase < 0f) fase -= Mathf.Floor(fase);
            return fase;
        }

        /// <summary>Dente de serra com anti-alias leve (PolyBLEP) — base de baixo e naipe de metais.</summary>
        public static float Dente(float fase, float incremento)
        {
            fase -= Mathf.Floor(fase);
            float s = fase * 2f - 1f;
            return s - PolyBlep(fase, incremento);
        }

        /// <summary>Onda quadrada com largura de pulso variável — palhetado seco, sanfona, órgão.</summary>
        public static float Quadrada(float fase, float incremento, float largura)
        {
            fase -= Mathf.Floor(fase);
            float s = fase < largura ? 1f : -1f;
            s += PolyBlep(fase, incremento);
            float b = fase - largura;
            s -= PolyBlep(b - Mathf.Floor(b), incremento);
            return s;
        }

        public static float Triangulo(float fase)
        {
            fase -= Mathf.Floor(fase);
            return 4f * (fase < 0.5f ? fase : 1f - fase) - 1f;
        }

        /// <summary>Correção de degrau: sem ela o dente de serra grave vira chiado agudo no celular.</summary>
        private static float PolyBlep(float t, float dt)
        {
            if (dt <= 0f) return 0f;
            if (t < dt)          { t /= dt;            return t + t - t * t - 1f; }
            if (t > 1f - dt)     { t = (t - 1f) / dt;  return t * t + t + t + 1f; }
            return 0f;
        }

        // ------------------------------------------------------------------ ruído
        /// <summary>
        /// Ruído branco por xorshift de 32 bits. O jogo usava <c>sin(t·99991)·43758</c>, que não é ruído
        /// branco de verdade: tem estrutura periódica audível e chia diferente conforme <c>t</c> cresce.
        /// Xorshift é tão barato quanto e realmente plano no espectro.
        /// </summary>
        public struct Ruido
        {
            private uint _estado;

            public Ruido(int semente) { _estado = (uint)(semente * 747796405 + 2891336453); if (_estado == 0u) _estado = 0x9E3779B9u; }

            public float Proximo()
            {
                _estado ^= _estado << 13;
                _estado ^= _estado >> 17;
                _estado ^= _estado << 5;
                return (_estado >> 8) * (1f / 8388608f) - 1f;   // 24 bits → -1..1
            }

            /// <summary>Sorteio 0..1 (usado para decidir eventos, não para gerar sinal).</summary>
            public float Sorte()
            {
                _estado ^= _estado << 13;
                _estado ^= _estado >> 17;
                _estado ^= _estado << 5;
                return (_estado >> 8) * (1f / 16777216f);
            }
        }

        // ------------------------------------------------------------------ filtros
        /// <summary>Passa-baixa de 1 polo. Barata e suficiente para abafar cabine, sopro e distância.</summary>
        public struct PassaBaixa
        {
            private float _z, _a;

            public void Ajustar(float corteHz, int taxa)
            {
                float x = Mathf.Exp(-Tau * Mathf.Clamp(corteHz, 10f, taxa * 0.45f) / taxa);
                _a = 1f - x;
            }

            public float Filtrar(float x)
            {
                _z += _a * (x - _z);
                return _z;
            }
        }

        /// <summary>Passa-alta de 1 polo — tira o barro do sinal e simula alto-falante pequeno.</summary>
        public struct PassaAlta
        {
            private float _z, _a;

            public void Ajustar(float corteHz, int taxa)
            {
                float x = Mathf.Exp(-Tau * Mathf.Clamp(corteHz, 5f, taxa * 0.45f) / taxa);
                _a = 1f - x;
            }

            public float Filtrar(float x)
            {
                _z += _a * (x - _z);
                return x - _z;
            }
        }

        /// <summary>
        /// Biquad (fórmulas de Robert Bristow-Johnson). É o que dá <b>identidade</b> ao som: os formantes
        /// da voz do locutor, a banda estreita do AM e o corpo do escapamento são todos o mesmo filtro
        /// com coeficientes diferentes.
        /// </summary>
        public struct Biquad
        {
            private float _b0, _b1, _b2, _a1, _a2;
            private float _x1, _x2, _y1, _y2;

            public void PassaBanda(float freq, float q, int taxa)
            {
                float w = Tau * Mathf.Clamp(freq, 20f, taxa * 0.45f) / taxa;
                float sn = Mathf.Sin(w), cs = Mathf.Cos(w);
                float alfa = sn / (2f * Mathf.Max(0.05f, q));
                float a0 = 1f + alfa;
                _b0 = alfa / a0; _b1 = 0f; _b2 = -alfa / a0;
                _a1 = -2f * cs / a0; _a2 = (1f - alfa) / a0;
            }

            public void PassaBaixa(float freq, float q, int taxa)
            {
                float w = Tau * Mathf.Clamp(freq, 20f, taxa * 0.45f) / taxa;
                float sn = Mathf.Sin(w), cs = Mathf.Cos(w);
                float alfa = sn / (2f * Mathf.Max(0.05f, q));
                float a0 = 1f + alfa;
                _b0 = (1f - cs) * 0.5f / a0; _b1 = (1f - cs) / a0; _b2 = _b0;
                _a1 = -2f * cs / a0; _a2 = (1f - alfa) / a0;
            }

            public void Pico(float freq, float q, float ganhoDb, int taxa)
            {
                float A = Mathf.Pow(10f, ganhoDb / 40f);
                float w = Tau * Mathf.Clamp(freq, 20f, taxa * 0.45f) / taxa;
                float sn = Mathf.Sin(w), cs = Mathf.Cos(w);
                float alfa = sn / (2f * Mathf.Max(0.05f, q));
                float a0 = 1f + alfa / A;
                _b0 = (1f + alfa * A) / a0; _b1 = -2f * cs / a0; _b2 = (1f - alfa * A) / a0;
                _a1 = -2f * cs / a0; _a2 = (1f - alfa / A) / a0;
            }

            public float Filtrar(float x)
            {
                float y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
                _x2 = _x1; _x1 = x;
                _y2 = _y1; _y1 = y;
                // trava contra denormal/NaN: um valor sujo aqui trava o filtro em silêncio pra sempre
                if (y > -1e-8f && y < 1e-8f) { _y1 = 0f; _y2 = 0f; }
                else if (float.IsNaN(y) || float.IsInfinity(y)) { _x1 = _x2 = _y1 = _y2 = 0f; return 0f; }
                return y;
            }
        }

        /// <summary>
        /// Compressor de ganho suave. Rádio de verdade comprime pesado — é por isso que a estação soa
        /// "colada" e alta mesmo com a música variando de dinâmica.
        /// </summary>
        public struct Compressor
        {
            private float _envelope;

            public void Configurar() { _envelope = 0f; }

            public float Processar(float x, float limiar, float razao, float ataque, float alivio)
            {
                float nivel = x < 0f ? -x : x;
                _envelope += (nivel > _envelope ? ataque : alivio) * (nivel - _envelope);
                if (_envelope <= limiar) return x;
                float excesso = _envelope - limiar;
                float ganho = (limiar + excesso / razao) / _envelope;
                return x * ganho;
            }
        }

        /// <summary>Envelope exponencial de decaimento (percussão, transientes).</summary>
        public static float Decaimento(float x, float k) => x < 0f ? 0f : Mathf.Exp(-x * k);

        /// <summary>Saturação suave — corta pico sem o estalo do <c>Clamp</c> duro que havia antes.</summary>
        public static float Saturar(float x)
        {
            if (x < -3f) return -1f;
            if (x >  3f) return  1f;
            return x * (27f + x * x) / (27f + 9f * x * x);
        }

        public static float Db(float db) => Mathf.Pow(10f, db * 0.05f);

        /// <summary>Semitons acima da referência → multiplicador de frequência.</summary>
        public static float Semitom(int n) => Mathf.Pow(2f, n / 12f);

        /// <summary>Hash estável de string (não usa <c>GetHashCode</c>, que varia entre execuções).</summary>
        public static int Hash(string s)
        {
            if (string.IsNullOrEmpty(s)) return 17;
            unchecked
            {
                int h = 17;
                for (int i = 0; i < s.Length; i++) h = h * 31 + s[i];
                return h & 0x7fffffff;
            }
        }
    }
}
