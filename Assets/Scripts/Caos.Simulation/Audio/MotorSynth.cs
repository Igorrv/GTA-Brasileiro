using UnityEngine;

namespace Caos.Simulation.Audio
{
    /// <summary>
    /// O veículo inteiro num fluxo só: motor, pneu e vento.
    ///
    /// <b>Por que síntese em vez de <c>pitch</c> num clipe.</b> A versão anterior tinha a ideia certa —
    /// o tom segue o <i>giro</i>, e é por isso que a troca de marcha é audível — mas executava esticando
    /// um laço de meio segundo de 0,62× a 2,45×. Dois problemas nascem daí: o clipe de 0,5 s continha
    /// 27,5 ciclos de 55 Hz, ou seja, <b>não fechava no ponto de laço</b> e estalava duas vezes por
    /// segundo; e o <c>pitch</c> arrasta junto o chiado da admissão, então em alta rotação o sopro vira
    /// um assobio que nenhum motor faz. Aqui a fundamental é calculada a partir do RPM real
    /// (ordem de ignição = giro ÷ 30 para quatro cilindros), as harmônicas são somadas na hora e o
    /// ruído de admissão fica no lugar dele, independente do tom.
    ///
    /// O que se ganha em jogo: marcha lenta oscila, carga muda o timbre e não só o volume, tirar o pé
    /// dá retorno de escapamento, o corte de giro pipoca de verdade e a moto não soa como um Fusca
    /// acelerado — a mesma <c>Timbre</c> do catálogo agora desloca a ordem de ignição, não uma amostra.
    /// </summary>
    public sealed class MotorSynth : IFluxoPcm
    {
        // ---- parâmetros escritos pela thread principal (float é atômico nas plataformas-alvo) ----
        public float Rpm         = 900f;
        public float RpmMin      = 900f;
        public float RpmMax      = 6200f;
        public float Carga;                 // acelerador 0..1
        public float Timbre      = 1f;      // do catálogo: moto sobe, caminhão desce
        public float Derrapagem;            // 0..1 pneu cantando
        public float Velocidade01;          // 0..1 para o vento
        public float NaCabine;              // 1 = janelas fechadas, o mundo abafa
        public float Ligado;                // 0 = a pé, 1 = dirigindo

        // Solavanco é impulso, não nível: a thread principal só incrementa o contador e a de áudio
        // compara com o último que viu. Assim nenhuma das duas escreve no campo da outra.
        private int   _solavancoSeq;
        private float _solavancoForca;

        /// <summary>Sacode a lataria: buraco, batida, troca de marcha bruta.</summary>
        public void Sacudir(float forca)
        {
            _solavancoForca = Mathf.Clamp01(forca);
            _solavancoSeq++;
        }

        private readonly int _taxa;

        // ---- estado da thread de áudio ----
        private float _f, _carga, _derrapa, _vel, _cabine, _ligado, _solavanco;
        private float _p1, _p2, _p3, _p4, _p6, _pMeia, _pLenta, _pCorte;
        private CaosDsp.Ruido _rnd = new CaosDsp.Ruido(90210);
        private CaosDsp.Biquad _admissao, _escape, _pneu, _cabineLp;
        private CaosDsp.PassaAlta _ventoHp;
        private CaosDsp.PassaBaixa _ventoLp;
        private int _solavancoVisto;
        private float _estouro;

        public MotorSynth(int taxa)
        {
            _taxa = Mathf.Clamp(taxa, 8000, 48000);
            _admissao.PassaBanda(700f, 0.9f, _taxa);
            _escape.Pico(240f, 1.2f, 7f, _taxa);
            _pneu.PassaBanda(1400f, 3.2f, _taxa);
            _cabineLp.PassaBaixa(4200f, 0.8f, _taxa);
            _ventoHp.Ajustar(200f, _taxa);
            _ventoLp.Ajustar(2600f, _taxa);
        }

        public void Render(float[] destino, int quadros, int taxa, long amostraInicial)
        {
            float dt = 1f / taxa;

            // alvos lidos uma vez por bloco: a thread principal pode estar escrevendo agora
            float rpm     = Mathf.Clamp(Rpm, 0f, 12000f);
            float rpmMin  = Mathf.Max(200f, RpmMin);
            float rpmMax  = Mathf.Max(rpmMin + 500f, RpmMax);
            float rpm01   = Mathf.Clamp01((rpm - rpmMin) / (rpmMax - rpmMin));
            float timbre  = Mathf.Clamp(Timbre, 0.35f, 2.5f);
            float alvoF   = Mathf.Max(12f, rpm / 30f * timbre);      // ordem de ignição de 4 cilindros
            float alvoCar = Mathf.Clamp01(Carga);
            float alvoDer = Mathf.Clamp01(Derrapagem);
            float alvoVel = Mathf.Clamp01(Velocidade01);
            float alvoCab = Mathf.Clamp01(NaCabine);
            float alvoLig = Mathf.Clamp01(Ligado);

            int seq = _solavancoSeq;
            if (seq != _solavancoVisto) { _solavancoVisto = seq; _solavanco = Mathf.Max(_solavanco, _solavancoForca); }

            // um recálculo de filtro por bloco (~20 ms) acompanha o giro de sobra e custa quase nada
            _admissao.PassaBanda(Mathf.Lerp(420f, 1500f, rpm01), 0.9f, taxa);
            _pneu.PassaBanda(Mathf.Lerp(1000f, 1900f, alvoVel), 3.2f, taxa);
            _cabineLp.PassaBaixa(Mathf.Lerp(9000f, 3200f, alvoCab), 0.8f, taxa);

            for (int i = 0; i < quadros; i++)
            {
                // ---- suavização (a 25 Hz o ouvido não ouve degrau de parâmetro) ----
                float k = Mathf.Min(1f, dt * 25f);
                _f       += (alvoF   - _f) * k;
                _carga   += (alvoCar - _carga) * k;
                _derrapa += (alvoDer - _derrapa) * Mathf.Min(1f, dt * 8f);
                _vel     += (alvoVel - _vel) * Mathf.Min(1f, dt * 3f);
                _cabine  += (alvoCab - _cabine) * Mathf.Min(1f, dt * 4f);
                _ligado  += (alvoLig - _ligado) * Mathf.Min(1f, dt * 5f);
                _solavanco *= 1f - Mathf.Min(1f, dt * 9f);

                if (_ligado < 0.0005f && _vel < 0.002f) { destino[i] = 0f; continue; }

                // ---- marcha lenta: um motor parado nunca fica exatamente no mesmo giro ----
                _pLenta = CaosDsp.Avancar(_pLenta, 3.1f * dt);
                float instavel = 1f + (1f - rpm01) * 0.02f * CaosDsp.Seno(_pLenta);
                float f = _f * instavel;

                // ---- corte de giro: o combustível é interrompido em pulsos, não some ----
                float portao = 1f;
                if (rpm01 > 0.985f)
                {
                    _pCorte = CaosDsp.Avancar(_pCorte, 19f * dt);
                    portao = CaosDsp.Seno(_pCorte) > 0f ? 1f : 0.25f;
                }

                float inc = f * dt;
                _pMeia = CaosDsp.Avancar(_pMeia, inc * 0.5f);
                _p1    = CaosDsp.Avancar(_p1, inc);
                _p2    = CaosDsp.Avancar(_p2, inc * 2f);
                _p3    = CaosDsp.Avancar(_p3, inc * 3f);
                _p4    = CaosDsp.Avancar(_p4, inc * 4f);
                _p6    = CaosDsp.Avancar(_p6, inc * 6f);

                // carga alta enche as harmônicas superiores — é isso que soa como "esforço"
                float bruto = Mathf.Lerp(0.25f, 0.75f, _carga);
                float motor = CaosDsp.Seno(_pMeia) * 0.22f                    // meia ordem: o "trepidar"
                            + CaosDsp.Seno(_p1)    * 0.55f
                            + CaosDsp.Seno(_p2)    * (0.28f + bruto * 0.30f)
                            + CaosDsp.Dente(_p3, f * 3f / taxa) * bruto * 0.20f
                            + CaosDsp.Seno(_p4)    * bruto * 0.16f
                            + CaosDsp.Seno(_p6)    * bruto * 0.09f;

                motor = _escape.Filtrar(motor);
                motor += _admissao.Filtrar(_rnd.Proximo()) * (0.10f + _carga * 0.42f) * (0.4f + rpm01);

                // ---- retomada: pé fora em giro alto estala no escapamento ----
                if (_carga < 0.06f && rpm01 > 0.4f)
                {
                    if (_rnd.Sorte() < 0.0016f * rpm01) _estouro = 1f;
                    _estouro *= 1f - Mathf.Min(1f, dt * 22f);
                    motor += _rnd.Proximo() * _estouro * 0.55f;
                    motor *= 0.72f;                                            // freio-motor é mais discreto
                }

                float volMotor = Mathf.Lerp(0.30f, 0.95f, _carga * 0.45f + rpm01 * 0.55f) * portao;
                motor *= volMotor * _ligado;

                // ---- pneu cantando ----
                float pneu = _derrapa > 0.001f
                    ? _pneu.Filtrar(_rnd.Proximo()) * 2.4f * _derrapa * (0.5f + 0.5f * _vel)
                    : 0f;

                // ---- vento: cresce com o quadrado da velocidade, como o arrasto ----
                float vento = _ventoHp.Filtrar(_ventoLp.Filtrar(_rnd.Proximo())) * _vel * _vel * 0.55f;

                float s = motor * 0.45f + pneu * 0.30f + vento * 0.5f;

                // ---- solavanco: buraco e batida sacodem a lataria ----
                if (_solavanco > 0.001f) s += (_rnd.Proximo() * 0.6f + CaosDsp.Seno(_pMeia) * 0.4f) * _solavanco * 0.5f;

                destino[i] = CaosDsp.Saturar(_cabineLp.Filtrar(s) * 1.15f);
            }
        }
    }
}
