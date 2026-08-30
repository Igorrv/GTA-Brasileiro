using UnityEngine;

namespace Caos.Simulation.Audio
{
    /// <summary>
    /// A cama sonora de São Genésio.
    ///
    /// O que existia antes era um pad de duas senoides em 110/165 Hz com um LFO de 0,2 Hz num clipe de
    /// 2 s. Além de não soar como cidade nenhuma, o LFO dava só 0,4 ciclo dentro do clipe, então <b>a
    /// amplitude saltava a cada volta do laço</b> — um pulso surdo a cada dois segundos, ligado o jogo
    /// inteiro.
    ///
    /// Aqui a cidade é montada por camadas que respondem ao mundo: lastro grave, lavagem de tráfego,
    /// buzina distante (é o Brasil), passarinho de manhã, grilo de madrugada e a sirene da polícia
    /// chegando de longe conforme o nível de procurado. Dentro do carro tudo isso passa por um filtro
    /// de cabine e cai de nível — o mesmo motivo pelo qual fechar o vidro muda o som na vida real.
    /// </summary>
    public sealed class CidadeAmbiente : IFluxoPcm
    {
        // ---- parâmetros escritos pela thread principal ----
        public float Hora = 12f;         // 0..24
        public float NaCabine;           // 0..1
        public float Movimento = 0.6f;   // densidade de tráfego 0..1
        public float Sirene;             // 0..1 — proximidade da viatura mais próxima
        public float Urgencia;           // 0..1 — nível de procurado (muda o padrão da sirene)

        private float _cabine, _movimento = 0.6f, _sirene, _urgencia;

        private CaosDsp.Ruido _rnd = new CaosDsp.Ruido(31337);
        private CaosDsp.Biquad _trafego, _bicho, _cabineLp, _sireneBanda;
        private CaosDsp.PassaBaixa _lastroLp;
        private float _pLastro, _pSub, _pSirene, _pBichos, _pBuzina;
        private float _buzinaT, _buzinaFreq = 420f;
        private float _grilos, _passaros;
        private int   _coefSirene;

        public CidadeAmbiente(int taxa)
        {
            _trafego.PassaBanda(320f, 0.65f, taxa);
            _bicho.PassaBanda(4200f, 6f, taxa);
            _cabineLp.PassaBaixa(6000f, 0.8f, taxa);
            _sireneBanda.PassaBanda(1200f, 2.2f, taxa);
            _lastroLp.Ajustar(180f, taxa);
        }

        public void Render(float[] destino, int quadros, int taxa, long amostraInicial)
        {
            float dt = 1f / taxa;

            float alvoHora = Mathf.Repeat(Hora, 24f);
            float alvoCab  = Mathf.Clamp01(NaCabine);
            float alvoMov  = Mathf.Clamp01(Movimento);
            float alvoSir  = Mathf.Clamp01(Sirene);
            float alvoUrg  = Mathf.Clamp01(Urgencia);

            _cabineLp.PassaBaixa(Mathf.Lerp(7500f, 900f, alvoCab), 0.8f, taxa);

            // dia/noite: às 6h os pássaros entram, às 20h os grilos assumem
            float dia   = Mathf.Clamp01(Mathf.InverseLerp(5f, 7.5f, alvoHora)) * (1f - Mathf.Clamp01(Mathf.InverseLerp(17.5f, 20f, alvoHora)));
            float noite = 1f - dia;

            for (int i = 0; i < quadros; i++)
            {
                float k = Mathf.Min(1f, dt * 2f);
                _cabine    += (alvoCab - _cabine) * Mathf.Min(1f, dt * 4f);
                _movimento += (alvoMov - _movimento) * k;
                _sirene    += (alvoSir - _sirene) * Mathf.Min(1f, dt * 3f);
                _urgencia  += (alvoUrg - _urgencia) * k;
                _passaros  += (dia   * (1f - _cabine * 0.85f) - _passaros) * k * 0.5f;
                _grilos    += (noite * (1f - _cabine * 0.9f)  - _grilos)   * k * 0.5f;

                // ---- lastro: a cidade tem um grave que nunca cala ----
                _pLastro = CaosDsp.Avancar(_pLastro, 47f * dt);
                _pSub    = CaosDsp.Avancar(_pSub, 0.07f * dt);
                float lastro = CaosDsp.Seno(_pLastro) * 0.09f * (0.7f + 0.3f * CaosDsp.Seno(_pSub));
                lastro += _lastroLp.Filtrar(_rnd.Proximo()) * 0.16f;

                // ---- lavagem de tráfego: ruído em banda modulado por carros passando ----
                float trafego = _trafego.Filtrar(_rnd.Proximo()) * (0.35f + 0.65f * _movimento) * 1.5f;

                // ---- buzina distante: item de série da paisagem sonora brasileira ----
                if (_buzinaT <= 0f && _rnd.Sorte() < 0.0000075f * (0.3f + _movimento) * (0.35f + dia))
                {
                    _buzinaT = 0.45f;
                    _buzinaFreq = 330f + _rnd.Sorte() * 260f;
                }
                float buzina = 0f;
                if (_buzinaT > 0f)
                {
                    _buzinaT -= dt;
                    _pBuzina = CaosDsp.Avancar(_pBuzina, _buzinaFreq * dt);
                    float env = Mathf.Clamp01(_buzinaT * 8f) * Mathf.Clamp01((0.45f - _buzinaT) * 22f);
                    buzina = (CaosDsp.Seno(_pBuzina) + CaosDsp.Seno(_pBuzina * 1.26f) * 0.6f) * env * 0.05f;
                }

                // ---- bichos ----
                float bichos = 0f;
                if (_passaros > 0.01f || _grilos > 0.01f)
                {
                    float agudo = _bicho.Filtrar(_rnd.Proximo());
                    _pBichos = CaosDsp.Avancar(_pBichos, 11f * dt);
                    float pulsar = 0.5f + 0.5f * CaosDsp.Seno(_pBichos);
                    bichos = agudo * (_grilos * pulsar * 0.22f + _passaros * 0.06f * (_rnd.Sorte() < 0.002f ? 6f : 1f));
                }

                // ---- sirene: varredura de duas notas; quanto mais estrelas, mais nervosa ----
                float sirene = 0f;
                if (_sirene > 0.002f)
                {
                    _pSirene = CaosDsp.Avancar(_pSirene, Mathf.Lerp(0.34f, 1.15f, _urgencia) * dt);
                    if (--_coefSirene <= 0)
                    {
                        _coefSirene = 16;
                        _sireneBanda.PassaBanda(Mathf.Lerp(640f, 1320f, 0.5f + 0.5f * CaosDsp.Seno(_pSirene)), 6f, taxa);
                    }
                    // distante = abafada; a banda estreita já faz o papel do ar entre o jogador e a viatura
                    sirene = _sireneBanda.Filtrar(_rnd.Proximo()) * 3.2f * _sirene * _sirene * (1f - _cabine * 0.5f);
                }

                float s = (lastro + trafego * 0.5f + buzina + bichos) * (1f - _cabine * 0.62f) + sirene * 0.5f;
                destino[i] = CaosDsp.Saturar(_cabineLp.Filtrar(s) * 0.9f);
            }
        }
    }
}
