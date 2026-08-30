using UnityEngine;

namespace Caos.Simulation.Audio
{
    /// <summary>
    /// Os instrumentos da rádio. Cada voz é disparada por um sequenciador de semicolcheias e depois
    /// só é renderizada — <b>o envelope conta o tempo desde a batida do instrumento</b>, não desde o
    /// início do compasso.
    ///
    /// Essa distinção parece detalhe e não é: a versão anterior media o envelope a partir do começo da
    /// batida, então todo golpe fora do tempo forte (o bumbo sincopado do funk, o tapa da zabumba, o
    /// cavaco no contratempo) nascia com <c>e^-10</c> de amplitude — ou seja, <b>era calculado e não
    /// era ouvido</b>. Era exatamente a síncope, que é o que faz esses ritmos serem esses ritmos, que
    /// sumia.
    /// </summary>
    internal static class BandaProcedural
    {
        // ================================================================== percussão de pele
        /// <summary>Bumbo, surdo e zabumba: seno com queda de tom, mais o estalo do batedor.</summary>
        internal struct Tambor
        {
            private float _t, _vel, _fase;
            private float _tomIni, _tomFim, _queda, _decai, _estalo;
            private CaosDsp.Ruido _rnd;

            public void Configurar(float tomIni, float tomFim, float queda, float decai, float estalo, int semente)
            {
                _tomIni = tomIni; _tomFim = tomFim; _queda = queda; _decai = decai; _estalo = estalo;
                _rnd = new CaosDsp.Ruido(semente);
                _t = 99f;
            }

            public void Tocar(float vel) { _t = 0f; _vel = vel; _fase = 0f; }

            public float Render(float dt)
            {
                if (_vel <= 0f || _t > 2.5f) return 0f;
                float f = _tomFim + (_tomIni - _tomFim) * CaosDsp.Decaimento(_t, _queda);
                _fase = CaosDsp.Avancar(_fase, f * dt);
                float corpo  = CaosDsp.Seno(_fase) * CaosDsp.Decaimento(_t, _decai);
                float estalo = _estalo > 0f ? _rnd.Proximo() * CaosDsp.Decaimento(_t, 260f) * _estalo : 0f;
                _t += dt;
                return (corpo + estalo) * _vel;
            }
        }

        /// <summary>Caixa, tamborim e o tapa da zabumba: ruído em banda com um pouco de corpo afinado.</summary>
        internal struct Caixa
        {
            private float _t, _vel, _fase;
            private float _corpoHz, _decai, _mistura, _bandaHz, _q;
            private CaosDsp.Ruido _rnd;
            private CaosDsp.Biquad _banda;
            private int _coef;

            public void Configurar(float corpoHz, float bandaHz, float q, float decai, float mistura, int taxa, int semente)
            {
                _corpoHz = corpoHz; _decai = decai; _mistura = mistura;
                _bandaHz = bandaHz; _q = q;
                _rnd = new CaosDsp.Ruido(semente);
                _banda.PassaBanda(bandaHz, q, taxa);
                _t = 99f;
            }

            public void Tocar(float vel) { _t = 0f; _vel = vel; _fase = 0f; }

            public float Render(float dt, int taxa)
            {
                if (_vel <= 0f || _t > 1.2f) return 0f;
                if (--_coef <= 0) { _coef = 256; _banda.PassaBanda(_bandaHz, _q, taxa); }

                float env = CaosDsp.Decaimento(_t, _decai);
                _fase = CaosDsp.Avancar(_fase, _corpoHz * dt);
                float corpo = CaosDsp.Seno(_fase) * (1f - _mistura);
                float pele  = _banda.Filtrar(_rnd.Proximo()) * _mistura * 3f;
                _t += dt;
                return (corpo + pele) * env * _vel;
            }
        }

        /// <summary>Chimbal, ganzá e chocalho: ruído agudo, ataque instantâneo, cauda curta.</summary>
        internal struct Chocalho
        {
            private float _t, _vel, _z, _a;
            private float _decai;
            private CaosDsp.Ruido _rnd;

            public void Configurar(float corteHz, float decai, int taxa, int semente)
            {
                _decai = decai;
                _a = 1f - Mathf.Exp(-CaosDsp.Tau * Mathf.Clamp(corteHz, 20f, taxa * 0.45f) / taxa);
                _rnd = new CaosDsp.Ruido(semente);
                _t = 99f;
            }

            public void Tocar(float vel) { _t = 0f; _vel = vel; }

            public float Render(float dt)
            {
                if (_vel <= 0f || _t > 0.6f) return 0f;
                float x = _rnd.Proximo();
                _z += _a * (x - _z);
                float agudo = x - _z;                       // passa-alta de 1 polo
                float env = CaosDsp.Decaimento(_t, _decai);
                _t += dt;
                return agudo * env * _vel;
            }
        }

        /// <summary>
        /// Triângulo e agogô: parciais <b>inarmônicas</b>. Metal não é harmônico, e é justamente essa
        /// desafinação entre os parciais que faz o ouvido reconhecer "metal" em vez de "flauta".
        /// </summary>
        internal struct Metal
        {
            private float _t, _vel;
            private float _f1, _f2, _f3, _decai;
            private float _a1, _a2, _a3;
            private float _p1, _p2, _p3;

            /// <summary>
            /// Os parciais acima de Nyquist são <b>zerados</b>, não só somados. Com base em 4,1 kHz a
            /// segunda parcial cai em 11,3 kHz, acima dos 11,025 kHz de Nyquist a 22,05 kHz: sem essa
            /// checagem ela reaparece rebatida como um assobio grave e desafinado, que é o oposto do
            /// que um triângulo deveria fazer.
            /// </summary>
            public void Configurar(float baseHz, float decai, int taxa)
            {
                float limite = taxa * 0.45f;
                _f1 = baseHz; _f2 = baseHz * 2.76f; _f3 = baseHz * 5.40f;
                _a1 = _f1 < limite ? 0.5f : 0f;
                _a2 = _f2 < limite ? 0.32f : 0f;
                _a3 = _f3 < limite ? 0.18f : 0f;
                _decai = decai;
                _t = 99f;
            }

            public void Tocar(float vel) { _t = 0f; _vel = vel; }

            public float Render(float dt)
            {
                if (_vel <= 0f || _t > 1.6f) return 0f;
                _p1 = CaosDsp.Avancar(_p1, _f1 * dt);
                _p2 = CaosDsp.Avancar(_p2, _f2 * dt);
                _p3 = CaosDsp.Avancar(_p3, _f3 * dt);
                float s = CaosDsp.Seno(_p1) * _a1 + CaosDsp.Seno(_p2) * _a2 + CaosDsp.Seno(_p3) * _a3;
                float env = CaosDsp.Decaimento(_t, _decai);
                _t += dt;
                return s * env * _vel;
            }
        }

        // ================================================================== cordas
        /// <summary>
        /// Corda pincada por Karplus-Strong: um ruído curto circulando numa linha de atraso com filtro.
        /// São ~20 linhas de código e uma tabela de 1 KB, e resolve violão, viola caipira e cavaquinho
        /// de um jeito que soma de senos não resolve — o ataque tem a "unha" e o corpo decai como corda.
        /// </summary>
        internal sealed class Corda
        {
            private readonly float[] _linha = new float[1024];
            private int _n, _pos;
            private float _decai = 0.9995f, _amortece = 0.6f, _vel;
            private CaosDsp.Ruido _rnd;

            public Corda(int semente) { _rnd = new CaosDsp.Ruido(semente); }

            public void Tocar(float freq, float vel, int taxa, float sustentacao, float amortecimento)
            {
                _n = Mathf.Clamp(Mathf.RoundToInt(taxa / Mathf.Max(35f, freq)), 8, _linha.Length);
                for (int i = 0; i < _n; i++) _linha[i] = _rnd.Proximo();
                _pos = 0;
                _vel = vel;
                _amortece = Mathf.Clamp(amortecimento, 0.05f, 1f);
                _decai = Mathf.Exp(-1f / Mathf.Max(1f, sustentacao * taxa));
            }

            public void Silenciar() { _vel = 0f; }

            public float Render()
            {
                if (_n <= 0 || _vel <= 0f) return 0f;
                float atual = _linha[_pos];
                int prox = _pos + 1 >= _n ? 0 : _pos + 1;
                float media = (atual + _linha[prox]) * 0.5f;
                _linha[_pos] = (atual + (media - atual) * _amortece) * _decai;
                _pos = prox;
                return atual * _vel;
            }
        }

        // ================================================================== sopros e teclas
        /// <summary>
        /// Palhetas batendo juntas e levemente desafinadas — o <i>musette</i> da sanfona. Também serve
        /// de órgão gospel quando o desafino cai a zero e as parciais viram registros.
        /// </summary>
        internal struct Palheta
        {
            private float _p1, _p2, _p3, _env, _tremolo;
            private CaosDsp.PassaBaixa _lp;
            private bool _pronto;

            public float Render(float dt, int taxa, float freq, float alvo, float desafino, float brilhoHz, float velTremolo)
            {
                if (!_pronto) { _lp.Ajustar(brilhoHz, taxa); _pronto = true; }
                _env += (alvo - _env) * Mathf.Min(1f, dt * (alvo > _env ? 14f : 6f));
                if (_env < 0.0005f) return 0f;

                _p1 = CaosDsp.Avancar(_p1, freq * dt);
                _p2 = CaosDsp.Avancar(_p2, freq * (1f + desafino) * dt);
                _p3 = CaosDsp.Avancar(_p3, freq * 2f * dt);

                float s = CaosDsp.Dente(_p1, freq / taxa) * 0.42f
                        + CaosDsp.Dente(_p2, freq / taxa) * 0.34f
                        + CaosDsp.Seno(_p3) * 0.16f;

                if (velTremolo > 0f)
                {
                    _tremolo = CaosDsp.Avancar(_tremolo, velTremolo * dt);
                    s *= 0.85f + 0.15f * CaosDsp.Seno(_tremolo);
                }
                return _lp.Filtrar(s) * _env;
            }
        }

        /// <summary>Baixo elétrico/acústico: seno grave para o corpo e dente filtrado para o ataque.</summary>
        internal struct Baixo
        {
            private float _fase, _t, _vel, _freq, _freqAlvo;
            private CaosDsp.PassaBaixa _lp;
            private bool _pronto;

            public void Tocar(float freq, float vel) { _freqAlvo = freq; if (_vel <= 0f) _freq = freq; _t = 0f; _vel = vel; }
            public void Silenciar() { _vel = 0f; }

            public float Render(float dt, int taxa, float decai, float corteHz)
            {
                if (_vel <= 0f) return 0f;
                if (!_pronto) { _lp.Ajustar(corteHz, taxa); _pronto = true; }

                _freq += (_freqAlvo - _freq) * Mathf.Min(1f, dt * 60f);   // ligadura entre as notas
                _fase = CaosDsp.Avancar(_fase, _freq * dt);

                float env = CaosDsp.Decaimento(_t, decai) * Mathf.Min(1f, _t * 400f);
                float s = CaosDsp.Seno(_fase) * 0.75f + _lp.Filtrar(CaosDsp.Dente(_fase, _freq / taxa)) * 0.45f;
                _t += dt;
                return s * env * _vel;
            }
        }

        /// <summary>Voz solista: dente por filtro ressonante com envelope — serve de metal, teclado e apito.</summary>
        internal struct Solo
        {
            private float _fase, _t, _vel, _freq, _freqAlvo;
            private CaosDsp.Biquad _lp;
            private int _coef;

            /// <summary>Nota nova. Em <paramref name="ligado"/> a frequência escorrega da anterior (portamento).</summary>
            public void Tocar(float freq, float vel, bool ligado)
            {
                _freqAlvo = freq;
                if (!ligado || _vel <= 0f) _freq = freq;
                _t = 0f;
                _vel = vel;
            }

            public void Silenciar() { _vel = 0f; }

            public float Render(float dt, int taxa, float decai, float corteHz, float q, float portamento)
            {
                if (_vel <= 0f || _t > 3f) return 0f;
                _freq += (_freqAlvo - _freq) * Mathf.Min(1f, dt * portamento);
                _fase = CaosDsp.Avancar(_fase, _freq * dt);

                float env = CaosDsp.Decaimento(_t, decai) * Mathf.Min(1f, _t * 120f);
                if (--_coef <= 0) { _coef = 128; _lp.PassaBaixa(Mathf.Clamp(corteHz * (0.6f + env), 200f, taxa * 0.42f), q, taxa); }

                float s = _lp.Filtrar(CaosDsp.Dente(_fase, _freq / taxa));
                _t += dt;
                return s * env * _vel;
            }
        }

        /// <summary>Naipe sustentado (coral, cordas, pad de teclado): três senos com batimento lento.</summary>
        internal struct Naipe
        {
            private float _p1, _p2, _p3, _env, _lfo;

            public float Render(float dt, float freq, float alvo, float largura, float velLfo)
            {
                _env += (alvo - _env) * Mathf.Min(1f, dt * (alvo > _env ? 2.2f : 1.4f));
                if (_env < 0.0005f) return 0f;

                _p1 = CaosDsp.Avancar(_p1, freq * dt);
                _p2 = CaosDsp.Avancar(_p2, freq * (1f + largura) * dt);
                _p3 = CaosDsp.Avancar(_p3, freq * 2.001f * dt);
                _lfo = CaosDsp.Avancar(_lfo, velLfo * dt);

                float s = CaosDsp.Seno(_p1) * 0.45f + CaosDsp.Seno(_p2) * 0.35f + CaosDsp.Seno(_p3) * 0.20f;
                return s * _env * (0.82f + 0.18f * CaosDsp.Seno(_lfo));
            }
        }
    }
}
